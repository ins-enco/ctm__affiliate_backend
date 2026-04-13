# Data Model: Generic Paginated Response

**Branch**: `feature/003-generic-paged-response`  
**Phase**: 1 — Design

> This feature adds a single generic type to `CopyTradeMarketApi.Shared`. There is no database entity, no EF migration, and no HTTP endpoint. The "data model" is the C# type contract.

---

## Core Type: PagedResponse\<T\>

### Fields

| Field        | Type                        | Nullable | Description                                                              |
|--------------|-----------------------------|----------|--------------------------------------------------------------------------|
| `Items`      | `IReadOnlyList<T>`          | No       | The list of records for this response (full list or one page's worth)    |
| `TotalCount` | `int`                       | No       | Total records in the full dataset before any paging is applied           |
| `Page`       | `int?`                      | Yes      | Current page number (1-based). `null` when no pagination was requested.  |
| `PageSize`   | `int?`                      | Yes      | Records per page. `null` when no pagination was requested.               |
| `TotalPages` | `int?`                      | Yes      | Total page count. `null` when no pagination was requested.               |

### Invariants

- `Items` is never `null` — an empty list `[]` is used when there are no results.
- `TotalCount` is always present and reflects the full unfiltered count, even for paginated responses.
- When `Page`, `PageSize`, and `TotalPages` are all `null`: the response contains all records (non-paginated mode).
- When any pagination field is non-null: all three should be populated (enforced by the `Paginated()` factory).
- `TotalPages = Math.Ceiling(TotalCount / PageSize)` — computed by the `Paginated()` factory, not stored independently.

### C# Type Definition

```csharp
namespace CopyTradeMarketApi.Shared.Responses;

public record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int? Page,
    int? PageSize,
    int? TotalPages
)
{
    /// <summary>
    /// Creates a non-paginated response containing all provided items.
    /// Page, PageSize, and TotalPages are null in the response.
    /// </summary>
    public static PagedResponse<T> All(IReadOnlyList<T> items) =>
        new(items, items.Count, null, null, null);

    /// <summary>
    /// Creates a paginated response for a specific page slice.
    /// Computes TotalPages from totalCount and pageSize.
    /// </summary>
    public static PagedResponse<T> Paginated(
        IReadOnlyList<T> items,
        int totalCount,
        int page,
        int pageSize) =>
        new(
            items,
            totalCount,
            page,
            pageSize,
            (int)Math.Ceiling((double)totalCount / pageSize)
        );
}
```

---

## Factory Method Contracts

### `PagedResponse<T>.All(items)`

| Input   | Type                   | Rule                    |
|---------|------------------------|-------------------------|
| `items` | `IReadOnlyList<T>`     | Must not be null        |

**Output**: `PagedResponse<T>` where `TotalCount = items.Count`, `Page = null`, `PageSize = null`, `TotalPages = null`.

---

### `PagedResponse<T>.Paginated(items, totalCount, page, pageSize)`

| Input         | Type               | Rule                          |
|---------------|--------------------|-------------------------------|
| `items`       | `IReadOnlyList<T>` | Must not be null              |
| `totalCount`  | `int`              | ≥ 0                           |
| `page`        | `int`              | ≥ 1 (caller must validate)    |
| `pageSize`    | `int`              | ≥ 1 (caller must validate)    |

**Output**: `PagedResponse<T>` with `TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)`.

**Note**: The factory does not validate `page` or `pageSize` — that responsibility stays in the calling service (per Single Responsibility). Invalid values are rejected by the service before calling `Paginated()`.

---

## JSON Serialization Shape

Serialized via ASP.NET Core's global `JsonNamingPolicy.CamelCase` — no per-property attributes needed.

### Non-paginated (`All()`)
```json
{
  "items": [...],
  "totalCount": 20,
  "page": null,
  "pageSize": null,
  "totalPages": null
}
```

### Paginated (`Paginated()`)
```json
{
  "items": [...],
  "totalCount": 20,
  "page": 1,
  "pageSize": 5,
  "totalPages": 4
}
```

---

## File Location

```
Backend/src/Shared/CopyTradeMarketApi.Shared/
└── Responses/
    └── PagedResponse.cs
```

---

## Adoption in Spec 004

When spec 004 (Subscription History) adopts this type, the inline `SubscriptionHistoryResponse` record is replaced by `PagedResponse<SubscriptionHistoryItem>`. The service returns:

- `PagedResponse<SubscriptionHistoryItem>.All(mockedList)` — when no pagination params
- `PagedResponse<SubscriptionHistoryItem>.Paginated(slice, totalCount, page, pageSize)` — when paginated
