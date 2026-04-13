# Feature Specification: Subscription History List Endpoint

**Feature Branch**: `004-subscription-history-list`  
**Created**: 2026-04-13  
**Updated**: 2026-04-13 (added filtering and ordering)  
**Status**: Updated  
**Input**: "create a endpoint to return a list of Subscription History (can mock the List instead of creating tables) add pagi, return all if client not ask for pagi" + "Update Spec 004 to contain query like and OrderBy also"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Retrieve All Subscription History (Priority: P1)

A client calls the subscription history endpoint without any parameters and receives the complete list of all subscription history records in a single response, ordered newest first by default.

**Why this priority**: This is the core deliverable — providing access to subscription history data. All other features (pagination, filtering, ordering) build on top of this foundational capability.

**Independent Test**: Can be fully tested by sending a request with no parameters and verifying the full mocked dataset is returned with correct structure and field values in reverse-chronological order.

**Acceptance Scenarios**:

1. **Given** a client sends a request with no parameters, **When** the endpoint is called, **Then** it returns all subscription history records with status 200 and a consistent data structure.
2. **Given** the mocked dataset contains records with various action types (Subscribe/Unsubscribe), **When** the endpoint is called, **Then** all records are included in the response regardless of action type.
3. **Given** the endpoint is called, **When** it responds, **Then** each record contains: timestamp, client name, account number, strategy name, equity connect amount, equity disconnect amount, and action type.
4. **Given** no ordering parameter is provided, **When** the endpoint responds, **Then** records are returned newest first (descending timestamp).

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

### User Story 3 - Filter Subscription History by Query (Priority: P2)

A client provides a search query and receives only the subscription history records that match the criteria.

**Why this priority**: Filtering allows clients to find relevant records quickly without processing the full dataset on their side.

**Independent Test**: Can be fully tested by supplying a `query` value that matches only a subset of records and verifying only matching records are returned with an accurate total count.

**Acceptance Scenarios**:

1. **Given** a client sends `query=Alice`, **When** the endpoint is called, **Then** only records whose client name, account number, or strategy name contains "Alice" (case-insensitive) are returned with status 200.
2. **Given** a client sends a `query` value that matches no records, **When** the endpoint is called, **Then** it returns an empty list with status 200 and a total count of 0.
3. **Given** the `query` parameter is combined with pagination, **When** the endpoint is called, **Then** pagination applies to the filtered dataset (not the full dataset), and the total count reflects the number of matching records.

---

### User Story 4 - Sort Subscription History by Field and Direction (Priority: P3)

A client provides an ordering field and/or direction and receives records sorted accordingly.

**Why this priority**: Flexible ordering lets clients consume data in the order most useful to them without client-side sorting.

**Independent Test**: Can be fully tested by requesting `orderBy=clientName&orderDirection=asc` and verifying records are returned in ascending alphabetical order by client name.

**Acceptance Scenarios**:

1. **Given** a client sends `orderBy=clientName&orderDirection=asc`, **When** the endpoint is called, **Then** records are sorted alphabetically by client name in ascending order.
2. **Given** a client sends `orderBy=timestamp&orderDirection=asc`, **When** the endpoint is called, **Then** records are sorted oldest first.
3. **Given** a client sends `orderBy=equityConnect&orderDirection=desc`, **When** the endpoint is called, **Then** records are sorted by equity connect amount from highest to lowest.
4. **Given** a client sends an invalid `orderBy` field name, **When** the endpoint is called, **Then** it returns a 400 Bad Request error with a descriptive validation message listing the allowed field names.
5. **Given** a client sends an invalid `orderDirection` value, **When** the endpoint is called, **Then** it returns a 400 Bad Request error.
6. **Given** ordering is combined with pagination, **When** the endpoint is called, **Then** ordering is applied first, then the correct page slice is extracted.

---

### Edge Cases

- What happens when the mocked dataset is empty? The endpoint should return an empty list with total count of 0.
- What happens when `pageSize` is very large (e.g., 10,000)? The system should still return all matching records for that page without error.
- What happens when only `page` is provided without `pageSize`? A default page size of 20 is applied.
- What happens when only `pageSize` is provided without `page`? Defaults to page 1.
- What happens when `query` is an empty string? It is treated as no query filter — all records are returned.
- What happens when `orderBy` is omitted? The default ordering is by timestamp descending (newest first).
- What happens when `orderDirection` is provided without `orderBy`? The direction is applied to the default `timestamp` field.
- What happens when all three features are combined (query + pagination + ordering)? Filter is applied first, then ordering, then pagination slice.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST expose an HTTP GET endpoint that returns a list of subscription history records.
- **FR-002**: System MUST return all subscription history records when no pagination, filter, or ordering parameters are provided.
- **FR-003**: System MUST support optional pagination via `page` and `pageSize` query parameters; when provided, only the corresponding page of records is returned.
- **FR-004**: System MUST return pagination metadata alongside paginated results, including: current page, page size, total record count, and total page count.
- **FR-005**: System MUST return each subscription history record with the following fields: timestamp, client name, account number, strategy name, equity connect amount, equity disconnect amount, and action type.
- **FR-006**: System MUST use mocked in-memory data instead of a real database table for the subscription history records.
- **FR-007**: System MUST validate pagination parameters and return a 400 Bad Request response when `page` or `pageSize` values are zero or negative.
- **FR-008**: System MUST return a consistent response envelope regardless of whether pagination is applied, differentiating paginated vs. non-paginated mode through the presence or absence of pagination metadata.
- **FR-009**: System MUST support an optional `query` parameter that filters records by performing a case-insensitive partial match against client name, account number, and strategy name simultaneously.
- **FR-010**: System MUST apply filter parameters before applying pagination, so that total count and page slices reflect the filtered dataset.
- **FR-011**: System MUST support an optional `orderBy` parameter that sorts records by one of the allowed fields: `timestamp`, `clientName`, `accountNumber`, `strategyName`, `equityConnect`. The default when omitted is `timestamp`.
- **FR-012**: System MUST support an optional `orderDirection` parameter with accepted values `asc` and `desc`. The default when omitted is `desc`.
- **FR-013**: System MUST validate the `orderBy` and `orderDirection` parameters and return a 400 Bad Request response when an unrecognised value is supplied.
- **FR-014**: System MUST apply ordering after filtering and before pagination slicing, so the page slice reflects the correctly ordered and filtered dataset.

### Key Entities

- **SubscriptionHistoryRecord**: Represents a single subscription event. Key attributes: timestamp (date and time of event), client name (display name of the client), account number (numeric identifier), strategy name (name of the trading strategy), equity connect amount (monetary value at subscription), equity disconnect amount (monetary value at unsubscription, nullable), action type (Subscribe or Unsubscribe).
- **PaginationMetadata**: Describes the paging context of a response. Key attributes: current page number, page size, total record count, total page count.
- **SubscriptionHistoryResponse**: The response envelope returned by the endpoint, containing the list of records and optional pagination metadata when pagination is requested.
- **FilterCriteria**: The optional filtering input supplied by the caller. Key attribute: query string (partial match text).
- **SortCriteria**: The optional ordering inputs supplied by the caller. Key attributes: order by field name, order direction (ascending or descending).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: The endpoint responds to all valid requests within 500 milliseconds under normal load conditions.
- **SC-002**: Clients receive all records in a single response when no pagination parameters are provided, with no records missing or duplicated.
- **SC-003**: Paginated responses return exactly the expected slice of records (correct offset and count) with accurate total count metadata.
- **SC-004**: Invalid pagination inputs (zero or negative values) are rejected 100% of the time with a clear, descriptive error message.
- **SC-005**: The endpoint returns a consistent response structure in all scenarios (paginated, non-paginated, empty dataset), allowing clients to parse responses with the same code path.
- **SC-006**: Filtered responses return only records matching all supplied filter criteria, with a total count that reflects only matching records.
- **SC-007**: Ordered responses return records in the correct sequence for the requested field and direction; the order is stable (consistent across repeated identical requests).
- **SC-008**: Invalid ordering inputs (unrecognised `orderBy` field or `orderDirection` value) are rejected 100% of the time with a clear error message listing allowed values.

## Assumptions

- The subscription history data is mocked in-memory at application startup; no database tables or migrations are required for this feature.
- The endpoint does not require authentication for this iteration (auth can be added later if the project authentication layer requires it).
- Default page size when `pageSize` is omitted but `page` is provided is 20 records per page.
- Equity connect and equity disconnect amounts are represented as decimal numbers with currency context understood by the consumer (not formatted as strings).
- Action types are limited to two values: "Subscribe" and "Unsubscribe".
- The default sort order is by timestamp descending (newest first) when no ordering parameters are provided, consistent with the UI design reference.
- The `query` filter performs a partial, case-insensitive match against client name, account number, and strategy name. It does not filter on timestamp or equity amounts.
- An empty or whitespace-only `query` value is treated equivalently to omitting the parameter (no filter applied).
- When `orderDirection` is provided without `orderBy`, the direction is applied to the default field (timestamp).
- Processing order for each request: filter → sort → paginate.
- Export functionality visible in the UI design is out of scope for this endpoint.
