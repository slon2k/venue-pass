# VenuePass — Delivery Plan

## Delivery Model

- Solo developer, sequential slices
- Each slice is a vertical increment: domain → persistence → endpoints → tests
- A slice is **done** when its acceptance criteria pass and architecture tests still pass
- Supporting docs (API contracts, outbox behavior, auth rules) are written just-in-time per slice

## CI Maturity Plan

### S0-S2: Baseline CI (required)

- Workflow: `fast-ci`
- Trigger: every pull request and push to `main`
- Steps:
  - restore
  - build (warnings as errors for CI)
  - architecture tests
  - unit tests
- Goal: fast feedback and boundary protection from day one

### S3-S5: Expanded CI (required)

- Keep `fast-ci` required on all PRs
- Add workflow: `integration-ci`
- Trigger:
  - pull requests with integration-impact label (optional during transition)
  - push to `main`
- Steps:
  - integration tests with SQL Server service container
  - migration smoke check (`database update` on clean DB)
  - outbox dispatch/integration tests
- Goal: verify persistence + integration behavior in CI, not only locally

### S6-S7: Release CI/CD (optional initially)

- Keep `fast-ci` and `integration-ci`
- Optionally add:
  - Docker image build
  - artifact publish
  - demo environment deployment workflow (manual approval)
- Goal: make demo/release repeatable without overloading early development

### Branch protection policy

- S0-S2: require `fast-ci`
- S3-S5: require `fast-ci`; phase in required `integration-ci` when integration suite is stable
- S6-S7: require both `fast-ci` and `integration-ci`; keep release workflow approval-gated

---

## Slice Order

```text
S0: Project Scaffolding
 │
 ▼
S1: Events — Venue & Manifest Setup
 │
 ▼
S2: Events — Event Lifecycle
 │
 ▼
S3: Ticketing — Offers & Inventory
 │
 ▼
S4: Ticketing — Reservation & Purchase
 │
 ▼
S5: Attendance — Check-In
 │
 ▼
S6: Identity — Custom Auth
 │
 ▼
S7: Integration Hardening
```

---

## S0: Project Scaffolding

**Goal:** Runnable empty solution with module boundaries enforced.

**Scope:**

- Create solution file (.slnx) and project structure
- VenuePass.Api with basic Program.cs (Minimal API host)
- VenuePass.BuildingBlocks with small technical abstractions only:
  - Entity
  - ValueObject
  - Result
  - DomainEvent
  - IntegrationEvent
- Four empty module projects with ModuleConfiguration stubs
- Docker Compose with SQL Server
- Architecture test project with initial boundary rules
- `dotnet user-jwts` configured for dev tokens
- global.json, Directory.Build.props, Directory.Packages.props
- Initial CI workflow (baseline): restore, build, architecture tests, unit tests

**Prerequisites:** None

**Acceptance criteria:**

- [ ] `dotnet build` succeeds with zero warnings
- [ ] `docker compose up` starts SQL Server and it accepts connections
- [ ] Architecture tests pass:
  - no dependency on another module's internals
  - domain has no infrastructure dependencies
- [ ] API host starts and returns 200 on a health endpoint
- [ ] JWT authentication middleware validates `dotnet user-jwts` tokens
- [ ] Baseline CI workflow runs successfully on PR

**Exit criteria:** Solution compiles, runs, and boundary rules are enforced.

---

## S1: Events — Venue & Manifest Setup

**Goal:** Create venues and define manifest templates with sections, seats, and GA areas.

**Scope:**

- Domain:
  - Venue
  - ManifestTemplate
  - Section
  - Row
  - SeatDefinition
  - GeneralAdmissionArea
- Persistence:
  - EventsDbContext
  - `events` schema
  - migrations
  - entity configurations
- Features:
  - CreateVenue
  - GetVenue
  - CreateManifestTemplate
  - GetManifestTemplate
- Outbox table in `events` schema (infrastructure ready, no events published yet)
- Unit tests for domain invariants
- Integration tests for persistence

**Prerequisites:** S0 complete

**Acceptance criteria:**

- [ ] Can create a venue via API and retrieve it
- [ ] Can create a manifest template with mixed seating (assigned + GA) and retrieve it
- [ ] Venue and manifest data persists in `events` schema
- [ ] Domain enforces:
  - venue requires a name
  - manifest requires at least one sellable section/area
- [ ] Architecture tests still pass

**Exit criteria:** Venue and manifest setup works end-to-end with persistence.

---

## S2: Events — Event Lifecycle

**Goal:** Create events, assign manifests, publish events, and emit integration events.

**Scope:**

- Domain:
  - Event (lifecycle: Draft → Published → Canceled)
  - Manifest (snapshot created from selected ManifestTemplate)
  - EventManagerAssignment
- Features:
  - CreateEvent (assigns venue and creates Manifest from chosen ManifestTemplate)
  - AssignEventManager
  - PublishEvent (locks Manifest, writes `EventPublished` to Outbox)
  - GetEvent
- Outbox dispatcher (background service) — first real usage
- Integration event: `EventPublished(EventId, ManifestId)`
- Unit tests for event lifecycle rules
- Integration tests for Outbox write + dispatch

**Prerequisites:** S1 complete

**Acceptance criteria:**

- [ ] Can create an event with venue + manifest template
- [ ] Creating an event creates a Manifest snapshot
- [ ] Can assign event manager (requires EventManager role claim)
- [ ] Can publish event → state transitions to Published
- [ ] Cannot publish event without a Manifest
- [ ] Publishing writes `EventPublished(EventId, ManifestId)` to Outbox
- [ ] Background dispatcher picks up and dispatches the event (observable in logs/tests)
- [ ] Structural changes to Manifest are rejected after publication
- [ ] Architecture tests still pass
- [ ] Baseline CI remains green (restore/build/architecture/unit)

**Exit criteria:** Full event lifecycle works and the Outbox dispatcher is operational.

---

## S3: Ticketing — Offers & Inventory

**Goal:** Create ticketing inventory for published events and configure sellable offers.

**Scope:**

- Domain:
  - PublishedEventReference (local ticketing view of published event)
  - Offer
  - PriceLevel
  - Inventory
  - InventorySeat
  - GeneralAdmissionPool
- Persistence:
  - TicketingDbContext
  - `ticketing` schema
  - migrations
- Features:
  - CreateOffer
  - ConfigurePricing
  - GetOffer
  - GetInventoryStatus
- Contract: `IEventsModule.GetManifestForTicketingAsync(...)`
- Integration subscriber:
  - handles `EventPublished`
  - stores local published event reference
  - fetches manifest export from Events
  - creates Inventory
- Unit tests for offer/pricing invariants
- Integration tests for inventory snapshot creation

**Prerequisites:** S2 complete (EventPublished flowing, Outbox dispatcher operational)

**Acceptance criteria:**

- [ ] Ticketing receives `EventPublished`
- [ ] Ticketing stores a local published event reference
- [ ] Ticketing fetches manifest data through the Events contract
- [ ] Ticketing creates Inventory automatically from the published event manifest
- [ ] Inventory correctly mirrors the Manifest structure (seats + GA quantities)
- [ ] Can create an offer for a published event
- [ ] Can define price levels for an offer
- [ ] Cannot create an offer for an unpublished event
- [ ] Architecture tests still pass:
  - Ticketing does not depend on Events internals
  - only public contracts may be used across the boundary
- [ ] Expanded CI runs integration tests with SQL Server service container
- [ ] Expanded CI runs migration smoke check successfully

**Exit criteria:** Sellable inventory exists and Ticketing is ready for reservations.

---

## S4: Ticketing — Reservation & Purchase

**Goal:** Reserve seats/GA, complete purchase, issue tickets, and support cancellation.

**Scope:**

- Domain:
  - Reservation
  - ReservedSeatItem
  - ReservedGeneralAdmissionItem
  - Order
  - Ticket
  - inventory concurrency rules
- Features:
  - ReserveSeats
  - ReserveGeneralAdmission
  - ConfirmPurchase
  - GetOrder
  - GetTicket
  - CancelTicket
- Integration events: `TicketIssued`, `TicketCanceled`
- Reservation expiry: background check or on-access validation
- Unit tests for:
  - reservation rules
  - purchase flow
  - inventory concurrency
- Integration tests for full purchase flow

**Prerequisites:** S3 complete

**Acceptance criteria:**

- [ ] Can reserve specific seats or GA quantity
- [ ] Reserved inventory is no longer available to others
- [ ] Can confirm purchase → order created, tickets issued
- [ ] Mock payment succeeds by default
- [ ] Tests can simulate payment failure → reservation is released
- [ ] `TicketIssued` integration event is written to Outbox
- [ ] Cannot reserve already-sold seats
- [ ] Expired reservations release inventory
- [ ] Can cancel an issued ticket
- [ ] `TicketCanceled` integration event is written to Outbox
- [ ] Architecture tests still pass
- [ ] Expanded CI includes Outbox integration tests and remains green

**Exit criteria:** Complete purchase flow works; tickets are issued and cancellation is supported.

---

## S5: Attendance — Check-In

**Goal:** Scan tickets, validate eligibility, record check-in, and reject duplicates.

**Scope:**

- Domain:
  - CheckIn
  - ScanAttempt
  - AttendanceRecord
- Persistence:
  - AttendanceDbContext
  - `attendance` schema
  - migrations
- Features:
  - ScanTicket
  - GetAttendanceRecord
- Contract: `ITicketingModule.GetTicketForValidationAsync()`
- Integration subscribers:
  - `TicketIssued` → update local ticket projection
  - `TicketCanceled` → mark local ticket projection invalid
- Integration event: `TicketCheckedIn`
- Unit tests for check-in rules:
  - valid ticket
  - duplicate ticket
  - canceled ticket
- Integration tests for full scan flow

**Prerequisites:** S4 complete (`TicketIssued` and `TicketCanceled` flowing)

**Important rule:**

- Ticketing remains the source of truth for ticket validity
- Attendance owns duplicate-check prevention and check-in records
- Ticket scans use a fresh validation call to Ticketing
- Local ticket projection is a read model, not the authority for validation

**Acceptance criteria:**

- [ ] Attendance receives `TicketIssued` and stores/updates local ticket projection
- [ ] Attendance receives `TicketCanceled` and updates local ticket projection
- [ ] Can scan a valid ticket → check-in recorded, success returned
- [ ] Scanning the same ticket again → rejected as duplicate
- [ ] Scanning a canceled ticket → rejected as invalid
- [ ] Scan flow validates current status through the Ticketing contract
- [ ] `TicketCheckedIn` integration event is written to Outbox
- [ ] Architecture tests still pass
- [ ] Expanded CI validates integration tests for TicketIssued/TicketCanceled consumer paths

**Exit criteria:** Full admission flow works end-to-end from ticket issuance to check-in.

---

## S6: Identity — Custom Auth

**Goal:** Replace `dotnet user-jwts` with a real Identity module that issues tokens.

**Scope:**

- Domain:
  - User
  - Role
  - Permission
- Persistence:
  - IdentityDbContext
  - `identity` schema
  - migrations
- Features:
  - Register
  - Login (returns JWT with role claims)
  - GetCurrentUser
- Seed data:
  - Admin
  - EventManager
  - Customer
- Update module tests to use Identity-issued tokens
- Optional contract: IIdentityModule for current-user lookups if needed

**Prerequisites:** S5 complete (all other modules work with stub tokens)

**Acceptance criteria:**

- [ ] Can register a new user
- [ ] Can login and receive a valid JWT
- [ ] JWT contains correct `sub` and `role` claims
- [ ] All existing endpoints still work with Identity-issued tokens
- [ ] Invalid/expired tokens are rejected
- [ ] Seed script creates default test users
- [ ] Architecture tests still pass

**Exit criteria:** Self-contained Identity module replaces dev-token stubs.

---

## S7: Integration Hardening

**Goal:** Ensure the full system is robust, testable, and demo-ready.

**Scope:**

- End-to-end tests for primary flows:
  - event setup → inventory creation → offer → purchase → check-in
  - duplicate check-in rejection
  - canceled ticket rejection
- Outbox reliability:
  - retry behavior for failed dispatches
  - poison message handling (log + skip after N retries)
  - idempotency verification
- API documentation: OpenAPI generation and verification
- README with setup instructions
- Docker Compose infrastructure validation
- Seed script validation
- Optional release automation:
  - Docker image build
  - artifact publish
  - demo deployment workflow (manual approval)

**Prerequisites:** S6 complete

**Acceptance criteria:**

- [ ] Full happy-path E2E test passes
- [ ] Duplicate check-in rejection E2E test passes
- [ ] Canceled ticket rejection E2E test passes
- [ ] Outbox retries failed messages up to configured limit
- [ ] Poison messages are logged and skipped
- [ ] Duplicate event delivery does not corrupt state
- [ ] OpenAPI spec is generated and accessible at `/swagger`
- [ ] `docker compose up` starts required infrastructure
- [ ] App runs locally against the started infrastructure
- [ ] Seed script prepares the system for demo
- [ ] All architecture tests pass
- [ ] README documents setup, run, test, seed, and demo steps
- [ ] Optional release workflow can build and publish versioned demo artifacts

**Exit criteria:** Robust demo application, fully tested and documented.

---

## Slice Dependency Graph

```text
S0 ─► S1 ─► S2 ─► S3 ─► S4 ─► S5 ─► S6 ─► S7
                    │
                    └──► Outbox operational from S2 onward
```

All slices are sequential. No parallelism is required.

---

## Supporting Docs (Created Per Slice)

| Doc | Created during |
|-----|----------------|
| API contracts — Events | S1–S2 |
| API contracts — Ticketing | S3–S4 |
| API contracts — Attendance | S5 |
| API contracts — Identity | S6 |
| Outbox operational spec | S2 |
| Authorization rules | S2 (EventManager) + S6 (full) |
| Test strategy details | S0 (architecture tests) + S7 (E2E) |
| Persistence/migration playbook | S0–S1 |

---

## Final Notes

**Delivery principles:**

- Keep slices thin but complete
- Prefer working end-to-end behavior over premature completeness
- Add complexity only when a later slice truly needs it
- Keep architecture tests active from the start
