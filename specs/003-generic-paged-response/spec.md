# Feature Specification: Generic Paginated Response

**Feature Branch**: `003-generic-paged-response`  
**Created**: 2026-04-13  
**Status**: Draft  
**Input**: User description: "Create a generic paginated response wrapper in CopyTradeMarketApi.Shared so all modules can reuse a unified paged or full-list response envelope instead of defining their own inline pagination fields"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Any module returns a paginated list using the shared type (Priority: P1)

A developer implementing a feature that returns a list of records (e.g., subscription history, click events, affiliates) uses the shared `PagedResponse<T>` type from `CopyTradeMarketApi.Shared` as the return value instead of defining custom pagination fields inline in each module. When no pagination is requested, the same type carries all records with null pagination metadata. When pagination is requested, it carries the sliced result with full metadata.

**Why this priority**: This is the single deliverable of this feature. All other benefits (consistency, reuse, reduced duplication) follow from having this shared type available. Without it, every feature that needs pagination defines its own shape — leading to divergent API responses.

**Independent Test**: Can be fully tested by: (1) instantiating `PagedResponse<string>` with null pagination metadata and verifying fields; (2) instantiating with paginated metadata and verifying all fields; (3) confirming JSON serialization produces the expected property names.

**Acceptance Scenarios**:

1. **Given** a developer references `CopyTradeMarketApi.Shared`, **When** they create `new PagedResponse<T>(items, totalCount, null, null, null)`, **Then** the instance compiles and serializes to JSON with `items`, `totalCount`, `page: null`, `pageSize: null`, `totalPages: null`.
2. **Given** a developer creates `new PagedResponse<T>(items, totalCount, 1, 10, 5)`, **When** serialized to JSON, **Then** all fields are present: `items`, `totalCount`, `page: 1`, `pageSize: 10`, `totalPages: 5`.
3. **Given** any existing module (e.g., SubscriptionHistory spec 004) imports the shared type, **When** the module's service returns `PagedResponse<SubscriptionHistoryItem>`, **Then** no additional pagination fields need to be defined in that module.

---

### Edge Cases

- What happens when `items` is an empty list? `PagedResponse<T>` should accept an empty `IReadOnlyList<T>` without error.
- What happens when the generic type `T` is a complex record with nested objects? JSON serialization should handle it naturally — no constraints on `T`.
- What happens when `totalCount` is 0 and pagination metadata is set? Valid — `TotalPages` would be 0; the type does not validate this.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide a generic type `PagedResponse<T>` in the `CopyTradeMarketApi.Shared` project that any module can reference without creating additional inter-module dependencies.
- **FR-002**: `PagedResponse<T>` MUST carry: a list of items of type `T`, a total record count, and optional pagination metadata (current page, page size, total pages).
- **FR-003**: The pagination metadata fields (page, pageSize, totalPages) MUST be nullable — when `null`, the response indicates that all records were returned without paging.
- **FR-004**: `PagedResponse<T>` MUST be immutable — fields set at construction and not mutated afterwards.
- **FR-005**: `PagedResponse<T>` MUST serialize to JSON with property names matching the existing API contract (camelCase: `items`, `totalCount`, `page`, `pageSize`, `totalPages`).
- **FR-006**: The shared project MUST provide a static factory or convenience constructors to create both paginated and non-paginated responses, reducing boilerplate at the call site.

### Key Entities

- **PagedResponse\<T\>**: Generic shared response envelope. Fields: `Items` (`IReadOnlyList<T>`), `TotalCount` (`int`), `Page` (`int?`), `PageSize` (`int?`), `TotalPages` (`int?`).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Any module in the solution can return `PagedResponse<T>` without adding a new project reference (only `CopyTradeMarketApi.Shared` is required).
- **SC-002**: Zero inline pagination field definitions remain in any module that adopts this shared type — duplication is fully eliminated.
- **SC-003**: JSON serialization of `PagedResponse<T>` produces output identical to the manually defined response shapes it replaces (verified by comparing serialized output before and after adoption).
- **SC-004**: The type compiles with `T` constrained to any reference or value type — no generic constraints needed.

## Assumptions

- `CopyTradeMarketApi.Shared` is already referenced by all module `.Application` projects; no new project dependencies are introduced by adding this type there.
- JSON property naming follows the existing global convention (camelCase via `JsonNamingPolicy.CamelCase` in the host); no per-type serialization attributes are needed.
- The type does not enforce validation of pagination logic (e.g., `TotalPages == Math.Ceiling(TotalCount / PageSize)`) — that responsibility stays in each module's service layer.
- Spec 004 (Subscription History) will be updated to use `PagedResponse<SubscriptionHistoryItem>` once this spec is implemented, replacing the inline `SubscriptionHistoryResponse` record.
- No breaking changes to existing endpoints — existing modules are not forced to migrate immediately; adoption is incremental.
