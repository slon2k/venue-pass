---
name: roadmap-review
description: "Review and adjust roadmap after milestone progress, blockers, or reprioritization. Use when: milestone closes or scope drifts."
agent: agent
---

Review and update roadmap based on actual progress.

## Inputs

- roadmap_file (file path): ${{ input:roadmap_file }}
- completed_work (string — bullet list): ${{ input:completed_work }}
- blockers (string — bullet list or "none"): ${{ input:blockers }}

## Output

Update roadmap with:

1. Completed milestone outcomes
2. Changes in Now, Next, Later
3. Reprioritization decisions
4. New risks and dependencies

Keep policy:
- one active milestone detailed
- one upcoming milestone light
- remaining milestones as placeholders
