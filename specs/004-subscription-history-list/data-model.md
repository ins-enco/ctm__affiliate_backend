# Data Model: Subscription History List Endpoint

**Branch**: `004-subscription-history-list`  
**Phase**: 1 — Design

> No database tables or EF migrations in this feature. All data shapes below are in-memory DTOs only.

## Dependency on Spec 003

Once **spec 003 (Generic Paginated Response)** is implemented and merged, the inline `SubscriptionHistoryResponse` record defined in this spec is replaced by `PagedResponse<SubscriptionHistoryItem>` from `CopyTradeMarketApi.Shared.Responses`. The service returns:
- `PagedResponse<SubscriptionHistoryItem>.All(mockedList)` — when no pagination params provided
- `PagedResponse<SubscriptionHistoryItem>.Paginated(slice, totalCount, page, pageSize)` — when paginated

No changes to the HTTP response JSON shape are required — field names and nullability are identical.

---

## Core Entities (In-Memory DTOs)

### SubscriptionHistoryItem

Represents a single subscription lifecycle event for a client.

| Field             | Type       | Required | Rules                                                           |
|-------------------|------------|----------|-----------------------------------------------------------------|
| `Timestamp`       | `DateTime` | Yes      | UTC; represents when the event occurred; used for sort order   |
| `ClientName`      | `string`   | Yes      | Display name of the client (e.g., "Aleš Chromec")              |
| `AccountNumber`   | `string`   | Yes      | Client account identifier (e.g., "31028")                      |
| `StrategyName`    | `string`   | Yes      | Name of the trading strategy (e.g., "Super duper Pro FX")      |
| `EquityConnect`   | `decimal`  | Yes      | Monetary value at time of subscription; always positive        |
| `EquityDisconnect`| `decimal?` | No       | Monetary value at time of unsubscription; null for Subscribe actions |
| `ActionType`      | `string`   | Yes      | Enum-like value: `"Subscribe"` or `"Unsubscribe"`              |

**C# record**:
```csharp
public record SubscriptionHistoryItem(
    DateTime Timestamp,
    string ClientName,
    string AccountNumber,
    string StrategyName,
    decimal EquityConnect,
    decimal? EquityDisconnect,
    string ActionType
);
```

**Validation rules** (enforced in service, surfaced as 400 ProblemDetails via middleware):
- `ActionType` must be `"Subscribe"` or `"Unsubscribe"` (checked only at mock-data construction time)
- `EquityConnect` must be > 0

---

### SubscriptionHistoryResponse

Unified response envelope returned by the endpoint in all scenarios.

| Field        | Type                              | Required | Notes                                                        |
|--------------|-----------------------------------|----------|--------------------------------------------------------------|
| `Items`      | `IReadOnlyList<SubscriptionHistoryItem>` | Yes | Empty list `[]` when no records match the page           |
| `TotalCount` | `int`                             | Yes      | Total number of records in the full dataset (before paging)  |
| `Page`       | `int?`                            | No       | Current page number; `null` when no pagination requested     |
| `PageSize`   | `int?`                            | No       | Page size applied; `null` when no pagination requested       |
| `TotalPages` | `int?`                            | No       | Total page count; `null` when no pagination requested        |

**C# record**:
```csharp
public record SubscriptionHistoryResponse(
    IReadOnlyList<SubscriptionHistoryItem> Items,
    int TotalCount,
    int? Page,
    int? PageSize,
    int? TotalPages
);
```

---

## Query Parameters

| Parameter        | Type     | Required | Default                        | Validation |
|------------------|----------|----------|--------------------------------|------------|
| `query`          | `string?`| No       | `null`                         | Empty/whitespace treated as no filter |
| `orderBy`        | `string?`| No       | `timestamp`                    | Allowed: `timestamp`, `clientName`, `accountNumber`, `strategyName`, `equityConnect` |
| `orderDirection` | `string?`| No       | `desc`                         | Allowed: `asc`, `desc` |
| `page`           | `int?`   | No       | `1` (when `pageSize` provided) | Must be ≥ 1 if present |
| `pageSize`       | `int?`   | No       | `20` (when `page` provided)    | Must be ≥ 1 if present |

**Pagination mode detection**:
- Neither `page` nor `pageSize` → return all records; `Page`, `PageSize`, `TotalPages` in response are `null`
- Either or both provided → apply defaults, paginate, populate all response fields
- Processing order is always: filter → sort → paginate

---

## Mock Dataset Structure

The mocked dataset contains **20 records** initialized in `SubscriptionHistoryService` sorted newest-first. The dataset covers:

- Multiple clients (at least 3 distinct `ClientName` values)
- Multiple account numbers
- Multiple strategies (at least 3 distinct `StrategyName` values)
- Mix of `Subscribe` and `Unsubscribe` actions (≈60% Subscribe, ≈40% Unsubscribe)
- `EquityDisconnect` is populated for `Unsubscribe` records; `null` for `Subscribe` records
- Timestamps span a realistic date range (e.g., Jan 2021 – Dec 2021) matching the UI screenshot

---

## Service Interface

```csharp
public interface ISubscriptionHistoryService
{
    Task<SubscriptionHistoryResponse> GetAsync(
        int? page,
        int? pageSize,
        string? query = null,
        string? orderBy = null,
        string? orderDirection = null);
}
```

**Behavior contract**:
1. If `page < 1` or `pageSize < 1` → throw `ArgumentException` (caught by `ExceptionHandlingMiddleware` → 400 ProblemDetails)
2. If no pagination params → return all records; no pagination metadata
3. If pagination params → apply effective `page` (default 1) and `pageSize` (default 20); compute slice and metadata
4. `TotalPages = (int)Math.Ceiling((double)TotalCount / effectivePageSize)`
5. Filter is applied first (`query`)
6. Ordering is applied second (`orderBy`, `orderDirection`)
7. Pagination is applied last (`page`, `pageSize`)

---

## State Transitions

Not applicable — subscription history is read-only in this feature. No write operations.

---

## Relationships to Existing Domain

| Existing Entity | Relationship | Notes |
|-----------------|--------------|-------|
| `User` (Auth)   | Conceptual   | `ClientName` corresponds to a user's display name. No direct FK — data is mocked. |
| `Affiliate`     | Conceptual   | Subscriptions are attributed to affiliate-referred users. No direct FK — data is mocked. |

No EF relationships, foreign keys, or navigation properties — all data is in-memory mock.
