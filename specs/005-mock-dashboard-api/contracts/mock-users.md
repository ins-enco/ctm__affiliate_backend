# Contract: GET /api/mock/users

**Feature**: [spec.md](../spec.md) — FR-001, FR-007, SC-003, SC-004, SC-005

## Endpoint

| Property | Value |
|----------|-------|
| Method | `GET` |
| Path | `/api/mock/users` |
| Auth | None |
| Query Parameters | None (ignored if supplied) |

## Response — 200 OK

Returns an array of user records.

```json
[
  {
    "id": 1,
    "name": "Carlos Silva",
    "role": "Signal Provider"
  },
  {
    "id": 2,
    "name": "Ana Costa",
    "role": "Affiliate"
  },
  {
    "id": 3,
    "name": "John Doe",
    "role": "Client"
  }
]
```

### Schema

| Field | Type | Constraints |
|-------|------|-------------|
| `id` | integer | positive, unique |
| `name` | string | non-empty |
| `role` | string | one of: `Client`, `Signal Provider`, `Affiliate` |

### Guarantees

- The array contains at least 5 records on every call.
- All three role values (`Client`, `Signal Provider`, `Affiliate`) appear at least once.
- Response is deterministic — identical on every call.

## Error Responses

| Status | When |
|--------|------|
| 405 Method Not Allowed | Any method other than GET |
