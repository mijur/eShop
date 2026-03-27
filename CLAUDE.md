# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Important Instructions

- **Fully honor any blocks coming from tool hooks** — when tool hooks provide structured information or guidance blocks, follow their instructions precisely and completely. Do not try to override or ignore them. They are there to ensure you produce code that fits the project's standards and patterns.

## What This Is

eShop is a .NET reference application demonstrating microservices architecture using .NET Aspire for orchestration. It includes multiple web APIs, background workers, frontend apps, and shared libraries.

## Prerequisites

- .NET 10 SDK (global.json requires 10.0.100 with `allowPrerelease: true`)
- Docker Desktop (required for infrastructure containers and functional tests)
- Node.js (for Playwright E2E tests only)

## Build & Run Commands

```bash
# Build entire solution
dotnet build eShop.slnx

# Build web projects only (excludes MAUI)
dotnet build eShop.Web.slnf

# Run the application (starts all services via Aspire)
dotnet run --project src/eShop.AppHost/eShop.AppHost.csproj

# Run all tests
dotnet test eShop.slnx

# Run a single test project
dotnet test tests/Ordering.UnitTests/

# Run a specific test by name
dotnet test tests/Ordering.UnitTests/ --filter "FullyQualifiedName~TestMethodName"

# Functional tests (require Docker running)
dotnet test tests/Catalog.FunctionalTests/
dotnet test tests/Ordering.FunctionalTests/

# E2E tests (require app running + Node.js)
npx playwright test
```

## Build Configuration

- **TreatWarningsAsErrors** is enabled globally via `Directory.Build.props`
- **UseArtifactsOutput** is enabled — build output goes to `artifacts/` instead of per-project `bin/obj`
- Central package versioning via `Directory.Packages.props` — specify versions there, not in individual .csproj files
- Test runner is **Microsoft.Testing.Platform** (configured in global.json)
- Unit tests use **MSTest.Sdk 4.0.2**; functional tests use **xUnit v3 via MTP**

## Architecture

### Aspire Orchestration

`src/eShop.AppHost` is the entry point that orchestrates all services and infrastructure:

**Infrastructure containers:** PostgreSQL (with pgvector), Redis, RabbitMQ (persistent lifetime)

**Databases:** catalogdb, identitydb, orderingdb, webhooksdb (all PostgreSQL)

### Services

| Service | Type | Key Dependencies |
|---|---|---|
| **Catalog.API** | Web API | catalogdb, RabbitMQ, optional AI (OpenAI/Ollama for vector search) |
| **Basket.API** | Web API + gRPC | Redis, RabbitMQ, Identity |
| **Ordering.API** | Web API (DDD) | orderingdb, RabbitMQ, Identity |
| **Identity.API** | OpenID Connect (Duende IdentityServer) | identitydb |
| **Webhooks.API** | Web API | webhooksdb, RabbitMQ, Identity |
| **OrderProcessor** | Background worker | orderingdb, RabbitMQ (waits for Ordering.API migrations) |
| **PaymentProcessor** | Background worker | RabbitMQ |
| **WebApp** | Blazor/Razor Pages | basket-api, catalog-api, ordering-api, RabbitMQ |
| **mobile-bff** | YARP reverse proxy | Routes to catalog-api, ordering-api, identity-api |

### Shared Projects

- **eShop.ServiceDefaults** — Common service configuration: OpenTelemetry, health checks, API versioning, JWT auth, OpenAPI/Scalar
- **EventBus / EventBusRabbitMQ** — Abstract event bus with RabbitMQ implementation for integration events
- **IntegrationEventLogEF** — Durable integration event storage via EF Core
- **Ordering.Domain** — DDD domain model (entities, value objects, aggregates, domain events)
- **Ordering.Infrastructure** — EF Core data access with migrations for the ordering bounded context

### Key Patterns

- **Event-driven communication** between services via RabbitMQ integration events
- **Domain-Driven Design** in the Ordering bounded context (domain layer, infrastructure layer, API layer)
- **Database migrations** auto-applied on startup via `AddMigration<TContext, TSeed>()` extension
- **gRPC** for high-performance Basket service communication (see `basket.proto`)
- **YARP** reverse proxy as mobile BFF with path prefix stripping
- **OAuth 2.0 / OpenID Connect** via Identity.API for auth flows
- **AI/LLM integration** is optional — controlled by Aspire parameters for OpenAI, Azure OpenAI, or Ollama

### Test Organization

- **Unit tests** (`tests/*UnitTests/`): MSTest.Sdk, fast, no containers, mocks via NSubstitute
- **Functional tests** (`tests/*FunctionalTests/`): xUnit v3, Aspire-based, spin up real containers — require Docker
- **E2E tests** (`e2e/`): Playwright (TypeScript), browser automation against running app on `localhost:5045`
- **Never use FluentAssertions** — use the built-in assertions from MSTest or xUnit instead
