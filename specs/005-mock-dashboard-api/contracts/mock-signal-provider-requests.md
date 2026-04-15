# Contract: GET /api/dashboard/signalProviderRequests

**Feature**: [spec.md](../spec.md) — FR-004, FR-008, SC-002, SC-004, SC-005

## Endpoint

| Property | Value |
|----------|-------|
| Method | `GET` |
| Path | `/api/dashboard/signalProviderRequests` |
| Auth | None |
| Query Parameters | None (ignored if supplied) |

## Response — 200 OK

Returns a `PagedResponse<SignalProviderRequestDto>` (non-paginated envelope) of exactly 10 records.

```json
{
  "items": [
    { "timestamp": "2026-04-13T09:00:00Z", "name": "Marco Rossi",  "kycStatus": "Pending" },
    { "timestamp": "2026-04-12T11:45:00Z", "name": "Yuki Tanaka",  "kycStatus": "Verified" }
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
| `items` | array of SignalProviderRequestDto | see below |
| `totalCount` | integer | always 10 |
| `page` | null | not paginated |
| `pageSize` | null | not paginated |
| `totalPages` | null | not paginated |

### Schema — SignalProviderRequestDto

| Field | Type | Constraints |
|-------|------|-------------|
| `timestamp` | string (ISO 8601, UTC) | non-null |
| `name` | string | non-empty, signal provider display name |
| `kycStatus` | string | one of: `Pending`, `Verified`, `Rejected` |

### Guarantees

- Always returns exactly 10 records in `items`.
- All `kycStatus` values are from the allowed set.
- All timestamps are UTC in ISO 8601 format.
- Response is deterministic — identical on every call.

## Error Responses

| Status | When |
|--------|------|
| 405 Method Not Allowed | Any method other than GET |
