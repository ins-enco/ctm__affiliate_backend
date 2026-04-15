# Contract: GET /api/currentActiveUser

**Feature**: [spec.md](../spec.md) — FR-002, FR-013, FR-014, SC-004, SC-005

## Endpoint

| Property | Value |
|----------|-------|
| Method | `GET` |
| Path | `/api/currentActiveUser` |
| Auth | `API-KEY` header (Development only — see below) |
| Query Parameters | None |

## Request Headers

| Header | Required (Development) | Value |
|--------|----------------------|-------|
| `API-KEY` | Yes | `SimulatedKeyForDev` |

In Production the endpoint is not registered (FR-011), so the header has no effect outside Development. The validation logic is implemented in `CopyTradeMarketApi.Shared.Filters.DevApiKeyFilter` and is reusable across any module.

## Response — 200 OK

Returns a single user object representing the currently active dashboard user.

```json
{
  "id": "1",
  "name": "Carlos Silva",
  "abbreviation": "CS",
  "role": "Signal Provider"
}
```

### Schema

| Field | Type | Constraints |
|-------|------|-------------|
| `id` | string | non-empty mock identifier |
| `name` | string | non-empty |
| `abbreviation` | string | exactly 2 uppercase characters; first letter of first name + first letter of last name; for single-word names: first 2 letters |
| `role` | string | one of: `Client`, `Signal Provider`, `Affiliate` |

### Guarantees

- Always returns exactly one object (not wrapped in a `PagedResponse`).
- `abbreviation` is always exactly 2 characters.
- Response is deterministic — identical on every valid call.

## Error Responses

| Status | When |
|--------|------|
| 401 Unauthorized | `API-KEY` header is absent or its value ≠ `SimulatedKeyForDev` (Development only) |
| 405 Method Not Allowed | Any method other than GET |
