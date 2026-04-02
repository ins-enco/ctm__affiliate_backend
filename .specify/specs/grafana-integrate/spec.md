# Feature: Grafana Integration

## Overview
This feature integrates the CopyTradeMarket API with Grafana to provide centralized observability for both business and system metrics. Application logs are shipped from Serilog to Grafana Loki, enabling admins and developers to monitor affiliate activity, conversion funnels, and platform health through unified dashboards — without direct database access.

Access is restricted to privileged internal users (admins and developers). Affiliates do not have access to Grafana.

## Constraints
- Log transport: Serilog → Grafana Loki (existing Serilog infrastructure extended with a Loki sink)
- Viewer access: Admin/Dev roles only; no affiliate-facing dashboards

## User Stories

### US1 - Ship Application Logs to Grafana Loki (P1)
**As an** admin/developer  
**I want** all application logs to be forwarded to Grafana Loki  
**So that** I have a single place to search, filter, and correlate logs across all modules

**Acceptance Criteria:**
- [ ] All log entries (Info, Warning, Error) are visible in Grafana Loki within 10 seconds of emission
- [ ] Logs include structured fields: `module`, `level`, `requestId`, `timestamp`
- [ ] Sensitive fields (IP address, email) are never forwarded at Info level or above
- [ ] Log shipping does not block the request pipeline (fire-and-forget / async sink)

---

### US2 - Business Metrics Dashboard (P1)
**As an** admin  
**I want** a Grafana dashboard showing affiliate business metrics  
**So that** I can monitor click volume, conversion rates, and affiliate performance in real time

**Acceptance Criteria:**
- [ ] Dashboard shows total clicks per affiliate over a selectable time range
- [ ] Dashboard shows total conversions (Registration / Deposit) per affiliate
- [ ] Dashboard shows conversion rate (conversions / clicks) per affiliate
- [ ] Data refreshes at most every 30 seconds without manual intervention
- [ ] All panels are filterable by affiliate code and time range

---

### US3 - System Health Dashboard (P1)
**As a** developer  
**I want** a Grafana dashboard showing API health and performance metrics  
**So that** I can detect degradation, high error rates, or resource pressure before they impact users

**Acceptance Criteria:**
- [ ] Dashboard shows HTTP request rate, error rate (4xx/5xx), and P95 latency
- [ ] Dashboard shows unhandled exception count grouped by exception type
- [ ] Dashboard shows log volume over time (Info / Warning / Error breakdown)
- [ ] Alerts trigger when error rate exceeds 5% over a 5-minute window
- [ ] Alerts trigger when P95 latency exceeds 2 seconds over a 5-minute window

---

### US4 - Access Control for Grafana (P2)
**As a** system administrator  
**I want** Grafana access restricted to admin/developer accounts  
**So that** business data and system internals are not visible to unauthorized users

**Acceptance Criteria:**
- [ ] Grafana is not publicly accessible (behind VPN or internal network, or protected by auth)
- [ ] Affiliate-level data (clicks, conversions) is not exposed in any public endpoint or dashboard
- [ ] A documented process exists for provisioning new admin/dev Grafana accounts

---

### US5 - Log-Based Error Alerting (P2)
**As a** developer  
**I want** Grafana to alert me when critical errors occur  
**So that** I am notified of production issues without manually watching dashboards

**Acceptance Criteria:**
- [ ] Alerts fire when `Error`-level log volume exceeds threshold within a rolling window
- [ ] Alert notifications are delivered via at least one channel (email or webhook)
- [ ] Alerts include: module name, error message, and timestamp
- [ ] Alert silence/snooze is configurable per alert rule

---

## Out of Scope
- Affiliate-facing dashboards or self-service reporting
- Real-time streaming dashboards (sub-second refresh)
- Metrics exposed via a Prometheus `/metrics` scrape endpoint
- Direct Grafana → MySQL datasource connection
- Multi-tenancy or per-affiliate Grafana organizations

## Success Metrics
- 100% of application log entries are queryable in Grafana within 30 seconds
- Admin team can identify the root cause of a production error within 5 minutes using dashboards alone
- Zero sensitive fields (email, raw IP) appear in Loki log streams at Info level
- System health alerts fire within 5 minutes of a sustained error spike

## Open Questions
- Which alert notification channel is preferred: email, Slack webhook, or PagerDuty?
- Should dashboards be provisioned as code (JSON in repo) or managed manually in Grafana UI?
- Is Grafana hosted in Docker Compose alongside the API, or on a separate server?
