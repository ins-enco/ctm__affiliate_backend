# Research: Generic Paginated Response

**Branch**: `feature/003-generic-paged-response`  
**Phase**: 0 — Outline & Research

---

## Decision 1: Placement in CopyTradeMarketApi.Shared

**Decision**: Add `PagedResponse<T>` to `Backend/src/Shared/CopyTradeMarketApi.Shared/Responses/PagedResponse.cs`.

**Rationale**: `CopyTradeMarketApi.Shared` is already referenced by all module Application projects (Auth.Application, Affiliate.Application, Tracking.Application, future SubscriptionHistory.Application). Adding the type there makes it immediately available to every module without any new project dependency. The `Responses/` subfolder follows the existing pattern of organizing types by concern (e.g., `Abstractions/`, `Exceptions/`, `Validation/`, `Cache/`).

**Alternatives considered**:
- Create a separate `CopyTradeMarketApi.Pagination` project — rejected: unnecessary project proliferation for a single type; every module would need a new reference.
- Put it inline in each module — rejected: this is exactly the duplication the feature is designed to eliminate.
- Add to the Host project — rejected: Host is not referenced by module Application layers; the type would be unreachable from services.

---

## Decision 2: C# Record vs Class

**Decision**: Use `public record PagedResponse<T>` (positional record).

**Rationale**: Records are the established convention for DTOs in this codebase (constitution C# conventions: "Records for DTOs — immutable, value semantics"). A positional record gives immutability, structural equality, and deconstruction for free. JSON serialization of records works correctly with `System.Text.Json` in ASP.NET Core 8.

**Alternatives considered**:
- `class` with init-only properties — rejected: more verbose for no benefit; constitution explicitly favors records for DTOs.
- `struct` / `readonly struct` — rejected: reference list field (`IReadOnlyList<T>`) makes struct semantics awkward; stack allocation offers no benefit here.

---

## Decision 3: Static Factory Methods

**Decision**: Provide two static factory methods on the record: `PagedResponse<T>.All(items)` and `PagedResponse<T>.Paginated(items, totalCount, page, pageSize)`.

**Rationale**: Without factories, every call site must manually compute `TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)` — a repeatable calculation that belongs in one place. `All()` further reduces boilerplate by inferring `TotalCount` from `items.Count`. The factories do not add complexity — they are simple one-liners on the record.

**Alternatives considered**:
- No factories, require callers to use the positional constructor — rejected: `TotalPages` computation would be duplicated across every service that paginates.
- Separate static `PagedResponseFactory` class — rejected: adds a second file and a second type for trivial two-method logic; static methods on the record itself are discoverable and idiomatic in C# 12.

---

## Decision 4: Generic Constraints

**Decision**: No generic constraints on `T` (`public record PagedResponse<T>` — unconstrained).

**Rationale**: The type holds a list and metadata — it never calls methods on `T`, never compares `T` values, and never needs `T` to be serializable at the type level (serialization is handled by the host's JSON pipeline at runtime). Constraining to `class` or an interface would unnecessarily restrict use with value types or future types.

**Alternatives considered**:
- `where T : class` — rejected: rules out potential use with value-type items; no benefit since the type never operates on `T`.
- `where T : IDto` — rejected: would require a marker interface on every DTO in the codebase; heavyweight for zero runtime benefit.

---

## Decision 5: Namespace

**Decision**: `namespace CopyTradeMarketApi.Shared.Responses`.

**Rationale**: Follows the existing Shared project namespace pattern (`CopyTradeMarketApi.Shared.Abstractions`, `CopyTradeMarketApi.Shared.Exceptions`, `CopyTradeMarketApi.Shared.Validation`). Consistent with the folder name `Responses/`.

---

## Decision 6: Test Project

**Decision**: Add a new `Backend/tests/CopyTradeMarketApi.Shared.Tests/` project for unit tests of the Shared type.

**Rationale**: The existing module test projects (Auth.Application.Tests, etc.) should not carry tests for unrelated Shared types — that violates single-responsibility for test organization. A dedicated Shared.Tests project keeps Shared's tests co-located with the Shared source. The project is lightweight: no EF, no Moq needed — just xUnit and the Shared assembly.

**Alternatives considered**:
- Add tests to `Integration.Tests` — rejected: Integration.Tests tests full HTTP round-trips; record instantiation and serialization tests are unit tests.
- Skip tests entirely — rejected: constitution DoD requires unit tests pass; a shared type used by all modules should have its own test coverage.

---

## Decision 7: JSON Serialization Compatibility

**Decision**: No explicit `[JsonPropertyName]` attributes needed. The host's global `JsonNamingPolicy.CamelCase` policy converts `TotalCount` → `totalCount`, `PageSize` → `pageSize`, etc. automatically.

**Rationale**: All existing DTOs in the codebase rely on the host-level camelCase policy without per-property attributes. `PagedResponse<T>` follows the same pattern. The field names chosen (`Items`, `TotalCount`, `Page`, `PageSize`, `TotalPages`) all camelCase correctly and match the contract defined in spec 004.

**Alternatives considered**:
- Add explicit `[JsonPropertyName]` attributes — rejected: unnecessary; breaks consistency with how all other DTOs work in this project.
