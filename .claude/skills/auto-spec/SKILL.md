---
name: auto-spec
description: Run one spec end to end by sequencing /new-spec, /start-spec and /finish-spec, with two human gates in between. Adds no steps of its own and stops on the first refusal.
argument-hint: <service> <NN> [slug] ["title"]
disable-model-invocation: true
allowed-tools: Read, Write, Edit, Glob, Bash(git:*), Bash(dotnet build:*)
---

# /auto-spec

Arguments: `$ARGUMENTS`, parsed as `<service> <NN> [slug] ["title"]`. `slug` and `title` go only to /new-spec, which needs them for a spec that doesn't exist yet.

The three skills are manual-only, so run each one by reading its `SKILL.md` and following it exactly, with `$ARGUMENTS` set as below. Its file is the only source of truth: don't add, skip or reorder steps.
A skill's first lines may hold `` !`git …` `` commands. These aren't run automatically when you read the file, so run them yourself first and use their output as the state.

## Steps

1. **New spec:** follow `.claude/skills/new-spec/SKILL.md` with `<service> <NN> [slug] ["title"]`.
2. **Gate one:** show me the completed spec and ask whether it is buildable. On anything but yes, stop.
3. **Start spec:** follow `.claude/skills/start-spec/SKILL.md` with `<service> <NN>`.
4. **Gate two:** its plan is the approval point. On anything but approval, stop.
5. **Finish spec:** follow `.claude/skills/finish-spec/SKILL.md` with `<service> <NN>`. The plan from gate two is the approved plan, and its own confirmations (the Definition of Done checks and the merge) still apply.
6. **Refusals:** if any skill refuses or stops, stop immediately. Report which skill it was, which check failed, and what that skill says to do next. Never work around a refusal.
7. **Close-out:** print /finish-spec's report.

## When not to use this

Run the three skills by hand instead when:
- the spec introduces a pattern this repository has not built before;
- the spec touches more than one service;
- the spec's Definition of Done contains a check nobody can actually run.
