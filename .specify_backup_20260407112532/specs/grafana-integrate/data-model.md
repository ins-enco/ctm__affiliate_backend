# Data Model: Grafana Integration

> No new database entities. This document defines the structured log event fields the API emits — consumed by Grafana via LogQL.

---

## Loki Stream Labels (low-cardinality, indexed)

| Label | Values | Source |
|---|---|---|
| `app` | `copytrade-api` | Static — set in Loki sink config |
| `environment` | `Development`, `Production` | `ASPNETCORE_ENVIRONMENT` |
| `level` | `Information`, `Warning`, `Error`, `Fatal` | Serilog log level |

---

## Structured Log Properties

### All Requests (via `UseSerilogRequestLogging`)

| Property | Type | Description |
|---|---|---|
| `RequestPath` | string | e.g. `/api/tracking/click` |
| `StatusCode` | int | HTTP response code |
| `Elapsed` | double | Duration in milliseconds |
| `RequestId` | string | ASP.NET Core trace ID |
| `MachineName` | string | From `Enrich.WithMachineName()` |

### Business Events (emitted in `TrackingService`)

| Property | Type | Event | Description |
|---|---|---|---|
| `EventType` | string | Both | `ClickRecorded` or `ConversionRecorded` |
| `AffiliateCode` | string | Both | 8-char referral code |
| `SessionId` | string | Both | SHA256 fingerprint |
| `ConversionType` | string | Conversion only | `Registration` or `Deposit` |

### Error Events

| Property | Type | Description |
|---|---|---|
| `ExceptionType` | string | Exception class name |
| `ExceptionMessage` | string | Exception message |
| `SourceContext` | string | Logger class name |

---

## Explicitly Excluded (never in log stream)

| Field | Reason |
|---|---|
| `IpAddress` | PII — only used in-memory for SHA256 session hash |
| `UserAgent` | PII — only used in-memory for SHA256 session hash |
| `Email` | PII — auth module must never log raw email at Info+ |
| `Password` | Never logged at any level |
