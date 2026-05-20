---
name: feature-plan
description: "Decompose a feature into acceptance criteria and tasks. Use when: preparing a feature issue, splitting work, identifying risks."
agent: agent
---

Create a feature execution plan.

## Inputs

- milestone_file (file path): ${{ input:milestone_file }}
- feature_title (string): ${{ input:feature_title }}

## Workflow

Before producing output, interview the user about the feature:
- Ask questions **one at a time**, providing your recommended answer for each.
- If a question can be answered by exploring the codebase or milestone docs, explore instead of asking.
- Cover at minimum: scope boundaries, acceptance criteria, edge cases, risks, and dependencies.
- Continue until you have shared understanding on all key decisions.

## Output

Produce markdown suitable for a feature issue body:

1. Summary
2. Scope
3. Acceptance criteria
4. Tasks (checklist and child issue split)
5. Risks and assumptions
6. Definition of done

Use the split rule:
- less than half-day task: checklist item
- half-day or more or separate PR: child issue

## Vertical slice rule

Decompose tasks as **vertical slices** — each task cuts through all layers end-to-end (schema, API, UI, tests), not one layer at a time.

- WRONG: "Create database tables" → "Build API endpoints" → "Build UI"
- RIGHT: "User can register (schema + API + UI + test)" → "User can log in (schema + API + UI + test)"

A completed slice must be independently demoable or verifiable.
