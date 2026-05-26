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
- docs/milestones/milestone-xx.md: per-milestone source of truth for scope, slices, and completion state
- docs/tech-decisions.md: ADR-style technical decisions

## GitHub Project Structure

Create one repository-level GitHub Project with the following fields:

- Status: Todo, In progress, Done
- Milestone: 00 - Foundation: Project Scaffolding, 01 - Events: Venues & Manifest Templates, 02 - Events: Event Creation & Publication, 03 - Ticketing: Event Sync, Inventory & Offers, 04 - Ticketing: Reservation, Orders & Ticket Issuance, 05 - Attendance: Check-In, 06 - Identity: Users, Roles & Authentication, 07 - Integration & Operational Hardening
- Module: Events, Ticketing, Attendance, Identity, Cross-Cutting
- Slice Type: Domain, Persistence, Endpoint, Integration, Test, Docs

Use labels instead of extra fields for secondary tracking:

- needs-adr
- docs-update
- architecture-impact

## Planning Levels

Use the following planning levels consistently:

1. Milestone = delivery phase
2. Parent issue = feature (capability issue)
3. Sub-issue = vertical slice

Definitions:

- A milestone groups related work into a delivery phase with a clear outcome.
- A parent issue describes a meaningful feature/capability the system should gain.
- A sub-issue describes one concrete vertical slice that moves that capability forward.

Examples:

Good parent issues:

- Venue management
- Manifest template management

Good sub-issues:

- Create venue
- Get venue
- Create manifest template
- Get manifest template

Less ideal sub-issues:

- Add DbContext
- Add EF config
- Add endpoint

Those technical tasks are usually implementation checklist items inside a vertical slice, not standalone planning items.

## Work Item Hierarchy

Model work in this hierarchy:

1. Milestone issue (delivery-phase level)
2. Parent feature (capability) issue
3. Sub-issues as vertical slices

Rules:

- Each milestone in docs/roadmap.md has one milestone issue.
- Each in-scope item in the active `docs/milestones/milestone-xx.md` maps to one or more parent feature (capability) issues.
- Each capability is decomposed into small, user-meaningful vertical slices.
- Avoid decomposing planning into layer-only issues unless the work is genuinely cross-cutting and independently valuable.

Use standalone technical issues only when the work unlocks multiple capabilities or applies across modules.

Examples of acceptable standalone technical issues:

- Establish architecture test harness
- Add baseline CI workflow
- Introduce shared outbox dispatcher

## Slice Definition

Each slice should be small, testable, and mergeable in one pull request.

Typical slice path:

1. Domain model change
2. Persistence mapping/migration update
3. Endpoint/handler change
4. Tests
5. Docs update

A slice is done only when acceptance criteria pass and architecture constraints remain intact.

A good slice may include domain, persistence, endpoint, tests, and docs changes together when that is the thinnest path to delivering observable behavior.

## Issue and PR Standards

## Issue template expectations

Each issue should include:

- Outcome statement
- Acceptance criteria
- Module ownership
- Boundary impact note
- Test plan
- Docs to update

Parent feature (capability) issues should also include:

- Capability statement
- In-scope sub-slices
- Explicit out-of-scope notes where useful

Sub-issues should describe the behavior being added, not the implementation layers used to add it.

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
- If milestone scope changes, update the active `docs/milestones/milestone-xx.md` and `docs/roadmap.md` first

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
- active `docs/milestones/milestone-xx.md` updated to closed state
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
- Seed project with 01 - Events: Venues & Manifest Templates milestone issue and in-scope capability issues
- Verify links between board items and planning docs
