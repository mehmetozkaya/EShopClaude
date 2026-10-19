---
name: finish-spec
description: Finish one spec from the _agenticdev/WOW.md cycle (steps 5-9) on its feature branch. Implements the approved plan, verifies the Definition of Done with the user, marks the spec Done, then merges into main with --no-ff after confirmation and deletes the branch. Never runs on main.
argument-hint: <service> <NN>
disable-model-invocation: true
allowed-tools: Read, Write, Edit, Glob, Bash(git:*), Bash(dotnet build:*)
---

# /finish-spec

Current branch: !`git branch --show-current`
Commits so far:
!`git log --oneline main..HEAD`
Working tree:
!`git status --porcelain`

Arguments: `$ARGUMENTS`, parsed as `<service> <NN>` (folder name such as `catalog-api`, two-digit number).
The spec is the single match of `_agenticdev/microservices/<service>/<NN>-*.md`. Its branch is `feature/<service without -api>-<NN>-<slug>`, the Branch column of its row in `_agenticdev/microservices/README.md`.

## Steps

1. **Preflight.** Stop and explain if:
   - the current branch is `main`, or isn't this spec's branch;
   - the branch doesn't exist (`git branch --list <branch>`): tell me to run `/start-spec <service> <NN>` first;
   - the spec commit isn't in "Commits so far";
   - the spec's Status is already Done.
2. **Develop.** Implement the spec, following the plan approved in this conversation. If there is no approved plan here (for example, a fresh session), enter plan mode, plan from the spec, and wait for my approval first.
   - Stay inside the spec's Scope and Tasks.
   - Honour its Explicit non-goals and CLAUDE.md's conventions.
   - Commit on the branch as you go, staging files by explicit path.
3. **Verify.** Run `dotnet build src/EShopClaude.sln` and stop on any error. Then walk the spec's Definition of Done line by line:
   - Run each check you can (build, `dotnet test`, `git diff --stat main`) and record its result.
   - For each check you can't run yourself (`aspire run` healthy, pgAdmin tables or rows, `.http` requests, dashboard traces, browser), ask me one question per line with the expected result, and wait.
   - Any failure or "no" stops the skill here, before anything is marked or merged. Say what failed.
4. **Mark Done.** Take `git rev-parse --short HEAD`. In the spec, set `**Status:** Done (<sha>)`.
5. **Status table.** Set the spec's row in `microservices/README.md` to `Done`.
6. **Doc commit.** `git add <spec path> _agenticdev/microservices/README.md`, then `git commit -m "Mark <service>/<NN>-<slug> Done"`. Never use `git add -A` or `git add .`.
7. **Confirm, then merge.** Show the branch, the target `main`, the commits to merge, and whether `main` will be pushed afterwards. Wait for my explicit yes. Then:
   1. `git checkout main`
   2. `git merge --no-ff <branch>`. On a conflict, stop and report it (`git status`). Don't resolve it and don't delete the branch.
   3. `git branch -d <branch>`
   4. `git push origin main`, only if a remote exists (`git remote`). Otherwise say it was skipped.
8. **Report.** Give:
   - the merge commit SHA;
   - the files changed (`git diff --stat <merge>^1 <merge>`);
   - a table of each Definition of Done line and how it was verified (ran it, with the result, or confirmed by me).

## Rules

- Never push a feature branch. Only `main` is pushed, after the merge.
- Never commit on `main` directly, never force anything (`--force`, `-D`, `reset --hard`), never use `git add -A`.
- No work beyond the spec. If the spec turns out wrong or incomplete, stop and tell me to fix the spec first (WOW.md session hygiene).
