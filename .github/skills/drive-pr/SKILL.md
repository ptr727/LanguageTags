---
name: drive-pr
description: >-
  Drives a ptr727/ProjectTemplate fleet pull request through its review loop, feature branch into
  develop and, when asked, on to a mergeable develop -> main promotion PR, applying the
  pr-review-conduct disposition to every reviewer finding along the way: fix it, decline it with
  evidence, defer it behind a filed issue, or put the call to the maintainer and wait for an
  explicit answer in the same turn. Use this whenever asked to drive, land, take, chase, or push
  a PR toward develop or main, or to run the review loop hands off instead of narrating each
  round. When the request does not say how far ("drive this PR", "land it"), ask once whether the
  target is develop or a mergeable main promotion PR, rather than guessing. Triggers even when
  only one PR is named, because a finding raised against the develop -> main promotion PR
  routinely needs its own feature -> develop fix cycle before the promotion PR can go green, and
  stopping at the first promotion-PR finding is the early exit this skill exists to prevent. Ends
  at develop merged, or at a promotion PR meeting the pr-review-conduct Merge Gate, never merges
  main itself, that is the separate merge-and-release skill, its own go-ahead.
---

# Drive PR

## Why This Exists

The same request repeats every time a change is ready: drive it through review, resolve whatever
a reviewer raises, and keep going until develop, or main, actually has it. Re-explaining the
finding-disposition policy and the promotion-PR wrinkle each time is the cost this skill removes.
The wrinkle: a finding raised against the develop -> main promotion PR usually cannot be fixed on
that PR directly, its diff is develop's diff against main, so the fix lands as its own
feature -> develop PR first. Stopping at the first such finding, or forgetting to loop back to the
promotion PR once the fix lands, is the early exit this skill exists to prevent.

## How Far to Drive

- Read the invocation for an explicit target first. "To develop" or "to dev" means stop once
  merged into develop. "To main", "through to main", or "all the way" means continue to a
  mergeable promotion PR. Act on either without asking.
- When the request names no target ("drive this PR", "land it", "take this PR"), ask once,
  before the first push: develop only, or all the way to a mergeable main promotion PR. Recommend
  "all the way to main" as the default, a promotion PR left to go stale once develop is ready is
  the more common regret than driving one step too far.
- A repo on the operational workflow model (registry `workflowModel: operational`) has no
  standing promotion PR expectation, confirm whether a promotion PR is even wanted before opening
  one, per operational-vs-release-workflow's "Operational repositories" delta.

## What Invoking This Skill Authorizes

- Naming this skill, and answering its how-far question, is the maintainer's explicit, current
  go-ahead for every feature -> develop squash merge the drive performs to reach that target.
- It is never authorization to merge the develop -> main promotion PR, or to dispatch a release.
  Those stay in merge-and-release, invoked on its own so the maintainer keeps a checkpoint before
  the harder-to-reverse step.
- The pr-review-conduct Merge Gate still gates every merge this skill performs on its own. The
  go-ahead removes the "may I merge to develop" question, not the gate itself, a feature PR with
  an open finding does not merge regardless of target.

## The Drive Loop

1. Isolate into a worktree per repo-worktree, based on the branch that skill's base rule names, develop unless the task is explicitly about main-only content, before the first edit.
2. Commit the work, then run `local-strict-review` and record its pass in the order that skill
   gives, its diff receipt following the commit, and its carried-content record instead preceding
   the commit where the change moves a carried canonical unit in the repository that authors one,
   because that ledger is tracked. Then push the branch and open the feature -> develop PR if it
   does not exist yet. A push refused by a `.husky/pre-push` hook, which the hub carries and a
   repository has only if it adds one, is that gate working rather than an
   obstacle to route around, and that
   skill's refusal table says what each refusal means and what clears it.
3. Drive pr-review-conduct's review loop on it to the Merge Gate, disposing of every finding per
   "Disposing of Every Finding" below.
4. Capture the branch's own tip before merging, `gh pr view <number> --repo <owner>/<repo> --json headRefOid --jq
   .headRefOid`, needed for the verify-then-delete step below since `gh pr merge` itself reports
   the resulting squash commit on `develop`, not the PR's `headRefOid`. Merge the feature PR into
   develop, `gh pr merge <number> --squash --repo <owner>/<repo>`. Never `--delete-branch` on this
   call, it is run from inside the task's own worktree per step 1, where the feature branch is
   checked out, and `gh pr merge --delete-branch` needs to switch that worktree to the base branch
   to delete it, which fails when `develop` is already checked out somewhere else, the ordinary
   case in this layout. Instead run repo-worktree's post-merge cleanup from the base clone, remove
   the worktree and delete the now-merged local task branch, then verify before deleting the remote
   one, which is this skill's own step rather than that one's. Both remote commands resolve `origin`,
   so they hold only where the pull request's head branch lives in this repository, which step 1
   guarantees by branching here. A pull request opened from a fork follows
   `upstream-contribution-workflow` instead and neither command applies to it, since `origin` would
   name the base repository and exit `2` would mean the branch was never there rather than already
   deleted. The object id in `git ls-remote --heads --exit-code -- origin "refs/heads/<branch>"`,
   which prints `<oid>\t<ref>` so the id is its first field, matches the `headRefOid` captured above, `--` before `origin` and the fully-qualified ref. `--heads origin
   "<branch>"` alone still tail-matches a differently-prefixed branch sharing the same suffix, and
   `--` placed after `origin` instead of before it is not equivalent either, verified empirically
   against a `refs/heads/other/--` ref: after-origin also matched it, before-origin matched only
   the one intended. `--exit-code` distinguishes exit `2`, branch genuinely gone, from any other
   non-zero exit, a failed query, an unreachable remote and a gone branch both print nothing to
   stdout otherwise. Exit `2` means the remote branch is already gone, so the delete is done and
   the step is complete. Stop and report either a mismatch or a failed query rather than deleting,
   someone could have pushed to the branch after the merge, or the name could have been reused.
   `<branch>` is the real value, substituted as its own quoted argument (a shell variable
   expansion such as `"$branch"`, or an argv element), never handed to `eval` or `sh -c` for a
   second round of shell parsing, the only way an embedded `$()` or backtick would actually run.
   A valid ref can start with `-` or carry a shell metacharacter, which is why it stays quoted
   regardless. Only once it matches, `git push origin --delete -- "<branch>"`. Never
   `--force-with-lease` here, git-commit-conventions forbids it
   unconditionally, this plain verify-then-delete is the safety gate, not a compare-and-swap at
   delete time. The
   repo's auto-delete-head-branches setting is kept off fleet-wide (to protect `develop` and
   `main` from it, GitHub has no per-branch exception), so nothing deletes an ordinary feature
   branch automatically. Stop here and report the merged PR when the target is develop only.
5. Open the develop -> main promotion PR if it does not exist yet, or find the existing one.
6. Drive its review loop the same way. A finding that needs a code change never gets pushed to
   the promotion PR directly, its head is develop, so land the fix as a fresh pass through steps
   1 to 4 in its own worktree and branch, then return here.
7. The fix landing on develop updates the promotion PR's diff and head SHA on its own, re-request
   a review on the new head and continue the loop.
8. Repeat 6 and 7 until the promotion PR itself carries no open finding and its checks are green
   on the current head.
9. Report the promotion PR number and its ready state. Do not merge it.

## Disposing of Every Finding

pr-review-conduct's five outcomes are the actual rule, this is the mapping to use while driving:

- Real, so fix it, then step 2's own order again before replying with the fixing commit SHA
  (outcome 1). This is the round the pass is most often skipped on, since the fix looks small and
  the branch was already reviewed once, and a fix push carries content no pass has read exactly as
  the first push did.
- Not real, or real but out of scope here, so decline in the thread with evidence: the command
  and its output, the code path, or the rule that governs it. An assertion never closes a finding
  on its own (outcome 2).
- Real and worth doing, but later, so file the issue first, then reply with its link (outcome 4).
- Real, fixable, but a value call rather than a scope boundary, or the agent genuinely does not
  know which of the above applies, so ask the maintainer directly, whatever the runtime's own
  interactive-question mechanism is, and get an explicit answer in the same turn, a plan to ask
  later is resolution by silence (outcome 3).
- The same finding keeps recurring against correct code, fix the class, sharpen a name, add a
  comment, or take the rule itself to the maintainer, rather than re-arguing the instance every
  round (outcome 5).

## Mechanics Live Elsewhere

- Review loop mechanics, the Merge Gate, and `scripts/pr_review.py`: pr-review-conduct.
- Branch rules, never delete develop, the EOL-only conflict, issue-closing keywords belonging on
  the promotion PR: operational-vs-release-workflow.
- Worktree isolation and post-merge cleanup: repo-worktree.

## Stop and Ask, Beyond the How-Far Question

- A genuine design trade-off, a recurring finding pattern, or an architectural redesign proposal
  each escalate per pr-review-conduct's own list, restated there, not duplicated here.
- An unrecognized review shape blocks the gate on its own, file an issue naming it and ask, never
  guess what new wording probably meant.
