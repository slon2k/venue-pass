---
name: changes-document
description: "Update README, docs, and ADR references to match implemented behavior. Use when: behavior or architecture changed."
agent: agent
---

Update project documentation to match implemented behavior.

## Inputs

- change_summary (string — bullet list or paragraph): ${{ input:change_summary }}

## Workflow

Review and update as needed:

1. README
2. docs/architecture*
3. docs/operations*
4. ADR references

## Output

Return:

1. What docs were updated
2. What changed in each doc
3. Any docs still pending
