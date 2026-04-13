# Feature Specification: Subscription History List Endpoint

**Feature Branch**: `004-subscription-history-list`  
**Created**: 2026-04-13  
**Status**: Draft  
**Input**: User description: "create a endpoint to return a list of Subscription History (can mock the List instead of creating tables) add pagi, return all if client not ask for pagi"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Retrieve All Subscription History (Priority: P1)

A client calls the subscription history endpoint without any pagination parameters and receives the complete list of all subscription history records in a single response.

**Why this priority**: This is the core deliverable — providing access to subscription history data. All other features (pagination, filtering) build on top of this foundational capability.

**Independent Test**: Can be fully tested by sending a request with no pagination parameters and verifying the full mocked dataset is returned with correct structure and field values.

**Acceptance Scenarios**:

1. **Given** a client sends a request with no pagination parameters, **When** the endpoint is called, **Then** it returns all subscription history records with status 200 and a consistent data structure.
2. **Given** the mocked dataset contains records with various action types (Subscribe/Unsubscribe), **When** the endpoint is called, **Then** all records are included in the response regardless of action type.
3. **Given** the endpoint is called, **When** it responds, **Then** each record contains: timestamp, client name, account number, strategy name, equity connect amount, equity disconnect amount, and action type.

---

### User Story 2 - Retrieve Paginated Subscription History (Priority: P2)

A client calls the subscription history endpoint with pagination parameters (page number and page size) and receives only the records for that specific page along with pagination metadata.

**Why this priority**: Pagination is essential for performance and usability when the dataset is large, allowing clients to load data incrementally.

**Independent Test**: Can be fully tested by requesting page 1 with a small page size and verifying only that slice of records is returned along with correct total count and page metadata.

**Acceptance Scenarios**:

1. **Given** a client sends a request with `page=1` and `pageSize=10`, **When** the endpoint is called, **Then** it returns at most 10 records, the current page number, page size, total record count, and total page count.
2. **Given** a client requests a page beyond the last available page, **When** the endpoint is called, **Then** it returns an empty records list with status 200 and accurate pagination metadata reflecting zero results for that page.
3. **Given** a client sends `pageSize=0` or a negative page size, **When** the endpoint is called, **Then** it returns a 400 Bad Request error with a descriptive validation message.
4. **Given** a client sends `page=0` or a negative page number, **When** the endpoint is called, **Then** it returns a 400 Bad Request error with a descriptive validation message.

---

### Edge Cases

- What happens when the mocked dataset is empty? The endpoint should return an empty list with total count of 0.
- What happens when `pageSize` is very large (e.g., 10,000)? The system should still return all matching records for that page without error.
- What happens when only `page` is provided without `pageSize`? A default page size of 20 is applied.
- What happens when only `pageSize` is provided without `page`? Defaults to page 1.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST expose an HTTP GET endpoint that returns a list of subscription history records.
- **FR-002**: System MUST return all subscription history records when no pagination parameters are provided in the request.
- **FR-003**: System MUST support optional pagination via `page` and `pageSize` query parameters; when provided, only the corresponding page of records is returned.
- **FR-004**: System MUST return pagination metadata alongside paginated results, including: current page, page size, total record count, and total page count.
- **FR-005**: System MUST return each subscription history record with the following fields: timestamp, client name, account number, strategy name, equity connect amount, equity disconnect amount, and action type.
- **FR-006**: System MUST use mocked in-memory data instead of a real database table for the subscription history records.
- **FR-007**: System MUST validate pagination parameters and return a 400 Bad Request response when `page` or `pageSize` values are zero or negative.
- **FR-008**: System MUST return a consistent response envelope regardless of whether pagination is applied, differentiating paginated vs. non-paginated mode through the presence or absence of pagination metadata.

### Key Entities

- **SubscriptionHistoryRecord**: Represents a single subscription event. Key attributes: timestamp (date and time of event), client name (display name of the client), account number (numeric identifier), strategy name (name of the trading strategy), equity connect amount (monetary value at subscription), equity disconnect amount (monetary value at unsubscription, nullable), action type (Subscribe or Unsubscribe).
- **PaginationMetadata**: Describes the paging context of a response. Key attributes: current page number, page size, total record count, total page count.
- **SubscriptionHistoryResponse**: The response envelope returned by the endpoint, containing the list of records and optional pagination metadata when pagination is requested.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: The endpoint responds to all valid requests within 500 milliseconds under normal load conditions.
- **SC-002**: Clients receive all records in a single response when no pagination parameters are provided, with no records missing or duplicated.
- **SC-003**: Paginated responses return exactly the expected slice of records (correct offset and count) with accurate total count metadata.
- **SC-004**: Invalid pagination inputs (zero or negative values) are rejected 100% of the time with a clear, descriptive error message.
- **SC-005**: The endpoint returns a consistent response structure in all scenarios (paginated, non-paginated, empty dataset), allowing clients to parse responses with the same code path.

## Assumptions

- The subscription history data is mocked in-memory at application startup; no database tables or migrations are required for this feature.
- The endpoint does not require authentication for this iteration (auth can be added later if the project authentication layer requires it).
- Default page size when `pageSize` is omitted but `page` is provided is 20 records per page.
- Equity connect and equity disconnect amounts are represented as decimal numbers with currency context understood by the consumer (not formatted as strings).
- Action types are limited to two values: "Subscribe" and "Unsubscribe".
- The endpoint returns records in reverse-chronological order (newest first) by default, consistent with the UI shown in the design reference.
- No filtering (by date range, status, or client) is in scope for this feature; that can be addressed separately.
- Export functionality visible in the UI design is out of scope for this endpoint.
