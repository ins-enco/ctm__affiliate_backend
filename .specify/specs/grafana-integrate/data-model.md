# Data Model: Grafana Integration

> No new database entities. This document defines the **Loki log schema** — the structured fields emitted by Serilog and consumed by Grafana dashboards via LogQL.

---

## Loki Labels (indexed, low-cardinality)

Labels are used for fast log stream selection in LogQL (`{label="value"}`). Keep cardinality low.

| Label | Values | Source |
|---|---|---|
| `app` | `copytrade-api` | Static — set in Loki sink config |
| `environment` | `Development`, `Production` | `ASPNETCORE_ENVIRONMENT` env var |
| `level` | `Information`, `Warning`, `Error`, `Fatal` | Serilog log level |
| `module` | `Auth`, `Tracking`, `Affiliate`, `Host` | Serilog enricher — set per source context |

---

## Structured Log Properties (indexed as JSON fields in Loki)

These are Serilog message template properties included in the log payload. Grafana dashboards use LogQL `json` parser to extract and aggregate these.

### All Log Events

| Property | Type | Description | Sensitive? |
|---|---|---|---|
| `MachineName` | string | Host machine name (from `Enrich.WithMachineName()`) | No |
| `RequestId` | string | ASP.NET Core trace identifier | No |
| `RequestPath` | string | HTTP path (e.g. `/api/tracking/click`) | No |
| `StatusCode` | int | HTTP response status code | No |
| `Elapsed` | double | Request duration in milliseconds | No |
| `SourceContext` | string | Logger class name (e.g. `TrackingService`) | No |
| `ExceptionType` | string | Exception class name (Error events only) | No |
| `ExceptionMessage` | string | Exception message (Error events only) | No |

### Business Event Log Properties

Emitted at `Information` level at domain event handler call sites.

| Property | Type | Emitted By | Description |
|---|---|---|---|
| `AffiliateCode` | string | `TrackingService`, event handlers | 8-char affiliate code |
| `SessionId` | string | `TrackingService` | SHA256 session fingerprint |
| `ConversionType` | string | Conversion event handler | `Registration` or `Deposit` |
| `EventType` | string | All domain event handlers | `ClickRecorded`, `ConversionRecorded`, `UserRegistered` |

### Explicitly Excluded (never logged at Info+)

Per P4 (Secrets Never In Source) and P6 error contract:

| Field | Reason |
|---|---|
| `IpAddress` | Raw IP is PII; only used in-memory for session fingerprint |
| `UserAgent` | PII risk; only used in-memory for session fingerprint |
| `Email` | PII; auth module must never log raw email at Info level |
| `Password` | Never logged at any level |

---

## LogQL Query Examples (for Dashboard Reference)

```logql
# Click rate per affiliate (last 1h)
sum by (AffiliateCode) (
  count_over_time({app="copytrade-api", level="Information"} | json | EventType="ClickRecorded" [1h])
)

# Error rate (errors per minute)
sum(rate({app="copytrade-api", level="Error"} [5m]))

# P95 request latency
quantile_over_time(0.95, {app="copytrade-api"} | json | unwrap Elapsed [5m])

# Conversion breakdown by type
sum by (ConversionType) (
  count_over_time({app="copytrade-api"} | json | EventType="ConversionRecorded" [1h])
)
```
