# Contract: GET /api/mock/signal-provider-requests

**Feature**: [spec.md](../spec.md) — FR-004, FR-008, SC-002, SC-004, SC-005

## Endpoint

| Property | Value |
|----------|-------|
| Method | `GET` |
| Path | `/api/mock/signal-provider-requests` |
| Auth | None |
| Query Parameters | None (ignored if supplied) |

## Response — 200 OK

Returns an array of exactly 10 signal provider request records.

```json
[
  {
    "timestamp": "2026-04-13T09:00:00Z",
    "name": "Marco Rossi",
    "kycStatus": "Pending"
  },
  {
    "timestamp": "2026-04-12T11:45:00Z",
    "name": "Yuki Tanaka",
    "kycStatus": "Verified"
  }
]
```

### Schema

| Field | Type | Constraints |
|-------|------|-------------|
| `timestamp` | string (ISO 8601, UTC) | non-null |
| `name` | string | non-empty, signal provider display name |
| `kycStatus` | string | one of: `Pending`, `Verified`, `Rejected` |

### Guarantees

- Always returns exactly 10 records.
- All `kycStatus` values are from the allowed set.
- All timestamps are UTC in ISO 8601 format.
- Response is deterministic — identical on every call.

## Error Responses

| Status | When |
|--------|------|
| 405 Method Not Allowed | Any method other than GET |
