# VenuePass - GitHub Projects Operating Process

## Purpose

This document defines how VenuePass planning docs connect to day-to-day execution in GitHub Projects.

Planning remains document-first. GitHub Projects is the execution and tracking surface.

## Source of Truth

Use these docs as canonical planning artifacts:

- project-plan.md: project scope, goals, and constraints
- docs/architecture-overview.md: full architecture decisions and constraints
- docs/architecture-outline.md: short architecture reference for planning and AI context
- docs/roadmap.md: milestone sequencing (Now, Next, Later)
- docs/milestone-plan.md: active milestone scope and definition of done
- docs/milestones/milestone-01.md: issue seed for current milestone feature and slices
- docs/tech-decisions.md: ADR-style technical decisions

## GitHub Project Structure

Create one repository-level GitHub Project with the following fields:

- Status: Todo, In progress, Done
- Milestone: 01 - Foundation, 02 - Messaging and Ticketing Bootstrap, 03 - Ticketing Sales Flows, 04 - Attendance Check-In Flows, 05 - Identity Module First Cut, 06 - Integration CI Expansion and Migration Smoke Checks
- Module: Events, Ticketing, Attendance, Identity, Cross-Cutting
- Slice Type: Domain, Persistence, Endpoint, Integration, Test, Docs

Use labels instead of extra fields for secondary tracking:

- needs-adr
- docs-update
- architecture-impact

## Work Item Hierarchy

Model work in this hierarchy:

1. Milestone Issue (epic-level)
2. Feature Issue (deliverable-level)
3. Slice Tasks (implementation-level)

Rules:

- Each milestone in docs/roadmap.md has one milestone issue.
- Each item in docs/milestone-plan.md In Scope maps to one or more feature issues.
- Each feature is decomposed into small vertical slices.

## Slice Definition

Each slice should be small, testable, and mergeable in one pull request.

Typical slice path:

1. Domain model change
2. Persistence mapping/migration update
3. Endpoint/handler change
4. Tests
5. Docs update

A slice is done only when acceptance criteria pass and architecture constraints remain intact.

## Issue and PR Standards

## Issue template expectations

Each issue should include:

- Outcome statement
- Acceptance criteria
- Module ownership
- Boundary impact note
- Test plan
- Docs to update

## Pull request expectations

Each PR should include:

- Linked issue
- What changed and why
- Test evidence summary
- Architecture boundary impact statement
- Docs updated confirmation

## Project Automation

Recommended built-in automation:

- Auto-add new issues and PRs to project
- Move item to In progress when work starts
- Move item to Done when PR merges

Recommended manual checks:

- If architecture-impact label is present, decide whether `docs/tech-decisions.md` needs an ADR entry
- If milestone scope changes, update docs/milestone-plan.md and docs/roadmap.md first

## Milestone Governance Cadence

Weekly cadence:

1. Review Now milestone board
2. Confirm risks and blockers
3. Validate milestone scope has not drifted
4. Ensure docs and board are in sync

Milestone close checklist:

- All in-scope items complete
- Build and tests pass
- Architecture constraints still enforced
- docs/milestone-plan.md updated to closed state
- docs/roadmap.md advanced (Next promoted to Now)

## Synchronization Rules

To avoid drift:

- Update planning docs before changing milestone scope on the board.
- Update docs in the same PR when architecture or behavior changes.
- Keep docs/architecture-outline.md short; do not duplicate full detail from docs/architecture-overview.md.

## Suggested Labels

Use labels to simplify project filters:

- module:events
- module:ticketing
- module:attendance
- module:identity
- type:feature
- type:slice
- type:docs
- architecture-impact
- needs-adr
- docs-update

## Minimal Setup Checklist

- Create GitHub Project and custom fields
- Add project automation rules
- Create issue and PR templates
- Seed project with 01 - Foundation milestone issue and in-scope feature issues
- Verify links between board items and planning docs
