# HomeAssistant-PurpleAir branch-scoped CI/CD migration + convergence plan

Repo: github.com/ptr727/HomeAssistant-PurpleAir (local clone: /home/pieter/homeassistant-purpleair, lowercase).
Type: Python Home Assistant custom integration distributed via HACS (custom_components/purpleair). No Docker, no NuGet.
Release artifact: a GitHub Release whose asset is `purpleair.zip` (the integration's files at archive root, HACS `zip_release` layout).

Reference ground truth read for this plan:
- /home/pieter/LanguageTags (NuGet canonical), /home/pieter/PlexCleaner (Docker canonical) workflows, repo-config, AGENTS.md, copilot-instructions.md.
- This repo's 8 workflows, version.json, hacs.json, manifest.json (main + develop), dependabot.yml, ha-test-versions.json, AGENTS.md, CODESTYLE.md, .gitattributes, git history.

HEADLINE: this repo is NOT a greenfield migration. `develop` has already been migrated most of the way to a
branch-scoped model in a prior round, and is heavily converged (AGENTS.md PR Review Etiquette, copilot Review
Runbook, NBGV versioning, HA-matrix bot, HACS zip-layout assertion are all already present on develop). The work
is (a) closing the remaining gaps to canon, (b) the crux release-trigger decision, (c) creating the missing
`repo-config/` 5D audit, and (d) a large branch-backlog + main/develop reconciliation cleanup. Do NOT rebuild from
the template; converge develop forward.

---

## 1. Current-state assessment

### 1.1 Branch hygiene (messy backlog - the big one)
- `develop` is 92 commits AHEAD of `main`, 0 behind. `git merge-base main develop` = `01e4292` (main tip,
  PR #60 "reseed main manifest to 0.3.0 after pipeline regression"). Local `develop` == `origin/develop` (`dc8b34b`), clean.
- `main` is stuck in the OLD release-please era: it still carries `.github/workflows/release-please.yml`, has NO
  `version.json`, NO `repo-config/`, manifest.json `version: 0.3.0`. develop has none of that legacy and has the
  full NBGV + HA-matrix + HACS-zip pipeline.
- 54 remote branches. Abandoned/superseded migration attempts and one-offs:
  - NBGV era leftovers: `nbgv`, `nbgv-prerelease-fix`, `restore-prerelease-gate`, `chore/seed-develop-prerelease-manifest`,
    `chore/restore-develop-manifests-beta`, `chore/reseed-main-manifest-0.3.0`.
  - HACS-zip pain (ALREADY merged into develop via PRs #80, #82): `fix-hacs-zip-layout`, `zip-layout-assertion-prefix-fix`. STALE.
  - Sync churn: `sync-main-into-develop`, `chore/sync-main-into-develop-2`, `ruleset-and-refs-followup`, `feature/sync-versioned-rulesets`.
  - release-please era: `release-please--branches--develop--...`, `release-please--branches--main--...`, `chore/remove-changelog`.
  - Many open dependabot/* branches targeting develop, plus feature branches (`subentry-reconfigure-readkey`,
    `feat/org-name-title-and-first-refresh-fix`, etc).
- VERIFY-before-delete: of the named "abandoned migration" branches, all six checked
  (`nbgv`, `nbgv-prerelease-fix`, `restore-prerelease-gate`, `fix-hacs-zip-layout`, `zip-layout-assertion-prefix-fix`,
  `sync-main-into-develop`) are NOT ancestors of develop, but the HACS-zip *content* already landed via squash PRs,
  so the branches are stale duplicates, not lost work. Treat NBGV branches the same way (the NBGV pipeline is live on develop).

### 1.2 Triggers and jobs today (on develop)
- `test-pull-request.yml`: triggers `pull_request: [main, develop]` + `push: [main, develop]` + `workflow_dispatch`.
  Jobs: `test-release` (calls test-release-task.yml) and aggregator `check-workflow-status`
  named **"Check pull request workflow status"** (NO trailing " job"). Concurrency `${{ github.workflow }}-${{ github.ref }}`.
  -> DIVERGES from canon: canon triggers `push: ['**']` (every branch), no `pull_request`; aggregator name is
  **"Check pull request workflow status job"**; aggregator must guard `!github.event.deleted` and treat success-only per job.
- `test-release-task.yml` (reusable, the validate+test set): jobs `ruff` (check + format --check), `mypy` (--strict),
  `pyright`, `read-versions` (parses ha-test-versions.json), `pytest` (matrix over minimum/latest-stable/latest-beta,
  uploads to Codecov), `hassfest`, `hacs`, and `build-release` (no-publish build gated by `build` input).
- `publish-release.yml`: triggers `workflow_dispatch: {}` + **`push: [develop]`**. Jobs: `gate` (assert dispatch from
  main), `test-release` (build:false), `create-release` (github:true), `date-badge` (dispatch-only), `cleanup-artifacts`.
  Concurrency `${{ github.workflow }}` global, cancel-in-progress:false.
  -> DIVERGES from canon: canon publisher has NO push trigger; merges never publish. THIS repo auto-publishes a
  prerelease on every develop push. This is the crux - see section 3.
- `build-release-task.yml` (reusable): `get-version` (calls get-version-task), `build` (stamp manifest with NBGV
  SemVer2, zip custom_components/purpleair at root, **zip-layout assertion already present**, upload artifact),
  `release` (download + softprops GitHub Release, `target_commitish: github.sha`, prerelease flag from get-version).
- `get-version-task.yml`: single NBGV run (setup-dotnet v10), outputs SemVer2/Tag/Prerelease. Prerelease
  detected by `-` in SemVer2. CORRECT single-run threading (gotcha 2 already satisfied). **nbgv currently
  SHA-pinned to v0.5.2 (`705dad19`) - CONVERT to `@master`** per the documented no-SHA-pin exception (the tag
  stream lags master; a pin draws Dependabot downgrade PRs). See gotcha 13 / briefing.
- `build-datebadge-task.yml`: BYOB "Last Build" badge, gated on Prerelease==false. Template cruft per gotcha 12.
- `check-ha-version.yml`: daily cron + dispatch. Monitors `pytest-homeassistant-custom-component` on PyPI (whose
  `homeassistant==` pin IS the upstream HA core version being tracked), resolves latest-stable + latest-beta HA pairs,
  opens ONE bundled PR on rolling branch `ha-version-bump/matrix` -> develop via the codegen App. Does NOT publish.
- `merge-bot-pull-request.yml`: `pull_request_target`. Jobs merge-dependabot (squash develop / merge main by base),
  merge-ha-version-bump (squash develop), disable-auto-merge-on-maintainer-push. Uses App token.
  -> DIVERGES: `gh pr merge --auto` with NO `--delete-branch` (gotcha 5).

### 1.3 Version scheme (NBGV present)
- `version.json`: base `version: "0.1"`, `publicReleaseRefSpec: ["^refs/heads/main$"]`, nugetPackageVersion.semVer 2.
  Standard branch-scoped floor. main ships `0.1.<height>`, develop ships `0.1.<height>-g<sha>`.
- manifest.json `version`: develop = `0.0.0` placeholder (stamped at build time from NBGV); main = `0.3.0` (legacy,
  release-please era). hacs.json has NO `version` field (HACS reads the stamped manifest). hacs.json `homeassistant`
  = `2026.4.0` (the user-facing MINIMUM, hand-maintained, must match ha-test-versions.json `minimum.ha` and the
  requirements.txt bootstrap pin series).
- The NBGV/version.json model is already correctly wired on develop; gotcha 1 is structurally satisfied. See 6.1.

### 1.4 repo-config / 5D audit
- DOES NOT EXIST. No `repo-config/` directory on develop or main. This is the single largest missing canonical piece.
  Rulesets are presumably configured live in the UI but are not codified or auditable. configure.sh + ruleset JSON
  must be created from the LanguageTags canonical (adapting only secret names + the publish manual-verify note).

### 1.5 SHA pins (gotcha 13, verified against GitHub API)
- setup-dotnet `9a946fd` = v5.3.0 (correct; canon prefers newer v5.4.0 - optional bump).
- nbgv `705dad19` = v0.5.2 - **SHA-pinned today, but CONVERT to `@master`** (documented no-SHA-pin exception;
  do NOT keep the pin).
- checkout `df4cb1c069...` = the v6.0.3 commit (annotated tag `v6.0.3` -> `df4cb1c`; verified by dereference). Correct
  (canon mentions v7.0.0 - a dependabot bump branch `dependabot/.../actions/checkout-7.0.0` already exists; let it land).
- All other pins (create-github-app-token v3.2.0 `bcd2ba49`, setup-python v6.3.0 `ece7cb06`, softprops v3.0.1
  `718ea10b`, codecov v6.0.1, upload/download-artifact, hacs/action 22.5.0) appear consistent; spot-verify any the
  reviewer challenges - do NOT trust Copilot SHA->version claims.

---

## 2. Target architecture

One run = one branch. Reconcile per-file. The HACS pull-model release cadence is the one place the repo legitimately
diverges from the Docker/NuGet canon; section 3 resolves it.

### 2.1 test-pull-request.yml (CI - align to canon)
- Triggers: `push: branches: ['**']` + `workflow_dispatch`. REMOVE the `pull_request` trigger and the `push:[main,develop]`
  restriction. Self-testing: pushing any branch IS the PR check.
  - Tension: the existing `push:[main,develop]` exists to re-upload Codecov on the post-merge SHA (default-branch badge).
    Under `push:['**']` that still happens (main/develop are members of `**`), so the Codecov goal is preserved. Keep the
    explanatory comment, retargeted.
- Jobs: keep `test-release` (calls test-release-task.yml). RENAME aggregator job to exactly
  **"Check pull request workflow status job"** (job key may stay `check-workflow-status`). Add `if: ${{ !github.event.deleted }}`
  to the head job(s) and the aggregator. Aggregator gate: per-need loop treating `success` as the only pass for a
  required need; for any conditionally-skipped need use the success-OR-skipped allowlist (gotcha 8). Today there is one
  need (`test-release`) which is never skipped, so the simple `!= 'success' -> exit 1` is correct; keep it but ensure the
  `!github.event.deleted` guard so deleting a branch does not red a phantom run (gotcha 4).
- Concurrency unchanged (`${{ github.workflow }}-${{ github.ref }}`, cancel-in-progress true).

### 2.2 test-release-task.yml (validate + test set - this is the Python adaptation of canon's validate/smoke)
- KEEP AS-IS structurally. This is the correct Python mapping of the canonical "validate + smoke/test" pair:
  - validate role: `ruff` (lint+format), `mypy --strict`, `pyright`, `hassfest`, `hacs` (replaces canon's
    markdownlint/cspell-only validate).
  - smoke/test role: the `pytest` matrix over `minimum` / `latest-stable` / `latest-beta` HA versions (this IS the
    HA-core-version test matrix the per-project note calls for) + `build-release` (no-publish build that exercises the
    zip/HACS-layout path, analogous to canon's `smoke-build`).
- Optionally add `markdownlint`/`cspell` validate jobs if the repo wants the canonical doc-lint parity (the repo has
  `.markdownlint-cli2.jsonc`); RECOMMEND keeping these as separate validate jobs only if they already pass clean -
  do not introduce new failing gates during migration. Flag as an open question (9.4).

### 2.3 publish-release.yml (the crux - see section 3 for the decision)
RECOMMENDED target (decision: dispatch-only publish, schedule retests only):
- Triggers: `workflow_dispatch: {}` + `schedule` (daily/weekly cron). REMOVE `push: [develop]`.
- `gate` job: keep, generalize to allow dispatch from `main` OR `develop`, guard
  `if: github.ref_name == 'main' || github.ref_name == 'develop'` (canon shape). main dispatch = stable; develop
  dispatch = prerelease (NBGV `-g<sha>` makes the tag unique). This restores "merges never publish".
- `schedule` leg: RETEST ONLY, NEVER publish. The schedule runs on the default branch (main) by GitHub rule; it should
  run `test-release` (validate+test) against main and STOP - no `create-release`. Rationale: HACS is a pull model and
  the maintainer explicitly does not auto-push; the schedule's job is to catch upstream HA/pytest-hacc drift breaking
  the shipped main, surfacing a red run for a maintainer to act on, not to cut a release. (`check-ha-version.yml`
  already handles the develop-side retest-and-bump; the publisher schedule covers main, which the matrix bot does not touch.)
  - Implement: `create-release` (and date-badge) gate `if: github.event_name == 'workflow_dispatch'`. The schedule path
    runs gate(skipped)->test-release->[create-release skipped]. Concurrency global, cancel-in-progress:false (unchanged).
- `create-release` job: unchanged otherwise (build:false on test-release, github:true on create-release, softprops with
  target_commitish github.sha, prerelease from NBGV).
- `date-badge`: FOLD or DELETE per gotcha 12 (template cruft). RECOMMEND delete build-datebadge-task.yml and the
  date-badge job - it is a "Last Build" vanity badge with no release-correctness role. Flag as open question 9.3 since it
  is currently wired and green.
- `cleanup-artifacts`: keep (always(), best-effort).

### 2.4 build-release-task.yml (release artifact - keep, it is the HACS zip producer)
- KEEP. The zip-layout assertion (manifest.json + __init__.py at root, no `purpleair/` wrapper, `./` normalization) is
  ALREADY baked in (lines 68-99) from PRs #80/#82 - gotcha-equivalent "bake the zip-layout assertion" is DONE. Do not
  remove it; verify it survives any edit. Single NBGV run via get-version-task threaded down as SemVer2 (gotcha 2 satisfied).

### 2.5 get-version-task.yml / merge-bot / check-ha-version / dependabot
- get-version-task: keep (correct single NBGV run).
- merge-bot-pull-request.yml: add `--delete-branch` to BOTH `gh pr merge --auto` calls (merge-dependabot,
  merge-ha-version-bump). Keep repo-wide auto-delete-on-merge OFF in settings.json so develop->main promotion does not
  delete develop (gotcha 5).
- check-ha-version.yml: keep as-is (it is the repo's upstream-monitor; already retest-not-publish). Confirm its rolling
  PR + bundle design is documented in AGENTS.md (it is).
- dependabot.yml: TODAY single-targets develop only (every ecosystem `target-branch: develop`). **DECIDED
  (2026-06-29): dual-target main AND develop** (gotcha 11) - the maintainer confirmed "dependabot should
  still keep main and develop updated, i.e. avoid merge drift." Converge all ecosystems (pip +
  github-actions) to dual-target like the four live repos; develop's bumps are sync-only and never publish.
  Open question 9.1 is resolved.

---

## 3. Release model decision (THE CRUX)

Question: how does the HACS-zip release map onto one-branch, given releases are NOT automatic on merge and HACS is a
pull model?

What is monitored upstream: `check-ha-version.yml` monitors `pytest-homeassistant-custom-component` on PyPI, whose
`homeassistant==` pin is the de-facto upstream **HA core version** (stable and beta). `aiopurpleair` (the API client,
pinned as `aiopurpleair-ptr727==<date>` in manifest/requirements) is NOT auto-monitored; it moves via Dependabot pip
PRs (note develop manifest pins 2026.8.0 vs main 2026.4.0). So "upstream" = HA core (via pytest-hacc), monitored daily,
retested on develop via a bundled bot PR; it does not publish.

**The monitor is a breakage tripwire (maintainer-confirmed intent):** the HA-version bump updates the test
matrix with the new HA release specifically so a breaking upstream change makes the bot PR's CI **fail**; a
human then intervenes, fixes, and releases manually via dispatch. The monitoring exists to surface breakage
early, not to ship - which is exactly why publish is dispatch-only and the schedule retests but never
publishes.

How/when the HACS zip is cut today: TWO paths - (a) every develop push auto-cuts a PRERELEASE GitHub Release; (b)
`workflow_dispatch` from main cuts a STABLE release. Path (a) directly contradicts the maintainer's stated model ("does
NOT auto push") and the canonical rule "merges never publish".

DECISION: **dispatch-gated publish; schedule retests only; NO push-to-develop publish.**
- Stable release: `gh workflow run publish-release.yml --ref main` (gate asserts main). Cuts `0.1.<height>` clean.
- Prerelease (when a beta tester build is wanted): `gh workflow run publish-release.yml --ref develop`. NBGV emits
  `0.1.<height>-g<sha>`, softprops marks it prerelease. This replaces the automatic develop-push prerelease with an
  on-demand one - same artifact, same uniqueness guarantee, but maintainer-initiated.
- Schedule (daily/weekly): runs `test-release` against main ONLY (retest the shipped integration against the latest HA
  matrix). NEVER publishes. This is the publisher-side complement to check-ha-version's develop-side retest.

Rationale:
1. Matches the maintainer's explicit "monitor + retest but do NOT auto push (HACS is pull)" requirement verbatim.
2. Restores the canonical invariant "merges never publish" (briefing model + gotcha-adjacent). The AGENTS.md "Merging is
   not releasing" verbatim contract currently lies, because develop-push DOES release; this decision makes the docs true.
3. Removes the develop-push trigger entirely, eliminating the only `push`-on-merge publish path - aligns with the
   PlexCleaner Docker canon (dispatch + schedule, no push) which is the closest precedent for a non-NuGet artifact.
4. Keeps NBGV GITHUB_REF classification correct (gotcha 1): a develop dispatch has `github.ref = refs/heads/develop`
   (one run = one branch), so NBGV classifies it prerelease with no IGNORE_GITHUB_REF hack. A main dispatch is clean.

TENSION FLAGGED: the existing code+docs treat develop-push prerelease as a feature ("beta testers always have the
latest"). Dropping it trades automatic prereleases for on-demand. If the maintainer wants beta testers to keep getting
every develop push automatically, the alternative is to KEEP `push:[develop]` as a documented, deliberate exception to
the one-branch publisher (the repo already gates it correctly on test success). Surface both; RECOMMEND dispatch-only to
honor the stated "does NOT auto push" requirement. See open question 9.2.

---

## 4. Files to create / edit / delete

### CREATE (8)
1. `repo-config/configure.sh` - port LanguageTags verbatim; change only:
   - `REQUIRED_ACTIONS_SECRETS=(CODEGEN_APP_CLIENT_ID CODEGEN_APP_PRIVATE_KEY CODECOV_TOKEN)` (no NuGet/Docker; Codecov
     token is the repo's one publish-ish secret - it is Actions-only, NOT Dependabot).
   - `REQUIRED_DEPENDABOT_SECRETS=(CODEGEN_APP_CLIENT_ID CODEGEN_APP_PRIVATE_KEY)` (the codegen App secrets must be in BOTH
     stores so a Dependabot-triggered push-CI can mint the App token; Codecov is not needed by Dependabot runs).
   - `REQUIRED_CHECK="Check pull request workflow status job"` (identical string).
   - cmd_check manual-verify note: "GitHub Releases / HACS zip publish is dispatch-gated; no external publish policy to verify."
   - check_app note: "verify the codegen App is installed".
   - Keep jq_lacks / check_secrets / ruleset_id / check_app helper BODIES byte-identical to canon (gotcha 6).
2. `repo-config/ruleset-develop.json` - port verbatim (squash-only, linear, signed, deletion+non_fast_forward,
   required check "Check pull request workflow status job" with integration_id 15368, copilot_code_review, 0 approvals,
   strict false).
3. `repo-config/ruleset-main.json` - port verbatim (merge-only, NO linear, signed, same required check, copilot review).
4. `repo-config/settings.json` - port verbatim (allow_squash+merge, rebase off, auto_merge on, delete_branch_on_merge:false).
5. `repo-config/README.md` - port verbatim, substitute repo name; document the dispatch-only HACS publish in place of the
   NuGet/Docker publish line.
6. (optional) `markdownlint`/`cspell` validate jobs - only if 9.4 says yes; else skip.

### EDIT (8-9)
1. `.github/workflows/test-pull-request.yml` - triggers -> `push:['**']` + dispatch (drop pull_request); aggregator name
   -> "Check pull request workflow status job"; add `!github.event.deleted` guards.
2. `.github/workflows/publish-release.yml` - drop `push:[develop]`; add `schedule`; gate dispatch main||develop; gate
   create-release/date-badge on `github.event_name == 'workflow_dispatch'`; (delete date-badge if 9.3 yes).
3. `.github/workflows/merge-bot-pull-request.yml` - add `--delete-branch` to both `gh pr merge --auto` calls.
4. `.github/dependabot.yml` - dual-target main AND develop (if 9.1 yes).
5. `AGENTS.md` - update "Release flow" + "Merging is not releasing" to match the dispatch-only decision (remove the
   "Push to develop -> automatic prerelease" bullet; document dispatch-from-develop + schedule-retests-main). Add a
   "Where rules live" pointer if missing; verify Comments subsection matches canon verbatim. Add `repo-config/` pointer.
6. `CODESTYLE.md` - STRIP the `.NET` section (lines 39-343) - template cruft for a Python-only repo (gotcha 12). Keep
   General + Python.
7. `.github/copilot-instructions.md` - verify Review Runbook is byte-converged with canon; substitute owner/name
   `ptr727/homeassistant-purpleair` in any hardcoded GraphQL snippets; confirm bot login `copilot-pull-request-reviewer`.
8. `custom_components/purpleair/manifest.json` (main side, during reconciliation) - main's `0.3.0` must become `0.0.0`
   placeholder to match the stamp-at-build model when develop lands on main (handled by the promotion, see 8).
9. (if SHA convergence chosen) bump checkout v6.0.3->v7.0.0, setup-dotnet v5.3.0->v5.4.0 across workflows - or let the
   existing dependabot/checkout-7 branch land.

### DELETE (2-4)
1. `.github/workflows/build-datebadge-task.yml` - template cruft (gotcha 12), if 9.3 yes.
2. `.github/workflows/release-please.yml` - ONLY EXISTS ON MAIN (legacy). Removed automatically when develop overwrites
   main in the promotion; no develop-side action.
3. 40+ stale remote branches (backlog cleanup, see 8.6) - not files but a delete pass.
4. (n/a) No `dorny/paths-filter`, no `setup`/`PUBLISH_ON_MERGE`, no `merge-codegen`/`merge-upstream-version`,
   no `publish-docker-readme-task.yml` exist here - already absent.

Net: ~8 create, ~8-9 edit, ~2-4 delete (workflow-file deletes 1-2; plus a branch-backlog delete pass).

---

## 5. Convergence + backports

PORT VERBATIM from LanguageTags (byte-for-byte target):
- `repo-config/configure.sh` helper bodies (jq_lacks, check_secrets, ruleset_id, check_app, assert, apply_ruleset,
  check_ruleset, check_settings, check_security, cmd_apply/cmd_check/dispatch). Only the secret arrays + two note strings differ.
- `repo-config/ruleset-develop.json`, `ruleset-main.json`, `settings.json` (only the required-check string is shared and
  already canonical).
- AGENTS.md: Comments subsection, Git/Commit rules, "Where rules live" lead paragraph, PR Review Etiquette (already
  present and looks converged - diff against canon), Documentation Style Conventions incl. the line-endings
  "preserve current state" rule.
- `.github/copilot-instructions.md` Review Runbook (bot id read-from-review pattern, requestReviews mutation, coverage
  check, thread resolution).
- `.editorconfig` EOL rules (CRLF for .md/.yml/.json; LF for .sh/.py) - the repo already has a 10KB .editorconfig; diff
  the relevant blocks against canon.

ADAPT (Python-specific, not verbatim):
- `test-release-task.yml` validate/test set (ruff/mypy/pyright/hassfest/hacs/pytest-matrix) - no canonical sibling; this
  is the repo's correct Python mapping. Keep.
- publish-release.yml mechanics (HACS zip via softprops, dispatch+schedule-retest) - per-repo publish seam.
- configure.sh secret arrays + manual-verify note.

BACKPORT TO THE FOUR LIVE REPOS (drift found here worth propagating):
- The HACS-zip-layout assertion pattern is repo-unique; nothing to backport.
- check-ha-version.yml's PEP-440 walk + bundled rolling-PR design is more robust than a naive sort; if any sibling repo
  monitors a PyPI/upstream version, consider porting the `packaging.version` ordering. LanguageTags/PlexCleaner do not,
  so likely n/a.
- If the .editorconfig or Comments subsection here has drifted ahead of canon, reconcile toward the canonical text (do
  not let this repo's copy become a fork).

---

## 6. Gotcha checklist (mapped to THIS repo)

1. NBGV GITHUB_REF classification - SATISFIED on develop. version.json floor `0.1` + `publicReleaseRefSpec ^refs/heads/main$`;
   one-run-one-branch means github.ref already matches; no IGNORE_GITHUB_REF hack present or needed. ACTION: when adopting
   dispatch-only, a develop dispatch keeps github.ref=refs/heads/develop so NBGV stays prerelease - correct. Bump version.json
   `0.1`->`0.2` once to exercise the publish path during go-live verification.
2. NBGV threading - SATISFIED. Single nbgv run in get-version-task; SemVer2 threaded into build-release-task's stamp +
   softprops. No nested get-version. Keep it that way.
3. Docker creds in both secret stores - N/A (no Docker). The ANALOG: codegen App secrets (CODEGEN_APP_CLIENT_ID/PRIVATE_KEY)
   MUST be in both Actions AND Dependabot stores, because a Dependabot-merged push fires develop CI and the merge-bot mints
   the App token. configure.sh REQUIRED_DEPENDABOT_SECRETS encodes this.
4. Branch-deletion guard - NOT PRESENT today (test-pull-request uses pull_request, not push:['**']). ADD `!github.event.deleted`
   when switching to push:['**'] so deleting a branch does not fire a phantom CI run.
5. merge-bot --delete-branch - MISSING. ADD to both merge calls. Keep repo-wide auto-delete OFF.
6. 5D audit hardened form - configure.sh DOES NOT EXIST; create from the final hardened canonical (jq_lacks exit-4 case,
   check_secrets fail-on-API-error, ruleset_id first//empty + visible gh error, check_app best-effort note, fail-when-cannot-verify).
7. Required-check name lockstep - the ruleset JSON required-check, the aggregator job name, and the live ruleset must all
   read "Check pull request workflow status job". Today the workflow says "...status" (no " job") and there is no ruleset JSON.
   Fix the workflow name AND create the JSON AND run `apply` in the same change that ships the workflow edit, then `check`.
8. Aggregator success/skipped allowlist - today one never-skipped need, so success-only is fine. If validate jobs are split
   out (markdownlint/cspell) keep the simple loop but ensure any conditionally-skipped need uses success-OR-skipped.
9. EOL discipline - CRLF for .md/.yml/.json/.code-workspace, LF for .sh/.py/Dockerfile. Repo has .gitattributes (`* -text`
   + LF pins for *.sh/scripts/*) and a large .editorconfig. VERIFY .json/.yml CRLF with `tr -cd '\r' | wc -c` (file(1)
   lies for JSON). New repo-config/*.json must be CRLF; configure.sh must be LF.
10. Copilot review loop - copilot-instructions.md Review Runbook already present; bot login `copilot-pull-request-reviewer`
    (GraphQL, no [bot]) / `...[bot]` (REST). Re-request via requestReviews mutation each head. Expect 1-3 rounds; snupkg/OIDC
    false positives are N/A (no NuGet); HACS-zip / NBGV-prerelease are the likely recurring false positives here - decline
    with rationale. Use `gh api -X PATCH .../pulls/N -F body=@file` for body edits.
11. Dependabot dual-target - today develop-only. DECISION (9.1): converge to dual-target main AND develop to match canon
    and avoid the non-linear merge-block; unless maintainer confirms main-bumps are pointless under HACS pull. Default: dual.
12. Strip template cruft - DELETE build-datebadge-task.yml + date-badge job; STRIP CODESTYLE.md .NET section. No
    paths-filter / PUBLISH_ON_MERGE / merge-codegen / docker-readme exist (already clean). release-please.yml is main-only
    legacy, removed by the promotion.
13. Action SHAs - VERIFIED: checkout df4cb1c=v6.0.3, setup-dotnet 9a946fd=v5.3.0, nbgv 705dad19=v0.5.2. Canon
    prefers checkout v7 / setup-dotnet v5.4 (optional; dependabot/checkout-7 branch exists - let it land).
    **nbgv must become `@master`, NOT stay SHA-pinned** (documented no-SHA-pin exception; the pin draws
    Dependabot downgrade PRs). Re-verify any SHA the reviewer disputes; never trust Copilot's SHA->version mapping.
14. Prose rules - no em-dashes (sweep of workflows/AGENTS/CODESTYLE = clean today; re-sweep after edits). US English. Terse
    comments, one line <=120, top-of-file workflow summary. Never edit human-authored comments. NOTE: check-ha-version.yml's
    apply step deliberately preserves a U+2014 em-dash in ha-test-versions.json's `$comment` via ensure_ascii=False - that
    em-dash is in a DATA file's content, not prose authored by us; leave the workflow logic, but confirm the comment text we
    author elsewhere stays em-dash-free.

---

## 7. Verification

Static (run all before pushing):
- `actionlint` on every .github/workflows/*.yml.
- `bash -n repo-config/configure.sh`; `shellcheck` it.
- `python3 -m json.tool` parse on each repo-config/*.json + ha-test-versions.json + manifest.json + hacs.json.
- EOL: `for f in repo-config/*.json .editorconfig; do printf '%s ' "$f"; tr -cd '\r' < "$f" | wc -c; done` (expect >0 for
  CRLF JSON, 0 for configure.sh). `grep -rIl $'\r' repo-config/configure.sh` must be empty.
- Em-dash sweep: `grep -rn $'—' .github/ AGENTS.md CODESTYLE.md repo-config/` must be empty (the ha-test-versions.json
  `$comment` em-dash is acceptable data, exclude it).
- markdownlint-cli2 (config present) / cspell if the workspace has a dict - report-only if not gating.
- zip-layout assertion smoke: locally `cd custom_components/purpleair && zip -r /tmp/p.zip . && unzip -Z1 /tmp/p.zip |
  sed 's|^\./||'` and confirm manifest.json + __init__.py at root, no `purpleair/` prefix.

Config audit:
- `REPO=ptr727/HomeAssistant-PurpleAir ./repo-config/configure.sh check` BEFORE apply -> expect drift (rulesets not codified
  / required-check name mismatch). Run `apply`. Re-run `check` -> expect "matches".

Live dispatch verification (after merge):
- `gh workflow run publish-release.yml --ref develop` -> confirm a PRERELEASE GitHub Release with `0.1.<h>-g<sha>` tag and
  a `purpleair.zip` asset whose layout passes the assertion.
- `gh workflow run publish-release.yml --ref main` -> confirm a STABLE `0.1.<h>` release.
- Confirm the schedule leg (or a dispatched no-publish test) retests without creating a release.
- Bump version.json `0.1`->`0.2` to exercise a new minor and confirm NBGV height resets.

---

## 8. Go-live sequence

8.1 Branch from develop: `feature/branch-scoped-cicd-convergence`. Make ALL edits + creates from section 4 there.
8.2 Push -> the new `push:['**']` CI runs on the feature branch (self-testing). Green it (ruff/mypy/pyright/pytest-matrix/
    hassfest/hacs + no-publish build).
8.3 Open PR feature -> develop (squash). Copilot dance: re-request via requestReviews on each head; resolve every thread;
    expect 1-3 rounds; HACS-zip/NBGV-prerelease comments are likely false positives - decline with rationale. Maintainer
    approves explicitly (Merge Gate).
8.4 Required-check lockstep: in the SAME change, `REPO=ptr727/HomeAssistant-PurpleAir ./repo-config/configure.sh apply`
    against the live repo so the ruleset's required check becomes "Check pull request workflow status job" and matches the
    renamed aggregator - otherwise the PR's new check name is not the required one and the PR cannot satisfy the old name.
    Then `check` to confirm "matches". (Order: apply right before/with the merge so the new green check is the required one.)
8.5 Squash-merge to develop. develop CI re-runs (no publish - push trigger removed).
8.6 Branch-backlog cleanup (do this around the promotion, carefully):
    - Confirm each candidate is fully merged or truly abandoned: `git branch -r --merged origin/develop` for the safe set;
      for the NBGV/HACS/sync branches verify their content is on develop (HACS-zip already is via #80/#82; NBGV pipeline is
      live) then `git push origin --delete <branch>`.
    - Delete release-please-- branches, nbgv*, *sync*, ruleset-*, fix-hacs-zip-layout, zip-layout-assertion-prefix-fix,
      chore/seed-*, chore/reseed-*, chore/restore-* after confirming superseded.
    - Leave OPEN dependabot/* branches (they auto-close on merge or get superseded); leave live feature branches the
      maintainer still wants. Get maintainer sign-off on the delete list (open question 9.5) - do not bulk-delete unilaterally.
8.7 Reconcile main: develop is 92 ahead / 0 behind, so a develop->main merge-commit PR is clean. Open develop->main PR,
    "Create a merge commit" (main ruleset is merge-only; develop becomes a real ancestor so the next promotion is clean).
    NO admin bypass. This brings version.json, repo-config, the new workflows, the 0.0.0 placeholder manifest, and DELETES
    release-please.yml from main in one node. Copilot-review the promotion; decline pure-prose nits (would diverge main).
    - Watch the manifest version: main currently 0.3.0, develop 0.0.0. The merge takes develop's 0.0.0 (correct - stamped
      at build). The first STABLE dispatch from main will then publish NBGV `0.1.<height>` (NOT 0.3.x). FLAG to maintainer
      (9.6): the public version series effectively resets from release-please 0.3.0 to NBGV 0.1.<h>. If continuity matters,
      bump version.json base to `0.3` (or higher) BEFORE the first main dispatch so NBGV emits >=0.3.x.
8.8 Apply main ruleset: `configure.sh apply` already wrote both rulesets in 8.4; re-`check` post-promotion to confirm main
    ruleset live.
8.9 Dispatch the publisher from main: `gh workflow run publish-release.yml --ref main`; verify clean release + zip asset +
    layout. Optionally dispatch from develop for a prerelease.
8.10 Confirm develop SURVIVES: repo-wide auto-delete is OFF; the merge-commit promotion + per-merge --delete-branch on bot
     PRs only delete bot branches, never develop. Verify `origin/develop` still exists post-promotion.

---

## 9. Open questions for the maintainer (with recommended defaults)

9.1 Dependabot targeting: converge to dual-target main AND develop (canon, avoids non-linear merge-block) vs keep
    develop-only (current, documented)? DEFAULT: dual-target (match the four live repos; the maintainer already rejected
    single-target elsewhere).
9.2 Release trigger (THE CRUX): adopt dispatch-only publish + schedule-retests-main, dropping the automatic develop-push
    prerelease? DEFAULT: YES - it honors the stated "monitor + retest but do NOT auto push (HACS pull)" requirement and
    restores "merges never publish". Alternative if beta testers must keep auto-prereleases: keep push:[develop] as a
    documented deliberate exception.
9.3 Delete build-datebadge-task.yml + the date-badge job (template cruft)? DEFAULT: YES (vanity "Last Build" badge, no
    release-correctness role). Keep only if the README badge is load-bearing for the maintainer.
9.4 Add markdownlint/cspell validate jobs for canonical doc-lint parity? DEFAULT: only if they already pass clean; do not
    add a new failing gate during migration. Otherwise defer to a follow-up.
9.5 Branch-backlog delete list: approve bulk deletion of the ~40 superseded/abandoned branches (nbgv*, release-please--*,
    *sync*, ruleset-*, hacs-zip-*, chore/seed|reseed|restore-*)? DEFAULT: delete after per-branch superseded-confirmation;
    leave live dependabot/* and wanted feature branches.
9.6 Version continuity: main is at release-please 0.3.0; NBGV base is 0.1, so the first stable dispatch ships 0.1.<height>,
    a version REGRESSION below 0.3.0. Bump version.json base to >=0.3 before the first main dispatch to preserve monotonic
    public versions? DEFAULT: YES, set base to `0.3` (or `0.4`) so HACS users do not see a downgrade.
