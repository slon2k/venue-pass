---
name: milestone-plan
description: "Detail the active milestone scope and exit criteria. Use when: preparing current release slice, selecting features, defining out-of-scope."
agent: agent
---

Define the active milestone in detail.

## Inputs

- roadmap_file (file path): ${{ input:roadmap_file }}
- milestone_name (string): ${{ input:milestone_name }}

## Workflow

Before producing output, interview the user about milestone scope:
- Ask questions **one at a time**, providing your recommended answer for each.
- If a question can be answered by exploring the codebase or roadmap, explore instead of asking.
- Cover at minimum: what's in vs out of scope, exit criteria, risks, and dependencies.
- Continue until you have shared understanding on all key decisions.

## Output

Produce markdown suitable for `docs/milestones/${{ input:milestone_name }}.md`:

1. Milestone outcome
2. In-scope features
3. Out-of-scope list
4. Milestone definition of done
5. Validation checklist
6. Risks and dependencies

This milestone should be actionable without decomposing every future milestone.

## Vertical slice rule

Scope features as **vertical slices** — each feature cuts through all layers end-to-end, not one layer at a time.

- WRONG: "Set up database" → "Build all APIs" → "Build all UI"
- RIGHT: "User registration (end-to-end)" → "User login (end-to-end)"

Each feature in the milestone must be independently demoable or verifiable.
