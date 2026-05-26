# Copilot Instructions (Core)

## Workflow baseline

- Plan before implementation.
- Keep scope strict to the current task.
- Prefer small, testable increments.
- Update docs when behavior changes.

## Planning rules

- Start with project-plan and architecture-plan.
- Create roadmap via roadmap-plan.
- Define the active milestone in roadmap and detail it in docs/milestones/milestone-xx.md.
- Decompose each feature before implementation.
- Never reference file paths or line numbers in plans — describe behaviors and contracts.

## Execution rules

- Implement one feature slice at a time.
- Run build and tests before summary output.
- Use changes-review before finalizing work.
- Use changes-document to keep README and docs aligned.

## Milestone policy

- Keep one active milestone fully detailed.
- Keep one upcoming milestone lightly drafted.
- Keep future milestones as one-line placeholders.
- Revisit milestone list in roadmap-review.

---

# dotnet module instructions

These supplement the core copilot-instructions.md.

- Prefer solution-first project organization.
- Keep domain and application boundaries explicit.
- Use xUnit for tests.
- Run dotnet build and dotnet test before final summaries.

