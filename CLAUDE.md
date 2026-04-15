# CopyTradeMarket Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-04-15

## Active Technologies
- .NET 8 / C# 12 + ASP.NET Core 8, Entity Framework Core 8 (Pomelo MySQL), Serilog, xUnit + Moq (feature/002-email-verification-service)
- MySQL 8.0 (production), SQLite in-memory (integration tests) (feature/002-email-verification-service)
- C# 12 / .NET 8 + ASP.NET Core 8, Swashbuckle (Swagger), xUnit + Moq (003-subscription-history-list)
- N/A — mocked in-memory list (no EF, no migrations) (003-subscription-history-list)
- C# 12 / .NET 8 + None new — `System.Collections.Generic` (already in scope), xUnit (for tests) (feature/003-generic-paged-response)
- N/A — library type only, no persistence (feature/003-generic-paged-response)
- N/A — mocked in-memory static data; no EF, no migrations (feature/005-mock-dashboard-api)

- [e.g., Python 3.11, Swift 5.9, Rust 1.75 or NEEDS CLARIFICATION] + [e.g., FastAPI, UIKit, LLVM or NEEDS CLARIFICATION] (001-update-register-api)

## Project Structure

```text
backend/
frontend/
tests/
```

## Commands

cd src; pytest; ruff check .

## Code Style

[e.g., Python 3.11, Swift 5.9, Rust 1.75 or NEEDS CLARIFICATION]: Follow standard conventions

## Recent Changes
- feature/005-mock-dashboard-api: Added C# 12 / .NET 8 + ASP.NET Core 8, Swashbuckle (Swagger)
- feature/003-generic-paged-response: Added C# 12 / .NET 8 + None new — `System.Collections.Generic` (already in scope), xUnit (for tests)
- 003-subscription-history-list: Added C# 12 / .NET 8 + ASP.NET Core 8, Swashbuckle (Swagger), xUnit + Moq


<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
