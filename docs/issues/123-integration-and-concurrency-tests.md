# Capability E: Integration and concurrency tests

## Summary

Add the integration and concurrency test coverage needed to prove the M04 ticketing slice works end to end: reservation flows, rejection paths, expiration and cancellation behavior, checkout with ticket issuance, oversell protection, and authorization enforcement.

## Scope

- In scope:
  - Reservation flow tests for active offers and priced seats / GA pools
  - Reservation rejection tests for unavailable targets, inactive offers, and sale-window violations
  - Expiration and cancellation tests that release inventory
  - Checkout tests that create orders, mark inventory sold, and issue tickets
  - Concurrency tests that prevent double booking / oversell
  - End-to-end coverage from published event through ticket issuance
  - Authorization tests across the new Ticketing endpoints
- Out of scope:
  - Production-grade scheduling and distributed retry behavior
  - Ticket check-in / admission lifecycle
  - Refunds, order cancellation, and payment integration

## Acceptance Criteria

- Reservation tests cover successful seat and GA pool reservation from an active offer.
- Rejection tests cover unavailable inventory, inactive offers, and invalid sale windows.
- Expiration and cancellation tests confirm inventory is released correctly.
- Checkout tests confirm exactly one order is created and tickets are issued.
- Concurrency tests demonstrate that duplicate reservation attempts do not oversell inventory.
- End-to-end tests cover publish event -> inventory -> offer -> reservation -> order -> tickets.
- Authorization tests verify the new customer-facing ticketing endpoints require authentication.

## Test Matrix

- Reservation creation success
- Reservation rejection paths
- Reservation expiration and cancellation
- Checkout order creation and ticket issuance
- Idempotent checkout behavior
- Double-reservation / oversell protection
- End-to-end ticketing flow
- Authentication and authorization enforcement

## Notes

- Prefer integration tests that exercise the real module wiring and persistence layer.
- Keep concurrency tests deterministic and scoped to the specific reservation / inventory rows involved.
- Reuse existing seed helpers and test fixtures where possible.