---
name: project-plan
description: "Define project scope, goals, constraints, and success criteria. Use when: starting a new project, writing scope, shaping first release."
agent: agent
---

Create or update a project scope draft.

## Inputs

- project_name (string): ${{ input:project_name }}
- project_idea (string — short paragraph): ${{ input:project_idea }}
- constraints (string — comma-separated or paragraph): ${{ input:constraints }}

## Workflow

Before producing output, interview the user about the project idea:
- Ask questions **one at a time**, providing your recommended answer for each.
- If a question can be answered by exploring the codebase or docs, explore instead of asking.
- Cover at minimum: goals, constraints, scope boundaries, target users, risks, and unknowns.
- Continue until you have shared understanding on all key decisions.

## Output

Produce markdown suitable for `docs/project-scope.md` with these sections:

1. Problem statement
2. Goals
3. Non-goals
4. Constraints
5. First release scope
6. Success criteria
7. Open questions

Keep scope practical for a first milestone.
