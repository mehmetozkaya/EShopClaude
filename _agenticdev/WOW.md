# EShopClaude — Way of Working

How the team and Claude Code deliver changes on this repository.
`CLAUDE.md` covers commands, architecture and code conventions. This file covers process only.

## Principles
- One spec is one sub-feature, one feature branch and one merge to `main`.
- A spec changes exactly one microservice plus that service's own AppHost registration.
- `main` always builds and runs. Every merge leaves a working solution.
- Every spec is self-contained. A fresh session only needs to read that one file.

## Branches and merges
- One spec gets one feature branch, branched from `main`.
- Branch name: `feature/<service>-<NN>-<short-name>`, for example `feature/catalog-01-endpoints`.
- The spec file is the first commit on the branch, before any code.
- Merge into `main` with `git merge --no-ff`, then delete the branch.
- Feature branches stay local and are never pushed.
- No direct commits to `main`.

## Folder layout
- Specs live in `_agenticdev/microservices/<service>/NN-<short-name>.md`.
- Specs are numbered per service (`01`, `02`, ...) and are never renumbered.
- Every spec follows `_agenticdev/templates/spec-template.md`.
- To start a new spec, copy [the spec template](templates/spec-template.md) and fill in every section.
- `_agenticdev/microservices/README.md` holds the status table for all specs.

```
_agenticdev/
  PROJECT.md
  PLAN.md
  WOW.md
  templates/
    spec-template.md
  microservices/
    README.md
    <service>/
      NN-<short-name>.md
```

## The cycle, per spec
1. **Pick** the next spec from the status table in `microservices/README.md`, following the ordering rules below.
2. **Branch:** create `feature/<service>-<NN>-<short-name>` from an up-to-date `main`, and commit the spec file first.
3. **Fresh session:** start a new Claude Code session, or clear the context, and point it at the spec file.
4. **Plan:** in plan mode, Claude proposes a plan from the spec. A human reviews and approves it.
5. **Implement:** in auto mode, Claude implements the approved plan and nothing beyond it.
6. **Verify manually:** run the spec's Definition of Done checks (build, `aspire run`, `.http` requests, traces, pgAdmin, browser).
7. **Commit and merge:** commit on the feature branch, then `git checkout main` and `git merge --no-ff <branch>`.
8. **Mark Done:** set the status to Done in the spec file and in the `microservices/README.md` status table.
9. **Delete the branch:** `git branch -d <branch>`.

## Ordering rules
- Specs run in number order inside a service.
- A spec does not start until every spec it depends on is merged to `main`.
- Each spec lists its dependencies, so the status table shows what is ready to start.

## Session hygiene
- One spec, one fresh session. Clear the context between specs.
- Plan mode always comes before implementation, and a human approves the plan.
- If the plan drifts outside the spec, stop and fix the spec first, not the code.
- Before any commit:
  - `dotnet build` passes.
  - `aspire run` starts every resource healthy.
  - The spec's Definition of Done passes.
