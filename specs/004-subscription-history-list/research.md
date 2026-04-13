# Research: Subscription History List Endpoint

**Branch**: `004-subscription-history-list`  
**Phase**: 0 — Outline & Research

---

## Decision 1: Module Placement

**Decision**: Create a new `SubscriptionHistory` module within the modular monolith.

**Rationale**: The subscription history data (client subscriptions/unsubscriptions to trading strategies) is a distinct domain concept that does not belong to the existing Auth, Tracking, or Affiliate modules. Creating a self-contained module preserves module isolation (Constitution P1 — Modules are islands) and allows the domain to evolve independently without coupling to existing module DbContexts or services.

**Alternatives considered**:
- Add to Affiliate module — rejected: subscription history is not about referral attribution; mixing it into Affiliate violates single responsibility.
- Add to Tracking module — rejected: Tracking owns click/conversion events, not subscription lifecycle events; different domain concepts.
- Add to Auth module — rejected: Auth owns identity only.

**Constitution alignment note**: The constitution defines this API as the "affiliate and attribution service." Subscription history (showing which clients connected/disconnected from trading strategies) is contextually linked to affiliate-attributed conversions — a subscriber is a client who arrived via an affiliate link. This module tracks the downstream subscription lifecycle of attributed users, which is within the spirit of the attribution boundary. This should be revisited if the product roadmap expands subscription history beyond affiliate-attributed users.

---

## Decision 2: Layer Structure (2-layer vs 4-layer)

**Decision**: Use a 2-layer module — `SubscriptionHistory.API` + `SubscriptionHistory.Application` only. No Domain or Infrastructure layers.

**Rationale**: Since data is mocked in-memory (no database entities, no EF migrations), there are no domain entities with business rules (no Domain layer needed) and no persistence infrastructure (no Infrastructure layer needed). Adding empty placeholder projects would be speculative over-engineering.

**Alternatives considered**:
- Full 4-layer (Domain + Application + Infrastructure + API) — rejected for this iteration: creates 2 empty skeleton projects with no content; adds unnecessary project references and build overhead for mocked data.
- Single project (everything in API layer) — rejected: violates separation of concerns; service logic would bleed into controller layer.

**When to revisit**: If subscription history is later backed by a real database table, add Domain and Infrastructure layers at that time with a proper EF migration.

---

## Decision 3: Pagination Strategy

**Decision**: Optional pagination via `page` and `pageSize` query parameters. When absent → return all. When present → return sliced result with metadata.

**Rationale**: The spec explicitly requires "return all if client doesn't ask for pagination." A unified response envelope with nullable pagination fields allows the client to use the same response-parsing code regardless of mode.

**Implementation approach**:
- Both `page` and `pageSize` are nullable int query parameters.
- If neither is provided → return all records, pagination metadata fields are null.
- If either is provided → apply defaults (`page` defaults to 1, `pageSize` defaults to 20) and return the slice with full metadata.
- Validation: `page < 1` or `pageSize < 1` → 400 Bad Request (ProblemDetails per Constitution P6).

**Alternatives considered**:
- Separate endpoints (`/subscription-history` and `/subscription-history/paged`) — rejected: adds unnecessary URL surface; query params handle this cleanly.
- Always paginate with a very large default — rejected: explicitly contradicts the spec requirement.

---

## Decision 4: Mocked Data Seeding

**Decision**: Static mocked data is initialized as a private `IReadOnlyList<SubscriptionHistoryItem>` inside `SubscriptionHistoryService`. The service is registered as a singleton so the list is created once per application lifetime.

**Rationale**: Simple, zero-dependency approach. No external seed files, no DB setup, no startup complexity. Consistent with the spec requirement of mocking the list.

**Mock dataset**: 20 records representing a realistic mix of Subscribe and Unsubscribe actions across multiple clients, account numbers, and strategies (matching the UI screenshot column structure). Records are pre-sorted newest-first.

**Alternatives considered**:
- JSON seed file loaded at startup — rejected: adds file I/O for no gain at this stage.
- Separate `ISubscriptionHistoryDataProvider` interface — rejected: premature abstraction for a single mock use-case.

---

## Decision 5: Response Envelope

**Decision**: Single unified response record with nullable pagination fields.

```
SubscriptionHistoryResponse {
  items:       SubscriptionHistoryItem[]
  totalCount:  int
  page:        int?   // null when not paginated
  pageSize:    int?   // null when not paginated
  totalPages:  int?   // null when not paginated
}
```

**Rationale**: One response shape means one parsing contract for the client. The presence or absence of `page`/`pageSize`/`totalPages` signals paginated vs. full-list mode. `totalCount` is always present (useful even in non-paginated mode to know how many records exist).

**Alternatives considered**:
- Two separate response types (one for paginated, one for full list) — rejected: forces client to branch on a discriminator; complicates Swagger documentation.
- Nested `pagination` object — rejected: slightly more complex serialization for marginal readability gain.

---

## Decision 6: Endpoint Authentication

**Decision**: No `[Authorize]` attribute for this iteration.

**Rationale**: The spec explicitly states authentication is out of scope for this iteration. The constitution only requires JWT validation on "every protected endpoint" — this endpoint is intentionally unprotected for now.

**When to revisit**: Add `[Authorize]` when the product requires user-specific subscription history or role-based access control.

---

## Decision 7: Sort Order

**Decision**: Default ordering is newest-first (`timestamp desc`) and clients may override using `orderBy` + `orderDirection`.

**Rationale**: Matches the UI design screenshot and the spec assumption while still allowing consumer-specific sorting needs.

**Allowed order fields**:
- `timestamp`
- `clientName`
- `accountNumber`
- `strategyName`
- `equityConnect`

**Direction values**:
- `asc`
- `desc`

**Defaults and behavior**:
- If `orderBy` omitted → default field `timestamp`
- If `orderDirection` omitted → default direction `desc`
- If only `orderDirection` is provided → it applies to default field `timestamp`

**Validation**:
- Unknown `orderBy` or `orderDirection` returns 400 ProblemDetails (via `ArgumentException` + middleware)

---

## Decision 8: Query Filtering

**Decision**: Support `query` as a case-insensitive partial-match filter across `clientName`, `accountNumber`, and `strategyName`.

**Rationale**: This gives flexible free-text lookup for common operator workflows while keeping request semantics simple.

**Behavior**:
- `query` empty or whitespace-only is treated as not provided

**Processing order**:
- Filter first (`query`)
- Sort second (`orderBy`, `orderDirection`)
- Paginate last (`page`, `pageSize`)
