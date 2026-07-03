# Instructions for AI Coding Agents

**LanguageTags** is a C# .NET library for handling ISO 639-2, ISO 639-3, and RFC 5646 / BCP 47 language tags. The library ships as the NuGet package `ptr727.LanguageTags` and is consumed directly from `main`. The repo also contains a CLI codegen tool (`LanguageTagsCreate/`) that refreshes embedded language data from upstream registries, and an xUnit test project (`LanguageTagsTests/`).

This file is the canonical reference for cross-cutting AI-agent rules. The CI/CD workflow contract and conventions live in [`WORKFLOW.md`](./WORKFLOW.md); C# code-style conventions live in [`CODESTYLE.md`](./CODESTYLE.md). Copilot review *mechanics* are owned by [`.github/copilot-instructions.md`](./.github/copilot-instructions.md) - this file delegates them there explicitly (see "PR Review Etiquette" below). High-level summaries in other docs (e.g. README's Contributing section) are allowed when they link back here; don't duplicate the rules themselves. The library's **project-specific conventions and public-API/behavioral contracts** also live here (the [Library API Conventions](#library-api-conventions) section), **not** in `.github/copilot-instructions.md` - that file targets GitHub Copilot / VS Code specifically, while this file is the agent-agnostic one every coding agent reads, so any rule a reviewer must honor has to live here to be provider-independent.

## Git and Commit Rules

- **Default to staging, not committing.** Stage changes with `git add` and leave `git commit` to the developer unless the developer has explicitly authorized the agent to commit for the current ask ("commit this", "open a PR", etc.). Authorization is scope-bound - it covers the commits needed for that specific task, not a blanket commit license for the rest of the session.
- **All commits must be cryptographically signed (SSH or GPG).** Branch protection enforces this on both branches; unsigned commits are rejected on push. Signing depends on environment configuration - `git config commit.gpgsign true`, a configured `user.signingkey`, and a working signing agent (loaded `ssh-agent` for SSH, or `gpg-agent` for GPG). If signing is not configured in the environment, **do not commit** - surface the missing config to the developer and stop at `git add`. Verify before any agent-authored commit (`git config --get commit.gpgsign && ssh-add -L` or the GPG equivalent). **Signing must be live before the *first* commit, not retrofitted.** Turning on `Require signed commits` against a branch that already has unsigned commits forces a rewrite of that entire history to re-sign it - changing every commit SHA and making whoever does the rewrite the committer and signer of every commit (a rebase preserves the `author` field but not the original signatures; you cannot sign another contributor's commits for them). During new-repo setup, never create commits until signing is verified.
- **Never force push.** Do not run `git push --force` or `git push --force-with-lease` under any circumstances. Force pushing rewrites shared history and can cause data loss.
- **Never run destructive git commands** (`git reset --hard`, `git checkout .`, `git restore .`, `git clean -f`) without explicit developer instruction.

## Branching Model

This is the developer-facing git policy. The branch rulesets that enforce it (merge methods, required check, strict-status settings, and the reasons), and the merge-bot and codegen workflow behavior, are specified in [`WORKFLOW.md`](./WORKFLOW.md) (rulesets in section 6, bots in D8) and codified in [`repo-config/`](./repo-config/). Do not restate them here.

- `develop` is the integration branch. Feature branches merge to `develop` **squash-only**, keeping develop linear. `develop` merges to `main` **merge-commit only** (no squash, no rebase), so `main` keeps a real reference to the develop commits a release came from.
- **`develop` is forward-only**: no `main -> develop` back-merges. Historical back-merge commits in `git log` predate this rule and must not be repeated.
- All commits on both branches are cryptographically signed (see Git and Commit Rules). Squash and merge commits created in the GitHub UI are signed by GitHub's web-flow key.
- **Bots target both `main` and `develop` directly.** Dependabot and codegen open PRs against each branch independently. This is deliberate: running a bot on one branch and merging its changes across to the other causes endless conflicts as the feature -> develop -> main flow moves underneath it, whereas landing the same dependency or data update directly in each branch keeps bot changes conflict-free regardless of what else is in flight, and keeps the `main` package fresh without waiting on a promotion. Dependabot security PRs open against `main`. The mechanics (Dependabot's per-target-branch config, codegen's per-branch matrix) are in [`WORKFLOW.md`](./WORKFLOW.md) D8.
- **Mirror to `develop` any change that lands on `main` outside the feature -> develop -> main flow.** "Mirror" means land the same fix directly on `develop` via a follow-up PR targeting `develop` - never a `main -> develop` back-merge, which the forward-only rule forbids. A reconciliation-branch fix made to resolve a `develop -> main` promotion conflict, or a security PR that merges only to `main`, leaves `develop` behind on that content - and forward-only `develop` never back-merges to catch up (the same parallel-target principle as the bots). Before basing new work on `develop`, or diagnosing a defect from it, compare content and not commit history: run `git diff origin/main origin/develop` and inspect its `-` lines - the `main`-side of each difference, to check for staleness. A `-`/`+` pair within one hunk is usually just `develop` modifying that code as normal unpromoted work (occasionally `develop` is reworking a `main`-side fix differently - worth a glance). The stronger staleness signal is a deletion-only hunk (`-` lines, no `+` lines): content on `main` that `develop` lacks entirely, i.e. a `main`-only fix `develop` never received, so the defect may already be fixed on `main`. Prefer this over a commit-log check like `git log origin/develop..origin/main`, which is noisy here because it also lists routine promotion merges and the `main`-direct bot commits whose content `develop` already carries via its own parallel bot PRs.
- **Put issue-closing keywords (`Closes #N`) where they fire on merge to the default branch (`main`).** GitHub closes an issue from a *PR description* only when that PR merges to `main`, so a `Closes #N` in a PR that targets `develop` never fires - put it in the `develop -> main` promotion PR instead. A closing keyword in a *commit message* does close the issue once that commit reaches `main` via promotion, but that is fragile across squash-merges, so prefer the promotion PR's description or close the issue manually once the fix lands on `main`.

## Release Model

The release and publish behavior - branch-scoped versioning (`main` = stable, `develop` = prerelease), the self-sufficient publish model (each shipped change auto-publishes; a maintainer dispatches to force a release), the pull-request smoke gate, and Dependabot/codegen auto-merge - is specified in [`WORKFLOW.md`](./WORKFLOW.md), the canonical CI/CD guide. Do not duplicate those rules here.

Versioning is the one release rule that is a **human process**, not a workflow outcome, so it lives here ([`WORKFLOW.md`](./WORKFLOW.md) D3.3 defers to this):

- The `version` (major.minor) in [`version.json`](./version.json) is the version floor; NBGV appends the git height (the SemVer patch position). `main` builds a stable `X.Y.<height>`; `develop` builds a prerelease `X.Y.<height>-g<sha>`. The maintainer edits `version.json`; dependency bumps, codegen refreshes, CI/workflow fixes, and doc edits leave it untouched.
- **Bump `version.json` only for functional changes, by maintainer instruction.** Raise the major/minor when the work warrants a new semantic version - a new feature, a behavior or API change, a breaking change - in the PR that introduces it (typically on `develop`). Do not bump on a fixed cadence or mechanically after a release.
- **No post-release bump; no develop-ahead requirement.** NBGV advances the patch (git height) on every commit, so a release always gets a fresh build version with no `version.json` edit and there is no `bump-version-X.Y` PR after a release. A `develop -> main` promotion carries whatever `version.json` is current: a promotion with a functional bump releases that new version on `main`; a maintenance-only promotion (dependency/codegen bumps, CI/doc fixes) carries the unchanged `version.json` and `main` advances only its NBGV height.
- **`dotnet/nbgv` is consumed via `@master`, never SHA-pinned.** Its tag stream lags `master` such that Dependabot tag-tracking would only propose downgrades to stale tags; this is the sole WORKFLOW.md D9.1 exception (rationale inline in the workflow). Do not SHA-pin it.

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

### Characters and voice

- **No em-dashes (`U+2014`), ever.** They are the clearest tell of machine-written prose and are not how this repo is written. Use a spaced hyphen ` - `, a comma, a colon, parentheses, or two sentences instead. Avoid the matching tell of long semicolon-chained sentences. Prefer plain, short sentences.
- **Default to ASCII.** Non-ASCII is allowed only where the character carries real visual or semantic meaning ASCII cannot - a warning or info icon in a README callout, or a unit symbol (ohm, micro, degree). Never use non-ASCII decoratively: no fancy quotes, no Unicode arrows (write `->`), no ellipsis character (write `...`), no en-dash (write `-`).
- Spell in US English, not UK English (see the PR-title rules).

### Markdown

- Use reference-style links for any URL referenced more than once or appearing in lists; alphabetize the reference definitions block.
- Inline single-use relative links (e.g. `[CODESTYLE.md](./CODESTYLE.md)`) are fine.
- One logical paragraph per line; no hard-wrap line-length limit. For an intentional hard line break within a block - stacked badges, status, or license lines - end the line with a trailing backslash (`\`); this explicit form is preferred over trailing whitespace and is not treated as a paragraph split.
- Headings follow the title-case-with-short-bind-words rule from the PR-title section.

### Comments

Applies to code and workflow (`#`) comments alike.

- Comment only when the code does not explain itself or the logic is genuinely complex. Self-evident code needs no comment.
- Judge "obvious" in context, not line by line. A note that reads as redundant on its own line can be essential in the larger flow - a comment marking a workflow step's exit condition, for example, even though the line itself plainly does a `return` or `exit`.
- Write for the human reading *this* project's code now: state what the code does and only the non-obvious *why*. No cross-project references (do not name other repos), no historic or design narrative, no rule citations - governance lives in this file, not echoed inline.
- Match the surrounding code's line length (typically ~120), not an 80-column wrap. For a multi-point comment, prefer short structured lines or `-` bullets over one long prose paragraph.
- **Do not accumulate comments.** When you change code or a comment, rewrite the whole comment fresh; never bolt a new comment onto an existing one or layer explanations across edits. Comment volume should stay flat or shrink over time, not grow.
- **Leave human-authored comments and emojis exactly as written** - do not reword, trim, reflow, or "clean" them, even if they seem to bend a rule. Revise only agent-authored comments, and match the surrounding voice when you do.

### Line Endings

- [`.editorconfig`](./.editorconfig) defines the correct ending per file type (CRLF for `.md`, `.cs`, XML/`.csproj`/`.props`, `.yml`/`.yaml`, `.json`, `.cmd`/`.bat`/`.ps1`; LF for `.sh`), and [`.gitattributes`](./.gitattributes) (`* -text`) stops git from normalizing.
- **Editing an existing file: preserve its current line endings** - do not reflow them as a side effect of a content change, even if the file is already non-compliant. After any programmatic edit, verify with `git diff --stat` (only changed lines) and `file <path>` (expected ending). Bring a non-compliant file to its `.editorconfig` ending only as a deliberate, isolated EOL-only change.

### Quantitative Claims

- Any quantitative claim in `README.md` (counts, sizes, version floors, supported platforms) must be verified against current code. If a doc number is derived from a code constant, mark the dependency in a source-code comment so the next editor knows to update both.

## PR Review Etiquette

> This "PR Review Etiquette" section is the provider-agnostic review-loop *contract*; the [`.github/copilot-instructions.md`](./.github/copilot-instructions.md) "GitHub Copilot Review Runbook" implements its mechanics. Without both, an agent has no pointer to the reliable Copilot mechanics and falls back to ad-hoc (and known-broken) behavior.

The repo runs a review loop on every PR: local agent iteration plus remote automated review (GitHub Copilot is the configured reviewer). Treat this as a contract regardless of which local agent authored the changes.

### Merge Gate (read this first)

**Do not merge - and do not enable auto-merge - unless ALL of these hold:**

1. Required status checks are green (`mergeStateStatus: CLEAN`), **and**
2. A Copilot review is confirmed on the **current head SHA** (not an earlier push), **and**
3. **Every** Copilot finding on that head SHA is closed out - all review threads resolved, **and** any issue-level Copilot comments (which have no resolve action) triaged and replied to - so zero outstanding findings remain, **and**
4. The maintainer has given **explicit** permission to merge.

`mergeStateStatus: CLEAN` reflects **only** required statuses - it never reflects open bot review comments, so `CLEAN` alone is **never** sufficient to merge. A green/`CLEAN` PR with an unresolved Copilot finding fails this gate; treat it as "not mergeable" no matter what the merge-state field says. The agent never merges on its own (consistent with "default to staging"; merging is maintainer-authorized).

**Merging a shipped change releases.** A merge to `main` or `develop` that changes a shipped input - including a dependency bump (`Directory.Packages.props`), so the published package's dependencies stay current - auto-publishes that branch (see [`WORKFLOW.md`](./WORKFLOW.md)); a merge confined to tests, tooling, docs, CI, or GitHub-Actions bumps does not. Releasing is a configured consequence of merging a shipped change, so weigh the release impact before merging to `main`. Never manually force a publish (`workflow_dispatch`) without explicit maintainer instruction.

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

- **Genuine design trade-off** surfaces (fail-open vs fail-closed, narrow vs broad refactor scope, "should we add a guardrail or trust the doc comment"). Triage, recommend, ask.
- **Repeated friction** across rounds without convergence - that's the rule-needs-updating signal. Stop, summarize the pattern, and let the user authorize the rule change.
- **Architectural redesign** is requested rather than a bug fix. Surface with a recommendation; never apply unilaterally.

Anti-pattern: don't keep flipping the code on the same style point. Flip the rule once and stick to the rule.

## Shared Configuration and Tooling

- **Config files.** [`.editorconfig`](./.editorconfig) (per-file-type EOL plus the C# / ReSharper style block), [`.gitattributes`](./.gitattributes), [`.markdownlint-cli2.jsonc`](./.markdownlint-cli2.jsonc), [`CODESTYLE.md`](./CODESTYLE.md) (C# code style), and [`.github/copilot-instructions.md`](./.github/copilot-instructions.md) (the Copilot review runbook) hold the repo's formatting, linting, and review-mechanics rules. `CODESTYLE.md` sits at the repo root because `AGENTS.md` and `copilot-instructions.md` link it by relative path. Keep `copilot-instructions.md` narrow (Copilot/VS Code mechanics plus the commit/PR-title summary); project-specific conventions and the public-API contract live in this file, not there.
- **Clean-compile gate.** Husky.Net pre-commit git hooks run the C# clean-compile checks (CSharpier format, then `dotnet format style --verify-no-changes`), installed with `dotnet tool restore` + `dotnet husky install`. The [`.vscode/tasks.json`](./.vscode/tasks.json) tasks `.NET Build`, `CSharpier Format`, and `.NET Format` are the canonical task names (owned by the `CODESTYLE.md` ".NET" section); do not loosen them. CI is the authoritative backstop: the `lint` job ([`WORKFLOW.md`](./WORKFLOW.md) D1.3) enforces CSharpier, `dotnet format style`, `markdownlint`, scoped `cspell`, and `actionlint` from the same config files, because a local hook can be bypassed. Keep the editor task, the hook, and CI in sync (CODESTYLE "Clean-Compile Verification").
- **Linting tools.** CI is the authoritative lint run; a local run is only for fast feedback. The `dotnet` checks need only the .NET SDK: `dotnet format style` is built into the SDK, and CSharpier is restored by `dotnet tool restore` against [`.config/dotnet-tools.json`](./.config/dotnet-tools.json). The markdown, spelling, and workflow linters have no committed manifest; run each from its official Docker image, the portable path that avoids a local Node or Go install, mounting the repo as the working directory: `cspell` from `ghcr.io/streetsidesoftware/cspell`, `markdownlint-cli2` from `davidanson/markdownlint-cli2`, and `actionlint` (which bundles `shellcheck`) from `rhysd/actionlint`, pinned to the versions [`validate-task.yml`](./.github/workflows/validate-task.yml) uses. Each takes the file globs directly, for example `docker run --rm -v "$PWD":/work -w /work ghcr.io/streetsidesoftware/cspell cspell README.md HISTORY.md` or `... davidanson/markdownlint-cli2 '**/*.md'`. The cspell accepted-word list and the path exclusions both live in [`cspell.json`](./cspell.json), the single source: the Code Spell Checker extension reads `cspell.json` ahead of the workspace `cSpell` settings (so GUI "Add to dictionary" lands words there), and the CLI and CI read the same file. Do not keep a parallel word list in the `.code-workspace` file. A local cspell or markdownlint result that reports zero files checked scanned nothing; ignore it. There is intentionally no wrapper script; the editor, these Docker images, and CI are the supported runners.
- **Codegen.** Embedded language data is regenerated by [`LanguageTagsCreate/`](./LanguageTagsCreate/), which pulls directly from the official ISO 639-2/3 + RFC 5646 registries. There is no external codegen API key.
- **Release notes.** Keep a short summary in [`README.md`](./README.md) and the full history in [`HISTORY.md`](./HISTORY.md); update both when cutting a release.

## Workflow YAML Conventions

The conventions for everything under `.github/workflows/` - action pinning, file/workflow/job/step naming, concurrency, shells, conditionals, boolean inputs, permissions, artifact handoff and cleanup, and release tagging - are specified in [`WORKFLOW.md`](./WORKFLOW.md) (sections 2 and 4), the canonical guide for this repo's CI/CD. New and modified workflows must respect it. Do not duplicate those rules here; this section is a pointer.

## Automating Workflow Validation

[`WORKFLOW.md`](./WORKFLOW.md) is a machine-followable rulebook, not just documentation: it defines a static audit (5A), end-to-end trace scenarios (5B), a live probe (5C), and a repository-configuration audit (5D) that together yield a binary **operational / not-operational** verdict. When asked to check, change, or troubleshoot the CI/CD workflows, **drive that methodology** - audit the workflow files and repository configuration against the section-4 contract, trace the affected scenarios, and report the verdict with `file:line` citations - rather than reasoning about the YAML ad hoc. A workflow change is not done until it has been re-validated this way (probe without publishing).

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
  - All NuGet package versions are centralized in `Directory.Packages.props`. `PackageReference` elements in `.csproj` files must not include a `Version` attribute. Asset metadata (`PrivateAssets`, `IncludeAssets`) stays in the `.csproj` `PackageReference` element.
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
