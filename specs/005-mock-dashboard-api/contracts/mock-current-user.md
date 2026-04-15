# Contract: GET /api/mock/current-user

**Feature**: [spec.md](../spec.md) — FR-002, SC-004, SC-005

## Endpoint

| Property | Value |
|----------|-------|
| Method | `GET` |
| Path | `/api/mock/current-user` |
| Auth | None |
| Query Parameters | None (ignored if supplied) |

## Response — 200 OK

Returns a single user object representing the currently active dashboard user.

```json
{
  "id": 1,
  "name": "Carlos Silva",
  "abbreviation": "CS",
  "role": "Signal Provider"
}
```

### Schema

| Field | Type | Constraints |
|-------|------|-------------|
| `id` | integer | positive |
| `name` | string | non-empty |
| `abbreviation` | string | exactly 2 uppercase characters; first letter of first name + first letter of last name; for single-word names: first 2 letters |
| `role` | string | one of: `Client`, `Signal Provider`, `Affiliate` |

### Guarantees

- Always returns exactly one object (not an array).
- `abbreviation` is always exactly 2 characters.
- Response is deterministic — identical on every call.

## Error Responses

| Status | When |
|--------|------|
| 405 Method Not Allowed | Any method other than GET |
