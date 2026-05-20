# 01 - Foundation First Feature: Solution Scaffolding and Module Boundaries

## Feature Issue Template

**Title:** feat: Solution scaffolding and module boundaries (01 - Foundation)

**Description:**

### Outcome
The solution project structure is complete with four module projects and shared building blocks, module boundaries are enforced via architecture tests, and the first Events vertical slice structure is ready.

### Acceptance Criteria
- [ ] .slnx solution file created with all projects added
- [ ] Four module projects created: Events, Ticketing, Attendance, Identity
- [ ] Shared BuildingBlocks project created
- [ ] Each module has correct internal folder structure (Features, Domain, Infrastructure, Contracts)
- [ ] Module boundaries are enforced via architecture tests
- [ ] CI baseline pipeline passes (restore, build, architecture tests, unit tests)
- [ ] First Events slice has domain, persistence, and endpoint scaffolding in place
- [ ] Docs aligned with implemented structure

### Module
Events (primary)

### Milestone
01 - Foundation

### Boundary Impact
Establishes module isolation rules enforced by architecture tests; critical for entire project.

### Test Plan
- Architecture tests:
  - No module accesses another module's tables
  - No shared business entities across modules
  - Dependency direction enforced (Features ← Infrastructure ← Domain)
- Unit tests: BuildingBlocks primitives and shared types
- Integration tests: First Events slice can read/write to Events schema

### Docs to Update
- [x] docs/milestone-plan.md (in progress tracking)
- [x] docs/roadmap.md (if scope changes)
- [x] docs/architecture-overview.md (reflects implementation)
- [x] project-plan.md (track progress)

### Labels
- type:feature
- module:cross-cutting
- milestone:01-foundation

---

## Slice Task Issues (6 tasks)

### Slice 1: Create solution structure and projects

**Title:** slice: Create .slnx and project layout

**Parent Feature Issue:** feat: Solution scaffolding and module boundaries (01 - Foundation)

**Module:** Cross-Cutting

**Slice Type:** Domain

**Scope:**
- Create VenuePass.slnx file
- Create VenuePass.Api project (ASP.NET Core)
- Create VenuePass.BuildingBlocks project (class library)
- Create VenuePass.Modules.Events project (class library)
- Rough folder structure for each module
- Add projects to solution and verify build

**Acceptance Criteria:**
- [ ] `dotnet build` succeeds for all projects
- [ ] Projects can be navigated in IDE
- [ ] Solution compiles cleanly with no warnings

**Docs Impact:**
- [ ] No doc changes needed for this slice

---

### Slice 2: Create BuildingBlocks shared abstractions

**Title:** slice: Implement BuildingBlocks primitives and contracts

**Parent Feature Issue:** feat: Solution scaffolding and module boundaries (01 - Foundation)

**Module:** Cross-Cutting

**Slice Type:** Domain

**Scope:**
- Create Result<T> type in BuildingBlocks.Application
- Create domain event base class in BuildingBlocks.Messaging
- Create integration event base interface in BuildingBlocks.Messaging
- Create ICurrentUser interface in BuildingBlocks.Infrastructure
- Add basic documentation on usage

**Acceptance Criteria:**
- [ ] All base types are abstract/sealed appropriately
- [ ] Unit tests verify basic Result<T> behavior
- [ ] No module internals leaked in BuildingBlocks

**Docs Impact:**
- [ ] Update docs/tech-decisions.md if new patterns emerge

---

### Slice 3: Create Events module internal structure

**Title:** slice: Events module folder layout and module configuration

**Parent Feature Issue:** feat: Solution scaffolding and module boundaries (01 - Foundation)

**Module:** Events

**Slice Type:** Domain

**Scope:**
- Create Features, Domain, Infrastructure, Contracts folders in Events module
- Create ModuleConfiguration.cs class
- Stub EventsDbContext in Infrastructure
- Create IEventsModule contract interface
- Register module in DI from Api startup

**Acceptance Criteria:**
- [ ] Folder structure matches architecture (see docs/architecture-overview.md section 4)
- [ ] ModuleConfiguration.cs can be discovered and instantiated
- [ ] No compilation errors in Api when Events is registered

**Docs Impact:**
- [ ] No doc changes needed

---

### Slice 4: Create architecture tests for boundary enforcement

**Title:** slice: Implement architecture test suite for module boundaries

**Parent Feature Issue:** feat: Solution scaffolding and module boundaries (01 - Foundation)

**Module:** Cross-Cutting

**Slice Type:** Test

**Scope:**
- Create VenuePass.ArchitectureTests project (xUnit)
- Write test rules enforcing:
  - No module accesses another module's tables/DbContext
  - No shared business entities across modules (except via DTOs)
  - Dependency direction: Features ← Infrastructure ← Domain
  - Domain layer has zero infrastructure dependencies
- Tests should be runnable in CI and fail-fast on violations

**Acceptance Criteria:**
- [ ] All architecture tests pass
- [ ] Each rule has at least one positive test (should pass) and one negative test (should fail if rule violated)
- [ ] Tests are clear and maintainable
- [ ] CI runs architecture tests as part of baseline pipeline

**Docs Impact:**
- [ ] Update docs/tech-decisions.md with architecture test strategy if not already present

---

### Slice 5: Create baseline CI pipeline

**Title:** slice: Set up baseline CI workflow (restore, build, arch tests, unit tests)

**Parent Feature Issue:** feat: Solution scaffolding and module boundaries (01 - Foundation)

**Module:** Cross-Cutting

**Slice Type:** Docs

**Scope:**
- Create or update .github/workflows/fast-ci.yml
- Define trigger: on PR and push to main
- Steps: restore, build (warnings as errors), architecture tests, unit tests
- Verify all steps pass locally before push

**Acceptance Criteria:**
- [ ] Workflow file is valid YAML
- [ ] Workflow runs on PR to main
- [ ] All steps pass in CI

**Docs Impact:**
- [ ] Update docs/delivery-plan.md if CI structure differs from original

---

### Slice 6: First Events vertical slice scaffolding

**Title:** slice: Events domain and persistence scaffolding for first slice

**Parent Feature Issue:** feat: Solution scaffolding and module boundaries (01 - Foundation)

**Module:** Events

**Slice Type:** Domain + Persistence

**Scope:**
- Create Venue aggregate in Events.Domain.Venues
- Create EventsDbContext in Events.Infrastructure with Venue DbSet
- Create Venue EF configuration (column mappings)
- Create initial migration for Events schema
- Verify migration runs cleanly on local SQL Server

**Acceptance Criteria:**
- [ ] Venue entity compiles and has basic properties (Id, Name, City, Capacity)
- [ ] EventsDbContext maps correctly to "events" schema
- [ ] Migration creates events.venues table
- [ ] Architecture tests still pass

**Docs Impact:**
- [ ] No doc changes needed for this slice

---

## Slice Execution Order
1. Slice 1 (Project structure)
2. Slice 2 (BuildingBlocks)
3. Slice 3 (Events module layout)
4. Slice 4 (Architecture tests)
5. Slice 5 (CI pipeline)
6. Slice 6 (First Events slice)

Each slice is a separate PR linked to its issue.
