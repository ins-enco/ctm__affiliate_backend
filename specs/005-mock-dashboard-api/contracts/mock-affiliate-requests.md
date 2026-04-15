# Contract: GET /api/dashboard/affiliateRequests

**Feature**: [spec.md](../spec.md) — FR-005, FR-008, SC-002, SC-004, SC-005

## Endpoint

| Property | Value |
|----------|-------|
| Method | `GET` |
| Path | `/api/dashboard/affiliateRequests` |
| Auth | None |
| Query Parameters | None (ignored if supplied) |

## Response — 200 OK

Returns a `PagedResponse<AffiliateRequestDto>` (non-paginated envelope) of exactly 10 records.

```json
{
  "items": [
    { "timestamp": "2026-04-13T08:00:00Z", "name": "Sofia Andrade", "kycStatus": "Verified" },
    { "timestamp": "2026-04-12T16:20:00Z", "name": "Li Wei",        "kycStatus": "Pending" }
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
| `items` | array of AffiliateRequestDto | see below |
| `totalCount` | integer | always 10 |
| `page` | null | not paginated |
| `pageSize` | null | not paginated |
| `totalPages` | null | not paginated |

### Schema — AffiliateRequestDto

| Field | Type | Constraints |
|-------|------|-------------|
| `timestamp` | string (ISO 8601, UTC) | non-null |
| `name` | string | non-empty, affiliate display name |
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
