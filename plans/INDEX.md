# Next-round migration plans — review index

Three execute-ready plans, one per remaining repo, plus a future PyPI project. Each follows the same
9-section structure and maps the shared gotcha checklist ([00-GOTCHA-BRIEFING.md](./00-GOTCHA-BRIEFING.md))
into repo-specific findings, so the post-merge surprises that cost time last round are surfaced BEFORE any
code changes.

- [esphome-nonroot-migration-plan.md](./esphome-nonroot-migration-plan.md) — Docker, upstream-pin check
- [homeassistant-purpleair-migration-plan.md](./homeassistant-purpleair-migration-plan.md) — Python/HACS zip
- [nxwitness-migration-plan.md](./nxwitness-migration-plan.md) — .NET codegen + multi-target Docker

> This INDEX is the **authoritative aligned decision record** (updated 2026-06-29 after maintainer review).
> Where an individual plan's prose predates these decisions, this file wins; the plans' release-model
> sections have been patched to match.

## Release-model playbooks (the framing)

We are converging on **release-model playbooks**: a WORKFLOW set defined per *release model*, the same way
CODESTYLE defines rules per *language*. Same-model repos share workflow definitions; a fix to one backports
to its model-siblings. A model can have sub-models (e.g. Docker splits by whether an external update trigger
exists). Current map:

| Release model | Publish trigger | Repos |
|---|---|---|
| NuGet push-publish (+ `Directory.Packages.props` as shipped input) | push-on-dep-change + dispatch | LanguageTags, Utilities |
| Native binary + multi-arch Docker | dispatch / on-demand | PlexCleaner |
| **Docker — vanilla** (no external trigger) | weekly schedule(main) + dispatch | VSCode-Server |
| **Docker — triggered** (daily external signal) | weekly schedule(main) + **push-on-pin/matrix-change(main)** + dispatch | ESPHome-NonRoot, NxWitness |
| HACS/Python — manual release + upstream-retest tripwire | dispatch only (schedule retests, never publishes) | HA-PurpleAir |
| PyPI (the "never done" one) | TBD | *future repo — which one?* |

**Capstone deliverable (after all migrations):** per-workflow flow diagrams in text notation (Mermaid) +
rendered PNG, showing entry points, triggers, outputs, decisions, and branching — making common-vs-unique
obvious across models and sub-models.

## Maintainer clarifications (incorporated)

- **`upstream-version.json` / `Matrix.json` are maintained by the repo's own daily scheduled job** (the
  upstream-version check for ESPHome; codegen for NxWitness), which checks upstream and records the
  last-released versions. NOT dependabot-updated. This is the repo's self-owned upstream-state pin.
- **The daily detection signal is 100% certain** an update is required; vanilla Docker has no such signal so
  it can only assume a weekly apt/base change. Hence: triggered repos publish on the signal AND weekly;
  vanilla publishes weekly only.
- **Dependabot (and codegen) dual-target main AND develop on every repo** — purely to avoid merge drift.
  develop's pin/matrix/dep updates are sync-only and never publish.
- **HA monitoring is a breakage tripwire**: the HA-version monitor bumps the test matrix so a breaking
  upstream change FAILS the PR build; a human fixes it and releases manually. That is why HA publish is
  dispatch-only.

## Release-trigger decision per repo (signed off)

- **ESPHome-NonRoot:** weekly `schedule` on main (baseline apt/base CVE refresh) **+ path-scoped `push` on
  main when `upstream-version.json` changes** (the daily upstream-check commits a real update -> publish
  now) **+ `workflow_dispatch`**. The daily upstream-check workflow stays as the detection mechanism. Cheap
  single image, so publish-on-trigger is clearly worth it. Drops PUBLISH_ON_MERGE; ordinary code merges do
  not touch the pin so they do not publish.
- **NxWitness:** weekly `schedule` on main (builds the full product matrix, baseline refresh + release)
  **+ path-scoped `push` on main when `Matrix.json` changes** (codegen commits a new matrix -> publish now)
  **+ `workflow_dispatch`**. **Supersedes the earlier weekly-only decision** — publish on matrix change is
  accepted despite the full-matrix build cost. Schedule is **main-only** (the earlier "paired develop
  dispatch to refresh :develop" is dropped; `:develop` builds on manual dispatch only). Codegen runs daily,
  dual-target main+develop.
- **HomeAssistant-PurpleAir:** **dispatch-only** publish; `schedule` retests main only and **never
  publishes**. The HA-version monitor updates the test matrix to trip on breaking changes (fail the PR ->
  human fix -> manual release). Drops the current `push:[develop]` auto-prerelease that violates the "merges
  never publish" invariant the docs already claim.

## Cross-repo recurring findings (same root causes as last round — now pre-empted)

1. **Nested NBGV in `build-docker-task.yml`** (ESPHome, NxWitness) — second NBGV run risks `:SemVer2` tag
   collision / misclassification. Fix: thread one `semver2` down, delete the nested `get-version`.
2. **merge-bot missing `--delete-branch`** (all three) — bot branches accumulate; auto-delete-setting stays
   OFF to protect develop on promotion.
3. **No `repo-config/` 5D audit** (ESPHome, NxWitness; HA also lacks it) — created from the canonical
   hardened `configure.sh` + ruleset JSON.
4. **Required-check name mismatch** (NxWitness aggregator is `...status` missing trailing ` job`; HA
   similar) — must move in lockstep with the first `apply` or PRs never go green.
5. **CI still on `pull_request` + `dorny/paths-filter`** (NxWitness) — move to `push: ['**']`, drop the
   filter, add the `!github.event.deleted` guard. (Note: the publisher's path-scoped `push` is separate and
   intentional — it lives in `publish-release.yml`, not CI.)
6. **Messy branch backlog** (all three; HA worst at 40+) — prune superseded branches after verifying they
   are merged/abandoned.
7. **Version-floor regressions to set first** — HA main is 0.3.0 but NBGV base would ship 0.1.x (bump base
   to >=0.3 first); ESPHome/NxWitness bump the floor to exercise the publish path.

## Suggested execution order (simplest -> hardest)

1. **ESPHome-NonRoot** — closest to the converged Docker reference; smallest delta; first to prove the
   triggered-Docker sub-model (weekly + push-on-pin).
2. **HomeAssistant-PurpleAir** — develop already ~90% converged; mostly main-catch-up + Python validate
   mapping + branch cleanup; the version regression needs deciding first.
3. **NxWitness** — most complex (codegen + multi-target matrix + push-on-matrix publish); do last with the
   pattern fresh.
4. **PyPI project** (future) — identify the repo and plan once the above land.

All SHA->version claims in the plans were verified against the GitHub API (no hallucinated pins). No code
was changed — these are plans only.
