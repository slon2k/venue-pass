# VenuePass — Architecture Outline

## Context and Assumptions

- VenuePass is a .NET modular monolith used as a learning and reference project.
- Team model is solo development with incremental vertical slices.
- Architecture prioritizes explicit boundaries, traceable behavior, and testability over framework complexity.
- Consistency is immediate inside a module and eventual across modules.

## Components

| Component | Responsibility |
|-----------|---------------|
| API Host | HTTP routing, middleware, auth pipeline, endpoint registration |
| Events Module | Source of truth for event structure and manifests |
| Ticketing Module | Inventory, sales offers, reservations, orders, tickets |
| Attendance Module | Admission/check-in decisions and attendance records |
| Identity Module | Users, roles/permissions, token issuance |
| Building Blocks | Shared primitives, result contracts, messaging abstractions |
| Outbox Dispatcher | Reliable asynchronous delivery of integration events |

## Data Flow

1. Request enters API endpoint.
2. Endpoint maps transport model to command/query and invokes a feature handler.
3. Handler applies domain rules and persists module-owned state.
4. If cross-module reaction is needed, handler writes integration event to module Outbox in the same transaction.
5. Background dispatcher publishes pending integration events.
6. Consumer module handles event asynchronously and updates its own local model.
7. When needed, consumer performs synchronous read/validation via owning module contract.

## Integration Points

- Internal integration is module-to-module via contracts (sync reads) and integration events (async reactions).
- Persistence integration is SQL Server, schema-per-module.
- Authentication integration is JWT-based with a phased identity approach.
- Local runtime integration uses Docker Compose for SQL Server.

## Deployment Assumptions

- Single deployable application process for API + modules.
- SQL Server runs as local container for development.
- CI baseline enforces restore, build, architecture tests, and unit tests.
- No distributed deployment or distributed transactions in the initial scope.

## Risks and Trade-offs

- Strong boundaries in one codebase require continuous architecture-test discipline.
- Eventual consistency introduces short-lived lag between module views.
- Hand-rolled Outbox gives control and learning value, but increases reliability ownership.
- Modular monolith simplifies delivery versus microservices, at the cost of independent deployability.

## ADR Candidates

- Dispatch strategy for use-case handlers.
- DbContext and schema isolation model per module.
- Outbox reliability model and delivery guarantees.
- Synchronous contract-call policy across modules.
- Notify-and-fetch synchronization pattern.
- Module-internal structure conventions (feature-first, aggregate-first).
