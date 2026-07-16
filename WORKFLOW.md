# WORKFLOW.md

The single guide for this repo's CI/CD **workflows** (GitHub Actions): **code style**, **architecture**,
a **behavioral contract** (expected inputs and outputs), and a **test methodology**. Source code style
lives in [`CODESTYLE.md`](./CODESTYLE.md). This file covers everything under
[`.github/workflows/`](./.github/workflows/).

It **describes required outcomes, not a required implementation.** A workflow is correct when it
satisfies the contract (section 4), whatever shape its YAML takes. Section 2 keeps workflows legible.
Section 3 is the model. Section 4 is what they must *do*. Sections 5 and 6 are how to verify it and the
configuration it assumes.

Each guarantee names the **failure it prevents**, so the reason survives a reimplementation.

## 0. The model at a glance

A run targets **one branch, the one it was triggered on** (`github.ref_name`): `main` builds a stable
release, `develop` a prerelease. The version is computed once and threaded downstream. A pull request
builds and tests but never publishes. The package **publishes itself** when a shipped input changes - the
source, the embedded data, the version floor, the build configuration, or the package versions
(`Directory.Packages.props`) - so releases track the code without a person cutting them. Listing the package
versions means a dependency bump republishes too, keeping the package's declared dependencies current. A
maintainer dispatches only to force a release. Dependabot and codegen
pull requests merge themselves once their checks pass.

### Glossary

- **Entry workflow** - has `push`/`pull_request`/`workflow_dispatch` triggers. The orchestrator a person
  or event starts.
- **Reusable workflow (task)** - a `workflow_call` workflow invoked from an entry workflow through a
  `uses:` reference. Never triggered directly.
- **Leaf** - the reusable task that produces the shipped artifact (here, the NuGet package).
- **Smoke build** - a pull-request build that compiles and packs the library to prove it still ships,
  publishing and uploading nothing. Linting and testing are the separate `validate` job. Driven by a
  `smoke: true` input.
- **Transfer artifact** - a workflow artifact that hands a file between jobs of one run (e.g. the built
  package passed to the release job). The durable copy lives on the GitHub release / NuGet.org.
- **Head-resolved vs base-resolved** - a `pull_request` event resolves a reusable `./...` reference from
  the **base** branch's copy, while a `push`/`workflow_dispatch` event resolves it from the **pushed**
  head. Self-testing (section 3) depends on this.
- **Shipped input** - a file that changes what the package ships: the library source (`LanguageTags/**`),
  the embedded data (`LanguageData/**`), the version floor (`version.json`), the build configuration
  (`Directory.Build.props`), or the package versions (`Directory.Packages.props`). It is an explicit
  **inclusion list** (the publisher's `on.push.paths`), so a change confined to tests, the codegen tool,
  GitHub Actions, docs, or CI is **not** a shipped input. Package versions are included because a NuGet
  version cannot be re-pushed (no scheduled rebuild like a Docker image), so a dependency bump must
  republish to keep the package's declared dependencies current and close the stale/vulnerable-dependency
  window. GitHub Actions bumps stay excluded - they do not ship in the package.
- **GitHub App token** - a short-lived installation token from `actions/create-github-app-token`, minted
  from the App credentials (`CODEGEN_APP_CLIENT_ID` / `CODEGEN_APP_PRIVATE_KEY`). Automation that must
  trigger downstream workflows or write to bot pull requests uses **this token, not `GITHUB_TOKEN`**: a
  commit pushed with the built-in token does not trigger downstream workflows (GitHub's recursion guard),
  and that token is read-only on Dependabot pull requests.

## 1. Purpose and how to use this document

- **Contract, not implementation.** Conform to the *outcomes* in section 4 and the *architecture* in
  section 3. Job names and file layout may vary. The input/output behavior and the branch-scoped,
  single-ref architecture may not.
- **"Operational" - the one definition.** The repo is **operational** when every applicable section-4
  guarantee holds, every applicable section-5B scenario's observed output equals its expected output
  (corroborated by a 5C live probe where a live signal exists), and the section-6 configuration is in
  place. Anything else is **not operational**. Every later use of "operational" means exactly this.
- **Defect vs N/A.** An item is **N/A** only when this repo has no such concern (for example a fork-PR
  scenario, since a fork cannot push here). It is **not** N/A because the workflow that should implement it
  is missing. A construct required by an applicable guarantee but absent is a **defect** (FAIL).
- **Guarantees are scored independently.** One line of YAML can satisfy one guarantee and violate
  another. Record each verdict on its own.
- **Default branch is `main`.** Guarantees say "default branch" portably. This repo writes the literal
  `main` in the prerelease expression and the validate gate, and the anchored `^refs/heads/main$` in
  `version.json`'s `publicReleaseRefSpec`. All three must designate `main`.
- **The verbs.** **Audit** (static 5A, configuration 5D), **Test** (trace + probe, 5B/5C), **Assess**
  (verdict). Section 5 gives the procedure.

## 2. Workflow style conventions

Legibility rules. Cheap to check, necessary but not sufficient: a perfectly styled workflow can still
violate section 4.

- **Action pinning.** Pin every action to a commit SHA with a trailing `# vX.Y.Z` comment, so a tag swap
  cannot change executed code while Dependabot can still bump it. Use `# vX` only when the upstream
  floating major tag has no specific patch SHA. The SHA-pin rule applies to `uses:` action references; a
  CLI tool an action downloads at runtime (e.g. the actionlint binary) is not a `uses:` ref and is out of
  scope. The sole `uses:` no-pin exception is `dotnet/nbgv@master`, whose tag stream lags `master` such that
  tag-tracking would downgrade.
- **Filename.** Reusable workflows (`on: workflow_call`) end in `-task.yml`. Entry-point workflows end in
  what they do (`-pull-request.yml`, `-release.yml`). Lowercase, hyphen-separated. A `-task.yml` is
  invoked through a `uses:` reference, never triggered directly.
- **Workflow `name:`.** Reusable workflow names end in **"task"**, entry-point names in **"action"**, so
  the UI label tells you orchestrator from callee at a glance.
- **Job and step `name:`.** Every job `name:` ends in **"job"**, every step `name:` in **"step"**, the
  aggregator included (`Check pull request workflow status job`). A job name also bound as a ruleset
  required-status-check `context:` is codified in [`repo-config/`](./repo-config/). It follows the suffix
  rule like any job, but changing it means updating those ruleset files and the live ruleset **in
  lockstep**, or required-check enforcement silently breaks.
- **Concurrency.** Every entry-point workflow declares a `concurrency` group. The default is
  `group: '${{ github.workflow }}-${{ github.ref }}'` with `cancel-in-progress: true`. Three entry
  workflows override it. The **publisher** uses a ref-independent group with `cancel-in-progress: false` so publishes
  serialize and none is cancelled mid-release. The **merge-bot** keys on the PR number with
  `cancel-in-progress: false` so each PR's events run to completion in order. The daily codegen workflow
  (`run-periodic-codegen-pull-request.yml`) uses the same ref-independent group expression as the publisher
  but with `cancel-in-progress: true`: it only opens the fixed `codegen-main`/`codegen-develop` pull
  requests, so a dispatch superseding an in-flight scheduled run is harmless, and the shared group still
  keeps the two from racing.
- **Shells.** Every multi-line bash `run:` starts with `set -euo pipefail`.
- **Conditionals.** Multi-line `if:` uses the folded scalar `if: >-`. A literal block `if: |` embeds
  newlines into the boolean and is wrong.
- **Boolean inputs.** A boolean used by both `workflow_call` and `workflow_dispatch` is declared in both
  trigger blocks. `workflow_dispatch` delivers the string `"true"`/`"false"`, so any `if:` consuming it
  compares both forms: `${{ inputs.foo == true || inputs.foo == 'true' }}`.
- **Reusable-workflow permissions.** Job-level `permissions:` are validated before `if:`, so even a
  skipped job's declared permissions must be valid. Grant least privilege. A callee's extra scope (e.g.
  `actions: write` to delete artifacts) is granted by the caller at the `uses:` job.
- **Allowlist `success` and `skipped` explicitly** when chaining across an optional dependency.
  `!= 'failure'` lets `cancelled` through. Use `(needs.X.result == 'success' || needs.X.result ==
  'skipped')`.
- **Line endings.** Workflow YAML follows [`.editorconfig`](./.editorconfig) (CRLF here). Preserve
  endings on every edit.

## 3. Architecture

### Branch-scoped, single-ref

A run targets one branch, `github.ref_name`. The branch alone decides everything: `main` builds a stable
release, every other branch a prerelease. One run never builds, versions, or publishes a second branch.
There is no branch matrix, no plan job fanning out to multiple branches, no `branch` input that can
disagree with the triggering ref. *Prevents the defect class where the CI ref, the checkout, and the
version classification disagree.*

### Versioning: compute once, thread everywhere

NBGV runs in exactly one job per run. Its outputs (`SemVer2`, `GitCommitId`, the assembly versions)
thread to every consumer through `outputs:`/`needs:`, and no other job re-invokes it. A build job may
check out a specific commit to **compile** it, but it consumes the threaded version. *This keeps the
package version and the release tag in agreement.*

Every run is a `push`/`workflow_dispatch` on a real branch, so `actions/checkout` lands on a branch tip
and NBGV classifies natively: the public-release ref (`publicReleaseRefSpec = ^refs/heads/main$`) builds a
clean `X.Y.Z`, every other branch a prerelease `X.Y.Z-g<sha>`. The detached-merge-ref case (NBGV seeing no
branch) never arises. `version.json`'s `version` is the major.minor floor, and NBGV appends the git height
as the patch.

### Validate at entry

When a run carries a cross-input or input-versus-derived-state invariant, a dedicated entry job/step
asserts it once and fails fast with `::error::` before any build or publish. Downstream jobs `needs:` it.

### Resource lifecycle

Workflow artifacts are an intra-run handoff. The durable copy lives on the GitHub release / NuGet.org. A
transfer artifact is deleted by exact name at the point it is consumed, the delete gated to the consumer's
condition and best-effort. Every `upload-artifact` sets `retention-days: 1` as a backstop, so a run that
skips the delete still reclaims its artifact. The run's artifact set is never blanket-deleted
(`.artifacts[].id`), which would destroy the diagnostic artifacts needed to debug a failed run. See D5.

### Fast pull-request feedback

A pull request validates fast and never publishes. Validation is a reusable `validate-task` holding two
jobs, `unit-test` (build and test) and `lint` (the editor's checks, enforced in CI). The pull request runs
it as a `validate` job alongside `smoke-build` (build and pack the library to prove it ships, uploading and
pushing nothing). Both run on every push with no paths filter (a branch-deletion push is the one exception -
a `!github.event.deleted` guard skips them, since `github.sha` is all-zeros and checkout would fail), so a
reusable-workflow change is always exercised head-resolved. Packaging validation as one task lets the
publisher run the identical gate (D4.6).
One required aggregator gates the merge. See D1.

### Self-testing workflows, and the required-context invariant

A pull request exercises its own workflow files. No change waits to reach `main` first.

- **CI runs on `push` to every branch.** GitHub head-resolves the reusable `./...` workflows from the
  pushed head, so a pull request that edits a reusable task tests its own copy. The push run is the **sole
  producer** of the aggregator's ruleset-bound `context:`, on the head SHA branch protection evaluates. CI
  never publishes.
- **Single-producer invariant.** Exactly one trigger path emits a given ruleset-bound context name. No
  `pull_request`-triggered job emits it, which would race two check-runs on one SHA.
- **Only `main`/`develop` produce releases.** The publisher also runs on `push` to the protected branches,
  gated on a shipped change (D4.1). CI and the publisher then run in separate workflows with separate
  concurrency, so they do not race: CI re-tests the merged tree, the publisher releases only on a shipped
  change.
- **A dispatched publish uses that branch's workflows**, so a workflow change is usable on the branch that
  introduces it.
- **Forks are the documented exception.** A fork cannot push here, so its pull request produces no run and
  no aggregator check, and a maintainer lands the change on an in-repo branch (which pushes, and so
  validates) before merging. Dependabot is not an exception: its pull requests are in-repo branches,
  validated head-resolved by their push (a read-only token and the Dependabot secret store, enough for the
  gate). See D6.

### Publishing: self-sufficient and branch-scoped

The package publishes itself when a shipped input changes, so releases track the code without a person
cutting them. Every publish targets only the branch it ran on (`develop` -> prerelease, `main` -> stable).
Two things publish:

- **An automatic release on a shipped change.** The publisher runs on `push` to `main`/`develop` with the
  `on.push.paths` inclusion list (`LanguageTags/**`, `LanguageData/**`, `version.json`,
  `Directory.Build.props`, `Directory.Packages.props`), so it triggers only when a shipped input changed.
  `.github/**` is not listed, so Actions bumps do not republish; `Directory.Packages.props` is listed, so a
  dependency bump republishes to keep the package's dependencies current. The merge-bot merges with the App
  token, so its merge commits reach this push trigger.
- **A manual release on demand.** A `workflow_dispatch` on a branch publishes it immediately, whatever
  changed - the "release now" control.

There is no scheduled publish and no publish-on-every-merge. Every publish runs the same `validate-task`
the pull request runs (the identical definition, not a copy) as a `validate` job the publish job `needs:`,
so nothing ships that would fail the pull-request gate (D4.6). This matters because `develop` squashes and
`main` merge-commits, so the published commit is not the feature-head the pull request smoke-tested. See
D4.

### Self-sufficiency: automatic updates

- **Dependabot pull requests merge themselves.** Every Dependabot pull request, any ecosystem and any tier
  (semver-major included), auto-merges once the required checks pass, using the App token. The checks are
  the safety net: an update that breaks the build or tests fails them, auto-merge does not complete, and
  GitHub notifies the maintainer.
- **Codegen refreshes data the same way.** The codegen workflow regenerates `LanguageData/` from its
  upstream registries daily, opens a pull request only when the data changed, and auto-merges it on green.
  The data is a shipped input, so the publisher then releases it.

The library is self-maintaining: data and dependencies stay current on both branches, each shipped change
releases automatically, and a person steps in only for a breaking change (a red check) or to force a
release by dispatch. A merged dependency bump republishes (its `Directory.Packages.props` change is a
shipped input), keeping the published package's dependencies current. See D8.

### Single-target output seam

The repo produces exactly one shipped artifact, the NuGet package. The leaf pushes the package, and where
symbols are enabled its symbol package, to NuGet.org via OIDC trusted publishing (no long-lived API key,
D4.7), and bundles them with the compiled library into a single fixed-name `LanguageTags.7z` attached to
the GitHub release. There is no generic multi-target abstraction: no `enable_<target>` flag selecting among
leaves, no `expect_release_assets` toggle, no `release-asset-<branch>-*` glob. The single asset,
`LanguageTags.7z`, is attached by its fixed name, so `releases/latest/download/LanguageTags.7z` is a stable
download URL.

### Flow diagrams

Three diagrams trace the architecture above: the pull-request gate, the self-publisher, and the bot
automation. They depict the same outcomes that the section 4 contract specifies, drawn from the workflow YAML; if a diagram and
a guarantee disagree, one of them is a defect. Triggers are blue, gates yellow, durable/published outputs
green, and stop/skip outcomes red.

**Pull request (CI) - `test-pull-request.yml`.** Every push head-resolves the reusable tasks, runs the
validate gate and a non-publishing smoke build, and a single aggregator produces the ruleset-bound required
check (D1, D6).

```mermaid
flowchart TD
    T(["push: every branch<br/>(or workflow_dispatch)"]):::trig
    T --> D{"github.event.deleted?"}
    D -- "yes: branch deletion" --> X(["all jobs + aggregator skip<br/>no failed run, no pending check"]):::stop
    D -- "no" --> V["validate job<br/>(validate-task.yml)"]
    D -- "no" --> S["smoke-build job<br/>build-release-task.yml<br/>smoke: true, publish: false"]
    subgraph VT ["validate-task.yml"]
        U["unit-test job<br/>dotnet test, warnings-as-errors"]
        L["lint job<br/>CSharpier, dotnet format,<br/>markdownlint, cspell, actionlint"]
    end
    V --> VT
    S --> SB["build + pack library<br/>head-resolved, no push, no uploads"]
    VT --> A
    SB --> A
    A{"Check pull request workflow status job<br/>validate AND smoke-build succeeded?"}:::gate
    A -- "yes" --> G(["required check passes<br/>merge unblocked"]):::pub
    A -- "no" --> R(["required check fails<br/>merge blocked"]):::stop
    classDef trig fill:#dbeafe,stroke:#2563eb,color:#1e3a8a
    classDef gate fill:#fef9c3,stroke:#ca8a04,color:#713f12
    classDef pub fill:#dcfce7,stroke:#16a34a,color:#14532d
    classDef stop fill:#fee2e2,stroke:#dc2626,color:#7f1d1d
```

**Publish - `publish-release.yml` -> `build-release-task.yml`.** A shipped-input push or a dispatch runs
the same validate gate, then versions once with NBGV, asserts branch-vs-version, builds the pinned commit,
pushes to NuGet via OIDC, and cuts the GitHub release (D2, D3, D4).

```mermaid
flowchart TD
    P1(["push: main/develop<br/>paths = shipped inputs"]):::trig --> VAL
    P2(["workflow_dispatch"]):::trig --> VAL
    VAL["validate job<br/>(validate-task.yml)"] --> PG{"publish guard<br/>push OR ref in (main, develop)"}:::gate
    PG -- "no" --> PSKIP(["publish skipped"]):::stop
    PG -- "yes" --> GV
    subgraph BRT ["build-release-task.yml (publish: true)"]
        GV["get-version job<br/>NBGV @master, runs once<br/>SemVer2 + GitCommitId"] --> VR{"validate-release<br/>branch matches version?"}:::gate
        VR -- "mismatch" --> VRX(["fail ::error::"]):::stop
        VR -- "agree" --> B["build job<br/>checkout GitCommitId<br/>build + pack"]
        B --> NP[("NuGet.org push<br/>OIDC key, skip-duplicate")]:::pub
        B --> GR{"github-release<br/>tag new OR dispatch?"}:::gate
        GR -- "exists, not dispatch" --> NOP(["skip create<br/>artifact reclaimed by backstop"]):::stop
        GR -- "create" --> REL[("GitHub release<br/>tag = SemVer2 at GitCommitId<br/>prerelease = ref != main")]:::pub
    end
    classDef trig fill:#dbeafe,stroke:#2563eb,color:#1e3a8a
    classDef gate fill:#fef9c3,stroke:#ca8a04,color:#713f12
    classDef pub fill:#dcfce7,stroke:#16a34a,color:#14532d
    classDef stop fill:#fee2e2,stroke:#dc2626,color:#7f1d1d
```

**Automation - codegen + Dependabot + merge-bot.** Daily codegen and Dependabot open in-repo bot PRs; the
merge-bot enables auto-merge (or disables it on a maintainer push); a merged shipped input then drives the
publisher above (D8).

```mermaid
flowchart TD
    SCH(["schedule daily 04:00 UTC<br/>(or workflow_dispatch)"]):::trig --> CG
    subgraph CGT ["run-codegen-pull-request-task.yml (matrix: main, develop)"]
        CG["codegen job per branch<br/>regenerate LanguageData<br/>(deterministic)"] --> CGC{"data changed?"}
        CGC -- "no" --> CGN(["no PR"]):::stop
        CGC -- "yes" --> CPR["open codegen-&lt;branch&gt; PR<br/>(App token)"]
    end
    DEP(["Dependabot opens PR<br/>any ecosystem/tier"]):::trig --> MB
    CPR --> MB
    subgraph MBT ["merge-bot-pull-request.yml (pull_request_target)"]
        MB{"event / author"}:::gate
        MB -- "opened/reopened<br/>bot author" --> EN["enable auto-merge<br/>squash develop / merge main"]
        MB -- "synchronize by maintainer" --> DIS["disable auto-merge"]
    end
    EN --> CK{"required checks pass?"}:::gate
    CK -- "yes" --> MRG(["PR merges (App token)"]):::pub
    CK -- "no" --> BLK(["merge blocked<br/>maintainer notified"]):::stop
    MRG -. "shipped input changed" .-> PUBR(["publisher auto-releases"]):::pub
    classDef trig fill:#dbeafe,stroke:#2563eb,color:#1e3a8a
    classDef gate fill:#fef9c3,stroke:#ca8a04,color:#713f12
    classDef pub fill:#dcfce7,stroke:#16a34a,color:#14532d
    classDef stop fill:#fee2e2,stroke:#dc2626,color:#7f1d1d
```

## 4. Behavioral contract - expected outcomes

Each is a **MUST**, stated as input -> output plus the failure it prevents. A workflow that violates any
applicable guarantee is not operational (section 1).

### D0 - Branch-scoped architecture

- **D0.1 One run, one branch.** Input: any triggered run. Output: it builds/versions/publishes exactly
  `github.ref_name`, with no job fanning out to a second branch. *Prevents: mis-classified versions and
  mismatched tags from cross-branch ref mixing.*
- **D0.2 One version, threaded.** Output: NBGV runs in exactly one job, on a real-branch-tip checkout on
  the publish path, and its outputs thread via `needs:` to all consumers. No second job recomputes a
  version. *Allowed:* checking out a specific commit to compile it, and recording the built commit's SHA
  as the release `target_commitish` (D4.3); neither re-runs NBGV. *Prevents: a checkout that versions a
  package differently from its tag.*

### D1 - Pull-request fast feedback

- **D1.1 Every push builds, lints, and tests.** Output: on any push the `validate` job - the reusable
  `validate-task`, holding the `unit-test` and `lint` jobs - and `smoke-build` run with no paths filter.
  The one exception is a branch-deletion push: a `!github.event.deleted` guard skips every job (and the
  aggregator skips too, so the required check is not left pending), because `github.sha` is all-zeros and a
  checkout/build would fail. `smoke-build` builds and packs the library in its branch configuration through
  the same `build-release-task` the publisher uses. *Prevents: a reusable-workflow change shipping untested
  because a filter excluded it; a build/packaging break slipping through; a branch-deletion push failing CI.*
- **D1.2 Unit tests always run.** Output: the `unit-test` job (in `validate-task`) runs `dotnet test`
  (build with `TreatWarningsAsErrors`, so analyzer/style warnings fail here), and the aggregator reaches
  it through the `validate` job it `needs:`.
- **D1.3 Lint enforces the editor checks in CI.** Output: the `lint` job runs CSharpier (`dotnet csharpier
  check`), `dotnet format style --verify-no-changes`, `markdownlint-cli2`, `cspell` on the user-facing
  docs (README, HISTORY), and `actionlint` (which shellchecks every `run:` block). These are the same
  checks the editor and the local Husky hook run, enforced from the same config files. *Prevents:
  formatting, markdown, spelling, or workflow-YAML defects reaching the branch on editor-faith.*
- **D1.4 Smoke never publishes and never uploads.** Output: full compile/pack, but no NuGet push, no
  GitHub release, no artifact uploads (every `upload-artifact` is gated `!smoke`). *Prevents: a PR
  publishing; orphaned artifacts.*
- **D1.5 One required aggregator gates merge.** Output: a single aggregator job must succeed (not merely
  "not fail"), `needs:` `validate` and `smoke-build` (and so transitively `unit-test` and `lint`), and
  blocks on any non-success. Its name is ruleset-bound, has a single producer (D6.2), and must not be
  renamed. *Prevents: a library, lint, or workflow defect merging unverified.*

### D2 - Validation at entry

- **D2.1 Validate before expensive work.** Output: a dedicated entry job/step asserts each
  cross-input/derived-state invariant and fails fast with `::error::` before builds. Downstream jobs
  `needs:` it.
- **D2.2 Branch matches version classification.** Input: a real (non-smoke) publish. Output: the gate
  fails loudly if `main` carries a prerelease suffix or a non-`main` branch carries none. It strips
  `+buildmetadata` before testing for the prerelease `-`. It is skipped on smoke (smoke never publishes,
  so the check is moot, and a smoke build on a feature branch versions as prerelease regardless).
  "Skipped on smoke" means the gate runs and self-skips its body to `success`, not that the job is absent.
  *Prevents: a develop build published as stable; a build-metadata false positive; the gate blocking a
  smoke build.*

### D3 - Versioning and classification

- **D3.1 One NBGV invocation, threaded.** Output: NBGV runs once, classifying from `github.ref_name`'s
  real-branch checkout on the publish path, and its outputs thread to build and release. No consumer
  re-invokes NBGV. *Prevents: a leg classified by the wrong ref; a package version diverging from the
  tag.*
- **D3.2 `main` = stable, others = prerelease.** Output: `main` -> `X.Y.Z` (`PublicRelease=true`), any
  other branch -> `X.Y.Z-g<sha>` (`PublicRelease=false`). The gate and the `prerelease` expression name
  `main`, and `version.json`'s `publicReleaseRefSpec` is `^refs/heads/main$`.
- **D3.3 Version floor + git height.** Output: `version.json` sets the major.minor floor, NBGV appends
  the git height as the patch, never bumped on a cadence. *(Who raises the floor and when is a
  human-process rule in `AGENTS.md`, out of scope for this verdict.)*
- **D3.4 NuGet prerelease is derived, not set.** Output: NuGet.org marks a package prerelease when its
  `PackageVersion` carries the SemVer2 `-g<sha>` suffix, a consequence of D3.2, not a flag the workflow
  sets. (Distinct from the GitHub-release `prerelease` boolean of D4.4, which the workflow does set.)

### D4 - Release / publish

- **D4.1 Publish only by dispatch or a shipped-input change.** Output: the publisher is reachable via (a)
  `workflow_dispatch` on a branch (force-publish, guarded to `main`/`develop`), or (b) a `push` to
  `main`/`develop` matching the **`on.push.paths` inclusion list** of shipped inputs (`LanguageTags/**`,
  `LanguageData/**`, `version.json`, `Directory.Build.props`, `Directory.Packages.props`). The list is
  inclusion-only: it does not list `.github/**`, docs, tests, or the codegen tool, so a GitHub Actions bump
  or a docs change does not republish. `Directory.Packages.props` **is** listed, so a dependency bump
  republishes (a NuGet version can't be re-pushed, so deps must republish to stay current). There is no
  `schedule` and no `PUBLISH_ON_MERGE`. *Prevents: a blind scheduled republish; a no-impact change (actions
  bump, docs) cutting a release; and a stale/vulnerable dependency lingering in the published package.*
- **D4.2 Publish exactly the triggering branch.** Output: the run publishes only `github.ref_name`
  (`develop` -> prerelease, `main` -> stable; a shipped change or dispatch on `main` cuts a stable release
  by design). *Prevents: a publish shipping the wrong branch.*
- **D4.3 Tag the built commit.** Output: the release `target_commitish` is the built commit's SHA (NBGV's
  `GitCommitId`), never `github.sha` of a moving ref. *Prevents: the tag landing on a different commit
  than was built.*
- **D4.4 Release contents and flag.** Output: every release is a tag on the built commit plus the auto
  source zip, README, and LICENSE, with a fixed-name `LanguageTags.7z` attached that bundles the compiled
  library, the `.nupkg`, and (where `IncludeSymbols`) the `.snupkg`. The GitHub-release `prerelease`
  boolean is set to `github.ref_name != 'main'`. *(GitHub computes the
  "Latest" badge from semver across non-prerelease releases, a consequence, not a workflow assertion.)*
- **D4.5 No-op republish.** Input: a re-run whose version is unchanged. Output: the release-create step
  is skipped when the tag already exists (refreshed only on `workflow_dispatch`). The NuGet push runs and
  the server dedupes (`dotnet nuget push --skip-duplicate` treats an existing-version 409 as success), the
  symbol push likewise. The paired transfer-artifact delete is gated to the release-create step, so on a
  no-op re-run the artifact is reclaimed by the `retention-days: 1` backstop. *Prevents: duplicate
  releases and wasted pushes.*
- **D4.6 Publish is tested as built.** Input: any publish (dispatch or shipped-change). Output: the
  publisher runs the same reusable `validate-task` (the D1.2/D1.3 `unit-test` + `lint` gate) as a
  `validate` job the publish job `needs:`, so the push and release are gated on its success. It is the
  identical definition the pull request runs, so nothing publishes that would fail the PR gate. The
  trade-off, accepted over polling a cross-workflow status check, is that a shipped-input push to a
  protected branch validates twice. *Prevents: an auto-publish shipping a merged tree tested only as the
  pre-merge PR head, since the squash/merge commit (D8.1) differs from what the PR tested.*
- **D4.7 Publish authenticates via OIDC trusted publishing.** Output: the publish job grants
  `id-token: write` and obtains a short-lived NuGet key from `NuGet/login@v1` (the action exchanges the
  GitHub OIDC token for a temporary key, using the `NUGET_USERNAME` profile name), and `dotnet nuget push`
  uses that key. There is **no** long-lived `NUGET_API_KEY` secret. The key is requested immediately
  before the push (1-hour lifetime, single use). The matching trusted-publishing policy on NuGet.org
  (section 6) names `build-release-task.yml`, the reusable task that requests the token (the OIDC
  `job_workflow_ref`), not the `publish-release.yml` entry workflow. *Prevents: a leaked long-lived publish
  credential.*

### D5 - Resource cleanup

- **D5.1 Delete at the point of consumption.** Output: a cross-job transfer artifact is deleted by exact
  name/pattern right after the job that consumes it.
- **D5.2 Gate the delete to the consumer's condition.** Output: the delete runs under the same condition
  as its consuming step. A no-op re-run that skips the consumer skips the delete too and relies on the
  D5.4 backstop. *Prevents: deleting a freshly built asset on a no-op re-run.*
- **D5.3 Best-effort.** Output: cleanup is `continue-on-error: true`, tolerates a failed listing, and
  deletes all matching ids. *Prevents: a cleanup hiccup reddening a successful publish.*
- **D5.4 Retention backstop.** Output: every `upload-artifact` sets `retention-days: 1`.
- **D5.5 Never blanket-delete.** Output: cleanup MUST NOT enumerate and delete the run's whole artifact
  set (`.artifacts[].id`). *Prevents: destroying diagnostic/build-record artifacts.*

### D6 - Self-testing workflows

- **D6.1 A change is testable on its own branch.** Output: a workflow or build change is exercised by CI
  on the branch that introduces it, with no dependency on the change first reaching `main`. *Prevents:
  the "promote to `main` to test the fix" trap.*
- **D6.2 Head-resolution, single producer, one exception.** Output: CI runs on `push` to every branch so
  reusable `./...` logic resolves from the head, and the aggregator's ruleset-bound `context:` is produced
  by that push run on the head SHA as the sole producer of that name. Dependabot pull requests are in-repo
  branches, so their push validates them the same way (restricted read-only token, enough for the gate). A
  fork is the one exception: it cannot push, so it has no run and is validated by maintainer action, never
  by a second producer of the gate context. *Prevents: a dual-producer context race; a false self-test
  claim for fork PRs.*

### D7 - Concurrency, permissions, safety

- **D7.1 The publisher does not cancel mid-flight.** Output: the publisher's concurrency uses a
  ref-independent group with `cancel-in-progress: false`. All other entry workflows use the
  `...-${{ github.ref }}` group with `cancel-in-progress: true`, except the merge-bot (PR-number group, D8.1) and the daily codegen
  workflow (ref-independent `${{ github.workflow }}` group with `cancel-in-progress: true`, section 2).
- **D7.2 Skipped jobs still need valid permissions.** Output: every reusable job runs under valid least-privilege
  `permissions:`, and a callee's extra scope is granted by the caller.
- **D7.3 Boolean inputs both forms.** Output: boolean inputs are declared in both trigger blocks and
  compared against `true` and `'true'`.
- **D7.4 Optional-dependency chaining.** Output: cross-job conditions allowlist `success`/`skipped`
  explicitly rather than `!= 'failure'`.

### D8 - Bots and automation

- **D8.1 Merge-bot.** Output: runs on `pull_request_target`, holds the **App token**, and merges the pull
  request by URL without checking out its code. Enables auto-merge on `opened`/`reopened`. Produces a
  linear (squashed) history on `develop` and a merge commit into `main`, chosen by the PR's base ref.
  Disables auto-merge when a maintainer pushes to a bot branch. Concurrency keyed on the PR number.
  *Prevents: two PRs colliding in auto-merge; a bot merge that fails to trigger downstream workflows.*
- **D8.2 Dependabot auto-merges on green, every tier.** Output: every Dependabot pull request, any
  ecosystem and semver-major included, auto-merges once the required checks pass, with no version-tier
  exception. A failing check blocks the merge and surfaces via GitHub's check-failure notification. A
  merged dependency bump **republishes** (`Directory.Packages.props` is a shipped input, D4.1), keeping the
  published package's declared dependencies current; a GitHub-Actions bump does not. *Prevents: a breaking
  update merging unverified; a safe update stalled waiting for a human; and a stale/vulnerable dependency
  lingering in the published package.*
- **D8.3 Codegen is deterministic and content-gated.** Output: codegen regenerates `LanguageData/` purely
  from its upstream sources (no per-run timestamps/GUIDs), opens a pull request only when the data changed,
  and auto-merges it on green. The merged data is a shipped input, so the publisher releases it (D4.1).
  Codegen is **dual-target**, the workflow analog of Dependabot's per-target-branch config: each branch is
  regenerated independently against its own checkout, into its own `codegen-<branch>` PR, so a data update
  never depends on a cross-branch merge-back. A matrix (one leg per branch) is the expected form; the
  no-branch-matrix rule (D0) is scoped to the build/version/publish path and does not apply here, since
  codegen neither versions nor publishes.

### D9 - Style, static, and dropped workflows (see section 2)

- **D9.1** Every action SHA-pinned with a version comment (sole exception: `dotnet/nbgv@master`). A tool an
  action *installs* (e.g. the actionlint binary behind `raven-actions/actionlint`) is not a `uses:` ref and is
  left unpinned to track latest, so CI picks up new lint rules.
- **D9.2** File/workflow/job/step names follow the suffix rules. A name also used as a ruleset
  required-check `context:` is codified in `repo-config/` and changed only in lockstep with the ruleset.
- **D9.3** Bash `run:` blocks start `set -euo pipefail`; multi-line `if:` uses `>-`.
- **D9.4** Line endings follow `.editorconfig`.
- **D9.5** No decorative / non-shipped workflow remains, in particular no date-badge workflow
  (`build-datebadge-*`). The contract ships exactly the package and its release. A workflow that produces
  neither is out of scope, and its presence is a defect to remove.
- **D9.6** Style is enforced in CI, not just the editor: the `lint` job (D1.3) runs CSharpier check,
  `dotnet format style`, `markdownlint-cli2`, `cspell` on the user-facing docs, and `actionlint`, from the
  same config files the editor and the Husky hook use (CODESTYLE clean-compile sync).

### D10 - Repository configuration

- **D10.1 Required configuration is present.** Output: the secrets, branch rulesets, and repository
  settings that section 6 lists are all in place. *Prevents: a green-looking repo whose first real
  publish or auto-merge fails on a missing secret, an unenforced ruleset, or a disabled setting.* The
  detail and the validation procedure are in section 6; the audit is 5D.

## 5. Test methodology

An agent verifies the repo in escalating modes, then renders the section-1 verdict. Skip N/A items
(section 1); a required-but-missing construct is a FAIL, not N/A.

### 5A. Static audit (no execution)

Read the workflow files plus `version.json` and assert the structural fact behind each applicable
guarantee, each pass/fail/N-A with a `file:line` citation:

- **D0:** no branch matrix and no plan job in the publisher; no `IGNORE_GITHUB_REF`, no `git checkout
  -B`, no `branch` input that can differ from `github.ref_name`; NBGV invoked in exactly one job, every
  other consumer reading it via `needs:` outputs (a second invocation that recomputes is the defect; a
  commit checkout that only compiles is allowed).
- **D1:** the PR workflow runs on `push` with no paths filter; the `validate` job (the reusable
  `validate-task`, holding `unit-test` + `lint`) and `smoke-build` run on every push except a branch deletion
  (every job, the aggregator included, carries a `!github.event.deleted` guard); the smoke call
  sets publish off and `smoke: true`; every build `upload-artifact` is gated `!smoke`; the `lint` job runs
  CSharpier check, `dotnet format style --verify-no-changes`, `markdownlint-cli2`, `cspell` on
  README/HISTORY, and `actionlint`; the aggregator `needs:` `validate` + `smoke-build` and blocks on any
  non-success.
- **D2:** the release gate checks both directions, strips `+buildmetadata`, and self-skips on smoke to
  `success`.
- **D3:** `main` appears in the gate and the `prerelease` expression (`!= 'main'`); `version.json`'s
  `publicReleaseRefSpec` is `^refs/heads/main$`.
- **D4:** the publisher's triggers are `workflow_dispatch` and a `push` to `main`/`develop` with an
  `on.push.paths` inclusion list of exactly `LanguageTags/**`, `LanguageData/**`, `version.json`,
  `Directory.Build.props`, `Directory.Packages.props` (no `.github/**`); no `schedule`, no
  `PUBLISH_ON_MERGE`; the dispatch path is guarded to `main`/`develop`; the publisher calls the same
  `validate-task` as a `validate` job and the publish job `needs:` it (D4.6); the run publishes only
  `github.ref_name`; `target_commitish` is the NBGV commit id; the GitHub-release `prerelease` boolean
  `== (github.ref_name != 'main')`; the release body attaches the source zip, README, LICENSE, and a
  fixed-name `LanguageTags.7z` bundle (compiled library + `.nupkg`/`.snupkg`); the leaf pushes `*.nupkg`
  and `*.snupkg` (symbols enabled) with `--skip-duplicate`; the publish job grants
  `id-token: write` and pushes with a `NuGet/login@v1` short-lived key, not a `NUGET_API_KEY` secret
  (D4.7); release-create gated `exists == false || workflow_dispatch`.
- **D5:** each cross-job transfer artifact has a delete gated to its consumer, `continue-on-error: true`,
  looping all ids; every upload sets `retention-days: 1`; no `.artifacts[].id` blanket delete exists.
- **D6:** PR-validated logic is head-resolved (a `push` trigger on every branch), and the ruleset-bound
  aggregator context has exactly one producer. Dependabot PRs are in-repo and validate via that push, a
  fork PR has no run and needs maintainer action, and there is no `pull_request`-triggered fallback.
- **D7:** the publisher group is ref-independent with `cancel-in-progress: false`; the merge-bot keys on
  PR number; other entry workflows use the standard group; reusable jobs declare permissions; boolean
  `if:` uses both forms.
- **D8/D9:** the merge-bot runs on `pull_request_target` with the App token and keys concurrency on PR
  number; Dependabot auto-merge has no semver-major exception (gated only on the required check); codegen
  is deterministic + per-branch; no multi-target `enable_*`/`expect_release_assets` abstraction; no
  date-badge / decorative workflow exists; actions SHA-pinned; names/shells/conditionals per section 2.

### 5B. End-to-end trace scenarios (deterministic from the YAML)

For each applicable scenario, evaluate every job's `if:`/`needs:` against the inputs and emit the
predicted **run/skip + version + release + artifact-end-state**, then compare to expected. *One input is
assumed as a given rather than re-derived from the YAML: the version classification (clean vs `-g<sha>`),
determined by NBGV from the checkout state in section 3.*

| # | Input | Expected output | Exercises |
| --- | --- | --- | --- |
| S1 | push touching `LanguageTags/**` | `validate` (`unit-test` + `lint`) and `smoke-build` all run; smoke (`smoke:true`) builds and packs, **no push, no uploads, no release**; validate-release self-skips on smoke; aggregator success; version = prerelease (branch is not `main`); no dangling artifacts | D0, D1, D2.2, D3 |
| S2 | push changing only docs/README | `validate` and `smoke-build` run; `lint` checks the markdown; `smoke-build` rebuilds the unchanged library; aggregator success; nothing publishes | D1, D1.5 |
| S3 | push changing only `.github/workflows/**` | `validate` and `smoke-build` run; `smoke-build` exercises the changed reusable workflow head-resolved (self-test); `lint` runs `actionlint` on it; aggregator success | D1.1, D6.1 |
| S4 | `workflow_dispatch` on `develop` | builds/publishes only develop; the `validate` task the publish job `needs:` gates it (D4.6); version `X.Y.Z-g<sha>`; release `prerelease=true`; NuGet prerelease; `target_commitish`=built SHA; transfer artifact consumed-then-deleted; no dangling artifacts | D0, D3, D4, D5 |
| S5 | `workflow_dispatch` on `main` | builds/publishes only main; the `validate` gate the publish job `needs:` gates it; version `X.Y.Z`; release `prerelease=false`; NuGet stable; `.snupkg` pushed; no dangling artifacts | D0, D3, D4, D5 |
| S6 | merge of a **source** change to `develop`/`main` | push changed a shipped input -> that branch **auto-publishes**, validated by the `needs: validate` gate before publish (D4.6) | D4.1, D4.6 |
| S7 | re-run, version unchanged (tag exists) | release-create skipped; transfer artifact reclaimed by backstop; NuGet push a `--skip-duplicate` no-op; no duplicate release | D4.5, D5.2 |
| S8 | branch/version classification disagree (e.g. `main` carries `-g`) | validate-release fails loud; build/publish skip | D2.2 |
| S9 | merged codegen `LanguageData/**` change | shipped input changed -> that branch **auto-publishes** | D4.1, D8.3 |
| S10 | merged GitHub-Actions version bump only | `.github/workflows/**` is not a shipped input -> **no publish** | D4.1 |
| S11 | merged dependency bump, any kind (e.g. `Microsoft.Extensions.Logging.Abstractions` or `xunit.v3`) | `Directory.Packages.props` is a shipped input -> that branch **auto-publishes**, keeping the package's declared dependencies current | D4.1, D8.2 |
| S12 | PR with a CSharpier, dotnet-format, markdown, spelling, or workflow-YAML violation | the `lint` job fails -> aggregator blocks the merge | D1.3, D1.5 |
| S13 | `version.json` floor bump merged to a branch | version floor is a shipped input -> **auto-publish** that branch at the new floor | D3.3, D4.1, D4.2 |
| S14 | Dependabot **major** bump whose tests fail | required check fails -> auto-merge does **not** complete; no merge, no publish; maintainer notified | D8.2 |
| S15 | `develop` -> `main` promotion (merge commit) carrying a shipped change | the merge commit's diff (`before..after`, `before` = prior `main` tip) includes the promoted shipped input -> `main` **auto-publishes the stable release**; a promotion carrying only non-shipped changes does not | D4.1, D4.2, D8.1 |
| S16 | a branch is **deleted** (a push event with `github.sha` all-zeros) | the `!github.event.deleted` guard skips `validate`, `smoke-build`, and the aggregator -> no failed CI run, no pending required check | D1.1 |

### 5C. Live probe (where warranted, never publishing)

- Open a trivial-change PR touching the library and confirm S1 (smoke builds, nothing pushed, aggregator
  green, 0 artifacts left).
- Drive a `smoke: true` push-probe of the release-build path on a throwaway branch for the `develop` and
  `main` classifications, and assert clean vs prerelease and that the gate passes, without publishing.
- After any real publish, query NuGet.org for the expected version + `isPrerelease`, confirm a re-run
  added no duplicate, and inspect the run for `PublicRelease`/`SemVer2` and the artifact lifecycle. The
  live-only guarantees a static read cannot settle (D4.5 server-dedupe, the artifact end-state, live
  `PublicRelease`) are what 5C confirms. Absent publish rights, record them **indeterminate** and rely on
  the 5A/5B static evidence.

### 5D. Configuration audit

Run the self-audit in [`AUDIT.md`](./AUDIT.md) (section 6). It confirms the listed secret names exist,
the `main`/`develop` rulesets match the committed payloads in [`repo-config/`](./repo-config/) (merge
method + status check + signed commits + strict-off), and the repository settings (auto-merge, allowed
merge methods) are in place. A missing or incorrect configuration item is a defect (D10). Secret *values*
cannot be read back, so the audit asserts the names exist and a GitHub App is installed. The NuGet.org
trusted-publishing policy (D4.7) lives outside GitHub and cannot be checked by `gh api`; it remains a
manual verification item.

### Assessment

Operational when every applicable 5A item passes, every applicable 5B scenario matches (corroborated by
5C where a live signal exists), and 5D configuration is in place. N/A items are excluded; a
required-but-missing construct is a FAIL. Procedure:

1. **Audit** with 5A and **5D**; record pass/fail/N-A with `file:line` or the config item.
2. **Trace** the applicable S-scenarios with 5B; diff predicted vs expected.
3. **Probe** with 5C only for what a static trace cannot settle, without publishing; where unprobeable,
   mark indeterminate.
4. **Verdict:** operational or not, with the failing guarantee(s), the triggering input for each, the
   items recorded N/A or indeterminate, and (during adoption) the conformance baseline so an expected
   pre-refactor failure is not read as a regression.

## 6. Repository configuration

The workflows depend on configuration outside the YAML: secrets, branch rulesets, and repository
settings. A misconfiguration surfaces only as a failed run (a missing secret, a merge that never
auto-completes, a tag on the wrong branch), so the configuration is part of "operational" and is testable
in its own right, not merely discoverable by failure (D10; audit 5D).

**Secrets.**

- `NUGET_USERNAME` - the NuGet.org profile name passed to `NuGet/login@v1` for OIDC trusted publishing
  (D4.7). Kept in **both** the Actions and Dependabot secret stores, like the App pair, so a publish run
  triggered by a Dependabot-actor merge still resolves it (Dependabot-triggered runs get the Dependabot
  store, not Actions secrets). **No `NUGET_API_KEY`** secret is used; publishing is keyless.
- `CODEGEN_APP_CLIENT_ID` / `CODEGEN_APP_PRIVATE_KEY` - the GitHub App credentials the merge-bot and
  codegen mint the App token from. Required in **both** the Actions and Dependabot secret stores: codegen
  and the publisher read them from Actions, but the merge-bot reads them from the Dependabot store when it
  acts on a Dependabot PR (Dependabot-triggered runs get the Dependabot store, not Actions secrets). The
  App must be installed on the repo with `contents: write` and `pull_requests: write`.
- The built-in `GITHUB_TOKEN` needs no setup. **No `PUBLISH_ON_MERGE` variable is used**; its presence is
  stale configuration to remove.

**NuGet.org trusted-publishing policy.** Publishing is keyless via OIDC (D4.7), so a trusted-publishing
policy must exist in the NuGet.org account naming Repository Owner `ptr727`, Repository `LanguageTags`, and
Workflow File `build-release-task.yml` (filename only) - the reusable task that runs `NuGet/login` and
requests the token, which the OIDC `job_workflow_ref` claim names rather than the `publish-release.yml`
entry workflow. It lives on NuGet.org, not GitHub, so `configure.sh` cannot read it - a manual checklist
item. A private-repo policy stays provisional for 7 days until the
first successful publish locks it to the repo and owner IDs.

**Branch rulesets.**

- `main` - merge-commit merges only; requires the aggregator status check (the ruleset-bound `context:`
  `Check pull request workflow status job`); requires signed commits; "require branches up to date before
  merging" is **off** (a forward-only `develop` makes every post-release `main` tip unreachable from
  `develop`, so the strict check would fail every release).
- `develop` - squash merges only (keeps history linear); requires the same status check; requires signed
  commits; "up to date" is **off** (so same-batch bot pull requests auto-merge in parallel without one
  pushing the other `BEHIND`).
- The required check's `context:` name matches the aggregator job name verbatim (D6.2, D9.2).

**Repository settings.**

- Auto-merge enabled. Both squash and merge-commit methods allowed (each ruleset narrows its branch to
  one).
- Actions enabled with permission to run the pinned actions. Dependabot version **and** security updates
  enabled.
- The GitHub App installed with the scopes above.

**Validation.** This configuration is codified in [`repo-config/`](./repo-config/): the branch rulesets
and repository settings as JSON. The self-audit in [`AUDIT.md`](./AUDIT.md) diffs the live rulesets,
settings, and secret names against the committed baseline; that self-audit **is** the 5D audit.
`repo-config/configure.sh` applies the rulesets and settings idempotently to a repository; Dependabot
vulnerability alerts and security updates are enabled once at provisioning time, outside the script.
Secret values cannot be read back, so the audit asserts the names exist and a GitHub App is installed
rather than checking contents.
