---
name: start-spec
description: Start one spec from the _agenticdev/WOW.md cycle (steps 2-4). Preflight-checks the repo and the spec, creates the feature branch from main, commits the spec as the branch's first commit, then plans the implementation in plan mode and stops for approval. Never implements, never pushes.
argument-hint: <service> <NN>
disable-model-invocation: true
allowed-tools: Read, Glob, Bash(git status:*), Bash(git branch:*), Bash(git checkout:*), Bash(git add:*), Bash(git commit:*)
---

# /start-spec

Current branch: !`git branch --show-current`
Working tree:
!`git status --porcelain`

Arguments: `$ARGUMENTS`, parsed as `<service> <NN>` (folder name such as `catalog-api`, two-digit number).

## Steps

1. **Resolve.** Glob `_agenticdev/microservices/<service>/<NN>-*.md`. If there is not exactly one match, stop.
2. **Preflight.** Use the state above. Stop without changing anything if any check fails:
   - The current branch is not `main`.
   - The working tree has changes other than the spec file and `_agenticdev/microservices/README.md` (the status table that /new-spec updates with a new spec).
   - The spec file has no changes against `main`, so there is nothing to commit.
   - An entry in the spec's "Depends on" is not `Done` in the `microservices/README.md` table.
   - A section is empty or still holds the template's `_italic placeholder_` text. Tell me to run `/new-spec <service> <NN>` first.
   - The spec's Status is `Done`.
3. **Branch name.** Apply the WOW.md rule `feature/<service>-<NN>-<short-name>`. `<service>` drops the `-api` suffix and `<short-name>` is the spec's file slug, for example `catalog-api/04-get-product.md` → `feature/catalog-04-get-product`. It must match the spec's Branch column in README.md. If it doesn't match, or `git branch --list <name>` shows the branch already exists, stop. Never invent a name and never ask me for one.
4. **Branch.** `git checkout -b <branch> main`. Uncommitted spec changes move onto the branch.
5. **Spec commit.** `git add` only the spec file, plus README.md if it changed. Then `git commit -m "Add spec <service>/<NN>-<slug>" -m "Spec commit before implementation; no code yet."`. This is the branch's first commit.
6. **Plan.** Enter plan mode. Read the spec and propose an implementation plan:
   - Scoped to its Scope and Tasks items, and its Files touched if listed.
   - Bounded by its Explicit non-goals and by CLAUDE.md's conventions.
   - Ends with the spec's Definition of Done as the checks to run.

   If the spec is too vague to plan a step, say so and don't fill the gap with your own design.
7. **Stop.** Present the plan for my approval. Do not edit any file in `src/` or implement anything.

## Rules

- Never push. Feature branches stay local in this repository. Never merge, never delete files.
- Leave Status as it is. It changes to Done only after the merge (WOW.md cycle step 8).
- When you refuse, name the check that failed, show the evidence (the branch, the file or the table row), and say what I should do next, such as `git checkout main`, commit or stash the other changes, finish spec `<dep>` first, or run `/new-spec`.
