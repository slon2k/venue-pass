# VenuePass — Ubiquitous Language

This document defines the preferred vocabulary for VenuePass.

Its purpose is to keep naming consistent across:

- domain model
- APIs
- integration events
- database tables
- tests
- documentation

## Quick Glossary (Top 10)

| Term | Short meaning |
|---|---|
| `Event` | A scheduled occurrence at a venue based on one `Manifest`. |
| `ManifestTemplate` | Reusable venue layout definition. |
| `Manifest` | Event-specific structural snapshot derived from a template. |
| `GeneralAdmissionArea` | Unassigned access area in the event structure. |
| `Inventory` | Ticketing-owned sellable availability derived from `Manifest`. |
| `GeneralAdmissionPool` | Quantity-based ticketing capacity for a GA area. |
| `Offer` | Sales configuration for a published event. |
| `Reservation` | Temporary hold before purchase confirmation. |
| `Order` | Confirmed purchase after checkout/payment. |
| `Ticket` | Single admission entitlement; normally one successful `CheckIn`. |

## Core naming principles

1. **Prefer short, clear domain names**
2. **Use module context to provide meaning**
3. **Do not share business entities across module boundaries**
4. **Use more explicit names at boundaries if needed**
5. **Avoid synonyms for the same concept**

---

## General rules

### Avoid these names
- `Booking` as a generic catch-all
- `GA seat` as a core domain term

### Abbreviation rule
- In code, prefer `GeneralAdmission` over `GA` in type names
- `GA` is acceptable in:
  - UI text
  - comments
  - docs
  - enum labels if already obvious

Examples:
- good: `GeneralAdmissionArea`
- good: `TicketType.GeneralAdmission`
- acceptable in UI: `"GA Ticket"`
- avoid in core type names: `GaSeat`, `GaPool`, `GaInv`

---

## Cross-Module Terms

| Term | Owner | Meaning |
|---|---|---|
| Event | Events | A scheduled occurrence at a venue, based on a specific manifest |
| Published Event | Events | An event that is structurally finalized and eligible for ticketing |
| Ticket | Ticketing | A single admission entitlement issued after purchase |
| Check-In | Attendance | A successful admission action for a ticket |
| User | Identity | A person authenticated in the system |
| Role | Identity | An authorization grouping such as Admin, EventManager, Customer |

---

## Events Module Vocabulary

### Venue
**Meaning:** A physical place where events happen.

**Contains / relates to:**
- one or more `ManifestTemplate`s
- identifying and descriptive venue data

**Do not use as:**
- a ticketing concept
- a substitute for `Manifest`

---

### ManifestTemplate
**Meaning:** A reusable structural layout definition for a venue.

**May contain:**

- seated sections
- rows
- seats
- general admission areas

**Important rule:**  
Changes to a `ManifestTemplate` do **not** affect already-created `Manifest`s.

**Do not call it:**

- layout
- seating plan template

---

### Manifest

**Meaning:** An event-specific structural snapshot created from a selected `ManifestTemplate`.

**Important rule:**  
A `Manifest` belongs to exactly one `Event`.

**Lifecycle rule:**  
A `Manifest` becomes structurally locked when the event is published.

**Do not call it:**

- inventory
- seating template

---

### Section

**Meaning:** A named area within a manifest.

**Examples:**

- `Front Left`
- `Balcony A`
- `Standing Floor`

**Notes:**

- a seated section may contain rows and seats
- a general admission section may be represented as a `GeneralAdmissionArea`

---

### Row

**Meaning:** A row inside a seated section.

---

### Seat

**Meaning:** A specific physical seat position in the manifest.

**Examples:**

- Section A / Row 4 / Seat 12

**Important rule:**

Only use `Seat` when it is an exact assigned place.

**Do not use for:**

- general admission capacity
- unassigned access

---

### GeneralAdmissionArea

**Meaning:** A manifest-defined area with unassigned admission capacity.

**Examples:**

- Standing Floor
- Lawn
- Open Seating Zone

**Important rule:**  
This is **not** a specific seat.

**Preferred over:**

- `GA seat`
- `GeneralAdmissionSeat`

---

### Event

**Meaning:** A business event scheduled at a venue and backed by a specific `Manifest`.

**Common state examples:**

- `Draft`
- `Published`
- `Canceled`

---

### EventManagerAssignment

**Meaning:** A business assignment linking a user to management responsibility for an event.

---

## Ticketing Module Vocabulary

### PublishedEventReference

**Meaning:** Ticketing’s local reference to an event published by the Events module.

**Purpose:**  
Allows Ticketing to link offers and inventory to an event without owning the full Events domain model.

**Do not treat as:**

- the source of truth for event structure
- a shared Event entity

---

### Inventory

**Meaning:** Ticketing-owned sellable inventory derived from a published `Manifest`.

**Purpose:**  
This is Ticketing’s source of truth for availability.

**Important rule:**  
`Inventory` is created from `Manifest`, but it is not the same thing.

**Do not call it:**

- manifest
- seating map

---

### InventorySeat

**Meaning:** A sellable assigned seat in Ticketing inventory.

**Tracks things like:**

- availability
- reservation state
- sold state

**Important rule:**  
Use this only for exact seats.

---

### GeneralAdmissionPool

**Meaning:** Ticketing-owned pooled admission capacity for a general admission area.

**Tracks things like:**

- total capacity
- reserved quantity
- sold quantity
- available quantity

**Preferred over:**

- `GeneralAdmissionInventory`
- `GA seat`

**Why:**  
It makes quantity-based access clearer and avoids implying exact seating.

---

### Offer

**Meaning:** A commercial sales configuration for a published event.

**Examples of what it may define:**

- sales timing
- visibility
- associated price levels

**Important distinction:**  
An event can be published before sales are open.

---

### PriceLevel

**Meaning:** A pricing category used when selling tickets.

**Examples:**

- Standard
- VIP
- Child
- Early Bird

**Important rule:**  
A `PriceLevel` is not a seat or area. It is a pricing concept.

---

### Reservation

**Meaning:** A temporary hold on inventory before purchase is confirmed.

**Examples:**

- exact assigned seats held for checkout
- GA quantity held for checkout

**Important distinction:**  
A reservation is **not** a completed purchase.

**Do not call it:**

- order
- booking

---

### ReservedSeatItem

**Meaning:** A reservation line representing one reserved assigned seat.

**Important rule:**  
Quantity is always `1`.

---

### ReservedGeneralAdmissionItem

**Meaning:** A reservation line representing a held quantity from a `GeneralAdmissionPool`.

---

### Order

**Meaning:** A confirmed purchase created after successful checkout/payment.

**Important distinction:**  
An `Order` is the commercial confirmation; a `Reservation` is only a temporary hold.

---

### Ticket

**Meaning:** A single admission entitlement issued from a confirmed order.

**Important rule:**  
One ticket = one scan/check-in opportunity.

**For GA:**  
A reservation for quantity `N` usually produces `N` tickets.

---

### TicketType

**Preferred values:**

- `AssignedSeat`
- `GeneralAdmission`

**Do not use:**

- `Seated`
- `GASeat`

---

### Assigned Seat Ticket

**Meaning:** A ticket tied to an exact seat.

**Fields often present:**

- section
- row
- seat

---

### General Admission Ticket

**Meaning:** A ticket granting access to an unassigned admission area.

**Important note:**  
In docs/UI, calling this a **GA ticket** is fine.

**Do not call it:**

- GA seat
- unassigned seat ticket

---

## Attendance Module Vocabulary

### ScanAttempt

**Meaning:** An attempt to validate a ticket at entry, successful or not.

**Can represent:**

- successful check-in
- duplicate scan
- invalid ticket
- canceled ticket

---

### CheckIn

**Meaning:** A successful admission of a valid ticket.

**Important rule:**  
A ticket should normally have at most one successful check-in.

---

### AttendanceRecord

**Meaning:** Attendance-owned record of admission status for reporting or lookup.

**Purpose:**  
Represents the attendance outcome, not ticket ownership.

---

## Identity Module Vocabulary

### User

**Meaning:** An authenticated actor in the system.

**Examples:**

- Admin
- EventManager
- Customer
- Staff

---

### Role

**Meaning:** A named authorization grouping applied to users.

**Examples:**

- `Admin`
- `EventManager`
- `Customer`
- `DoorStaff`

---

### Permission

**Meaning:** A finer-grained authorization capability, if needed.

**Note:**  
For MVP, role-based checks may be enough.

---

## Important Distinctions

### ManifestTemplate vs Manifest

- `ManifestTemplate` = reusable structural definition
- `Manifest` = event-specific structural snapshot

---

### Manifest vs Inventory

- `Manifest` = structural truth in Events
- `Inventory` = sellable availability truth in Ticketing

---

### Reservation vs Order

- `Reservation` = temporary hold
- `Order` = confirmed purchase

---

### Ticket vs Check-In

- `Ticket` = entitlement to attend
- `CheckIn` = successful admission event

---

### Published Event vs Sales Open

- `Published Event` = Events lifecycle state
- `Sales Open` = Ticketing/Offer state

These are not the same thing.

---

## Naming Conventions for APIs and Features

### Commands / features

Prefer:

- `CreateVenue`
- `CreateManifestTemplate`
- `CreateEvent`
- `PublishEvent`
- `CreateOffer`
- `ConfigurePricing`
- `ReserveSeats`
- `ReserveGeneralAdmission`
- `ConfirmPurchase`
- `CancelTicket`
- `ScanTicket`

Avoid vague names like:

- `Book`
- `ProcessEvent`
- `HandleInventory`
- `DoCheckout`

---

### Query names

Prefer:

- `GetVenue`
- `GetManifestTemplate`
- `GetEvent`
- `GetOffer`
- `GetInventoryStatus`
- `GetOrder`
- `GetTicket`
- `GetAttendanceRecord`

---

## Naming Conventions for Integration Events

Prefer past-tense business facts:

- `EventPublished`
- `TicketIssued`
- `TicketCanceled`
- `TicketCheckedIn`

Avoid:

- `PublishEventMessage`
- `OnTicketIssue`
- `TicketCheckInHappenedEvent`

---

## Terms We Intentionally Do Not Use

| Avoid | Use instead | Why |
|---|---|---|
| `EventManifest` | `Manifest` | Module context already makes meaning clear |
| `InventorySnapshot` | `Inventory` | Shorter, still clear |
| `GA seat` | `GeneralAdmissionArea`, `GeneralAdmissionPool`, `GeneralAdmissionTicket` | GA is not necessarily a seat |
| `Booking` | `Reservation` or `Order` | Too ambiguous |
| `Layout` | `ManifestTemplate` or `Manifest` | Too vague |
| `Stock` | `Inventory` / `GeneralAdmissionPool` | Less domain-appropriate here |

---

## Recommended Final Vocabulary Set

### Events

- `Venue`
- `ManifestTemplate`
- `Manifest`
- `Section`
- `Row`
- `Seat`
- `GeneralAdmissionArea`
- `Event`
- `EventManagerAssignment`

### Ticketing

- `PublishedEventReference`
- `Inventory`
- `InventorySeat`
- `GeneralAdmissionPool`
- `Offer`
- `PriceLevel`
- `Reservation`
- `ReservedSeatItem`
- `ReservedGeneralAdmissionItem`
- `Order`
- `Ticket`
- `TicketType`

### Attendance

- `ScanAttempt`
- `CheckIn`
- `AttendanceRecord`

### Identity

- `User`
- `Role`
- `Permission`

---

## Final Rule of Thumb

> Use `Seat` only for an exact assigned place.  
> Use `GeneralAdmission` for pooled or unassigned access.  
> Use `Manifest` for event structure.  
> Use `Inventory` for ticketing availability.