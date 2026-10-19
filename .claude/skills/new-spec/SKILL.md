---
name: new-spec
description: Create or complete one feature spec under _agenticdev/microservices for the spec-driven workflow in _agenticdev/WOW.md. Drafts from PROJECT.md and PLAN.md, asks the open design questions, and updates the status table. Markdown only, no branch, no commit, no code.
argument-hint: <service> <NN> [slug] ["title"]
disable-model-invocation: true
allowed-tools: Read, Write, Edit, Glob
---

# /new-spec

Arguments: `$ARGUMENTS`, parsed as `<service> <NN> [slug] ["title"]`.
`<service>` is the folder name (`catalog-api`, `basket-api`, `ordering-api`), `<NN>` is two digits.
`slug` is required only when the spec does not exist yet. If `title` is missing, derive it from the slug.

## Steps

1. **Resolve.** Glob `_agenticdev/microservices/<service>/<NN>-*.md`.
2. **New spec.** If no file matches, copy `_agenticdev/templates/spec-template.md` to `<NN>-<slug>.md`, heading `# <service> <NN> — <title>`, `**Status:** Not started`. Draft each section from the matching parts of `_agenticdev/PROJECT.md` and `_agenticdev/PLAN.md`. Never overwrite an existing file: if the target name exists, stop and say so.
3. **Existing spec.** If it exists, read it and report section by section: **complete**, **stub** (template placeholder or empty) or **too vague to build from** (say what is missing). Then continue to fill the gaps.
4. **Status table.** Read `_agenticdev/microservices/README.md`. Fill "Depends on" from the table and from the earlier specs of the same service. If this spec has no row, add one in service/number order, with branch `feature/<service without -api>-<NN>-<slug>` and status `Not started`.
5. **Ask before writing.** List the design decisions the documents do not settle (field names and types, how the schema is created, how the service gets its connection string, anything else). One numbered question per decision, each with your suggested answer. Stop and wait for my answers.
6. **Write the spec**, keeping exactly the template's sections:
   - **Goal:** the context, why this sub-feature exists, and why it is one spec.
   - **Scope:** one `- [ ]` item per task, naming the file it touches (`Models.cs`, `Program.cs`, AppHost, `.http`). Put the data model here as typed record/entity shapes.
   - **Explicit non-goals:** later specs' work and anything in PROJECT.md "Out of scope".
   - **Definition of Done:** every line is a runnable check: the exact command or HTTP request, and the expected result (status code, body, pgAdmin table, dashboard state). Rewrite any "it works" line.
   - **References:** the PROJECT.md § and PLAN.md step numbers used.
7. **Report.** Print the spec path, then a list of anything still to be decided by hand.

## Rules

- Touch only Markdown under `_agenticdev/`. No branch, no commit, nothing in `src/`.
- Leave Status as `Not started`, in the spec and in the table. A status that runs ahead of reality is worse than no status.
- One spec = one microservice plus its own AppHost registration (WOW.md). If the work needs more, say so and propose a split instead of writing it.
- Don't invent requirements: anything not in PROJECT.md, PLAN.md or my answers goes on the "still to decide" list.
