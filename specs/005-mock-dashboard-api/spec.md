---
id: 005-mock-dashboard-api
version: 1.0.0
status: draft
owners:
  - tech-lead
  - engineering
last-reviewed: 2026-04-15
---

# Feature Specification: Mock Module — Dashboard API

**Feature Branch**: `005-mock-dashboard-api`  
**Created**: 2026-04-15  
**Status**: Draft  
**Jira**: [CR-22 — Create mock API for Dashboard screen](https://insenco.atlassian.net/browse/CR-22)  
**Input**: "create Mock Module like in the document" (CR-22: 5 in-memory endpoints for the Dashboard screen)

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Retrieve User List for Dropdown Search (Priority: P1)

A consumer of the dashboard API requests the list of all users and receives a flat collection of user records, each containing an ID, display name, and role. This list powers the dropdown search component on the dashboard.

**Why this priority**: The user dropdown is the primary navigation control on the dashboard. Without this data, the dashboard UI cannot render its core interaction. All role-filtered views depend on this list.

**Independent Test**: Can be fully tested by calling the users endpoint with no parameters and verifying the response contains multiple users, each with a non-empty ID, name, and a role from the allowed set (Client, Signal Provider, Affiliate).

**Acceptance Scenarios**:

1. **Given** the endpoint is called in the Development environment, **When** it responds, **Then** it returns a list of user records with status 200, each containing: id, name, and role.
2. **Given** the response is received, **When** the role field is inspected for any record, **Then** it is one of: `Client`, `Signal Provider`, or `Affiliate`.
3. **Given** the endpoint is called, **When** it responds, **Then** the list contains at least 5 users covering all three role types.
4. **Given** the application is running in a non-Development environment, **When** any mock endpoint is called, **Then** the server returns HTTP 404 Not Found.

---

### User Story 2 - Retrieve Current Active User Information (Priority: P1)

A consumer requests the current active user endpoint and receives a single record representing the currently logged-in user, including display name, ID, two-character abbreviation, and role. This data powers the user profile/header section of the dashboard.

**Why this priority**: The active user identity block appears on every dashboard screen. It is required for the header to render correctly and is independent of all list data.

**Independent Test**: Can be fully tested by calling the current-user endpoint and verifying a single object is returned with all four fields present and populated.

**Acceptance Scenarios**:

1. **Given** the endpoint is called, **When** it responds, **Then** it returns a single user object with status 200 containing: id, name, abbreviation (2 characters), and role.
2. **Given** the response is received, **When** the abbreviation field is inspected, **Then** it is exactly 2 characters (e.g., `"CS"` for "Carlos Silva").
3. **Given** the response is received, **When** the role is inspected, **Then** it matches one of the allowed role values.

---

### User Story 3 - Retrieve Client Requests List (Priority: P2)

A consumer requests the client requests endpoint and receives exactly 10 records representing recent client subscription or strategy requests, each with timestamp, client name, equity amount, strategy name, and strategy license.

**Why this priority**: Client requests is one of three dashboard list panels. It can be developed and tested independently without the signal provider or affiliate panels.

**Independent Test**: Can be fully tested by calling the client-requests endpoint and verifying exactly 10 records are returned with all required fields populated.

**Acceptance Scenarios**:

1. **Given** the endpoint is called, **When** it responds, **Then** it returns exactly 10 client request records with status 200.
2. **Given** the response is received, **When** each record is inspected, **Then** it contains: timestamp, name, equity (decimal), strategy, and strategyLicense.
3. **Given** the equity field is inspected for any record, **Then** it is a positive decimal number.

---

### User Story 4 - Retrieve Signal Provider Requests List (Priority: P2)

A consumer requests the signal provider requests endpoint and receives exactly 10 records representing pending KYC or onboarding requests from signal providers, each with timestamp, name, and KYC status.

**Why this priority**: Signal provider requests is the second dashboard list panel. It shares the same KYC data shape as affiliate requests and can be developed in parallel with User Story 5.

**Independent Test**: Can be fully tested by calling the signal-provider-requests endpoint and verifying exactly 10 records with timestamp, name, and a valid KYC status value.

**Acceptance Scenarios**:

1. **Given** the endpoint is called, **When** it responds, **Then** it returns exactly 10 signal provider request records with status 200.
2. **Given** the response is received, **When** each record is inspected, **Then** it contains: timestamp, name, and kycStatus.
3. **Given** the kycStatus field is inspected for any record, **Then** it is one of: `Pending`, `Verified`, or `Rejected`.

---

### User Story 5 - Retrieve Affiliate Requests List (Priority: P2)

A consumer requests the affiliate requests endpoint and receives exactly 10 records representing pending KYC or onboarding requests from affiliates, each with timestamp, name, and KYC status.

**Why this priority**: Affiliate requests is the third dashboard list panel. Identical data shape to signal provider requests; independently testable.

**Independent Test**: Can be fully tested by calling the affiliate-requests endpoint and verifying exactly 10 records with timestamp, name, and a valid KYC status value.

**Acceptance Scenarios**:

1. **Given** the endpoint is called, **When** it responds, **Then** it returns exactly 10 affiliate request records with status 200.
2. **Given** the response is received, **When** each record is inspected, **Then** it contains: timestamp, name, and kycStatus.
3. **Given** the kycStatus field is inspected for any record, **Then** it is one of: `Pending`, `Verified`, or `Rejected`.

---

### Edge Cases

- What happens when the user list endpoint is called multiple times? It returns the same static data every time (fully deterministic, no randomness).
- What happens if a consumer sends unexpected query parameters to any mock endpoint? They are silently ignored — each endpoint always returns its full fixed dataset.
- What happens if the abbreviation must be derived from a single-word name? The first two letters of that name are used in uppercase (e.g., `"Na"` for `"Nam"`).
- What happens when KYC status is compared between signal provider and affiliate requests? Both use the same allowed set: Pending, Verified, Rejected — same validation applies to both.
- What happens when a consumer calls a mock endpoint with a method other than GET (e.g., POST)? The server returns HTTP 405 Method Not Allowed.
- What happens when any mock endpoint is called in a non-Development environment (e.g., Production, Staging)? The server returns HTTP 404 Not Found — the endpoints are not registered outside the Development environment.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST expose a `GET /api/mock/users` endpoint that returns a list of user records, each containing: id, name, and role.
- **FR-002**: System MUST expose a `GET /api/mock/current-user` endpoint that returns a single user object containing: id, name, abbreviation (2-character string), and role.
- **FR-003**: System MUST expose a `GET /api/mock/client-requests` endpoint that returns exactly 10 client request records, each containing: timestamp, name, equity (decimal), strategy, and strategyLicense.
- **FR-004**: System MUST expose a `GET /api/mock/signal-provider-requests` endpoint that returns exactly 10 signal provider request records, each containing: timestamp, name, and kycStatus.
- **FR-005**: System MUST expose a `GET /api/mock/affiliate-requests` endpoint that returns exactly 10 affiliate request records, each containing: timestamp, name, and kycStatus.
- **FR-006**: System MUST serve all five endpoints from mocked in-memory data — no database tables or migrations required.
- **FR-007**: The `role` field in user list records MUST be one of: `Client`, `Signal Provider`, `Affiliate`. The user list MUST contain at least one record of each role type.
- **FR-008**: The `kycStatus` field in signal provider and affiliate request records MUST be one of: `Pending`, `Verified`, `Rejected`.
- **FR-009**: The `equity` field in client request records MUST be a positive decimal value.
- **FR-010**: All five endpoints MUST return HTTP 200 with a consistent JSON structure for all valid GET requests.
- **FR-011**: All five mock endpoints MUST only be available in the Development environment. In any other environment (Staging, Production, etc.), the endpoints MUST NOT be registered — callers receive HTTP 404 Not Found.

### Key Entities

- **User**: Represents a platform user in the dropdown search list. Key attributes: id (integer identifier), name (display name), role (one of: Client, Signal Provider, Affiliate).
- **CurrentActiveUser**: Represents the currently logged-in dashboard user shown in the header. Key attributes: id (integer), name (display name), abbreviation (2-character initials), role.
- **ClientRequest**: Represents a client's subscription or strategy request. Key attributes: timestamp (UTC datetime of request), name (client display name), equity (monetary decimal value), strategy (strategy name), strategyLicense (license identifier string).
- **SignalProviderRequest**: Represents a signal provider's KYC or onboarding request. Key attributes: timestamp (UTC datetime), name (signal provider display name), kycStatus (one of: Pending, Verified, Rejected).
- **AffiliateRequest**: Represents an affiliate's KYC or onboarding request. Key attributes: timestamp (UTC datetime), name (affiliate display name), kycStatus (one of: Pending, Verified, Rejected).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All five endpoints respond to any valid GET request within 500 milliseconds under normal load conditions.
- **SC-002**: The client requests, signal provider requests, and affiliate requests endpoints each return exactly 10 records — verified by automated tests.
- **SC-003**: The user list endpoint returns records covering all three role types (Client, Signal Provider, Affiliate) in a single response.
- **SC-004**: All required fields are present and non-null in every record returned by every endpoint, verified by automated contract tests.
- **SC-005**: All five endpoints return HTTP 200 with a well-formed JSON body on 100% of valid GET requests in the Development environment.
- **SC-006**: All five mock endpoints return HTTP 404 on 100% of valid GET requests when the application is running in any non-Development environment.

## Assumptions

- No authentication or authorization is required for any of the five mock endpoints — they exist solely for dashboard UI development and demo purposes.
- All five mock endpoints are only registered and available when the application is running in the Development environment. They are not registered in Staging, Production, or any other environment.
- All mock data is static and read-only; no write operations (POST, PUT, DELETE) are in scope.
- None of the five endpoints require pagination or filtering — each returns its full fixed dataset on every call.
- Role values are limited to exactly three types: `Client`, `Signal Provider`, `Affiliate`.
- KYC status values are limited to three states: `Pending`, `Verified`, `Rejected`.
- The abbreviation in `CurrentActiveUser` is a 2-character string representing the user's initials (first letter of first name + first letter of last name, uppercased). For single-word names, the first two letters of that name are used.
- Equity values in `ClientRequest` records represent monetary amounts in USD; no currency symbol is included in the response.
- All timestamps are UTC in ISO 8601 format.
- The `strategyLicense` field in `ClientRequest` is a short string identifier; its format is not validated by the API.
- These five mock endpoints will be superseded by real data-backed endpoints in a future iteration; this feature is scoped to the mock layer only.
- The module follows the existing modular-monolith pattern (API + Application layers, no Domain/Infrastructure), consistent with the SubscriptionHistory module (feature 004).
