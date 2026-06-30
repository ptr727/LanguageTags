# Branch-scoped CI/CD migration — shared planning briefing

You are writing a **detailed, review-ready migration + convergence plan** for one ptr727 repo. Four
sibling repos (LanguageTags, Utilities, PlexCleaner, VSCode-Server-DotNetCore) are already migrated to
this model and live. Your plan must **front-load every gotcha below** so nothing is rediscovered after
merge (the maintainer lost a lot of time to post-merge surprises in the last round and explicitly wants
that avoided this round).

Do **NOT** make code changes. Produce a plan file only. Be concrete: name files, jobs, triggers, and the
exact guards/SHAs. Where the repo's release model genuinely conflicts with the canonical model, FLAG the
tension explicitly with a recommendation rather than papering over it.

## Canonical references to read first (ground every claim in these, not assumptions)

- Docker model + terse comments + 5D audit: `/home/pieter/PlexCleaner/` — read `WORKFLOW.md`,
  `.github/workflows/*.yml`, `repo-config/configure.sh`, `repo-config/README.md`, `AGENTS.md`,
  `CODESTYLE.md`, `.github/copilot-instructions.md`.
- NuGet/.NET model: `/home/pieter/LanguageTags/` (same files). (Only relevant if your repo publishes
  NuGet — none of the three remaining do, but the .NET tooling sections still apply to NxWitness.)
- The just-completed Docker-only plan (closest template for a no-NuGet Docker repo):
  `/home/pieter/.claude/plans/we-are-investigate-enhancement-peppy-bentley.md`.
- Memory (durable gotchas, read all): `/home/pieter/.claude/projects/-home-pieter-LanguageTags/memory/`.
  Especially `branch-scoped-cicd-review-gotchas.md`, `nbgv-publicrelease-githubref-leak.md`,
  `docker-publishing-pattern.md`, `branch-scoped-migration-playbook.md`, `github-auto-delete-branch-gotcha.md`,
  `copilot-review-flow.md`, `terse-comments.md`.

## The model (one sentence): one run = one branch.

- **CI (`test-pull-request.yml`)**: triggers on **push to every branch** (not `pull_request`); runs
  validate + the project's smoke/test + a single aggregator job named exactly
  `Check pull request workflow status job` (the ruleset's required check, matched by string). Self-testing:
  pushing the branch IS the PR check.
- **Publisher (`publish-release.yml`)**: `workflow_dispatch` + `schedule` only. **NO `push` trigger for
  Docker repos.** Schedule builds **main only** via `github` context. Dispatch builds `github.ref_name`,
  guarded `if: github.ref_name == 'main' || github.ref_name == 'develop'`, passing `ref`/`branch` =
  `github.ref_name`. **No branch matrix, no branch switching, no two-phase setup job, no PUBLISH_ON_MERGE.**
  Merges never publish.
- **Promotion**: `develop -> main` PR, **merge-commit**, Copilot-reviewed, **no admin bypass**.

## Gotchas to bake into the plan (each caused a real post-merge fix last round)

1. **NBGV GITHUB_REF classification.** NBGV picks prerelease vs stable from `GITHUB_REF`, which is
   **read-only** (cannot be overridden in-job — a past attempt was a silent no-op). The one-branch model is
   the fix: `github.ref` already matches the built branch, so no `IGNORE_GITHUB_REF` hack is needed (that
   override only ever made sense for the old branch-matrix publisher). `version.json` floor +
   `publicReleaseRefSpec` `^refs/heads/main$`: main ships clean `X.Y.<height>`, develop ships
   `X.Y.<height>-g<sha>` prerelease. Bump the version floor to exercise the publish path.
2. **NBGV threading.** Run NBGV **once** per leg in `get-version`/`build-release-task` and **thread the
   computed `semver2` down** into the docker/build task. Do **not** add a nested `get-version` inside the
   build task — a second NBGV run can reclassify and produce a `:SemVer2` tag collision or a wrong
   stable/prerelease tag.
3. **Docker creds in BOTH secret stores.** Docker Hub (and App) credentials must exist in **both** the
   Actions **and** Dependabot secret stores: a Dependabot-triggered run is given the Dependabot store, and
   that run's push-CI does the Docker smoke/login. The `configure.sh` required-secret lists encode this.
4. **Branch-deletion guard.** Every `push`-triggered workflow job must guard `if: !github.event.deleted`
   (and the head jobs) so deleting a branch does not fire a phantom run.
5. **merge-bot `--delete-branch`.** The merge-bot must merge bot PRs with
   `gh pr merge --auto --delete-branch`. The repo-wide **auto-delete-on-merge setting stays OFF** (so a
   `develop -> main` promotion does not delete `develop`); per-merge deletion is explicit instead. Without
   `--delete-branch`, bot branches accumulate.
6. **5D audit (`repo-config/configure.sh`) — use the final hardened canonical form:**
   - `jq_lacks`: `jq -e ... >/dev/null || rc=$?; case "$rc" in 0) return 1 ;; 1|4) return 0 ;; *) return "$rc" ;; esac`
     — exit **4** (no output) is a "lacks" case (NOT just 1); keep **stderr** (only redirect stdout) so a real
     jq error (2/3/5) shows its diagnostic.
   - `check_secrets`: do **not** swallow gh stderr; an API/auth error **FAILs** the audit (cannot verify =
     must fail), distinct from a genuinely missing secret.
   - `ruleset_id`: let gh print its own error (no `2>/dev/null`), add a context line, return non-zero; select
     the first match **inside jq** (`first // empty`), not `| head -1` (SIGPIPE under pipefail).
   - `check_app`: best-effort **note**, never fails the audit (precise check needs app-level auth).
   - The audit must **fail when it cannot verify**, never pass by default.
7. **Required-check name lockstep.** The ruleset's required status-check string, the aggregator job name
   (`Check pull request workflow status job`), and the ruleset JSON must move in lockstep. The **first
   `apply`** against the live repo is what lets a PR on the new workflows go green. Run `apply` in the same
   change that ships the workflow edit, then `check`.
8. **Aggregator success/skipped allowlist (D7.4).** The aggregator gate must treat **success OR skipped**
   as passing for conditionally-skipped jobs, else a legitimately-skipped job blocks the PR.
9. **EOL discipline.** CRLF for `.md`/`.yml`/`.json`/`.code-workspace`; LF for `.sh`/`Dockerfile`/`.py`.
   `file` does NOT report CRLF for JSON — verify with `tr -cd '\r' | wc -c` or `grep -c $'\r'`. Pin these in
   `.gitattributes`/`.editorconfig`.
10. **Copilot review loop.** Comments lag the run (wait + buffer). Threads must be **resolved** to merge.
    Re-request review via the `requestReviews` GraphQL mutation with bot id `BOT_kgDOCnlnWA` — Copilot does
    **not** auto-review every push, so re-request after each new head. `gh pr edit --body` fails on the
    projects-classic GraphQL error → use `gh api -X PATCH repos/OWNER/REPO/pulls/N -F body=@file`. snupkg /
    OIDC / NBGV-prerelease are recurring **false positives** — decline with rationale. Pure-prose/format
    nits on a promotion PR: decline (would diverge main from develop). Every PR finds something new — expect
    1–3 rounds and budget for them.
11. **Dependabot + codegen dual-target main AND develop.** Deliberate — it solves the non-linear rebase /
    merge-block conflicts that arose with single-target. Keep both targets unless you can prove a simpler
    scheme merges develop->main without bypass; the maintainer has already rejected single-target.
12. **Strip template cruft.** Remove `build-datebadge-task.yml`, `publish-docker-readme-task.yml` (fold the
    Docker Hub overview into the docker task via a `peter-evans/dockerhub-description` step on a **main**
    publish), `dorny/paths-filter`, the `setup`/`PUBLISH_ON_MERGE` machinery, and any `merge-codegen` /
    `merge-upstream-version` jobs the repo does not actually use.
13. **Action SHAs.** Use the converged newer pins (e.g. `actions/setup-dotnet` v5.4.0, `actions/checkout`
    v7.0.0). **Verify any SHA->version claim against the GitHub API** before asserting it — Copilot has
    hallucinated SHA/version mappings; do not trust them. **EXCEPTION — `dotnet/nbgv` is consumed via
    `@master`, NEVER SHA-pinned.** The upstream tag stream lags `master` substantially, so a SHA pin draws
    spurious Dependabot downgrade PRs; this is a deliberate documented exception (see ESPHome AGENTS.md
    "Action pinning"). Do not convert `nbgv@master` to a SHA. Repos are currently split (PlexCleaner /
    VSCode-Server / HA-PurpleAir SHA-pin it; ESPHome / NxWitness float `@master`) — converge toward `@master`
    and never the other way. Likewise never edit a human-authored rule/rationale comment during a
    terse-comment pass (the `@master` rationale comment must survive).
14. **Prose rules.** No em-dashes anywhere (hard rule). US English. Terse comments: one line if it fits
    ~120 cols, structured, ASCII only; each workflow gets a top-of-file summary comment. Never edit
    human-authored comments — only agent-authored ones, to the terse style.

## Convergence requirement

The shared sections must converge **byte-for-byte where possible** across all repos: the Comments
subsection, Git/Commit rules, "Where rules live", PR Review Etiquette, Documentation Style Conventions
(incl. the "write docs in the current state" rule), the `repo-config/` ruleset JSON and `configure.sh`
helper bodies, the copilot Review Runbook. Per-repo differences are limited to: project description,
secret names, target-specific D-guarantees, and the publish mechanics. Your plan must call out which
canonical sections port verbatim and which adapt.

## What the plan file must contain (sections)

1. **Current-state assessment** — exact triggers, jobs, SHAs, version scheme, branch hygiene, and how the
   repo publishes today. Note partial-migration state and any messy branch backlog.
2. **Target architecture** — each workflow file: triggers, jobs, guards, threaded values, the publish
   trigger, and the release artifact. Reconcile the repo's release cadence with the one-branch model and
   FLAG conflicts.
3. **Release model decision** — the crux for each repo (see per-project note). State the recommended
   trigger explicitly and why.
4. **Files to create / edit / delete** — full list.
5. **Convergence + backports** — canonical sections to port verbatim vs adapt; any drift to backport to the
   four live repos.
6. **Gotcha checklist** — map each numbered gotcha above to where it applies in THIS repo (or "n/a, why").
7. **Verification** — static (actionlint/markdownlint/cspell/parse/bash -n/EOL/em-dash sweep), config audit
   (`configure.sh check` expected drift then "matches"), and live dispatch verification.
8. **Go-live sequence** — PR -> develop -> Copilot dance -> `apply` lockstep -> squash to develop ->
   promote develop->main (no bypass) -> dispatch publisher -> verify artifacts -> confirm develop survives.
9. **Open questions for the maintainer** — anything genuinely ambiguous, with your recommended default.

Write the plan to the path given in your task prompt. Make it thorough enough to execute from without
re-deriving the model.
