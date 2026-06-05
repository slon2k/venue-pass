# Architecture Outline

## System Purpose

<!-- One paragraph: what does this system do and why does it exist? -->

## Tech Stack

| Concern | Choice |
|---------|--------|
| Runtime | |
| API | |
| Persistence | |
| Messaging | |
| Testing | |
| Hosting | |

## Module Map

<!-- List each module with ownership. Use a diagram if helpful. -->

| Module | Source of truth for | Key concepts |
|--------|---------------------|--------------|
| | | |

## Project Structure

<!-- Top-level folder layout and conventions (feature-first, aggregate-first, etc.) -->

## Communication Model

<!-- How do modules/components interact? Cover:
     - Inside a module (direct calls, domain events)
     - Across modules (integration events, sync contracts)
     - Consistency model (immediate vs eventual) -->

## Boundary Rules

<!-- Non-negotiable constraints that prevent coupling.
     These should be enforceable (architecture tests, code review). -->

1. Rule 1
2. Rule 2

## Persistence Model

<!-- DB topology, schema strategy, migration ownership -->

| Property | Value |
|----------|-------|
| Database | |
| Schema strategy | |
| DbContext | |
| Migrations | |

## Integration Points

<!-- External systems, APIs, or services this project depends on -->

## Deployment Assumptions

<!-- Where and how will this run? CI/CD expectations -->

## Risks and Trade-offs

<!-- Known architectural risks and the trade-offs accepted -->

## ADR Candidates

<!-- Non-obvious decisions worth recording. Each becomes a separate ADR. -->

- [ ] ADR: (decision title)
