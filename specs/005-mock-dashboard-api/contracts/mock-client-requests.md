# Contract: GET /api/dashboard/clientRequests

**Feature**: [spec.md](../spec.md) — FR-003, FR-009, SC-002, SC-004, SC-005

## Endpoint

| Property | Value |
|----------|-------|
| Method | `GET` |
| Path | `/api/dashboard/clientRequests` |
| Auth | None |
| Query Parameters | None (ignored if supplied) |

## Response — 200 OK

Returns a `PagedResponse<ClientRequestDto>` (non-paginated envelope) of exactly 10 records.

```json
{
  "items": [
    {
      "timestamp": "2026-04-13T10:30:00Z",
      "name": "Jörg Müller",
      "equity": 12500.00,
      "strategy": "Alpha Growth",
      "strategyLicense": "LIC-001"
    },
    {
      "timestamp": "2026-04-12T14:15:00Z",
      "name": "Alice Johnson",
      "equity": 8750.50,
      "strategy": "Beta Momentum",
      "strategyLicense": "LIC-002"
    }
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
| `items` | array of ClientRequestDto | see below |
| `totalCount` | integer | always 10 |
| `page` | null | not paginated |
| `pageSize` | null | not paginated |
| `totalPages` | null | not paginated |

### Schema — ClientRequestDto

| Field | Type | Constraints |
|-------|------|-------------|
| `timestamp` | string (ISO 8601, UTC) | non-null |
| `name` | string | non-empty, client display name |
| `equity` | number (decimal) | > 0; represents monetary amount in USD; no currency symbol |
| `strategy` | string | non-empty, strategy name |
| `strategyLicense` | string | non-empty, short license identifier; format not validated |

### Guarantees

- Always returns exactly 10 records in `items`.
- All `equity` values are positive decimals.
- All timestamps are UTC in ISO 8601 format.
- Response is deterministic — identical on every call.

## Error Responses

| Status | When |
|--------|------|
| 405 Method Not Allowed | Any method other than GET |
