---
name: roadmap-plan
description: "Create initial roadmap from scope and architecture. Use when: defining milestones, building Now/Next/Later, setting release direction."
agent: agent
---

Create the initial roadmap.

## Inputs

- scope_file (file path): ${{ input:scope_file }}
- architecture_file (file path): ${{ input:architecture_file }}

## Workflow

Before producing output, interview the user about milestone priorities:
- Ask questions **one at a time**, providing your recommended answer for each.
- If a question can be answered by exploring the codebase or project docs, explore instead of asking.
- Cover at minimum: milestone ordering rationale, dependency sequencing, risk-based prioritization, and Now/Next/Later placement.
- Continue until you have shared understanding on all key decisions.

## Output

Produce markdown suitable for `docs/roadmap.md`:

1. Now (active milestone candidates)
2. Next (upcoming milestone candidates)
3. Later (placeholder milestones)
4. Milestone outcomes
5. Risks and dependencies

Keep one active milestone detailed and one upcoming milestone lightly drafted.
