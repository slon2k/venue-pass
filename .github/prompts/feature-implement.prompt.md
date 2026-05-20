---
name: feature-implement
description: "Implement a planned feature slice with validation. Use when: coding a feature from a prepared plan, running build/tests, drafting PR summary."
agent: agent
---

Implement one feature slice from a prepared plan.

## Inputs

- feature_reference (file path or issue URL): ${{ input:feature_reference }}
- branch_name (string — kebab-case): ${{ input:branch_name }}

## Workflow

1. Read the feature plan.
2. Create or switch to the target branch.
3. Implement scoped changes only.
4. Run relevant build and tests for the project.
5. Fix issues introduced by this scope.
6. Run changes-review to validate the implementation.
7. Run changes-document to update any affected docs.
8. Draft PR summary only after validation passes.

## Output

Return:

1. Change summary
2. Validation summary
3. PR summary draft
