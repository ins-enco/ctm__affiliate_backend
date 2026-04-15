# Contract: GET /api/dashboard/listOfUsers

**Feature**: [spec.md](../spec.md) — FR-001, FR-007, FR-012, SC-003, SC-004, SC-005

## Endpoint

| Property | Value |
|----------|-------|
| Method | `GET` |
| Path | `/api/dashboard/listOfUsers` |
| Auth | None |
| Query Parameters | `searchText` (optional, string) — case-insensitive partial match on user name; absent or empty returns all users |

## Response — 200 OK

Returns a `PagedResponse<UserDto>` (non-paginated envelope).

```json
{
  "items": [
    { "id": "1", "name": "Carlos Silva", "role": "Signal Provider" },
    { "id": "2", "name": "Ana Costa",    "role": "Affiliate" },
    { "id": "3", "name": "John Doe",     "role": "Client" }
  ],
  "totalCount": 10,
  "page": null,
  "pageSize": null,
  "totalPages": null
}
```

### Schema — Envelope

| Field | Type | Notes |
|-------|------|-------|
| `items` | array of UserDto | see below |
| `totalCount` | integer | count of records in this response |
| `page` | null | not paginated |
| `pageSize` | null | not paginated |
| `totalPages` | null | not paginated |

### Schema — UserDto

| Field | Type | Constraints |
|-------|------|-------------|
| `id` | string | non-empty, unique mock identifier |
| `name` | string | non-empty |
| `role` | string | one of: `Client`, `Signal Provider`, `Affiliate` |

### Guarantees

- Without `searchText`: `items` contains all 10 mock users; all three role values appear at least once.
- With `searchText`: `items` contains only users whose name contains the search text (case-insensitive). Returns empty `items` if no match.
- `totalCount` always equals `items.length`.
- Response is deterministic for the same input.

## Error Responses

| Status | When |
|--------|------|
| 405 Method Not Allowed | Any method other than GET |
