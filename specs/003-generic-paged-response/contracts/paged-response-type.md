# Type Contract: PagedResponse\<T\>

**Branch**: `feature/003-generic-paged-response`  
**Project**: `CopyTradeMarketApi.Shared`  
**Namespace**: `CopyTradeMarketApi.Shared.Responses`

> This is a C# library type contract, not an HTTP endpoint contract. The "interface" this feature exposes is the public API of `PagedResponse<T>` — its constructor, properties, and factory methods — which all modules consume.

---

## Public API Surface

### Type Signature

```csharp
public record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int? Page,
    int? PageSize,
    int? TotalPages
);
```

### Static Factory Methods

```csharp
// Non-paginated: returns all items, pagination metadata is null
PagedResponse<T>.All(IReadOnlyList<T> items) → PagedResponse<T>

// Paginated: returns a slice with computed TotalPages
PagedResponse<T>.Paginated(
    IReadOnlyList<T> items,
    int totalCount,
    int page,
    int pageSize
) → PagedResponse<T>
```

---

## Caller Contract

Any service returning a list that may or may not be paginated:

```csharp
// Non-paginated — return all
return PagedResponse<MyItem>.All(allItems);

// Paginated — after validating page/pageSize ≥ 1
var slice = allItems.Skip((page - 1) * pageSize).Take(pageSize).ToList();
return PagedResponse<MyItem>.Paginated(slice, allItems.Count, page, pageSize);
```

Validation of `page` and `pageSize` (must be ≥ 1) is the **caller's responsibility** before invoking `Paginated()`. `ArgumentException` thrown by the service is caught by `ExceptionHandlingMiddleware` → 400 ProblemDetails.

---

## JSON Output Contract

The following property names are guaranteed in the serialized JSON response (camelCase applied by host-level policy):

| C# Property  | JSON Key      | Always Present | Notes                                    |
|--------------|---------------|----------------|------------------------------------------|
| `Items`      | `items`       | Yes            | Array; empty `[]` when no results        |
| `TotalCount` | `totalCount`  | Yes            | Full dataset count before paging         |
| `Page`       | `page`        | Yes            | Integer or `null`                        |
| `PageSize`   | `pageSize`    | Yes            | Integer or `null`                        |
| `TotalPages` | `totalPages`  | Yes            | Integer or `null`                        |

All five keys are always present in the JSON — nullable fields serialize as `null`, not omitted. This ensures clients can parse the response with the same schema regardless of mode.

---

## Compatibility Guarantee

`PagedResponse<T>` is a **non-breaking addition** to `CopyTradeMarketApi.Shared`. Existing modules are not required to adopt it immediately. Adoption is incremental:

1. Spec 003 introduces the type (this feature).
2. Spec 004 (Subscription History) adopts it — replacing `SubscriptionHistoryResponse` with `PagedResponse<SubscriptionHistoryItem>`.
3. Future modules use `PagedResponse<T>` from the start.

Any module still using its own inline pagination record continues to compile and function without change.

---

## Breaking Change Policy

Changes to `PagedResponse<T>` that remove, rename, or change the type of any property are **breaking changes** that require:
1. A version bump in the spec.
2. Updating all adopting modules simultaneously.
3. PR tagged `breaking-change` per constitution.

Adding new optional members (new nullable properties) is non-breaking.
