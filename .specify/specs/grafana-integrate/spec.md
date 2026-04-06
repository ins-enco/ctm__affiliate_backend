# Feature: Grafana Integration

## Overview
This feature connects the CopyTradeMarket API to an existing Grafana/Loki instance by extending the Serilog pipeline with a Loki sink. The API emits structured log events — including business events (clicks, conversions) and system events (HTTP requests, errors) — so that they are queryable in Grafana.

Grafana setup, dashboard configuration, and alerting rules are out of scope. This feature is only concerned with what the API sends and how.

## Constraints
- Log transport: Serilog → Grafana Loki (Loki sink added to existing Serilog config)
- Grafana/Loki instance is assumed to already exist and be reachable
- Sensitive fields (email, raw IP) must never appear in log streams at `Information` level or above

## User Stories

### US1 - Ship Application Logs to Loki (P1)
**As a** developer  
**I want** all application logs forwarded to Loki from the API  
**So that** logs are queryable in Grafana without accessing the server directly

**Acceptance Criteria:**
- [x] All log entries (Info, Warning, Error) are delivered to Loki
- [x] Logs include structured fields: `module`, `level`, `requestId`, `timestamp`
- [x] Log shipping does not block the request pipeline (async/batched sink)
- [x] Loki endpoint is configurable via environment variable — not hardcoded

---

### US2 - Emit Structured Business Event Properties (P1)
**As a** developer  
**I want** click and conversion events to include structured properties in the log  
**So that** Grafana can derive business metrics via LogQL without a direct DB connection

**Acceptance Criteria:**
- [x] Click events include `AffiliateCode`, `SessionId`, `EventType=ClickRecorded`
- [x] Conversion events include `AffiliateCode`, `SessionId`, `ConversionType`, `EventType=ConversionRecorded`
- [x] Properties are present on `Information`-level log entries at domain event handler call sites

---

### US3 - Sensitive Field Exclusion (P1)
**As a** developer  
**I want** PII fields excluded from all log streams sent to Loki  
**So that** the API complies with P4 (Secrets Never In Source) and avoids logging raw user data

**Acceptance Criteria:**
- [x] `IpAddress` is never present in any Loki log entry at `Information` level or above
- [x] `Email` is never present in any Loki log entry at `Information` level or above
- [x] `UserAgent` is never present in any Loki log entry at `Information` level or above
- [x] A unit test asserts absence of these fields using an in-memory Serilog sink

---

## Out of Scope
- Grafana setup, dashboard creation, or alert rule configuration
- Docker Compose changes to add Loki or Grafana services
- Prometheus `/metrics` endpoint
- Direct Grafana → MySQL datasource
- Access control configuration in Grafana

## Success Metrics
- 100% of application log entries appear in Loki
- Zero sensitive fields (email, raw IP, user agent) present in Loki streams at Info level
- Loki endpoint switchable via environment variable with no code change

## Open Questions
- What is the Loki endpoint URL format for the target environment?
