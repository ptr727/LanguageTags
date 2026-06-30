# Migrate ESPHome-NonRoot to branch-scoped CI/CD + converge

Target: `ptr727/ESPHome-NonRoot` (`/home/pieter/ESPHome-NonRoot`). Docker-only image: layers a non-root
ESPHome + `esphome-device-builder` dashboard onto `python:3.14-slim`, ships one multi-arch image to
`docker.io/ptr727/esphome-nonroot`. No buildable app, no NuGet, no tests. Unique trait vs the other Docker
repos: it **tracks an upstream PyPI release** (`esphome` + `device_builder`) via a daily tracker that writes
`upstream-version.json` and opens auto-merged bump PRs. NBGV (`version.json`, floor `1.7`) still drives the
GitHub-release tag and `LABEL_VERSION`.

Canonical references read: PlexCleaner (`/home/pieter/PlexCleaner`) WORKFLOW.md + workflows + repo-config
(the converged Docker model, **one branch per run, no matrix**), the prior Docker-only plan
(`we-are-investigate-enhancement-peppy-bentley.md`, VSCode-Server), and memory (nbgv-githubref-leak,
docker-publishing-pattern, branch-scoped-cicd-review-gotchas, migration-playbook, auto-delete gotcha).
**Do not change code from this plan; this is a plan only.** All converged action SHAs were verified MATCH
against the GitHub API (setup-dotnet v5.4.0, checkout v7.0.0, nbgv v0.5.2, build-push v7.2.0).

---

## 1. Current-state assessment

ESPHome-NonRoot is on the **older ProjectTemplate two-phase model** - further behind than PlexCleaner was,
and it carries the upstream-tracker layer the pure-Dockerfile VSCode-Server repo did not.

**Workflows present (`.github/workflows/`):**

| file | trigger | shape |
|---|---|---|
| `publish-release.yml` | `push: [main, develop]` + `workflow_dispatch` + `schedule` (Mon 02:00) | two-phase: `setup` plan job reading `vars.PUBLISH_ON_MERGE`, then `[main,develop]` **matrix** `publish`, plus `date-badge`, `docker-readme`, `cleanup-artifacts` jobs |
| `test-pull-request.yml` | `pull_request: [main, develop]` + dispatch | `dorny/paths-filter` `changes` -> `smoke-build` -> aggregator `Check pull request workflow status` (**no ` job` suffix**) -> `cleanup-artifacts` |
| `build-release-task.yml` | `workflow_call` | `get-version` -> `build-docker` (gated `enable_docker`) -> `github-release`; **nested `get-version` re-run inside `build-docker-task`** |
| `build-docker-task.yml` | `workflow_call` | **has its own nested `get-version` job**; reads `upstream-version.json`; login gated on `push` (not on smoke); tags `latest`/`develop` + pinned esphome version |
| `get-version-task.yml` | `workflow_call` | NBGV; `setup-dotnet` **v5.3.0** (old SHA); **`nbgv@master`** (floated, not pinned) |
| `check-upstream-version.yml` | `schedule` (daily 05:00) + dispatch | entry-point; resolver curls PyPI for `esphome` + `esphome-device-builder` |
| `check-upstream-version-task.yml` | `workflow_call` | generic tracker: matrix over `[main, develop]`, App-signed `create-pull-request`, writes `upstream-version.json` (CRLF) |
| `merge-bot-pull-request.yml` | `pull_request_target` | `merge-dependabot` + **`merge-upstream-version`** + `disable-auto-merge`; **merges WITHOUT `--delete-branch`** |
| `build-datebadge-task.yml` | `workflow_call` | BYOB date badge (template cruft) |
| `publish-docker-readme-task.yml` | `workflow_call` | full generic docker-readme task (template cruft - fold into docker task) |

**Drift / debt vs canonical:**

- **Two-phase machinery present:** `setup` plan job, `vars.PUBLISH_ON_MERGE`, `push` publish trigger, the
  `[main,develop]` **branch matrix** in `publish` (the cross-branch NBGV-ref leak class, per
  `nbgv-publicrelease-githubref-leak`). Canonical is now **one branch per run, no matrix, no setup job**.
- **`pull_request` CI (not `push`):** PRs from forks satisfy the check, but a workflow-edit PR does not test
  its own copy, and there is no branch-deletion guard (none needed under `pull_request`, but needed after
  the switch to `push`).
- **`dorny/paths-filter`** gates the smoke build (strip per gotcha 12; always smoke, buildcache keeps fast).
- **Nested NBGV:** both `build-release-task` and `build-docker-task` run `get-version` - a double NBGV run,
  the exact gotcha-2 collision risk. Must thread `SemVer2` instead.
- **Aggregator name `Check pull request workflow status`** lacks the canonical ` job` suffix - the
  required-check string must move to `Check pull request workflow status job` in lockstep with the ruleset.
- **No `repo-config/`** at all (no ruleset JSON, no `configure.sh`, no `settings.json`). The live ruleset
  (if any) is hand-managed. This is the biggest missing piece.
- **No `WORKFLOW.md`**, **no `cspell.json`** (words live in the `.code-workspace`, ~60+ entries).
- **Old/floated SHAs:** `setup-dotnet` v5.3.0 (-> v5.4.0 `26b0ec1...`), `nbgv@master` (-> v0.5.2
  `705dad1...`). All Docker action SHAs already match canonical (qemu v4.1.0, buildx v4.1.0, login v4.2.0,
  build-push v7.2.0) - verified.
- **`docker-readme` is a separate task** + a `date-badge` task - both template cruft to strip.
- **CODESTYLE.md carries large `.NET` and `Python` sections** that are inert (no .cs, no buildable .py - the
  Python lives only inside the Dockerfile's uv venv). Per the repo's own "carry whole, don't trim" rule it
  kept them; the converged Docker repos drop them. **Flag for maintainer** (see open questions).

**Versioning:** NBGV, `version.json` floor `1.7`, `publicReleaseRefSpec ^refs/heads/main$`. Already correct
shape; floor bump to `1.8` to exercise the publish path (HISTORY tops out at 1.7).

**Branch hygiene / backlog (messy - clean before go-live):** `main` and `develop` have **diverged
substantially** - `git diff --stat origin/main origin/develop` = 12 files / ~1034 insertions (develop is
well ahead: `.editorconfig` +176, `CODESTYLE.md` +463 new, `Docker/Dockerfile` rewritten ~334 lines,
`merge-bot`, `publish-release`, `publish-docker-readme` all changed). Stale remote branches to triage/delete:
`chore/sync-template`, `feature/sync-versioned-rulesets`, `fix-devcontainer-venv-path`,
`fix/python-314-doc-refs`, `reconverge-upstream-tracker`, `resync-copilot-runbook-178`,
`resync-template-pr167`, `resync/projecttemplate-pr184`, `resync/projecttemplate-pr190`, `shields`,
`support-device-builder`, plus live tracker heads `upstream-version-main` / `upstream-version-develop` and
4 dependabot heads. **The migration must land on `develop` (the ahead branch), then promote develop->main**
- a promotion here also resolves the large develop/main content drift, so expect a substantive promotion PR.

**How a new upstream version becomes a published image today (the crux, traced):**

1. `check-upstream-version.yml` runs daily (05:00 UTC) + on dispatch; resolver curls PyPI for the latest
   `esphome` and `esphome-device-builder`, prints `{esphome, device_builder}`.
2. `check-upstream-version-task.yml` (matrix `[main, develop]`) rewrites `upstream-version.json` and opens an
   App-signed `upstream-version-<branch>` bump PR per branch.
3. `merge-bot-pull-request.yml`'s `merge-upstream-version` auto-merges each (squash to develop, merge to main).
4. On main, the merge is a `push` -> **today** `publish-release.yml` publishes **only if `PUBLISH_ON_MERGE`
   is `true`** (the maintainer's "releases on a dependabot/bump PR"); otherwise it waits for the weekly
   schedule. `build-docker-task` reads `upstream-version.json` for the image tag + build-args.

So `upstream-version.json` is **a committed build input**, read at build time - directly analogous to
`Directory.Packages.props` for the NuGet repos. The bump PR keeps it current; *something* then has to build.

---

## 2. Target architecture

Port PlexCleaner's converged Docker model **verbatim in shape**, dropping the executable target, and keep
ESPHome's two repo-specific leaves: `build-docker-task` (reads `upstream-version.json`, 3 build-args) and the
upstream-version tracker. **One branch per run, no matrix, no setup job, no PUBLISH_ON_MERGE.**

**`publish-release.yml`** (rewrite to the triggered-Docker one-branch model):
- Triggers: `workflow_dispatch` + `schedule` (`0 2 * * MON`, weekly baseline) **+ path-scoped `push` on
  main when the upstream pin changes** (`push: { branches: [main], paths: [upstream-version.json] }`). The
  daily upstream-check commits a real update to the pin -> this push publishes immediately; the weekly
  schedule covers base/apt rot when nothing upstream changed. Ordinary code merges do not touch the pin, so
  "merges never publish" still holds (a Dockerfile/README change ships on the next weekly run or a manual
  dispatch - see open questions for whether to widen the path set). Global concurrency group
  `${{ github.workflow }}`, `cancel-in-progress: false`.
- Single `publish` job, `if: github.ref_name == 'main' || github.ref_name == 'develop'`, calls
  `build-release-task.yml` with `ref: github.ref_name`, `branch: github.ref_name`, `smoke: false`,
  `github: true`, `dockerhub: true`. **Delete** `setup`, `date-badge`, `docker-readme`, and the run-level
  `cleanup-artifacts` jobs (the Docker push uploads no artifact; nothing to clean - matches PlexCleaner which
  has none). Schedule runs main only (`github.ref` = default branch); dispatch publishes its own ref.

**`build-release-task.yml`** (rewrite to thread NBGV, drop nested get-version):
- `validate` (gated `!smoke`, calls `validate-task`) + `get-version` (single NBGV) -> `build-docker`
  (gated `!cancelled() && get-version success && (validate success || skipped)`) -> `github-release`.
- `build-docker` is passed `ref: GitCommitId`, `branch`, `smoke`, `push: dockerhub && !smoke`, and the
  threaded `semver2` (+ the assembly versions, even though only `semver2` is consumed - keep the converged
  input set for byte-convergence). **Remove `enable_docker`** (always build the one target).
- `github-release`: unchanged canonical shape - main-only prerelease backstop, download
  `release-asset-<branch>-*` (matches nothing here, succeeds), no-op-if-tag-exists guard, dispatch refreshes,
  `target_commitish: GitCommitId`, `prerelease: branch != 'main'`, files `LICENSE` + `README.md`. **Keep
  `fail_on_unmatched_files` OFF** (or omit) - the Docker target ships no asset and the glob legitimately
  matches zero; PlexCleaner sets it true *because* its executable target must upload the 7z. Here it would
  red a clean Docker-only release. (This is a real per-repo divergence from PlexCleaner - flag in WORKFLOW.md.)

**`build-docker-task.yml`** (trim + thread, keep the upstream-version read):
- Inputs: `push`, `ref`, `branch` (required), `smoke`, **`semver2` (required, threaded)**. **Delete the
  nested `get-version` job** (gotcha 2) - consume `inputs.semver2`.
- Keep the `Get pinned versions step` reading `.esphome` / `.device_builder` from `upstream-version.json`.
- **Login on every build (incl. smoke)** like canonical (higher pull/cache rate limits; forks can't push) -
  change from today's `if: push`. This is what makes the Dependabot-store creds gotcha (3) load-bearing.
- Tags: `docker.io/ptr727/esphome-nonroot:${branch=='main' ? 'latest':'develop'}` + (main only) the pinned
  `:<esphome version>` tag. **Add a `:<semver2>` tag** to match canonical (every image carries its release
  version) - decide with maintainer whether to keep BOTH the esphome-version tag and the SemVer2 tag (see
  open questions). Branch-scoped buildcache (read both, write this branch on push), `mode=max`,
  `ignore-error=true`. Build-args `LABEL_VERSION=semver2`, `ESPHOME_VERSION`, `DEVICE_BUILDER_VERSION`.
- **Fold the Docker Hub overview in:** add a `peter-evans/dockerhub-description@v5.0.0` step gated
  `if: inputs.push && inputs.branch == 'main'`, `repository: ptr727/esphome-nonroot`,
  `readme-filepath: ./Docker/README.md` (already exists, 28 lines). Then **delete**
  `publish-docker-readme-task.yml`.

**`get-version-task.yml`**: bump `setup-dotnet` to v5.4.0 (`26b0ec14...`); **keep `nbgv@master`** (documented no-SHA-pin exception - do NOT pin it)
(`705dad19...`) - drop `@master`. Otherwise canonical.

**`validate-task.yml`** (NEW - lint-only, no unit-test): markdownlint (`**/*.md`) + cspell (README.md +
HISTORY.md) + actionlint, on `setup`-free runners. **No `unit-test`, no CSharpier/`dotnet format`** (no C#);
**no Python lint** (the Python is inside the Dockerfile only, exercised by the smoke build). Reused by CI and
each publish leg so the gates are identical. (PlexCleaner's `validate-task` has unit-test + C# lint; this is
the documented per-repo adapt.)

**`test-pull-request.yml`** (rewrite to push-CI canonical):
- `on: push: branches: ['**']` (not tags) + `workflow_dispatch`. **Drop `pull_request` and
  `dorny/paths-filter`.**
- `validate` (`if: !github.event.deleted`) + `smoke-build` (`build-release-task` with `smoke:true`,
  `github:false`, `dockerhub:false`, `branch: github.ref_name`, `if: !github.event.deleted`) +
  aggregator **`Check pull request workflow status job`** (`if: always() && !github.event.deleted`, fails
  unless every need is `success`). **Drop the terminal `cleanup-artifacts`** (smoke uploads nothing).
- Document the fork-PR exception (a fork PR produces no push -> no required check; a maintainer lands it on
  an in-repo branch first), same wording as PlexCleaner.

**`check-upstream-version.yml` + `check-upstream-version-task.yml`** (KEEP, light touch):
- These already match the canonical multi-key tracker shape (App-signed CRLF-writing PR, `[main,develop]`
  matrix, resolver inputs). Keep both. Only verify: action SHAs (`create-github-app-token` v3.2.0,
  `checkout` v7.0.0, `create-pull-request` v8.1.1 - already current), terse-comment conformance, and that
  the head-ref prefix `upstream-version` still matches the merge-bot's `merge-upstream-version` job refs.
- **The tracker's daily cadence is the staleness floor** for `upstream-version.json` currency; the *publish*
  cadence (section 3) is what turns a current pin into a pushed image.

**`merge-bot-pull-request.yml`** (converge, add `--delete-branch`):
- Keep `merge-dependabot`, `merge-upstream-version`, `disable-auto-merge-on-maintainer-push`.
- **Add `--delete-branch`** to both auto-merge calls (gotcha 5) - currently missing; bot/upstream branches
  accumulate without it. Repo-wide auto-delete stays OFF (settings.json) so develop survives promotion.
- The `disable-auto-merge` job already lists both bot logins (`dependabot[bot]`, `ptr727-codegen[bot]`) -
  keep (PlexCleaner only has dependabot, since it has no codegen/tracker bot; this is a justified per-repo
  superset, not drift to "fix").
- Converge comments to terse canonical.

**Release artifact:** a GitHub release **as version anchor** (tag on the built commit + `LICENSE` +
`README.md`, generated notes, no binary asset) plus the multi-arch Docker image on Docker Hub
(`latest`/`develop` + version tags) + the Docker Hub overview on a main publish.

### RESOLVED: release cadence vs the one-branch model (signed off 2026-06-29)

ESPHome is a **triggered Docker** repo: the daily upstream-check gives a 100%-certain "update required"
signal. Agreed model: **publish on the update trigger AND weekly** - a path-scoped `push` on main when
`upstream-version.json` changes publishes the new upstream immediately, and the weekly schedule refreshes the
base/apt layer when nothing upstream changed. This is NOT a merge-publish of arbitrary code (only the pin
file change publishes), so the "merges never publish" invariant and the one-branch NBGV correctness both
hold. See section 3 for the full rationale.

---

## 3. Release model decision (signed off 2026-06-29)

**Decision: triggered-Docker model - publish on `weekly schedule (main)` + `push-on-upstream-version.json-change (main)` + `workflow_dispatch`.** The daily upstream-check stays as the
detection mechanism (it owns the pin); a real upstream bump it commits to main publishes immediately via the
path-scoped push, and the weekly schedule refreshes the base/apt layer when nothing upstream changed.

**Why this shape:**

- **Best of both, no merge-publish.** ESPHome's daily check is a 100%-certain "update required" signal, so
  unlike vanilla Docker (VSCode-Server, which can only assume weekly apt rot) we publish the instant the pin
  changes - ~immediate upstream response - and still publish weekly for base CVEs. Only the pin file change
  publishes; arbitrary code merges do not (path filter), so the "merges never publish" invariant holds.
- **It is the `Directory.Packages.props` pattern.** The NuGet repos already publish on a push that touches
  their shipped dependency input; `upstream-version.json` is the exact Docker analog. This is a converged
  pattern, not a new one - and it is shared with NxWitness (push-on-`Matrix.json`-change), defining the
  **triggered-Docker sub-model**.
- **One-branch correctness preserved.** Every publish run is single-branch (`github.ref` == built branch),
  so NBGV classifies natively with no matrix, no `IGNORE_GITHUB_REF`, no cross-branch leak
  (`nbgv-publicrelease-githubref-leak`); `develop->main` still promotes via a normal Copilot-reviewed PR with
  no admin bypass. The push trigger is branch-filtered to `main`, so develop's daily pin update is sync-only
  (drift-avoidance) and never publishes.

**Staleness window:** new-upstream exposure is ~the daily-check interval (publish fires on the pin commit),
not a week; the weekly schedule only bounds *base-image* rot to <=7 days. Manual `gh workflow run
publish-release.yml` remains a zero-wait escape hatch.

**Open sub-decision (see section 9):** whether the publish `paths` filter should also include `Docker/**`
(so a Dockerfile change to main publishes too) or stay pin-only (Dockerfile/code changes wait for the weekly
run or a dispatch). Default recommended: **pin-only**, to keep "merges never publish" literal.

---

## 4. Files to create / edit / delete

**Create (8):**
- `WORKFLOW.md` - port PlexCleaner's, Docker-only + upstream-tracker variant (see section 5).
- `.github/workflows/validate-task.yml` - lint-only (markdownlint + cspell + actionlint).
- `repo-config/ruleset-develop.json` - byte-identical to canonical.
- `repo-config/ruleset-main.json` - byte-identical to canonical.
- `repo-config/settings.json` - byte-identical to canonical.
- `repo-config/configure.sh` - canonical helper bodies; secrets `DOCKER_HUB_USERNAME`,
  `DOCKER_HUB_ACCESS_TOKEN`, `CODEGEN_APP_CLIENT_ID`, `CODEGEN_APP_PRIVATE_KEY` in **both** stores; Docker
  Hub repo string `ptr727/esphome-nonroot`.
- `repo-config/README.md` - canonical, adapted repo name / Docker Hub repo.
- `cspell.json` - migrate the `.code-workspace` `cSpell.words` (~60+ entries) + any README/HISTORY words.

**Edit (10):**
- `.github/workflows/publish-release.yml` - rewrite to one-branch publisher (drop setup/matrix/push/
  PUBLISH_ON_MERGE/date-badge/docker-readme/cleanup jobs).
- `.github/workflows/build-release-task.yml` - add `validate` + thread NBGV; drop `enable_docker`; keep
  github-release (no `fail_on_unmatched_files`).
- `.github/workflows/build-docker-task.yml` - drop nested `get-version`; consume `semver2`; login always;
  add `:SemVer2` tag; fold in dockerhub-description step (main publish).
- `.github/workflows/get-version-task.yml` - setup-dotnet v5.4.0; **keep `nbgv@master`** (do NOT SHA-pin; documented exception).
- `.github/workflows/test-pull-request.yml` - push-CI, drop pull_request + dorny + cleanup; deletion guards;
  aggregator rename to `...status job`.
- `.github/workflows/merge-bot-pull-request.yml` - add `--delete-branch`; terse comments.
- `.github/workflows/check-upstream-version.yml` / `-task.yml` - terse-comment + SHA conformance only
  (no behavior change).
- `.github/dependabot.yml` - keep dual-target github-actions + docker (already correct); de-template
  comments only.
- `AGENTS.md` - rewrite Release Model (one-branch publisher + upstream-tracker), strip two-phase /
  PUBLISH_ON_MERGE / date-badge / docker-readme / paths-filter framing, point to `WORKFLOW.md`; add
  "Shared Configuration and Tooling" + "Write docs in the current state" + repo-config pointer; converge the
  shared sections (Comments, Git, Where rules live, PR Review Etiquette, Doc Style) byte-for-byte.
- `CODESTYLE.md` - **see open question** (drop inert .NET + Python sections to converge, OR keep per the
  repo's "carry whole" rule). `.github/copilot-instructions.md` - confirm byte-identical Runbook + repo
  placeholders (likely already current; edit only if drift).
- `version.json` - floor `1.7` -> `1.8`. `HISTORY.md` + `README.md` - add the 1.8 "CI/CD rework" entry.
- `ESPHome-NonRoot.code-workspace` / `.editorconfig` / `.gitattributes` - remove `cSpell.words` (moved to
  cspell.json), de-template comments, keep LF pins for `.sh`/`Dockerfile`/entrypoint scripts.

**Delete (2):**
- `.github/workflows/build-datebadge-task.yml`
- `.github/workflows/publish-docker-readme-task.yml`

(Net: ~8 create, ~14 edit, 2 delete. `Docker/Dockerfile`, `Docker/Compose.yml`, `Docker/entrypoint/*`,
`Docker/README.md`, `.devcontainer/*`, `LICENSE`, `.markdownlint-cli2.jsonc`, `.dockerignore`, `.gitignore`,
`.vscode/*` unchanged.)

---

## 5. Convergence + backports

**Port verbatim (byte-for-byte) from PlexCleaner:**
- `repo-config/ruleset-develop.json`, `ruleset-main.json`, `settings.json` (only the integration_id 15368
  and check string are shared constants - identical).
- `repo-config/configure.sh` helper bodies: `jq_lacks` (exit-4 + stderr handling), `check_secrets`
  (API-error-FAILs), `ruleset_id` (`first // empty`, no `2>/dev/null`), `check_app` (note-only),
  `check_ruleset` / `check_settings` / `check_security`. Only `REQUIRED_*_SECRETS`, the Docker Hub repo
  string, and `REPO` differ.
- AGENTS shared sections: **Comments** subsection, **Git and Commit Rules**, **Where rules live /
  Shared Configuration and Tooling**, **PR Review Etiquette** (Merge Gate, Expected Review Loop, Triaging,
  Responding, Escalating), **Documentation Style Conventions** incl. the **"Write docs in the current
  state"** rule, **Workflow YAML Conventions** pointer.
- `.github/copilot-instructions.md` GitHub Copilot Review Runbook (placeholders `<owner>/<repo>/<N>` only).
- WORKFLOW.md section skeleton (0 model + glossary, 1-2 style, 3 architecture, 4 D0-D10, 5 methodology incl.
  5D audit, 6 config).

**Adapt (per-repo):**
- WORKFLOW.md D4 (release/publish): **single Docker target + GitHub release as version anchor** (no
  executable/7z seam); add a **D-guarantee for the upstream-version tracker** (daily PyPI resolve ->
  App-signed bump PR -> auto-merge -> consumed by the next scheduled/dispatch publish) - the one genuine
  ESPHome-specific contract the siblings lack. Note the `fail_on_unmatched_files: false` divergence and why.
- `validate-task` is lint-only (no unit-test / C# / Python lint) - the documented adapt.
- WORKFLOW.md "Self-sufficiency": ESPHome **has** an upstream-version tracker (PlexCleaner's says "no
  codegen and no upstream-version tracker") - invert that sentence.
- `merge-bot` carries `merge-upstream-version` + a second bot login (justified superset).

**Backports to the four live repos (drift found):**
- None *new* surfaced beyond what the VSCode-Server plan already lists (the "Write docs in current state"
  rule missing from Utilities + LanguageTags; terse-comment alignment of `validate-task` / `merge-bot` in
  Utilities + LanguageTags). If those backports already landed with VSCode-Server, this migration introduces
  no new canonical drift - it **consumes** the converged form. Confirm the canonical `configure.sh` /
  ruleset JSON match the now-live PlexCleaner copies before porting (they are the source of truth).

---

## 6. Gotcha checklist (mapped to this repo)

1. **NBGV GITHUB_REF classification** - APPLIES. One-branch publisher fixes it natively: `github.ref` ==
   built branch, no `IGNORE_GITHUB_REF`. Floor `1.7`->`1.8` exercises the publish path. The dropped matrix
   removes the leak class entirely.
2. **NBGV threading (run once)** - **DIRECTLY APPLIES, current bug.** `build-docker-task` has its OWN nested
   `get-version`, *and* `build-release-task` has one - a double run. Delete the nested job; thread `semver2`
   from `build-release-task`'s single `get-version` into `build-docker-task` as an input. The pinned-version
   tag would otherwise be fine, but the `:SemVer2` tag could collide/misclassify.
3. **Docker creds in BOTH secret stores** - APPLIES (Dependabot auto-merges docker + actions bumps; their
   push-CI smoke build now logs in to Docker Hub because login moves to *always*). `configure.sh` requires
   `DOCKER_HUB_USERNAME` / `DOCKER_HUB_ACCESS_TOKEN` in both Actions and Dependabot stores; App creds too
   (the tracker is App-signed). **Verify the Dependabot store has them before go-live** or bot auto-merge
   stalls on a red smoke check.
4. **Branch-deletion guard** - APPLIES once CI moves to `push: ['**']`. Add `if: !github.event.deleted` to
   every `test-pull-request` job and `always() && !github.event.deleted` to the aggregator. (Not needed
   today under `pull_request`.)
5. **merge-bot `--delete-branch`** - **APPLIES, current gap.** Today's merge-bot merges WITHOUT
   `--delete-branch`, so `upstream-version-*` and dependabot heads accumulate (visible in the backlog). Add
   it to both merge calls; keep repo-wide auto-delete OFF in settings.json (so a develop->main promotion does
   not delete develop - `github-auto-delete-branch-gotcha`).
6. **5D audit hardened form** - APPLIES (creating `configure.sh` fresh). Use the final canonical bodies
   verbatim: `jq_lacks` exit-4-is-lacks + stderr kept; `check_secrets` API-error-FAILs; `ruleset_id`
   `first // empty` no-stderr-suppress; `check_app` note-only; audit FAILs when it cannot verify.
7. **Required-check name lockstep** - APPLIES. Rename aggregator to `Check pull request workflow status job`
   in `test-pull-request.yml`, set the same string in both ruleset JSONs and in `configure.sh`'s
   `REQUIRED_CHECK`. First `apply` against the live repo (in the same change shipping the workflow) is what
   lets the migration PR's required check resolve; then `check`.
8. **Aggregator success/skipped allowlist (D7.4)** - APPLIES. `validate` always runs; `smoke-build` always
   runs (no paths-filter now) so it won't skip - but use the canonical `success`-required loop (and the
   `build-release-task` build gate uses `(success || skipped)` with `!cancelled()` for the `validate`-skip
   on smoke). Don't use `!= 'failure'` (lets cancelled through).
9. **EOL discipline** - APPLIES. CRLF for `.md`/`.yml`/`.json`/`.code-workspace`; LF for `.sh`/`Dockerfile`/
   `entrypoint/*` (extensionless - `.gitattributes` already pins `*.sh` + `Dockerfile`; **add an explicit
   `Docker/entrypoint/** text eol=lf`** if the entrypoint scripts are extensionless, or confirm they end
   `.sh`). `upstream-version.json` must stay CRLF - the tracker writes it CRLF via `sed 's/$/\r/'`; verify
   with `tr -cd '\r' | wc -c`, not `file`.
10. **Copilot review loop** - APPLIES at go-live. Comments lag; resolve threads to merge; re-request via the
    `requestReviews` mutation (bot id `BOT_kgDOCnlnWA`); `gh api -X PATCH .../pulls/N -F body=@file` for body
    edits. OIDC/NBGV-prerelease/snupkg are false positives (no NuGet/OIDC here, so fewer). Budget 1-3 rounds.
11. **Dependabot dual-target main AND develop** - APPLIES. `dependabot.yml` already dual-targets both
    `github-actions` and `docker` - keep. (The *upstream-version tracker* also dual-targets via its
    `[main,develop]` matrix - same rationale: avoid non-linear rebase/merge-block conflicts.)
12. **Strip template cruft** - APPLIES. Delete `build-datebadge-task.yml` + `publish-docker-readme-task.yml`
    (fold overview into the docker task via `peter-evans/dockerhub-description` on a main publish); remove
    `dorny/paths-filter` (test-pull-request); remove `setup` / `PUBLISH_ON_MERGE` (publish-release). **Keep**
    `merge-upstream-version` and the `check-upstream-version*` tracker - this repo *does* use them (unlike the
    pure-Docker siblings); that is the documented exception to "drop unused merge-bot jobs."
13. **Action SHAs** - APPLIES. setup-dotnet -> v5.4.0 (`26b0ec14cb23fa6904739307f278c14f94c95bf1`); Docker
    actions already match canonical. **`nbgv` STAYS `@master` - do NOT SHA-pin it** (documented exception,
    ESPHome AGENTS.md "Action pinning": the tag stream lags master so a pin draws Dependabot downgrade PRs;
    the inline `@master` rationale comment is human-authored and must be preserved). Verify SHA->version
    claims against the GitHub API (do not trust Copilot's SHA claims).
14. **Prose rules** - APPLIES. No em-dashes; US English; terse comments (one line <=~120, top-of-file
    summary per workflow); never edit human-authored comments. Sweep the new/edited files.

---

## 7. Verification

**Static (local, before push):**
- `actionlint` (Docker: `docker run --rm -v "$PWD":/repo --workdir /repo rhysd/actionlint:latest -color`)
  over all `.github/workflows/*.yml`.
- `markdownlint-cli2` (`docker run --rm -v "$PWD":/workdir davidanson/markdownlint-cli2:latest "**/*.md"`).
- `cspell` over README.md + HISTORY.md (CI scope) using the new `cspell.json`.
- YAML + JSON parse (`python -c 'import yaml,sys,glob; [yaml.safe_load(open(f)) for f in glob.glob(...)]'`;
  `jq . repo-config/*.json upstream-version.json version.json`).
- `bash -n repo-config/configure.sh` and `bash -n` over inline resolver/run scripts where extractable.
- **EOL audit:** CRLF for `.md`/`.yml`/`.json`/`.code-workspace` (`grep -c $'\r'` or `tr -cd '\r' | wc -c`,
  NOT `file`); LF for `.sh`/`Dockerfile`/`Docker/entrypoint/*`. Re-check any `.md`/`.json` that Edit/Write
  touched (they can flip CRLF->LF).
- **Token sweep:** em-dash (`grep -rn $'—'`), plus
  `PUBLISH_ON_MERGE|two-phase|dorny|datebadge|build-datebadge|publish-docker-readme|setup job|ProjectTemplate`
  to confirm the cruft is gone (ProjectTemplate may legitimately remain in the Template-Lineage section if
  kept - decide with maintainer).

**Config audit (`repo-config/configure.sh`):**
- Before `apply`: `check` shows expected drift - the required-check rename (old live check
  `Check pull request workflow status` -> new `...status job`), Docker secrets possibly missing from the
  Dependabot store, no ruleset present (this repo has no repo-config today).
- `REPO=ptr727/ESPHome-NonRoot ./repo-config/configure.sh apply` then `check` -> "Configuration matches."

**Live (dispatch, never via merge):**
- `gh workflow run publish-release.yml --ref develop` -> develop prerelease `1.8.<height>-g<sha>`,
  `:develop` image (multi-arch: `docker buildx imagetools inspect` shows amd64+arm64), GitHub release
  (tag + LICENSE + README, marked prerelease, no binary asset).
- `gh workflow run publish-release.yml --ref main` (after promotion) -> clean `1.8.<height>`, `:latest` +
  `:<esphome>` (+ `:SemVer2`) image, non-prerelease release, Docker Hub overview = `Docker/README.md`.
- Re-dispatch main -> no duplicate release (no-op guard), image still re-pushed (base refresh).
- Confirm the image runs non-root (the repo's reason to exist): `docker run --user 1001:100 ... esphome
  version` succeeds against `/cache`.

---

## 8. Go-live sequence

1. **Triage the branch backlog first.** Decide which of the ~15 stale remote branches are obsolete; delete
   them (`gh pr close` / `git push origin --delete`). Confirm the live tracker heads `upstream-version-*`
   and dependabot heads are either merged or closed so they don't fight the migration.
2. Branch `feature/branch-scoped-cicd` off **`develop`** (the ahead branch). Apply iterations:
   (i) workflows + delete cruft; (ii) `WORKFLOW.md` + `repo-config/` + `cspell.json`; (iii) docs (AGENTS /
   CODESTYLE / copilot / dependabot / editorconfig / gitattributes / workspace / README / HISTORY /
   version 1.8). Run the full static verify after each.
3. Open PR -> `develop`. **Copilot dance** (`copilot-review-flow`): wait + buffer, re-request via mutation,
   resolve threads, decline false positives with rationale. Budget 1-3 rounds.
4. **`configure.sh apply` in lockstep** with the workflow edit (same change set), against the live repo -
   this creates the rulesets + renames the required check so the PR's `...status job` check can resolve and
   the Copilot rule attaches. Then `check`.
5. **Squash-merge to `develop`** (does NOT publish - one-branch model). Verify a develop push runs CI green.
6. **Promote `develop` -> `main` via a merge-commit PR, Copilot-reviewed, NO admin bypass.** Because main is
   well behind develop, this is a substantive promotion (12 files / ~1k lines of legit content drift, not
   just the migration). Watch for `migration-promotion-conflict` if main has straggler dependabot bumps in
   files develop rewrote; if so, a local signed merge commit (tree = develop) may be needed - but try the
   clean PR first. Decline pure-prose nits on the promotion PR (would diverge main from develop).
7. **Dispatch the publisher** on main (`gh workflow run publish-release.yml --ref main`) -> verify the main
   `1.8.x` stable artifacts (image + release + overview). Dispatch on develop to verify the prerelease leg.
8. **Confirm `develop` survives** the promotion (`github-auto-delete-branch-gotcha`: auto-delete is OFF;
   develop must still exist). Confirm the daily upstream tracker still opens bump PRs and the merge-bot
   auto-merges + deletes their heads.
9. Set/confirm the publish schedule cadence the maintainer signed off in section 3 (daily vs weekly).

---

## 9. Open questions for the maintainer

1. **Release cadence (the crux - REQUIRES SIGN-OFF).** Confirm option (a): drop `PUBLISH_ON_MERGE`/merge-
   publish; publish on schedule(main)+dispatch only; `upstream-version.json` is a shipped input. **Choose the
   schedule:** recommended **daily** (`0 2 * * *`, ~24h CVE/upstream window, matches the daily tracker), vs
   weekly (canonical default, ~7-day window), vs twice-weekly. Recommended default if no preference: **daily**.
   The manual `gh workflow run` escape hatch covers any "release this bump now" case regardless.
2. **CODESTYLE.md inert sections.** Drop the large `.NET` and `Python` sections to converge with the Docker
   siblings (which keep a General-only CODESTYLE), OR keep them per this repo's own "carry every section of a
   carried file even when inert here" rule? Recommended: **drop them** (the Python is Dockerfile-internal,
   not a maintained source tree; convergence wins). Needs a decision because it contradicts a stated repo rule.
3. **Image tag set.** Keep BOTH the pinned `:<esphome version>` tag (current behavior, user-meaningful) AND
   add the canonical `:<SemVer2>` tag, or only one? Recommended: **keep both** - the esphome tag is what
   users pin to; the SemVer2 tag matches the GitHub release. (Costs one extra tag push.)
4. **`fail_on_unmatched_files`.** Confirm leaving it OFF/omitted on the Docker-only `github-release` (the
   Docker target uploads no `release-asset-*`, so PlexCleaner's `true` would red a clean release). Recommended:
   **off**, documented as the per-repo divergence in WORKFLOW.md D4.
5. **Version floor bump 1.7 -> 1.8.** Confirm the deliberate infra bump to exercise the publish path (matches
   the sibling migrations: PlexCleaner 3.18->3.19, VSCode 1.0->1.1). Reconcile with the "routine edits leave
   version.json untouched" rule as a maintainer-directed overhaul bump.
6. **Template lineage framing.** AGENTS.md currently frames the repo as ProjectTemplate-derived "two-phase".
   Keep a (rewritten) Template-Lineage section pointing at the converged model, or drop the lineage framing
   entirely as the siblings did? Recommended: **keep a trimmed lineage note** (it is still a real downstream
   of ProjectTemplate) but rewrite it to the one-branch reality.
