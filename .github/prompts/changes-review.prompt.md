---
name: changes-review
description: "Review recent changes for bugs, regressions, security risks, and missing tests. Use when: validating implementation before merge."
agent: agent
---

Review recent changes with a risk-first approach.

## Inputs

- change_scope (string — e.g. "git diff", file list, or "latest commit"): ${{ input:change_scope }}

## Workflow

1. Identify the changes to review based on the change scope.
2. Run the project's build and test suite to detect regressions.
3. Inspect each change for bugs, security risks, missing tests, and regressions.
4. Classify findings by severity.

## Output

List findings by severity:

1. Critical
2. High
3. Medium
4. Low

For each finding include:
- file or area
- issue
- potential impact
- recommended fix

If no findings, state that clearly and list residual risks.
