---
name: architecture-plan
description: "Draft high-level architecture from project scope. Use when: selecting architecture, defining components, documenting integration boundaries."
agent: agent
---

Draft or update a high-level architecture plan.

## Inputs

- scope_file (file path): ${{ input:scope_file }}
- architecture_constraints (string — comma-separated or paragraph): ${{ input:architecture_constraints }}

## Workflow

Before producing output, interview the user about architecture decisions:
- Ask questions **one at a time**, providing your recommended answer for each.
- If a question can be answered by exploring the codebase or project docs, explore instead of asking.
- Cover at minimum: component boundaries, data flow, integration points, trade-offs, and deployment model.
- Continue until you have shared understanding on all key decisions.

## Output

Produce markdown suitable for `docs/architecture-outline.md` with:

1. Context and assumptions
2. High-level components
3. Data flow
4. Integration points
5. Deployment assumptions
6. Risks and trade-offs
7. Candidate ADRs

Do not over-specify implementation details.
