# NxWitness branch-scoped CI/CD migration + convergence plan

Repo: `/home/pieter/NxWitness` (GitHub `ptr727/NxWitness`). Default branch `main`; integration branch `develop`.
Target model: the branch-scoped self-publishing CI/CD proven on LanguageTags, Utilities, PlexCleaner, VSCode-Server.
Canonical Docker reference: `/home/pieter/PlexCleaner/`. Canonical .NET-tooling reference: `/home/pieter/LanguageTags/`.

This is the MOST complex remaining repo: it couples (1) a .NET **codegen tool** (`CreateMatrix` + `CreateMatrixTests` + `Make/`) that
fetches upstream Nx product versions and regenerates `Make/Version.json`, `Make/Matrix.json`, `Docker/*.Dockerfile`, and `Make/Test*.yml`;
with (2) a **multi-stage, multi-product, multi-base Docker** build (5 products x {plain, LSIO} = 10 product images, plus 2 shared base
images) published to 12 Docker Hub repos. There is **no NuGet publish** (`CreateMatrix.csproj` and the test csproj both set
`IsPackable=false`; repo-wide grep for `nuget push` / `dotnet pack` / `PackageId` / `NuGetApiKey` returns zero). The .NET is a build-time
generator only; the published artifacts are exclusively Docker Hub images.

IMPORTANT: NxWitness is **partially migrated already**. `publish-release.yml` is dispatch+schedule (no push), and codegen/Dependabot
already dual-target main AND develop. The work here is (a) closing the gaps to the canonical model, (b) adding the missing governance
surface (`WORKFLOW.md`, `repo-config/`, the converged AGENTS/CODESTYLE/copilot sections), and (c) **deciding the one big architectural
tension**: the publisher today builds BOTH branches in one run (build-main + build-develop legs), which is exactly the cross-branch NBGV
classification hazard the canonical one-branch model exists to remove.

---

## 1. Current-state assessment

### 1.1 Workflows present (`.github/workflows/`)
| File | Trigger | Role | Canonical status |
|---|---|---|---|
| `test-pull-request.yml` | `pull_request` [main, develop] + `workflow_dispatch` | CI: paths-filter -> test-release + smoke-build + aggregator | **DIVERGES**: uses `pull_request` not `push: ['**']`; uses `dorny/paths-filter`; aggregator name is `Check pull request workflow status` (missing trailing ` job`) |
| `publish-release.yml` | `workflow_dispatch` + `schedule` (Mon 02:00 UTC) | Publisher: get-version(main) + build-base(main) + build-main + build-develop + github-release + docker-readme + date-badge + cleanup | **DIVERGES**: builds BOTH branches in one run (two legs), not one-branch-per-run |
| `build-base-images-task.yml` | `workflow_call` | Builds shared `nx-base` + `nx-base-lsio` (matrix of 2), branch-scoped buildcache | repo-owned, keep |
| `build-docker-task.yml` | `workflow_call` | Builds product images from `Make/Matrix.json` (matrix over `.Images`), threads SemVer2 in | repo-owned, keep; NBGV threading OK (see 1.4) |
| `get-version-task.yml` | `workflow_call` (input `ref`) | Single NBGV run, exposes SemVer2 + assembly versions + GitCommitId | canonical-shaped; no IGNORE_GITHUB_REF |
| `test-release-task.yml` | `workflow_call` + `workflow_dispatch` | husky lint + `dotnet test` | rename/fold into `validate-task.yml` (see 2) |
| `run-codegen-pull-request-task.yml` | `workflow_call` | Matrix codegen main+develop -> `codegen-main`/`codegen-develop` PRs | dual-target, keep (gotcha 11) |
| `run-periodic-codegen-pull-request.yml` | `workflow_dispatch` + `schedule` (daily 04:00) | Calls the codegen task | keep |
| `merge-bot-pull-request.yml` | `pull_request_target` [opened, reopened, synchronize] | merge-dependabot + merge-codegen + disable-on-maintainer-push | **DIVERGES**: merge step lacks `--delete-branch` (gotcha 5) |
| `build-datebadge-task.yml` | `workflow_call` | BYOB "Last Build" badge | **STRIP** (gotcha 12) |
| `publish-docker-readme-task.yml` | `workflow_call` | Docker Hub overview via `peter-evans/dockerhub-description`, manifest-derived repo list | **FOLD** into the docker task as a main-publish step (gotcha 12) |

### 1.2 Governance surface (the big gap)
- **No `WORKFLOW.md`.** No `repo-config/` (no `configure.sh`, no ruleset JSON, no `settings.json`, no `repo-config/README.md`).
  The 5D audit and the GitHub-side config are entirely absent. This is the largest single body of new work.
- `AGENTS.md` exists with: Solution Structure, Build/Validation, Image Architecture, CI Pipeline, Versioning, Git and Commit Rules,
  PR Title/Commit Conventions, PR Review Etiquette (full canonical contract, with the "Mandatory in every derived repo" banner + Merge
  Gate), Coding Conventions (Highlights), Notes for Changes, Template adaptations. It does NOT reference WORKFLOW.md/repo-config.
  It LACKS a dedicated `### Comments` subsection and a `## Documentation Style Conventions` section (those live in PlexCleaner AGENTS.md).
- `.github/copilot-instructions.md` exists with the full canonical Review Runbook (requestReviews mutation, GraphQL-vs-REST login split,
  head-SHA coverage, bounded retry, thread resolution). Good - ports nearly verbatim, only owner/name strings change.
- `CODESTYLE.md` exists (25 KB). Needs a heading-by-heading diff against PlexCleaner's to converge the shared structure.

### 1.3 Version scheme
`version.json` floor `2.14`, `publicReleaseRefSpec: ["^refs/heads/main$"]`, `nugetPackageVersion.semVer: 2`. NBGV computes
`X.Y.<height>` on main, `X.Y.<height>-g<sha>` elsewhere. `get-version-task.yml` runs NBGV once (dotnet/nbgv@master, floated - tag
stream lags so Dependabot would propose a downgrade; a deliberate `@master` float, keep with the existing comment).

### 1.4 NBGV threading today (gotcha 2 status)
- `get-version-task.yml` runs NBGV **once** and exposes outputs. Good.
- `build-docker-task.yml` ALSO contains a nested `get-version` call (`needs: [get-version, ...]`) and threads
  `needs.get-version.outputs.SemVer2` into `LABEL_VERSION`. So in the PUBLISHER path, NBGV runs in TWO places: once at top-level
  `publish-release.yml::get-version` (ref: main, for the release tag) AND once inside each `build-docker-task` invocation (ref: the leg's
  ref). These are different NBGV runs on different refs. For the image LABEL_VERSION this is arguably intentional (the develop leg should
  label its images with the develop prerelease version), but it is a SECOND NBGV run per leg - exactly what gotcha 2 says to avoid, and it
  means the develop leg's NBGV classification depends on which `ref` it was handed, not on GITHUB_REF. This works today only because the
  legs pass an explicit `ref` (main commit / develop) and NBGV keys off the checked-out branch when given a real branch ref. See 3 for the
  recommended consolidation.

### 1.5 Cross-branch / NBGV-leak exposure (the crux)
`publish-release.yml` builds main and develop in ONE run via separate jobs. `GITHUB_REF` for the whole run is the dispatch ref. The
build legs each pass an explicit `ref` (`build-main` pins `get-version.outputs.GitCommitId`; `build-develop` passes `ref: develop`).
The repo does NOT set `IGNORE_GITHUB_REF` anywhere. The safety net is the **`Verify public release version step`** (the D2.2 backstop) in
`github-release`, which refuses to publish a `main` GitHub release carrying a prerelease `-`. So the GitHub RELEASE classification is
protected. The IMAGE tag/label classification, however, is governed by the nested `get-version` in each docker leg keyed on the leg's
`ref`, not on GITHUB_REF - so as long as `build-develop` passes `ref: develop`, its nested NBGV checks out develop and classifies
prerelease. This is the matrix-publisher case the memory `nbgv-publicrelease-githubref-leak.md` says either needs `IGNORE_GITHUB_REF` OR
(preferred) should migrate to one-branch-per-run.

### 1.6 Smoke / CI path
`test-pull-request.yml` runs on `pull_request`. `dorny/paths-filter` gates: builds smoke (NxMeta + NxMeta-LSIO, amd64, no push) only when
`Docker/**` / `Make/Matrix.json` / `Make/Version.json` changed; `build_base` only when a base Dockerfile changed. Aggregator
`check-workflow-status` (name `Check pull request workflow status`) treats success|skipped as pass, fails on failure|cancelled. Has a
branch-deletion concern: on `pull_request` there is no branch-deletion event, so gotcha 4's `!github.event.deleted` guard is **n/a while
the trigger stays `pull_request`** - but if we move to `push: ['**']` (canonical), the guard becomes mandatory.

### 1.7 Branch hygiene / backlog (messy)
`git branch -a` shows a substantial backlog of remote branches that must be reconciled or pruned before/around go-live:
`codegen`, `codegen-main`, `codegen-develop` (codegen working branches - expected, transient), `dependabot/...` on BOTH main and develop
(3 live), plus stragglers: `backport-cicd-fixes`, `bump-version-2.13`, `chore/sync-template`, `feature/sync-versioned-rulesets`,
`fix-lsio-puid-pgid-ordering`, `fix-release-version-tag-race`, `fix/release-skip-log-message`, `fix/release-tag-pinning-and-skip-existing`,
`propagate-versioning-policy`, `realign-template-lint-config`, `release-notes-2.14`, `shields`. Several look like prior CI-fix attempts.
Action: audit each before go-live; the migration branch should supersede the relevant `fix-release-*` / `*cicd*` / `*versioning*`
branches, and those should be closed (not merged) to avoid re-introducing superseded mechanics. Local-only branches not on origin
(`fix/release-skip-log-message`, `release-notes-2.14`) can be deleted locally.

### 1.8 EOL / prose
All 10 workflow files are CRLF (verified by `grep -c $'\r'`). `AGENTS.md` has 0 em-dashes; `README.md` has 1 em-dash (must be swept).
`.slnx` is **stale**: it references LanguageTags-shaped workflow files that do not exist here (`build-executable-task.yml`,
`build-library-task.yml`, `build-release-task.yml`, `publish-periodic-docker-release.yml`) and omits files that DO exist
(`build-base-images-task.yml`, `build-docker-task.yml`). Must be rebuilt to the real file set (gotcha: do not mirror LanguageTags' .slnx).

---

## 2. Target architecture

The target keeps NxWitness's legitimately repo-specific build layer (shared base + per-product matrix) while converging the
orchestration, governance, and classification model. Per-file target:

### 2.1 `test-pull-request.yml` (CI) -> converge to push-on-every-branch self-test
- **Trigger:** `push: branches: ['**']` (NOT `pull_request`) + `workflow_dispatch`. Rationale: the canonical model self-tests by pushing
  the branch; reusable `./...` logic resolves from the head; the aggregator's ruleset-bound context has a single producer (the push run).
  This is also required for the Dependabot-in-repo-branch path to produce the required check.
- **Concurrency:** `group: ${{ github.workflow }}-${{ github.ref }}`, `cancel-in-progress: true`.
- **Branch-deletion guard (gotcha 4):** every job `if: ${{ !github.event.deleted }}`; aggregator `if: ${{ always() && !github.event.deleted }}`.
- **Drop `dorny/paths-filter` (gotcha 12).** Two options for the smoke gate, FLAGGED for the maintainer (open question 9.1):
  - (A) Always run a minimal smoke (NxMeta + NxMeta-LSIO, amd64, no push) on every branch push. Simplest, matches PlexCleaner's
    unconditional smoke; costs a docker build on every doc-only push.
  - (B) Replace paths-filter with a cheap inline `git diff --name-only` step inside a single `changes` job (no third-party action) to
    keep the "only build images when image files changed" optimization. Preserves today's behavior without the dropped action.
  - **Recommendation: (B).** NxWitness pushes are frequent (codegen + 6 Dependabot ecosystems x 2 branches) and a full product smoke is
    heavier than PlexCleaner's single-target smoke; keep the change-gate but implement it inline to honor "strip paths-filter."
- **Jobs:** `validate` (lint + `dotnet test`, was `test-release-task.yml`), `changes` (inline diff, if option B), `smoke-build`
  (calls `build-docker-task.yml` smoke), `check-workflow-status` (aggregator), `cleanup-artifacts`.
- **Aggregator name MUST become exactly `Check pull request workflow status job`** (add trailing ` job`) to match the canonical
  required-check string used by `repo-config` (gotcha 7). Keep the success|skipped allowlist (gotcha 8) it already has; for `changes` keep
  the "must succeed" semantics (a failed `changes` must not let an image-changing PR through as a skip).
- **Smoke `branch` input:** today passes `github.base_ref` (a PR concept). Under `push`, there is no base_ref. Pass `github.ref_name`
  so a push to develop validates develop's Matrix rows and a push to main validates main's (matches the build task's branch filter).

### 2.2 `validate-task.yml` (NEW, rename of `test-release-task.yml`)
- `workflow_call` + `workflow_dispatch`. Jobs: dotnet restore/tool-restore, husky lint, `dotnet test`, plus the static doc validators
  if the canonical validate carries them (markdownlint/cspell scoped to README+HISTORY per LanguageTags convention). Name the aggregated
  job `Validate job` per canonical. This is the CI's quality gate; it does not build images.

### 2.3 `publish-release.yml` (Publisher) - DECIDED (triggered-Docker, one-branch-per-run)
**Decided shape (signed off 2026-06-29): triggered-Docker.** Triggers: `workflow_dispatch` + `schedule`
(weekly Mon 02:00, main baseline) **+ path-scoped `push` on main when the codegen matrix changes**
(`push: { branches: [main], paths: [<Matrix.json path - confirm exact path, e.g. CreateMatrix/Matrix.json> ] }`).
Keep: global non-ref-scoped concurrency (`group: ${{ github.workflow }}`, `cancel-in-progress: false`); the
`Verify public release version` D2.2 backstop on the main release; the skip-existing-release guard; the
artifact cleanup job.

**Run shape: one-branch-per-run, schedule is main-only.**
- Schedule -> builds `main` only (full product matrix; baseline base/CVE refresh + versioned release).
- Push-on-`Matrix.json`-change (main) -> builds `main` (codegen committed a new matrix => publish the new
  product versions immediately; this is the accepted publish-on-matrix-change, superseding the earlier
  weekly-only decision). Only the matrix file change publishes; ordinary code merges do not (path filter).
- Dispatch -> builds `github.ref_name`, guarded `if: github.ref_name == 'main' || github.ref_name == 'develop'`. **`:develop` is refreshed by manual dispatch only** (the earlier "paired develop re-dispatch to
  refresh :develop weekly" is DROPPED per maintainer: weekly builds main only).
- Jobs: `get-version` (ref: `github.ref_name`), `build-base` (push: true, ref: `github.ref_name`),
  `build-docker` (push: true, branch: `github.ref_name`, ref: pinned to `get-version.outputs.GitCommitId`
  for main / `github.ref_name` for develop), `github-release` (`if: github.ref_name == 'main'` - a develop
  dispatch publishes images + the `:develop` tag but cuts no versioned GitHub release), `docker-readme`
  (main only), `cleanup-artifacts`.
- NBGV classifies natively: main schedule/push/dispatch => clean version; a develop dispatch => prerelease.
  No `IGNORE_GITHUB_REF`, no cross-branch leg. The D2.2 backstop stays as defense-in-depth. The push trigger
  is branch-filtered to main, so develop's daily codegen matrix update is sync-only and never publishes.
- **Base-image sharing under one-branch:** the shared `nx-base` tag (`:ubuntu-noble`) is branch-agnostic. A
  develop dispatch rebuilding the base would overwrite the shared tag with develop's base. Recommendation:
  the develop dispatch sets `build_base: false` and pulls the main-built shared base; the weekly main
  schedule refreshes the base for CVEs. If base divergence between branches is a real risk, FLAG (open
  question 9.2).

**Cost note:** publish-on-matrix-change rebuilds the full product matrix on every codegen bump (the
maintainer accepted this over the cheaper weekly-only, for the tightest upstream-vuln window). The matrix
build keeps `max-parallel: 4` and branch-scoped buildcache to bound runner time.

### 2.4 `build-docker-task.yml` (repo-owned build layer) - keep, with NBGV consolidation
- Keep the `get-matrix` job (smoke filter / branch filter / full), the product matrix over `.Images`, the multi-arch build, the
  branch-scoped registry buildcache (read both `buildcache-main` + `buildcache-develop`, write only this branch on push).
- **NBGV (gotcha 2):** remove the nested `get-version` job; instead accept threaded inputs `semver2` (and assembly versions if the image
  embeds them) as REQUIRED workflow_call inputs, passed by the orchestrator's single `get-version` run. The orchestrator computes the
  version for the branch being built and threads it down. This matches PlexCleaner's `build-docker-task` (version threaded, never
  re-run). Smoke callers can pass a placeholder/threaded smoke version. (If the maintainer prefers each leg to label with its own branch
  version under shape 3.2, the orchestrator runs get-version per leg and threads each leg's value - still single-NBGV-per-leg, no nested
  re-run.)
- Keep `max-parallel: 4` on the product matrix.

### 2.5 `build-base-images-task.yml` - keep verbatim (repo-owned)
Shared `nx-base` / `nx-base-lsio` matrix, branch-scoped buildcache + inline cache. The `ref` input lets the publisher build from main.
Under shape 3.1, the develop run sets `build_base: false`.

### 2.6 `get-version-task.yml` - keep; conditionally add IGNORE_GITHUB_REF
Single NBGV run. Under shape 3.1: NO `IGNORE_GITHUB_REF` (native classification). Under shape 3.2: ADD `env: IGNORE_GITHUB_REF: "true"`.

### 2.7 Codegen (`run-codegen-pull-request-task.yml` + `run-periodic-codegen-pull-request.yml`) - keep, dual-target (gotcha 11)
Matrix runs codegen on main AND develop, opens `codegen-main->main` and `codegen-develop->develop` PRs via the App token (so
`pull_request`/push events fire), CSharpier formats, merge-bot auto-merges each independently. This is the canonical dual-branch codegen
case the briefing's gotcha 11 protects. KEEP both targets. The daily schedule (04:00) is staggered after the weekly publish (Mon 02:00).
Note: the codegen task today runs ONLY `matrix --updateversion` (regenerates `Make/Version.json` + `Make/Matrix.json`); it does NOT run
`make` (Dockerfiles/compose are regenerated by a human via `Make/Create.sh`). Document this seam in WORKFLOW.md (S-section): the codegen
PR keeps the matrix current with upstream Nx versions; Dockerfile changes are a separate human-driven path. FLAG (open question 9.3):
should codegen also run `make` so new upstream versions auto-regenerate Dockerfiles? Current answer: no - Dockerfile structure changes
are reviewed; only version data auto-updates. Recommend keeping as-is.

### 2.8 `merge-bot-pull-request.yml` - add `--delete-branch` (gotcha 5)
Add `--delete-branch` to the `gh pr merge --auto "$method"` calls in BOTH `merge-dependabot` and `merge-codegen` jobs (NOT to the
disable-auto job). Keep repo-wide `delete_branch_on_merge: false` in `settings.json` (gotcha 5 + github-auto-delete-branch-gotcha:
prevents a develop->main promotion from deleting develop). Per-merge deletion is explicit. Keep the per-base method case
(develop=squash, main=merge), the major-NuGet skip, the strict codegen head/base pairing, and `pull_request_target` + App-token model.

### 2.9 Docker Hub overview - fold into the docker task (gotcha 12)
Delete `publish-docker-readme-task.yml` as a standalone and add a `peter-evans/dockerhub-description` step that runs ONLY on a main
publish. Because NxWitness has 12 repos, the fold must iterate the repo list. Cleanest: a small `docker-readme` job inside
`publish-release.yml` gated `if: github.ref_name == 'main'`, deriving the repo list from `Make/Matrix.json` via the existing
`manifest-jq` (`[.Images[].Name | ascii_downcase | "ptr727/\(.)"] + ["ptr727/nx-base","ptr727/nx-base-lsio"] | sort | unique`) and
matrixing `peter-evans/dockerhub-description` over it. This keeps the behavior but removes the standalone reusable file the briefing
says to strip. (If the maintainer prefers to keep the reusable task file for clarity given 12 repos, that is a defensible per-repo
deviation - document it. Recommendation: fold, to match the canonical strip.)

### 2.10 `build-datebadge-task.yml` - DELETE (gotcha 12)
Remove the file and the `date-badge` job from `publish-release.yml`, and strip the badge from README if it points at the BYOB gist.

### 2.11 `WORKFLOW.md` (NEW) - port from PlexCleaner, adapt for codegen + multi-image
Structure mirrors PlexCleaner: model-at-a-glance, glossary, architecture, the D0..D10 behavioral contract, 5-test methodology (5A static,
5B trace scenarios S1..Sn, 5C live probe, 5D config audit), repository configuration. NxWitness-specific additions:
- D-guarantees for the **codegen seam**: codegen dual-targets main+develop; codegen PR regenerates `Version.json`+`Matrix.json` only;
  forward-only version guard (`ReleaseVersionForward`) prevents generic-tag regression; merge-bot auto-merges each codegen PR.
- D-guarantees for the **multi-image matrix**: shared base built once and reused; product matrix from `Matrix.json`; per-product Docker
  Hub repos + base repos; multi-arch amd64+arm64; branch-scoped buildcache; weekly base refresh for CVEs.
- The NBGV-classification guarantee adapted to the chosen shape (3.1 native vs 3.2 IGNORE_GITHUB_REF), with the D2.2 backstop guarantee.
- A "Template adaptations" appendix documenting the legitimate divergences (shared-base fan-out, Docker-only release with no
  release-asset files, folded docker-readme, codegen replacing merge-upstream-version).

### 2.12 `repo-config/` (NEW) - port from PlexCleaner verbatim, retarget strings
- `configure.sh`: copy PlexCleaner's verbatim (helper bodies `jq_lacks`, `check_secrets`, `ruleset_id`, `check_app`, `assert`, `pass`,
  `fail`, `note`, `apply_ruleset`, `cmd_apply`, `cmd_check`). Retarget: `REQUIRED_CHECK="Check pull request workflow status job"`,
  `REQUIRED_ACTIONS_SECRETS`/`REQUIRED_DEPENDABOT_SECRETS` = `(DOCKER_HUB_USERNAME DOCKER_HUB_ACCESS_TOKEN CODEGEN_APP_CLIENT_ID
  CODEGEN_APP_PRIVATE_KEY)` (identical set, both stores - gotcha 3), and the manual-verify note to enumerate the 12 NxWitness Docker Hub
  repos (or note "push to docker.io/ptr727/<images from Matrix.json>"). `cmd_check` order: ruleset develop (squash, linear),
  ruleset main (merge, non-linear), settings, security, secrets, app.
- `ruleset-develop.json`: condition `refs/heads/develop`; rules `deletion`, `non_fast_forward`, `required_linear_history`,
  `required_signatures`, `pull_request` (allowed_merge_methods `["squash"]`, `required_review_thread_resolution: true`,
  `dismiss_stale_reviews_on_push: true`, approvals 0), the required status check context
  `Check pull request workflow status job` (integration_id 15368), `copilot_code_review` (review_on_push true).
- `ruleset-main.json`: condition `refs/heads/main`; SAME minus `required_linear_history` (must allow the develop->main merge commit);
  `allowed_merge_methods: ["merge"]`; same status check + copilot rules.
- `settings.json`: `{ allow_squash_merge true, allow_merge_commit true, allow_rebase_merge false, allow_auto_merge true,
  delete_branch_on_merge false }`.
- `repo-config/README.md`: port PlexCleaner's; retarget the repo slug (`ptr727/NxWitness`), the secret set, the Docker Hub repo
  enumeration, and the required-check lockstep note.

### 2.13 `.slnx` - rebuild to the real file set
Replace the stale LanguageTags-shaped file list with the actual files: workflows
`build-base-images-task.yml`, `build-docker-task.yml`, `get-version-task.yml`, `merge-bot-pull-request.yml`, `publish-release.yml`,
`run-codegen-pull-request-task.yml`, `run-periodic-codegen-pull-request.yml`, `test-pull-request.yml`, `validate-task.yml`; plus
`CreateMatrix.csproj` + `CreateMatrixTests.csproj` projects and the solution items (`WORKFLOW.md`, `version.json`, etc.). Remove the
deleted `build-datebadge-task.yml` / `publish-docker-readme-task.yml` / non-existent template names.

### 2.14 Release artifact
None as a file. The GitHub release carries auto source zip + README + LICENSE only (Docker-only repo; `fail_on_unmatched_files` omitted).
The published artifacts are the 12 Docker Hub repos' multi-arch images. `:latest`/`:stable` from main, `:develop` from develop, plus
`:<product-version>` and `:develop-<product-version>` tags from `Matrix.json`.

---

## 3. Release model decision (signed off 2026-06-29)

**Decision: triggered-Docker, one-branch-per-run.** Publish on `weekly schedule (main)` +
`push-on-Matrix.json-change (main)` + `workflow_dispatch`. Schedule is **main-only**; `:develop` refreshes by
manual dispatch only.

**How it reconciles the codegen cadence:**
- Daily codegen keeps `Matrix.json`/`Version.json` current on **main AND develop** (dual-target, gotcha 11 -
  drift-avoidance only; develop's update is sync-only and never publishes).
- When codegen commits a new matrix **to main**, the path-scoped push publishes the new product versions
  immediately (the accepted **publish-on-matrix-change**, superseding the earlier weekly-only decision - the
  maintainer accepted the full-matrix rebuild cost for the tightest upstream window).
- The weekly main schedule still runs even with no matrix change, to refresh the shared base image for CVEs
  and re-cut from the current pin.
- A maintainer wanting an off-cycle or develop-channel build dispatches the publisher from the branch.
  Ordinary code merges never publish (only the matrix file path triggers).

**Why one-branch (not the old two-leg combined run):** each publish run is single-branch, so NBGV classifies
natively - no `IGNORE_GITHUB_REF`, no cross-branch leg, none of the `nbgv-publicrelease-githubref-leak` class
of bugs - and it converges NxWitness's publisher with the four live repos and the ESPHome triggered-Docker
sub-model (identical shape, only the trigger path file differs: `Matrix.json` here, `upstream-version.json`
there). The D2.2 `Verify public release version` backstop stays as defense-in-depth.

**Confirm during execution:** the exact repo-relative path of the codegen matrix output for the push
`paths` filter (e.g. `CreateMatrix/Matrix.json` vs `Make/Matrix.json`), and that codegen writes it on the
main branch directly (or via an auto-merged `codegen-main` PR whose merge is the publishing push).

---

## 4. Files to create / edit / delete

### Create (7)
1. `WORKFLOW.md` (port from PlexCleaner + codegen/multi-image D-guarantees).
2. `repo-config/configure.sh` (verbatim helpers, retargeted secrets/check/repo).
3. `repo-config/ruleset-main.json`.
4. `repo-config/ruleset-develop.json`.
5. `repo-config/settings.json`.
6. `repo-config/README.md` (port + retarget).
7. `.github/workflows/validate-task.yml` (rename of `test-release-task.yml`, canonical `Validate job`).

### Edit (10)
1. `.github/workflows/test-pull-request.yml` - trigger `push: ['**']`+dispatch; drop paths-filter (inline diff, option B);
   `!github.event.deleted` guards; aggregator rename to `Check pull request workflow status job`; smoke `branch: github.ref_name`;
   call `validate-task.yml`.
2. `.github/workflows/publish-release.yml` - one-branch-per-run (shape 3.1): single get-version/build-base/build-docker per run, main-only
   release+readme, develop re-dispatch for the develop channel; remove the `date-badge` job; fold docker-readme as a main-only job.
3. `.github/workflows/build-docker-task.yml` - remove nested `get-version`, accept threaded `semver2` (+ assembly versions) inputs.
4. `.github/workflows/get-version-task.yml` - keep single NBGV; (shape 3.2 only) add `IGNORE_GITHUB_REF`. Under 3.1 unchanged.
5. `.github/workflows/merge-bot-pull-request.yml` - add `--delete-branch` to both merge jobs.
6. `.github/dependabot.yml` - verify the 6 ecosystem x 2 branch entries cover the actions used in new/edited workflows; keep dual-target.
7. `AGENTS.md` - add `### Comments` subsection + `## Documentation Style Conventions` (converge with PlexCleaner); add a reference to
   `WORKFLOW.md` + `repo-config/`; refresh the "Template adaptations" section to match the chosen publisher shape and the folded
   docker-readme / dropped date-badge.
8. `CODESTYLE.md` - converge shared headings with PlexCleaner (diff and align General/.NET structure; keep NxWitness specifics).
9. `.github/copilot-instructions.md` - retarget owner/name strings in the Review Runbook (`ptr727/NxWitness`); otherwise verbatim.
10. `NxWitness.slnx` - rebuild to the real file set.
    (Plus: `README.md` em-dash sweep + strip date badge; `version.json` floor bump to mark the overhaul and exercise the publish path -
    reconcile with the AGENTS "routine edits leave version.json untouched" rule by noting a deliberate maintainer-directed infra bump.)

### Delete (2)
1. `.github/workflows/build-datebadge-task.yml`.
2. `.github/workflows/publish-docker-readme-task.yml` (folded into `publish-release.yml`).

Net: 7 create, ~12 edit (incl. README + version.json), 2 delete. (Counts exclude the branch-backlog cleanup in 1.7.)

---

## 5. Convergence + backports

### 5.1 Port VERBATIM (byte-for-byte, owner/name strings only)
- `repo-config/configure.sh` helper bodies (`jq_lacks`, `check_secrets`, `ruleset_id`, `check_app`, `assert`, `pass`/`fail`/`note`,
  `apply_ruleset`) - the hardened canonical forms (gotcha 6).
- `repo-config/ruleset-*.json` structure (only condition/merge-method/linear-history differ between main and develop, already canonical).
- `repo-config/settings.json` (identical to PlexCleaner).
- `.github/copilot-instructions.md` Review Runbook (only `ptr727/NxWitness` substitutions; bot id `BOT_kgDOCnlnWA`, the requestReviews
  mutation, GraphQL-vs-REST login split, known-broken `POST /requested_reviewers` note all carry verbatim).
- AGENTS.md shared subsections: `### Comments`, `## Git and Commit Rules`, the "Where rules live" lead-in, `## PR Review Etiquette`
  (already present and canonical here), `## Documentation Style Conventions` incl. "write docs in the current state".

### 5.2 Adapt (repo-specific)
- `configure.sh` required-secret list (same 4 names but the manual Docker Hub note enumerates 12 NxWitness repos) and `REPO` slug.
- `WORKFLOW.md` Docker mechanics + the NEW codegen and multi-image D-guarantees and the "Template adaptations" appendix.
- AGENTS.md "Template adaptations" and the codegen/Image-Architecture sections.
- `dependabot.yml` (6 ecosystems incl. docker, dual-target - already adapted).

### 5.3 Backports to the four live repos (drift found)
- Confirm all four live repos' `merge-bot` use `--delete-branch` (gotcha 5). NxWitness lacked it; the others were noted to have it -
  spot-check and backport if any regressed.
- If NxWitness's `configure.sh` helpers (ported from PlexCleaner) reveal any newer hardening than what LanguageTags/Utilities carry,
  backport the hardened helper to those NuGet repos (they share the helper bodies verbatim).
- The folded docker-readme pattern (main-only `dockerhub-description` step) should match PlexCleaner's approach; if PlexCleaner kept a
  reusable file vs an inline step, align NxWitness to whichever the maintainer blessed as canonical (PlexCleaner stripped the standalone -
  fold here too).

---

## 6. Gotcha checklist mapped to NxWitness

1. **NBGV GITHUB_REF classification.** APPLIES. Under shape 3.1 (recommended) github.ref matches the built branch -> native
   classification, no IGNORE_GITHUB_REF. Under shape 3.2 (combined two-leg) IGNORE_GITHUB_REF is REQUIRED. `version.json` floor 2.14,
   `publicReleaseRefSpec ^refs/heads/main$`. Bump the floor to exercise the publish path.
2. **NBGV threading.** APPLIES. Today `build-docker-task.yml` re-runs NBGV via a nested `get-version`. Fix: remove the nested job, thread
   `semver2` (+ assembly versions) from the orchestrator's single get-version run. This also removes the `:SemVer2`-tag-collision risk
   across the image matrix (one classification feeds all product legs).
3. **Docker creds in BOTH stores.** APPLIES. `DOCKER_HUB_USERNAME` + `DOCKER_HUB_ACCESS_TOKEN` must be in Actions AND Dependabot stores
   (Dependabot push CI smoke-builds and logs in to Docker Hub). `configure.sh` enforces both. Same for `CODEGEN_APP_*` (merge-bot).
4. **Branch-deletion guard.** APPLIES ONCE we move CI to `push: ['**']`. Add `!github.event.deleted` to every CI job + aggregator. n/a
   while the trigger stays `pull_request` (no such event), but the move to push makes it mandatory.
5. **merge-bot `--delete-branch`.** APPLIES. Missing today; add to both merge jobs. Repo-wide auto-delete stays OFF in settings.json.
6. **5D audit hardened helpers.** APPLIES. Port the final hardened `jq_lacks` (exit 4 = lacks; keep stderr), `check_secrets`
   (API error FAILs, both stores, paginate), `ruleset_id` (`first // empty` in jq, no `head -1`, let gh print error), `check_app`
   (best-effort note, never fails). Audit must fail when it cannot verify.
7. **Required-check name lockstep.** APPLIES + ACTIVE BUG. The aggregator is named `Check pull request workflow status` (missing
   ` job`). The required-check string, the aggregator job `name:`, and the ruleset JSON must all read `Check pull request workflow status
   job`. Run `configure.sh apply` in the same change that ships the workflow edit, then `check`.
8. **Aggregator success/skipped allowlist.** APPLIES; already correct (success|skipped pass, failure|cancelled fail). Keep the `changes`
   "must succeed" carve-out so an image-changing PR cannot merge on a `changes` failure treated as skip.
9. **EOL discipline.** APPLIES. All workflows are CRLF today; keep CRLF for md/yml/json/code-workspace/slnx, LF for .sh/Dockerfile/.py.
   Pin in `.gitattributes`/`.editorconfig` (present - verify they cover `.slnx`). Re-check after Write/Edit (they can flip CRLF to LF);
   verify with `grep -c $'\r'` not `file`.
10. **Copilot review loop.** APPLIES. Runbook already in `.github/copilot-instructions.md`. snupkg/OIDC/NBGV-prerelease false positives;
    decline with rationale. Expect 1-3 rounds. Use `gh api -X PATCH .../pulls/N -F body=@file` for body edits.
11. **Dependabot + codegen dual-target main AND develop.** APPLIES - this is the canonical case. Both `dependabot.yml` and
    `run-codegen-pull-request-task.yml` already dual-target. KEEP both; do not collapse to single-target (maintainer rejected it; it
    caused non-linear rebase/merge-block conflicts).
12. **Strip template cruft.** APPLIES. Delete `build-datebadge-task.yml`; fold `publish-docker-readme-task.yml` into a main-publish step;
    drop `dorny/paths-filter`; there is no `setup`/`PUBLISH_ON_MERGE` machinery here (already absent); merge-bot already omits
    `merge-upstream-version` (codegen replaces it) - keep that.
13. **Action SHAs.** APPLIES. Current pins look converged (setup-dotnet v5.4.0 `26b0ec14...`, checkout v7.0.0 `9c091bb2...`,
    create-github-app-token v3.2.0, docker actions v4.x/v7.2.0, softprops v3.0.1). VERIFY every SHA->version against the GitHub API before
    asserting in review; do not trust Copilot's SHA/version mapping.
14. **Prose rules.** APPLIES. No em-dashes (README has 1 - sweep it). US English. Terse comments, one line if <~120 cols, top-of-file
    workflow summaries (present). Never edit human-authored comments.

---

## 7. Verification

### 7.1 Static (local, before push)
- `actionlint` on all workflows (Docker image or npx).
- `markdownlint-cli2` on `WORKFLOW.md`, `AGENTS.md`, `CODESTYLE.md`, `README.md`, `repo-config/README.md`.
- `cspell` (scope = README + HISTORY per convention; add product/codegen terms to the dictionary as needed).
- YAML + JSON parse: every workflow, `Make/Matrix.json`, `Make/Version.json`, `version.json`, the three `repo-config/*.json`,
  `.slnx` well-formedness.
- `bash -n repo-config/configure.sh` and `shellcheck` (the helpers carry `# shellcheck disable` directives - preserve them).
- `dotnet build` + `dotnet test` (CreateMatrixTests) green; `dotnet csharpier --check` + `dotnet husky run` clean.
- EOL audit: `grep -c $'\r'` to confirm CRLF on md/yml/json/code-workspace/slnx and LF on .sh/Dockerfile (gotcha 9).
- Em-dash sweep: `grep -rn '—'` across the tree (expect 0 after the README fix).
- Token sweep for stale template references: `LanguageTags|ProjectTemplate|build-executable-task|build-library-task|
  build-release-task|publish-periodic-docker-release|datebadge|paths-filter|PUBLISH_ON_MERGE` (expect only intentional historical mentions).
- Codegen smoke: `dotnet run --project ./CreateMatrix -- matrix --versionpath=./Make/Version.json --matrixpath=/tmp/m.json
  --updateversion` against a copy, confirm it still produces a valid Matrix.json (and that the forward-only guard holds).

### 7.2 Config audit
- `repo-config/configure.sh check` BEFORE `apply`: expect drift (no ruleset yet, required-check name mismatch, possibly missing secrets
  in one store). Document the expected drift list.
- `repo-config/configure.sh apply` then `check`: expect "Configuration matches" (modulo the App best-effort note and the manual Docker
  Hub push note).

### 7.3 Live dispatch verification (post-merge)
- Dispatch `publish-release.yml` from `main`: confirm clean `X.Y.<height>` images on the 12 repos, `:latest`/`:stable` tags, multi-arch
  manifest (amd64+arm64), the versioned GitHub release, the Docker Hub overview pushed, NO prerelease `-` on the main release.
- Dispatch from `develop`: confirm `:develop` + `:develop-<version>` tags, prerelease classification (`X.Y.<height>-g<sha>` label), NO
  versioned GitHub release.
- Confirm the shared base tags (`nx-base:ubuntu-noble`, `nx-base-lsio:ubuntu-noble`) are intact and not overwritten by a develop run.
- Trigger codegen via dispatch: confirm both `codegen-main->main` and `codegen-develop->develop` PRs open and merge-bot auto-merges with
  branch deletion.

---

## 8. Go-live sequence

1. Branch `migrate/branch-scoped-cicd` off `develop`. Verify SSH signing is live before the first commit (committing is enabled here).
2. Apply all create/edit/delete (section 4). Run the full static + codegen verification (7.1) locally.
3. Push the branch (CI now runs via `push: ['**']`). Open PR -> `develop`.
4. Copilot dance (gotcha 10): poll for auto-review, re-request via `requestReviews` mutation after each push, resolve every thread,
   decline false positives (snupkg/OIDC n/a here; NBGV-prerelease, the IGNORE_GITHUB_REF presence/absence, and the two-leg-vs-one-branch
   choice are the likely debate points) with rationale. Budget 1-3 rounds.
5. `repo-config/configure.sh apply` against the live repo IN THE SAME change window (gotcha 7): this writes the rulesets, renames the
   required check to `Check pull request workflow status job` in lockstep with the workflow edit, and adds the Copilot rule - unblocking
   the PR. Then `configure.sh check` -> matches.
6. Squash-merge to `develop`. Confirm CI green on develop.
7. Promote `develop -> main` via a merge-commit PR, Copilot-reviewed, NO admin bypass (main ruleset allows the merge commit by omitting
   `required_linear_history`). Watch for the migration-promotion-conflict pattern if main has straggler bumps in rewritten files; if it
   bites, a local signed merge commit (tree=develop) is the documented escape, but try the normal PR first.
8. Dispatch `publish-release.yml` from `main` to verify the publish path end-to-end (7.3). Then dispatch from `develop` to verify the
   develop channel.
9. Confirm `develop` survives the promotion (github-auto-delete-branch-gotcha: delete_branch_on_merge stays OFF).
10. Prune the branch backlog (1.7): close superseded `fix-release-*` / `*cicd*` / `*versioning*` / `chore/sync-template` /
    `release-notes-2.14` / `shields` branches (do NOT merge them - the migration supersedes their mechanics); delete merged dependabot
    branches; let codegen branches recreate themselves on the next daily run.

---

## 9. Open questions for the maintainer (with recommended defaults)

1. **Smoke gate (2.1).** Always-smoke (A) vs inline-diff change gate (B)? **Default: B** - NxWitness has many frequent pushes and a
   full product smoke is heavier than PlexCleaner's single-target smoke; keep the change gate but implement inline (drop paths-filter).
2. **Base-image sharing under one-branch (2.3).** Build the base only on the main run and have develop reuse the published shared tag, or
   build per-branch? **Default: build on main, develop reuses (`build_base: false`)** - the `:ubuntu-noble` base tag is branch-agnostic; a
   develop rebuild would churn the shared tag. Confirm base Dockerfiles never diverge between branches.
3. **Codegen scope (2.7).** Should codegen also run `make` to auto-regenerate Dockerfiles/compose on a new upstream version, or stay
   version-data-only? **Default: stay version-data-only** - Dockerfile structure changes warrant human review; the matrix data is the
   safe-to-automate part.
4. **Publisher shape (3).** One-branch-per-run + develop re-dispatch (3.1) vs hardened combined two-leg run with IGNORE_GITHUB_REF (3.2)?
   **Default: 3.1** - converges with the four live repos and deletes the cross-branch NBGV-leak class. 3.2 is acceptable only if the
   maintainer specifically wants a single combined weekly run; then harden with IGNORE_GITHUB_REF + the D2.2 backstop and document the
   divergence.
5. **version.json floor bump.** Bump from 2.14 to mark the overhaul and exercise the publish path? **Default: yes (deliberate
   maintainer-directed infra bump)**, reconciled in AGENTS.md so it does not contradict the "routine edits leave version.json untouched"
   rule.
6. **Docker-readme fold (2.9).** Fold into a main-only `publish-release.yml` job (canonical strip) vs keep a reusable task file given the
   12-repo list? **Default: fold** - matches PlexCleaner's strip; the manifest-jq derivation moves inline.
