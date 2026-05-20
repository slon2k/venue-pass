# VenuePass

VenuePass is a modular monolith demo project for event management, ticketing, attendance, and identity.

## Current Status

Planning and architecture baseline is completed.

## Documentation

- Architecture overview: docs/architecture-overview.md
- Technical decisions: docs/tech-decisions.md
- Delivery plan: docs/delivery-plan.md
- Ubiquitous language: docs/ubiquitous-language.md

## Implementation Approach

- Vertical slices
- One project per module
- Reliable cross-module events with Outbox semantics
- Fast CI first, expanded integration CI as slices progress
