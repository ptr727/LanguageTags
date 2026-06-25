# Instructions for AI Coding Agents

**LanguageTags** is a C# .NET library for handling ISO 639-2, ISO 639-3, and RFC 5646 / BCP 47 language tags. The library ships as the NuGet package `ptr727.LanguageTags` and is consumed directly from `main`. The repo also contains a CLI codegen tool (`LanguageTagsCreate/`) that refreshes embedded language data from upstream registries, and an xUnit test project (`LanguageTagsTests/`).

This file is the canonical reference for cross-cutting AI-agent and workflow rules. C# code-style conventions live in [`CODESTYLE.md`](./CODESTYLE.md). Copilot review *mechanics* are owned by [`.github/copilot-instructions.md`](./.github/copilot-instructions.md) - this file delegates them there explicitly (see "PR Review Etiquette" below). High-level summaries in other docs (e.g. README's Contributing section) are allowed when they link back here; don't duplicate the rules themselves. The library's **project-specific conventions and public-API/behavioral contracts** also live here (the [Library API Conventions](#library-api-conventions) section), **not** in `.github/copilot-instructions.md` - that file targets GitHub Copilot / VS Code specifically, while this file is the agent-agnostic one every coding agent reads, so any rule a reviewer must honor has to live here to be provider-independent.

## Git and Commit Rules

- **Default to staging, not committing.** Stage changes with `git add` and leave `git commit` to the developer unless the developer has explicitly authorized the agent to commit for the current ask ("commit this", "open a PR", etc.). Authorization is scope-bound - it covers the commits needed for that specific task, not a blanket commit license for the rest of the session.
- **All commits must be cryptographically signed (SSH or GPG).** Branch protection enforces this on both branches; unsigned commits are rejected on push. Signing depends on environment configuration - `git config commit.gpgsign true`, a configured `user.signingkey`, and a working signing agent (loaded `ssh-agent` for SSH, or `gpg-agent` for GPG). If signing is not configured in the environment, **do not commit** - surface the missing config to the developer and stop at `git add`. Verify before any agent-authored commit (`git config --get commit.gpgsign && ssh-add -L` or the GPG equivalent). **Signing must be live before the *first* commit, not retrofitted.** Turning on `Require signed commits` against a branch that already has unsigned commits forces a rewrite of that entire history to re-sign it - changing every commit SHA and making whoever does the rewrite the committer and signer of every commit (a rebase preserves the `author` field but not the original signatures; you cannot sign another contributor's commits for them). During new-repo setup, never create commits until signing is verified.
- **Never force push.** Do not run `git push --force` or `git push --force-with-lease` under any circumstances. Force pushing rewrites shared history and can cause data loss.
- **Never run destructive git commands** (`git reset --hard`, `git checkout .`, `git restore .`, `git clean -f`) without explicit developer instruction.

## Branching Model

- `develop` is the integration branch. Feature branches -> `develop` is **squash-only**; develop is kept linear.
- `develop` -> `main` is **merge-commit only** (no squash, no rebase). Merge commits preserve develop's commit list as a real second-parent reference on main, which lets the release model attribute releases to the develop commits that produced them (relevant both for the weekly publish and the opt-in `PUBLISH_ON_MERGE` mode - see "Release Model" below). Branch protection enforces this: the develop ruleset allows only `squash`, the main ruleset allows only `merge`.
- All commits on both branches must be cryptographically signed (SSH or GPG). Squash and merge commits created via the GitHub UI are signed by GitHub's web-flow key.
- **`develop` is forward-only - no `main -> develop` back-merges.** The develop ruleset's squash-only setting physically blocks merge commits on develop. Historical back-merge commits visible in `git log` predate this rule and must not be repeated.
- **Both rulesets intentionally omit "Require branches to be up to date before merging" (`strict_required_status_checks_policy: false`), for two distinct reasons:**
  - *Main* - the check is graph-based; it asks whether main's tip commit is reachable from develop, not whether the two branches have the same content. After any develop -> main release, main's tip is a brand-new merge commit that develop's history doesn't contain. Forward-only develop never adds it (no back-merge of main into develop), so the check would fail on every subsequent release.
  - *Develop* - bot auto-merge incompatibility. When two bot PRs against develop land in the same minute (e.g. two grouped Dependabot PRs from the same daily run), the first to merge pushes the second into `mergeStateStatus: BEHIND`. GitHub's auto-merge will not fire while the strict flag is on, and nothing in the workflow set auto-updates a bot branch in that window - the merge-bot only *enables* auto-merge on `opened`/`reopened` (see [`merge-bot-pull-request.yml`](./.github/workflows/merge-bot-pull-request.yml)). Real file-level conflicts are still caught textually (`mergeable: CONFLICTING` blocks merge regardless); semantic-but-not-textual conflicts that combine cleanly are caught by the post-merge develop CI run rather than pre-merge. Do not reintroduce the strict flag on develop thinking it's hygiene - it breaks bot auto-merge.
- **Bots (Dependabot and codegen) target both `main` and `develop` in parallel.** [`.github/dependabot.yml`](./.github/dependabot.yml) duplicates every ecosystem entry (one per branch) and [`.github/workflows/run-codegen-pull-request-task.yml`](./.github/workflows/run-codegen-pull-request-task.yml) runs as a matrix over both branches with branch names `codegen-main` and `codegen-develop`. Each branch absorbs its own bot PRs independently, so neither falls behind, and the forward-only rule still holds (nothing is back-merged from main to develop - both branches receive their updates directly). Parallel auto-merge across same-batch bot PRs is race-proof only because both rulesets have the strict "up to date" flag off (see bullet above). The merge-bot ([`.github/workflows/merge-bot-pull-request.yml`](./.github/workflows/merge-bot-pull-request.yml)) dispatches `--squash` or `--merge` from each PR's base ref via a `case` statement so the form matches the ruleset on either base. Dependabot **security** PRs (CVE-driven) always open against the repo default branch (`main`) regardless of `target-branch` - the same `case` statement covers them.
- **Maintainer-pushed commits on a bot PR auto-disable auto-merge.** The merge-bot's `merge-dependabot` and `merge-codegen` jobs only fire on `opened` / `reopened` events (auto-merge is enabled exactly once per PR). When a maintainer pushes commits to a bot's branch (a `synchronize` event with an actor that isn't the same bot), the merge-bot's `disable-auto-merge-on-maintainer-push` job fires and calls `gh pr merge --disable-auto`. The maintainer's commits stay in the PR but won't auto-merge with the bot's content; re-enable auto-merge manually (`gh pr merge --auto <PR>` or the GitHub UI) when ready.
- **Why parallel dual-target rather than develop-only with eventual flow-through:** consumers (NuGet.org, GitHub releases) pull from `main` directly. A develop-only model would leave `main` running stale code during long-running develop features. Codegen content here is the embedded ISO 639-2/3 + RFC 5646 language data - production-critical - so both branches need fresh codegen on their own cadence (codegen PRs are opened **daily**; the actual release is **published weekly** - see "Release Model" below).
- **Codegen regenerates committed files; its output must be deterministic from its inputs, never per-run state.** The codegen workflow refreshes files checked into the repo: it runs a matrix over `main` and `develop`, each leg regenerating against its own checkout and opening its own PR (`codegen-main -> main`, `codegen-develop -> develop`). For the two legs not to conflict on `develop -> main`, the generated output must depend only on its inputs - never on per-invocation state (timestamps, GUIDs, build IDs), which would diverge every run and conflict on every release. [`LanguageTagsCreate/`](./LanguageTagsCreate/) downloads the ISO 639-2/3 + RFC 5646 registries from their official sources and emits the embedded [`LanguageData/`](./LanguageData/) files, so each leg's output is a pure function of those registries and the two branches stay conflict-free.

## Release Model

This repo uses a **two-phase model by default**: PRs build fast, publishing is batched weekly. The load-bearing rules:

- **PRs smoke-test only.** [`test-pull-request.yml`](./.github/workflows/test-pull-request.yml) always runs unit tests, then a `dorny/paths-filter` `changes` job gates a **reduced, never-published** library build (`smoke: true`) that runs only when the library actually changed. Build-workflow files are intentionally not in the path filter - a filter can't tell a logic change from an action-version bump - so a workflow-only change isn't smoke-built; the reusable workflows are exercised by the next run that uses them. There is no CI workflow-lint job; lint workflow edits with `actionlint` locally before pushing.
- **Merges don't publish by default.** [`publish-release.yml`](./.github/workflows/publish-release.yml) is the sole publisher: its **weekly schedule** (Mondays 02:00 UTC) and **manual `workflow_dispatch`** always do the full build/publish of **both** `main` and `develop` (a branch matrix). Its `push` trigger publishes only when the **`PUBLISH_ON_MERGE` repository variable** is `true` (opt-in legacy continuous-release). Unset/`false` = two-phase. Codegen runs **daily** ([`run-periodic-codegen-pull-request.yml`](./.github/workflows/run-periodic-codegen-pull-request.yml), 04:00 UTC), staggered after the weekly publish; Dependabot also runs daily - both only smoke-test on merge.
- **Required check.** The `changes` job is in the `Check pull request workflow status` aggregator's `needs` and **must succeed** (not just "not fail") - a paths-filter error must never let a library-changing PR merge with its smoke build silently skipped. Skipped smoke jobs (no matching change) pass; `failure`/`cancelled` blocks.
- **Reusable-task parameter contract.** [`build-release-task.yml`](./.github/workflows/build-release-task.yml) and [`build-nugetlibrary-task.yml`](./.github/workflows/build-nugetlibrary-task.yml) take `ref` (git ref to check out/version), `branch` (logical branch driving config/tags/prerelease - `main` => Release/non-prerelease, else Debug/prerelease), and where relevant `smoke`. **Branch-derived config keys off `inputs.branch`, never `github.ref_name`** - the publisher's matrix builds `develop` from a run whose `github.ref_name` is `main`, so `ref_name` would be wrong. Artifact names are branch-suffixed so both matrix legs coexist in one run. [`get-version-task.yml`](./.github/workflows/get-version-task.yml) takes a `ref` so NBGV versions the right branch, and exposes `GitCommitId` so the release tag and built artifacts pin to the exact built commit.

- **Versioning is semantic and maintainer-controlled.** The `version` (major.minor) in [`version.json`](./version.json) is the version floor; NBGV appends the git height (the SemVer patch position). `main` builds a stable `X.Y.<height>`; `develop` builds a prerelease `X.Y.<height>-g<sha>`. The maintainer edits `version.json`; dependency bumps, codegen refreshes, CI/workflow fixes, doc edits, and template re-syncs leave it untouched.
  - **Bump `version.json` only for functional changes, by maintainer instruction.** Raise the major/minor when the work warrants a new semantic version - a new feature, a behavior or API change, a breaking change - in the PR that introduces it (typically on `develop`). Do not bump on a fixed cadence or mechanically after a release.
  - **No post-release bump; no develop-ahead requirement.** NBGV advances the patch (git height) on every commit, so a release always gets a fresh build version with no `version.json` edit and there is no `bump-version-X.Y` PR after a release. A `develop -> main` promotion carries whatever `version.json` is current: a promotion with a functional bump releases that new version on `main`; a maintenance-only promotion (dependency/codegen bumps, CI/doc fixes, template re-syncs) carries the unchanged `version.json` and `main` advances only its NBGV height.

## Pull Request Title and Commit Message Conventions

### Format

- Imperative subject summarizing the change, <=72 characters, no trailing period. ("Add 24-hour PM2.5 average sensor", not "Added X" or "Adds X".)
- Optional body, blank-line separated, explaining *why* the change is being made when that's non-obvious. The diff shows *what*.

### Rules

- Don't write `update stuff`, `wip`, or other vague titles. (Dependabot's default `Bump X from Y to Z` titles are fine - keep them.)
- Don't add `Co-Authored-By:` lines unless the developer explicitly asks.
- Don't put release-bump magnitude in the title - no "minor", "patch", "release v0.2.0", etc. Nerdbank.GitVersioning computes the next release version from `version.json` + git history. Dependency versions in dependency-bump titles are fine and expected.
- Use US English spelling and match the existing heading style of the file you're editing: title case with lowercase short bind words (a, an, the, and, but, or, of, in, on, at, to, by, for, from); hyphenated compounds capitalize both parts unless the second is a short preposition (*Built-in*, *EPA-Corrected*, *24-Hour*).

### Examples

```text
Add structured logging extensions to LanguageTag
Pin softprops/action-gh-release to commit SHA
Refresh ISO 639-3 data table from SIL
Bump xunit.v3 from 3.2.2 to 3.3.0
Clarify LanguageTagBuilder usage in README
```

## Documentation Style Conventions

### Markdown

- Use reference-style links for any URL referenced more than once or appearing in lists; alphabetize the reference definitions block.
- Inline single-use relative links (e.g. `[CODESTYLE.md](./CODESTYLE.md)`) are fine.
- One logical paragraph per line; no hard-wrap line-length limit. For an intentional hard line break within a block - stacked badges, status, or license lines - end the line with a trailing backslash (`\`); this explicit form is preferred over trailing whitespace and is not treated as a paragraph split.
- Headings follow the title-case-with-short-bind-words rule from the PR-title section.

### Comments

Applies to code and workflow (`#`) comments alike.

- Comment only when the code does not explain itself or the logic is genuinely complex. Self-evident code needs no comment.
- Write for the human reading *this* project's code now: state what the code does and only the non-obvious *why*. No cross-project references (do not name other repos), no historic or design narrative, no rule citations - governance lives in this file, not echoed inline.
- Match the surrounding code's line length (typically ~120), not an 80-column wrap.

### Line Endings

- [`.editorconfig`](./.editorconfig) defines the correct ending per file type (CRLF for `.md`, `.cs`, XML/`.csproj`/`.props`, `.yml`/`.yaml`, `.json`, `.cmd`/`.bat`/`.ps1`; LF for `.sh`), and [`.gitattributes`](./.gitattributes) (`* -text`) stops git from normalizing. The defaults + per-extension EOL block is always-verbatim from the template; the `[*.cs]`/ReSharper style block is .NET-only and is carried because this repo ships .NET.
- **Editing an existing file: preserve its current line endings** - do not reflow them as a side effect of a content change, even if the file is already non-compliant. After any programmatic edit, verify with `git diff --stat` (only changed lines) and `file <path>` (expected ending). Bring a non-compliant file to its `.editorconfig` ending only as a deliberate, isolated EOL-only change.

### Quantitative Claims

- Any quantitative claim in `README.md` (counts, sizes, version floors, supported platforms) must be verified against current code. If a doc number is derived from a code constant, mark the dependency in a source-code comment so the next editor knows to update both.

## PR Review Etiquette

> **Mandatory in every derived repo.** This entire "PR Review Etiquette" section is the provider-agnostic review-loop *contract* and must be carried **verbatim** into every repo derived from this template, alongside the [`.github/copilot-instructions.md`](./.github/copilot-instructions.md) "GitHub Copilot Review Runbook" that implements it. Without both in-repo, an agent working in the derived repo has no pointer to the reliable Copilot mechanics and falls back to ad-hoc (and known-broken) behavior.

The repo runs a review loop on every PR: local agent iteration plus remote automated review (GitHub Copilot is the configured reviewer). Treat this as a contract regardless of which local agent authored the changes.

### Merge Gate (read this first)

**Do not merge - and do not enable auto-merge - unless ALL of these hold:**

1. Required status checks are green (`mergeStateStatus: CLEAN`), **and**
2. A Copilot review is confirmed on the **current head SHA** (not an earlier push), **and**
3. **Every** Copilot finding on that head SHA is closed out - all review threads resolved, **and** any issue-level Copilot comments (which have no resolve action) triaged and replied to - so zero outstanding findings remain, **and**
4. The maintainer has given **explicit** permission to merge.

`mergeStateStatus: CLEAN` reflects **only** required statuses - it never reflects open bot review comments, so `CLEAN` alone is **never** sufficient to merge. A green/`CLEAN` PR with an unresolved Copilot finding fails this gate; treat it as "not mergeable" no matter what the merge-state field says. The agent never merges on its own (consistent with "default to staging"; merging is maintainer-authorized).

**Merging is not releasing.** A merge to a release branch does **not** by itself publish; publishing is a separate step in the repo's release pipeline (a scheduled run or a manual dispatch), not an automatic consequence of merging. Never describe a merge as cutting a release, and never trigger a publish without explicit maintainer instruction.

### Expected Review Loop

1. Push changes to the PR branch.
2. Re-request a review for the **current head SHA**. Auto-trigger is unreliable, so request it explicitly via the `requestReviews` GraphQL mutation (now reliable end-to-end - see the runbook); the UI is only a fallback.
3. Wait for review activity on that head. A completed review that raises **no findings** is a valid terminal outcome for that head - proceed; do not re-trigger it or treat the absence of comments as a missing review.
4. Triage findings.
5. Apply fixes or write a rationale for declines.
6. Reply to each thread and resolve what was addressed.
7. Re-run the loop after every fix push until no actionable findings remain.

Drive the loop to green - review confirmed on the latest head SHA and every actionable finding closed - then stop and apply the **Merge Gate** above: all four preconditions must hold, and `mergeStateStatus: CLEAN` alone never satisfies it.

For provider-specific mechanics (how to request review, query review state, post replies, resolve threads), see the **GitHub Copilot Review Runbook** in [.github/copilot-instructions.md](./.github/copilot-instructions.md). This file owns the contract; that file owns the mechanics.

### Triaging Review Comments

For each comment, classify before responding:

- **Bug** - wrong behavior, missing test coverage, or a real divergence between code and docs. Fix it. Reply with the fixing commit SHA when done.
- **Style/convention** - the comment cites a rule from this file or a language-specific style guide. Two cases:
  - The cited rule matches what the existing codebase already does -> fix the offending code.
  - The cited rule contradicts what's in the tree, or industry norm -> **update the rule instead of the code**. The rule is wrong, not the code. Bouncing the same code across rounds is the symptom of a wrong rule. Heuristic: three rounds on the same style category means the rule needs adjusting and the user should authorize the rule change.
- **Architectural opinion** - the comment proposes a different design ("constrain this to disabled-by-default", "move it elsewhere", "add a runtime guardrail"). This is judgment, not a bug. Surface it to the user with a recommendation; don't apply unilaterally.

### Responding and Resolution Expectations

Reply inline with either the fixing commit SHA (for accepted issues) or a concise rationale (for declines). Resolve review threads when addressed or intentionally declined with rationale. Issue-level comments (those at `repos/.../issues/<N>/comments` rather than tied to a specific line) have no resolution action - acknowledge with a reply if needed and move on.

After the final push on a PR, sweep older threads from earlier rounds whose code paths no longer exist; otherwise stale unresolved markers remain in the review UI.

### Escalating to the User

Bring the user in when:

- **Genuine design trade-off** surfaces (fail-open vs fail-closed, narrow vs broad refactor scope, "should we add a guardrail or trust the docstring"). Triage, recommend, ask.
- **Repeated friction** across rounds without convergence - that's the rule-needs-updating signal. Stop, summarize the pattern, and let the user authorize the rule change.
- **Architectural redesign** is requested rather than a bug fix. Surface with a recommendation; never apply unilaterally.

Anti-pattern: don't keep flipping the code on the same style point. Flip the rule once and stick to the rule.

## Staying in Sync with the Template

This repo is derived from [`ptr727/ProjectTemplate`](https://github.com/ptr727/ProjectTemplate) and re-syncs against it periodically, not just at creation.

- **Verbatim carries.** Pull the current template version of each shared artifact and re-apply it, adapting only this repo's placeholders: [`.github/copilot-instructions.md`](./.github/copilot-instructions.md) (the Copilot review runbook - change only the `<owner>`/`<repo>`/`<N>` values in its API snippets), [`.markdownlint-cli2.jsonc`](./.markdownlint-cli2.jsonc), [`.editorconfig`](./.editorconfig), [`.gitattributes`](./.gitattributes), and this file's [PR Review Etiquette](#pr-review-etiquette) section. Carry [`.editorconfig`](./.editorconfig) **whole** - the EOL/per-extension block and the `[*.cs]`/ReSharper block both, even sections for languages this repo doesn't ship (an inert block costs nothing and keeps re-sync a clean overwrite). Keep `copilot-instructions.md` **narrow** (provider mechanics plus the commit/PR-title summary); project-specific conventions and API contracts live in this file (see [Library API Conventions](#library-api-conventions)), not there - non-Copilot agents are not directed to that file.
- **CODESTYLE.md.** Carry the **whole file verbatim** from the template, every language section included - the Python section is inert in this .NET-only repo but costs nothing and keeps re-sync a clean wholesale overwrite rather than a per-section merge. Repo-root placement is load-bearing - `AGENTS.md` and `.github/copilot-instructions.md` link it by relative path. Adapt the in-section repo-specific bits: the .NET project-folder list, the `InternalsVisibleTo` project names, and the VS Code task labels.
- **.vscode/tasks.json.** Carry the named **clean-compile** task definitions verbatim - `.NET Build`, `CSharpier Format`, and `.NET Format` (which chains the first two then `dotnet format style --verify-no-changes`). Their names are owned by the `CODESTYLE.md` ".NET" section and their command sequence + arguments are the canonical clean-compile spec; don't loosen them. Convenience tasks (`.NET Tool Update`, `.NET Outdated Upgrade`, `Husky.Net Run`) are the adapt zone.
- **Release notes.** Keep a short release-notes summary in [`README.md`](./README.md) and the full history in [`HISTORY.md`](./HISTORY.md); update both when cutting a release.
- **Report drift upstream.** When a re-sync surfaces a template gap, an outdated instruction, or something that bit this repo and would bite the next derived repo, open an issue in [`ptr727/ProjectTemplate`](https://github.com/ptr727/ProjectTemplate) rather than only patching locally - the template is the single source of truth, and this upstream-issue rule is this repo's only cross-repo obligation. Do not maintain or reference a "known downstream" registry, and do not name sibling repositories in docs, comments, or workflows - that registry and the maintainer fan-out duty live in the template hub only.

### Template adaptations

Intentional, documented deviations from the carried template state. Everything not listed here tracks the template verbatim.

- **Husky.Net pre-commit gate.** This repo wires the clean-compile checks as local Husky.Net pre-commit git hooks (installed via `dotnet tool restore` + `dotnet husky install`); the `Husky.Net Run` VS Code task runs them manually. The template ships no git hooks by default and treats CI as the only lint backstop, so [`CODESTYLE.md`](./CODESTYLE.md)'s git-hook note and the `.vscode/tasks.json` convenience-task set are adapted accordingly. CI still runs the same checks as a backstop.
- **Codegen uses the `LanguageTagsCreate` CLI, no `NINJA_API_KEY`.** Embedded language data is regenerated by the in-repo [`LanguageTagsCreate/`](./LanguageTagsCreate/) tool pulling directly from the official ISO 639-2/3 + RFC 5646 registries. There is no external codegen API, so this repo carries no `NINJA_API_KEY` secret or any reference to one.
- **`merge-upstream-version` job legitimately absent.** [`merge-bot-pull-request.yml`](./.github/workflows/merge-bot-pull-request.yml) carries only `merge-dependabot`, `merge-codegen`, and `disable-auto-merge-on-maintainer-push`. The template's `merge-upstream-version` job auto-merges an upstream-version-bump PR flow this repo does not run (LanguageTags pins no upstream binary version), so that job is intentionally not carried. The concurrency keying and the three carried jobs match the template verbatim.

## Workflow YAML Conventions

These conventions describe the target state. New and modified workflows must respect them; the rest of the repo is expected to be brought up to the same standard.

- **Action pinning**: pin **every** action - first-party (`actions/*`) and third-party - to a commit SHA with a trailing `# vX.Y.Z` comment, so Dependabot can still bump it but a tag swap can't change the executed code. Use `# vX` (major-only) only when the upstream's floating major tag doesn't correspond to a specific patch/minor release SHA - pinning to the floating-tag SHA still gives the SHA guarantee, the version comment just records the major line. Documented exception (no SHA pin at all): [`dotnet/nbgv`](./.github/workflows/get-version-task.yml) is consumed via `@master` because the upstream tag stream lags `master` substantially and Dependabot's tag-tracking would propose a downgrade.
- **Filename**: reusable workflows (those with `on: workflow_call`) end in `-task.yml`. Entry-point workflows (`on: push` / `pull_request` / `schedule` / `workflow_dispatch`) do NOT use the `-task` suffix; they end with what they do - `-pull-request.yml`, `-release.yml`, etc. The suffix carries semantic meaning: a `-task.yml` file is meant to be `uses:`-d, never triggered directly.
- **Workflow `name:`** (the top-level `name:` field): reusable workflow names end in **"task"** (e.g. `Build library task`); entry-point workflow names end in **"action"** (e.g. `Publish project release action`, `Test pull request action`). The displayed action name in the GitHub Actions UI tells you at a glance whether you're looking at an orchestrator or a callee.
- **Job and step `name:` suffixes**: every job's `name:` ends in **"job"**; every step's `name:` ends in **"step"**. **Exception**: a job whose `name:` is also referenced as a required-status-check `context:` in a branch ruleset (currently `Check pull request workflow status` in `test-pull-request.yml`) keeps the ruleset-bound name verbatim - renaming would silently break required-status-check enforcement. Do not "fix" that name; if a future job becomes ruleset-bound, mark it the same way.
- **Concurrency**: top-level workflows declare `concurrency: { group: '${{ github.workflow }}-${{ github.ref }}', cancel-in-progress: true }` so a fresh push supersedes an in-flight run on the same ref. **Documented exceptions** (both record the rationale inline in their header comment): (1) [`merge-bot-pull-request.yml`](./.github/workflows/merge-bot-pull-request.yml) uses `cancel-in-progress: false` because its three-job model (enable-auto-merge on opened, disable-auto-merge on maintainer-pushed synchronize, with method dispatched by base) requires each event to run to completion in arrival order - cancellation would leave auto-merge in an inconsistent state. (2) [`publish-release.yml`](./.github/workflows/publish-release.yml) uses both a **global, ref-independent group** for real publishes (`group: ${{ github.workflow }}`, dropping the usual `-${{ github.ref }}`) and `cancel-in-progress: false`. Its schedule/dispatch runs publish both branches regardless of the triggering ref, so a ref-scoped group would let a scheduled run (ref `main`) and a manual dispatch (ref `develop`) run concurrently and double-publish; and cancelling a publish mid-flight can leave a half-created GitHub release. Non-publishing (two-phase default) `push` runs get a unique per-run group so they never queue behind a real publish.
- **Shells**: multi-line `run:` blocks with bash start with `set -euo pipefail` - fail fast, fail on undefined vars, fail on a failed pipe segment.
- **Conditionals**: multi-line `if:` uses folded scalar `if: >-` so YAML preserves whitespace correctly. Literal block (`if: |`) is wrong because it embeds newlines inside the boolean expression.
- **Boolean inputs**: workflows triggered both via `workflow_call` and `workflow_dispatch` must declare each boolean input in *both* trigger blocks - one definition does not propagate to the other. `workflow_call` delivers booleans as actual booleans; `workflow_dispatch` delivers them as the *strings* `"true"`/`"false"`. Any `if:` consuming a boolean input must compare against both forms - `if: ${{ inputs.foo == true || inputs.foo == 'true' }}`.
- **Reusable workflows**: job-level `permissions:` are validated *before* the `if:` evaluates, so even a skipped job needs valid permissions declared. A `release` job with `permissions: contents: write` and `if: ${{ inputs.publish }}` will still cause `startup_failure` on a caller that doesn't grant `contents: write`. Either declare permissions at the call site, or omit the inner block and inherit.
- **Allowlist `success` and `skipped` explicitly** when chaining jobs across optional dependencies - `!= 'failure'` lets `cancelled` through (timeout, runner failure, manual cancel). Use `(needs.X.result == 'success' || needs.X.result == 'skipped')`.
- **Artifact handoff and cleanup**: a build job contributes files to the GitHub release by uploading an artifact named `release-asset-<branch>-<target>`; the verbatim `github-release` job collects every `release-asset-<branch>-*` by `pattern:` + `merge-multiple:` and never names a build job. **This name-pattern handoff is canonical even for this single-target repo** - do not switch to an `artifact-id` output plus `download-artifact` `artifact-ids:`, which looks tidier for 1:1 but forks the `github-release` download and breaks its verbatim carry. Artifacts are an intra-run handoff (durable copies live on the GitHub release, not in workflow artifacts), so every artifact-producing workflow ends with a terminal `cleanup-artifacts` job that deletes the run's artifacts via the REST API - `permissions: actions: write`, an `if:` that includes `always()`, `continue-on-error: true` on the delete step, kept out of any required status check so housekeeping never gates a merge; both [`publish-release.yml`](./.github/workflows/publish-release.yml) and [`test-pull-request.yml`](./.github/workflows/test-pull-request.yml) carry one. Set `retention-days: 1` on explicit uploads as a backstop.
- **Tag pinning on releases**: when using `softprops/action-gh-release` (or any tag-creating action), pass `target_commitish` explicitly - without it, GitHub's REST API defaults the new tag to the repository's default branch instead of the commit that built the artifact. Pin it to the **exact built commit's SHA** (the publisher uses NBGV's `GitCommitId` output), not `github.sha` (wrong branch in the publisher's branch matrix - a `develop` leg runs with `github.sha` = main's tip) and not a branch name (a moving ref that a mid-run commit could advance past the built tree).

## Project Structure

- **LanguageTags** (`LanguageTags/LanguageTags.csproj`)
  - Core library project, published as NuGet `ptr727.LanguageTags`
  - Target framework: .NET 10.0, AOT compatible (`<IsAotCompatible>true</IsAotCompatible>`)
- **LanguageTagsCreate** (`LanguageTagsCreate/LanguageTagsCreate.csproj`)
  - CLI codegen tool. Downloads ISO 639-2/3 + RFC 5646 / BCP 47 data from official sources (Library of Congress, SIL, IANA), converts to JSON, and generates C# data files. Invoked by [`.github/workflows/run-codegen-pull-request-task.yml`](./.github/workflows/run-codegen-pull-request-task.yml).
- **LanguageTagsTests** (`LanguageTagsTests/LanguageTagsTests.csproj`)
  - xUnit v3 test suite. Assertions via AwesomeAssertions.
- **`LanguageData/`** - embedded ISO/RFC data files refreshed by the codegen tool.
- **Build configuration**:
  - Common MSBuild properties (`TargetFramework`, `Nullable`, `ImplicitUsings`, `AnalysisLevel`, etc.) live in `Directory.Build.props` at the solution root. Do not duplicate these in individual `.csproj` files - only add a property to a `.csproj` when it is project-specific or overrides the shared default.
  - All NuGet package versions are centralised in `Directory.Packages.props`. `PackageReference` elements in `.csproj` files must not include a `Version` attribute. Asset metadata (`PrivateAssets`, `IncludeAssets`) stays in the `.csproj` `PackageReference` element.
- **Style guide**: [`CODESTYLE.md`](./CODESTYLE.md) for C# code conventions; [`.github/copilot-instructions.md`](./.github/copilot-instructions.md) for the Copilot review runbook.

## Key Public API

- `LanguageTag` - main entry point for parse/build/normalize/validate operations.
- `LanguageTagBuilder` - fluent builder for constructing tags.
- `LanguageLookup` - language code conversion and matching (IETF <-> ISO).
- `Iso6392Data`, `Iso6393Data`, `Rfc5646Data` - language data records (`Create()`, `FromDataAsync()`, `FromJsonAsync()`).
- `ExtensionTag`, `PrivateUseTag` - sealed records for extension and private-use subtags.
- `LogOptions` - static class for configuring library-wide logging via `ILoggerFactory`.

Internal: `LanguageTagParser` - use `LanguageTag.Parse()` instead.

## Library API Conventions

Contract rules for the public API; honor them when changing or reviewing library code.

- **Construction is factory-only.** Build tags with the static factory methods (`Parse`, `TryParse`, `ParseOrDefault`, `ParseAndNormalize`, `FromLanguage`/`FromLanguageRegion`/`FromLanguageScriptRegion`, `CreateBuilder`) or the fluent `LanguageTagBuilder`. Constructors are internal - do not expose them.
- **Tags are immutable.** Properties have internal setters and collections are exposed as `ImmutableArray<T>`; once constructed a tag does not change. `Normalize()` returns a new copy, it does not mutate in place.
- **Parse, validate, and normalize are distinct.** `Parse` returns null on failure; prefer `TryParse` or `ParseOrDefault` (falls back to `und`) for safe parsing. `Normalize()` does **not** validate - call `Validate()` separately when validity matters. `LanguageTagParser` is internal; all parsing goes through `LanguageTag`'s static methods.
- **Normalization casing follows RFC 5646.** Language, extended-language, variant, extension, and private-use subtags lowercase; script Title case; region UPPERCASE.
- **Tag semantics.** Grandfathered tags are auto-converted to their preferred values during parsing; all tag comparisons are case-insensitive; private-use tags use the `x-` prefix; extensions use single-character prefixes (except `x`, reserved for private use).
- **Accuracy caveat.** The parsing/normalization logic may be incomplete or inaccurate per RFC 5646; verify results for the specific use case, and add a test when fixing a discrepancy.
