# Research: Mock Module — Dashboard API

**Phase 0 — Resolve NEEDS CLARIFICATION**  
**Date**: 2026-04-15  
**Feature**: [spec.md](spec.md)

## Summary

No `[NEEDS CLARIFICATION]` markers existed in the spec. All decisions follow directly from the existing SubscriptionHistory module pattern or from explicit spec requirements. This document records the key design decisions and their rationale.

---

## Decision 1: Module Layer Structure

- **Decision**: API + Application layers only (no Domain or Infrastructure)
- **Rationale**: Identical to SubscriptionHistory (feature 004). Mock-only modules serve static in-memory data with no entities, no persistence, and no domain events. Adding Domain/Infrastructure layers would introduce unnecessary complexity with zero benefit.
- **Alternatives considered**: Full 4-layer structure (Domain + Infrastructure) — rejected; no EF context or migrations are required for mock data.

---

## Decision 2: Single Controller vs. Separate Controllers

- **Decision**: Single `MockController` at route `api/mock` with 5 `[HttpGet]` action methods, one per resource
- **Rationale**: All 5 endpoints share the same module namespace and are collectively the "mock dashboard" surface. A single controller is simpler, mirrors the SubscriptionHistoryController pattern (one controller per module), and keeps registration trivial.
- **Alternatives considered**: 5 separate controllers (e.g., `MockUsersController`, `MockCurrentUserController`) — rejected; over-engineered for 5 static endpoints with no shared state beyond the service.

---

## Decision 3: Response Format (plain list vs. PagedResponse)

- **Decision**: Plain `List<T>` response body for all 5 endpoints — no `PagedResponse<T>` wrapper
- **Rationale**: The spec explicitly states "None of the five endpoints require pagination or filtering — each returns its full fixed dataset on every call." Using `PagedResponse<T>` would add `page`, `pageSize`, `totalCount`, `totalPages` fields that are meaningless for fixed-size static datasets.
- **Alternatives considered**: `PagedResponse<T>` (as used in SubscriptionHistory) — rejected; would add spurious null/zero metadata fields and violate the "no pagination" spec constraint.

---

## Decision 4: Service Interface Shape

- **Decision**: 5 separate async methods on `IMockService`, one per endpoint
- **Rationale**: Each endpoint returns a structurally distinct response type. A single generic `GetAsync(resourceType)` would require casting/switching and weaken type safety. Separate methods match the Interface Segregation Principle (P-I from constitution).
- **Alternatives considered**: Single `GetAsync<T>(endpoint)` method — rejected; generic parameter would need runtime type dispatch, reducing clarity.

---

## Decision 5: DI Lifetime

- **Decision**: Singleton
- **Rationale**: Identical to SubscriptionHistory. The service holds only static immutable data initialized at construction time. Singleton avoids repeated list allocation per request with zero downside (no shared mutable state).
- **Alternatives considered**: Scoped or Transient — rejected; unnecessary allocation overhead for purely static data.

---

## Decision 6: No Authentication on Mock Endpoints

- **Decision**: No `[Authorize]` attribute on the controller or any action
- **Rationale**: Spec assumption: "No authentication or authorization is required for any of the five mock endpoints — they exist solely for dashboard UI development and demo purposes."
- **Alternatives considered**: Optional `[AllowAnonymous]` override — considered but unnecessary; by default, if no global auth policy is enforced, unannotated controllers are accessible. If a global `[Authorize]` policy is active in the host, explicit `[AllowAnonymous]` may be required. **Implementation note**: verify at host wiring time whether a global auth policy applies.

---

## Decision 7: Mock Data Content

- **Decision**: Hardcoded static data matching spec field constraints
  - Users: at least 5 entries, all 3 roles represented
  - Current user: 1 entry with 2-char abbreviation (initials from display name)
  - Client requests: exactly 10, equity > 0 decimal, ISO 8601 timestamps
  - Signal provider requests: exactly 10, kycStatus ∈ {Pending, Verified, Rejected}
  - Affiliate requests: exactly 10, kycStatus ∈ {Pending, Verified, Rejected}
- **Rationale**: Spec FR-006 requires in-memory data. Static hardcoded data is the simplest possible implementation that satisfies all acceptance scenarios deterministically (edge case: "returns the same static data every time").
- **Alternatives considered**: Random data generated at startup — rejected; spec edge case explicitly requires deterministic responses.

---

---

## Decision 8: Environment Gating (DEV-only)

- **Decision**: Conditionally register `MockModule` (services + controller routes) only when `IWebHostEnvironment.IsDevelopment()` is true in `Program.cs`. In all other environments the endpoints are not registered and calls return HTTP 404.
- **Rationale**: FR-011 / SC-006 require these mock endpoints are invisible in non-Development environments. Not registering the module entirely is the safest approach — it avoids a 401/403 auth bypass risk and ensures no route leaks into staging or production, even under misconfiguration.
- **Alternatives considered**:
  - `[ApiExplorerSettings(IgnoreApi = true)]` + runtime 404 middleware — rejected; route would still be registered, only hidden from Swagger. A determined caller could still hit it.
  - Feature flag — rejected; over-engineered for an environment check that .NET already provides natively.
  - Authorization policy on the controller — rejected; adds auth complexity to endpoints explicitly designed to be unauthenticated; doesn't protect non-Dev envs from route enumeration.

---

## Research Summary: No Unknowns Remain

All design decisions resolved. No NEEDS CLARIFICATION items to surface. Proceed to Phase 1.
