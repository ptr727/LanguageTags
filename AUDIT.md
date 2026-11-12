# AUDIT.md

How this repository audits itself against its committed baseline and reports drift. This is the repo-scoped adaptation of the fleet-wide AUDIT.md kept at the fleet hub. The hub's fleet-wide audit remains authoritative. General settings, rulesets, and secret names are hub-hosted ground truth in `repo-config/` and `spec/secrets.json`. They are checked from a hub checkout rather than carried here, per [GOVERNANCE.md "Hub-Hosted Tooling"][governance-hub-hosted-tooling]. The prose authorities are [`GOVERNANCE.md`][governance], [`CODESTYLE.md`][codestyle], and [`WORKFLOW.md`][workflow].

The audit is read-only. It diffs live state against the committed baseline and reports findings, and it never applies changes. The verdict vocabulary is [`WORKFLOW.md`][workflow]'s: **operational / not operational**, **N/A**, **defect**, and the applicable/absent rule.

## Scope

This is a release-model repo: the self-audit covers the `main` and `develop` rulesets, general repository settings, and secret names. Code-project conformance (analyzers, tests, coverage, codegen, and NuGet publishing) is the job of CI and of the hub's fleet-wide audit, not of this self-audit. See [GOVERNANCE.md "Branching Model"][governance-branching-model] for the model this baseline encodes.

## General Settings and Rulesets

Fetch the hub and check out `main`. Run `repo-config/configure.sh check ptr727/LanguageTags release` from that checkout. The command checks the shared settings and the state-dependent ones (`has_discussions` follows visibility, `default_branch` is `main`). It checks Dependabot security features, the fleet label set, and the link to the fleet project. It checks any deployment environment the registry declares, and both the `develop` and `main` rulesets against the hub payloads. It preserves and reports `bypass_actors` without asserting them, since who may bypass a ruleset is a human decision no payload declares. Both modes need project access on the token as well as admin on the repository. That is because the project link is read and written through GraphQL.

The result must be exactly two rulesets named `develop` and `main`. A missing ruleset or a divergent payload is a **defect**, and a duplicate or stray ruleset is a **drift finding**.

## Secrets

From the same hub checkout, run `uv run --no-project --script spec/audit.py LanguageTags` and read its secret findings. It resolves the required set from the hub's `spec/secrets.json` plus this repo's registry entry (`publish[]`/`types[]`/`requiredSecrets[]`). It confirms each required name exists (name only, not values) in the stores its mechanism claims. For LanguageTags that is the baseline App pair, `NUGET_USERNAME` for the NuGet OIDC push, and `CODECOV_TOKEN`, all in both the Actions and Dependabot stores. A workflow run triggered by a Dependabot pull request reads the Dependabot store, so a `CODECOV_TOKEN` missing there silently skips the coverage upload. `repo-config/configure.sh check ptr727/LanguageTags release` asserts the same names per store. `repo-config/configure.sh apply ptr727/LanguageTags release` writes each one from the host's `~/.secrets/` directory.

## Verdict and Follow-Up

A missing required item or a divergent payload is a **defect** (not operational). An equivalent outcome in a non-standard form is a **drift finding**. N/A items are excluded, never counted as failures. Surface findings as repository issues, and land fixes as a pull request to `develop` per [GOVERNANCE.md "Branching Model"][governance-branching-model]. To re-apply the whole baseline, run `repo-config/configure.sh apply ptr727/LanguageTags release` from a hub checkout.

<!-- Repo -->

[codestyle]: ./CODESTYLE.md
[governance]: ./GOVERNANCE.md
[governance-branching-model]: ./GOVERNANCE.md#branching-model
[governance-hub-hosted-tooling]: ./GOVERNANCE.md#hub-hosted-tooling
[workflow]: ./WORKFLOW.md
