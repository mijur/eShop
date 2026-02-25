# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Quick Commands

**Build and Run:**
```bash
# Build the solution
dotnet build eShop.Web.slnf

# Run the full application with Aspire orchestration
dotnet run --project src/eShop.AppHost/eShop.AppHost.csproj

# Run a single unit test project
dotnet test tests/Basket.UnitTests/Basket.UnitTests.csproj

# Run e2e tests (requires running app on localhost:5045)
npx playwright test
```

**View Aspire Dashboard:**
After running AppHost, look for a URL like: `http://localhost:19888/login?t=<token>` in console output.

## Architecture Overview

eShop is a microservices-based e-commerce application using **.NET Aspire** for service orchestration and infrastructure management.

### Core Services (in `src/`)

- **eShop.AppHost**: Aspire host that orchestrates all services and infrastructure (PostgreSQL, Redis, RabbitMQ)
- **WebApp**: Main web UI (Blazor/ASP.NET Core)
- **Catalog.API**: Product catalog service (PostgreSQL backend)
- **Basket.API**: Shopping cart service (Redis-backed)
- **Ordering.API**: Order management (PostgreSQL, Domain-Driven Design)
- **Identity.API**: Authentication/authorization service
- **OrderProcessor**: Background service processing orders from RabbitMQ
- **PaymentProcessor**: Background service processing payments
- **Webhooks.API**: Webhook management for external integrations
- **mobile-bff**: YARP reverse proxy for mobile client routing

### Data & Messaging

- **PostgreSQL** (with pgvector extension): Stores catalog, identity, orders, webhooks data
- **Redis**: Basket/cache data
- **RabbitMQ**: Event bus for async communication between services

### Key Infrastructure Files

- `eShop.Web.slnf`: Filtered solution file (web components only; use `eShop.slnx` for full solution)
- `Directory.Build.props`: Centralized NuGet package management (MSTest.Sdk 4.0.2, Aspire 13.1)
- `Directory.Build.targets`: Shared build configuration
- `global.json`: SDK version lock (.NET 10.0.100) and test runner config (Microsoft.Testing.Platform)

## Testing Strategy

### Unit Tests
- Located in `tests/<Project>.UnitTests/`
- Technologies: MSTest, NSubstitute (mocking)
- Run isolated from infrastructure
- **Example**: `tests/Basket.UnitTests/`, `tests/Ordering.UnitTests/`

### Functional Tests
- Located in `tests/<Project>.FunctionalTests/`
- **Requires Docker** and Aspire to spin up test containers
- Tests real service behavior with actual dependencies
- **Example**: `tests/Catalog.FunctionalTests/`, `tests/Ordering.FunctionalTests/`

### End-to-End Tests
- Located in `e2e/` (TypeScript/Playwright)
- Tests full user workflows through the web UI
- Configured in `playwright.config.ts` (baseURL: `http://localhost:5045`)
- Runs AppHost automatically via webServer config

## Development Workflow

1. **Modify code** in `src/<ServiceName>/`
2. **Write tests** in corresponding `tests/` directories
3. **Run AppHost** to validate service integration: `dotnet run --project src/eShop.AppHost/eShop.AppHost.csproj`
4. **Test with Aspire Dashboard** to monitor services and logs
5. **For functional tests**: Ensure Docker is running, then `dotnet test tests/<Project>.FunctionalTests/`

## Common Patterns & Dependencies

- **Dependency Injection**: All services use .NET DI via Aspire configuration
- **EF Core**: PostgreSQL databases use Entity Framework with migrations
- **gRPC**: Internal service-to-service communication (Basket.API uses gRPC)
- **Health Checks**: Services expose `/health` endpoints for orchestration
- **Event Bus**: Services publish/subscribe domain events via RabbitMQ
- **Aspire References**: Services reference infrastructure via `WithReference()` to get connection strings automatically

## Architecture Boundaries

**Domain-Driven Design (DDD) Scope:**
- **Ordering.API** is the ONLY service using DDD
  - `Ordering.Domain/`: Aggregates and domain logic (Order aggregate, invariants)
  - `Ordering.Infrastructure/`: EF Core repositories, Unit of Work
  - `Ordering.API/Application`: CQRS with MediatR handlers for commands/queries
- **Catalog.API** and **Basket.API** are intentionally kept simple with CRUD patterns
  - Direct EF Core DbContext access is appropriate here
  - DO NOT add aggregates, repositories, or domain events — unnecessary abstraction adds complexity without benefit

**Service Communication:**
- Inter-service communication is EXCLUSIVELY through **integration events** (EventBus/RabbitMQ)
- **NEVER** call other services via direct HTTP — this creates tight coupling and violates bounded contexts
- Each service maintains its own data model; no cross-service database access

**API Design:**
- **Minimal APIs** only — never use Controllers
- Services expose typed HTTP endpoints through MapGet/MapPost etc.
- WebApp consumes services through **typed HTTP clients** (not project references)

**Data Isolation:**
- Each service has its own database instance (or schema in PostgreSQL)
- Sharing data between services is ONLY through events, never shared tables

## Testing Conventions

**Test Framework & Patterns:**
- Use **xUnit + FluentAssertions** for all new tests
- Test file locations: `tests/{ServiceName}.UnitTests/` and `tests/{ServiceName}.FunctionalTests/`
- **Naming convention**: `MethodName_Scenario_ExpectedBehavior`
  - Example: `PlaceOrder_WithInvalidPaymentMethod_ThrowsValidationException()`

**What to Test:**
- **Ordering** service: Test aggregate invariants (what the Order aggregate enforces)
- **Catalog & Basket**: Test validation, happy paths, and edge cases
- Use **NSubstitute** for mocking external dependencies
- Functional tests validate real behavior with Aspire containers (use sparingly due to Docker overhead)

**Test Isolation:**
- Unit tests must not depend on running infrastructure
- Functional tests bootstrap services through Aspire; they are slower but validate real integration
- E2E tests (Playwright) test complete user workflows through the UI

## Do NOT Touch

**Why these files are restricted:**

- **`eShop.AppHost/Program.cs`**: Orchestrates all infrastructure (databases, message bus, services). Changes here cascade across the entire system and require careful validation. Only modify with explicit permission.

- **`Directory.Packages.props`**: Centralized NuGet version management. Uncontrolled changes can break multiple services. NuGet updates must go through review and testing.

- **`Directory.Build.props`**: Global build settings, compiler options, and analyzers. Changes affect all projects and CI/CD pipeline.

- **Existing EF Core migrations** in `src/*/Migrations/`: Migrations may already be applied in production or test environments. Modifying them can corrupt database state. If a migration is wrong, create a NEW migration to fix it.

- **`.github/` directory**: Contains CI/CD pipeline definition (GitHub Actions, Azure Pipelines). Changes require manual review and can break automated builds/deployments.

## Important Notes

- **Docker Required**: Functional tests and full application runtime require Docker Desktop running
- **Aspire Dashboard**: Provides visibility into all services, logs, and metrics
- **.env for e2e tests**: Playwright reads environment variables from `.env` if present
- **Test Parallelization**: Unit tests run in parallel (configured in GlobalUsings.cs)
- **CI Pipeline**: Uses Azure Pipelines (see `ci.yml`); builds `eShop.Web.slnf` and runs tests
