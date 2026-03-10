# CLAUDE.md — Ordering.API

## Overview

Ordering.API is the order management microservice in the eShop application. It implements **CQRS**, **Domain-Driven Design (DDD)**, and **event-driven architecture** with MediatR, FluentValidation, and RabbitMQ integration events.

## Project Structure

```
src/Ordering.API/
├── Program.cs                    # Minimal API host configuration
├── Program.Testing.cs            # Public Program class for test fixtures
├── Apis/
│   └── OrdersApi.cs              # Minimal API endpoint definitions
├── Application/
│   ├── Behaviors/                # MediatR pipeline behaviors
│   │   ├── LoggingBehavior.cs
│   │   ├── ValidatorBehavior.cs
│   │   └── TransactionBehavior.cs
│   ├── Commands/                 # CQRS command + handler pairs
│   ├── DomainEventHandlers/      # Domain event → integration event bridge
│   ├── IntegrationEvents/
│   │   ├── Events/               # Integration event definitions
│   │   └── EventHandling/        # Inbound integration event handlers
│   ├── Models/                   # DTOs (BasketItem, CustomerBasket)
│   ├── Queries/                  # CQRS read-side (IOrderQueries, OrderViewModel)
│   └── Validations/              # FluentValidation validators
├── Extensions/
│   └── Extensions.cs             # DI registration (AddApplicationServices)
└── Infrastructure/               # Logging utilities (OrderingApiTrace)
```

### Sibling Projects (same bounded context)

| Project | Role |
|---------|------|
| `Ordering.Domain` | DDD domain model: aggregates (Order, Buyer), value objects (Address), domain events, repository interfaces |
| `Ordering.Infrastructure` | EF Core DbContext (OrderingContext), repository implementations, entity configurations, migrations, idempotency (RequestManager) |

## Build & Run

```bash
# Build just Ordering.API
dotnet build src/Ordering.API/

# Run via Aspire (starts all dependencies)
dotnet run --project src/eShop.AppHost/eShop.AppHost.csproj

# Run unit tests
dotnet test tests/Ordering.UnitTests/

# Run specific test
dotnet test tests/Ordering.UnitTests/ --filter "FullyQualifiedName~OrderAggregateTest"

# Run functional tests (requires Docker)
dotnet test tests/Ordering.FunctionalTests/
```

## API Endpoints

All endpoints require authorization and use API versioning (v1.0).

```
GET    /api/orders                  — Get orders for authenticated user
GET    /api/orders/{orderId:int}    — Get specific order
GET    /api/orders/cardtypes        — Get card type lookup
POST   /api/orders/                 — Create order (requires x-requestid header)
POST   /api/orders/draft            — Create order draft
PUT    /api/orders/cancel           — Cancel order (requires x-requestid header)
PUT    /api/orders/ship             — Ship order (requires x-requestid header)
```

The `x-requestid` header (Guid) enables **idempotency** via `IdentifiedCommand<T, R>` and `RequestManager`.

## Key Patterns

### CQRS with MediatR

- **Commands** (`Application/Commands/`): State-changing operations dispatched through MediatR
- **Queries** (`Application/Queries/`): Read operations using raw SQL via Dapper (IOrderQueries)
- **Pipeline Behaviors** execute in order: Logging → Validation → Transaction

### MediatR Pipeline Behaviors

| Behavior | Purpose |
|----------|---------|
| `LoggingBehavior` | Logs command name and properties |
| `ValidatorBehavior` | Runs FluentValidation; throws on failure |
| `TransactionBehavior` | Wraps handler in DB transaction, publishes integration events after commit |

### Domain-Driven Design

**Order Aggregate** (root: `Order.cs`):
- Children: `OrderItem`
- Value object: `Address` (owned entity)
- Status transitions: Submitted → AwaitingValidation → StockConfirmed → Paid → Shipped (or Cancelled)
- Domain events raised on state changes

**Buyer Aggregate** (root: `Buyer.cs`):
- Children: `PaymentMethod`
- Lookup: `CardType`

### Idempotency

Commands wrapped in `IdentifiedCommand<T, R>` with a request GUID. `IdentifiedCommandHandler` checks `RequestManager` to skip duplicates. The `ClientRequest` entity tracks processed request IDs.

### Event Flow

1. Command handler creates domain entity → domain events added to aggregate
2. `SaveEntitiesAsync()` dispatches domain events via MediatR (same transaction)
3. Domain event handlers create integration events stored in outbox table
4. `TransactionBehavior` publishes integration events to RabbitMQ after commit

### Integration Events

**Published (outbound):**
- `OrderStartedIntegrationEvent` — triggers basket cleanup
- `OrderStatusChangedTo{AwaitingValidation|StockConfirmed|Paid|Shipped|Cancelled}IntegrationEvent`
- `OrderStatusChangedToSubmittedIntegrationEvent`

**Consumed (inbound):**
- `GracePeriodConfirmedIntegrationEvent` — from OrderProcessor
- `OrderStockConfirmedIntegrationEvent` / `OrderStockRejectedIntegrationEvent` — from Catalog.API
- `OrderPaymentSucceededIntegrationEvent` / `OrderPaymentFailedIntegrationEvent` — from PaymentProcessor

## Dependencies

### NuGet Packages (versions in `Directory.Packages.props`)
- `Asp.Versioning.Http` — API versioning
- `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` — PostgreSQL with Aspire
- `FluentValidation` + `FluentValidation.DependencyInjectionExtensions`
- `Microsoft.EntityFrameworkCore.Tools` — EF migrations
- MediatR (transitive)

### Project References
- `eShop.ServiceDefaults` — OpenTelemetry, health checks, JWT auth, OpenAPI
- `EventBusRabbitMQ` — RabbitMQ event bus
- `IntegrationEventLogEF` — Outbox pattern for durable event publishing
- `Ordering.Domain` — Domain model
- `Ordering.Infrastructure` — Data access

### Infrastructure
- **PostgreSQL** database (`orderingdb`, schema: `ordering`)
- **RabbitMQ** for integration events
- **Identity.API** for OAuth 2.0 / JWT (audience: `orders`)

## Database

- **DbContext**: `OrderingContext` in Ordering.Infrastructure
- **Schema**: `ordering`
- **Tables**: `orders`, `orderItems`, `buyers`, `paymentmethods`, `cardtypes`, `requests`
- **ID strategy**: Hi-Lo sequences (`orderseq`, `buyerseq`, `paymentseq`, `orderitemseq`)
- **Migrations**: Auto-applied on startup via `AddMigration<OrderingContext, OrderingContextSeed>()`
- **Seed data**: CardTypes (Amex, Visa, MasterCard)

To add a new migration:
```bash
dotnet ef migrations add <Name> --project src/Ordering.Infrastructure/ --startup-project src/Ordering.API/
```

## Testing

### Unit Tests (`tests/Ordering.UnitTests/`)
- **Framework**: MSTest.Sdk 4.0.2, parallelized at method level
- **Mocking**: NSubstitute
- **Coverage**: Domain aggregates (Order, Buyer, ValueObject), command handlers, API endpoint handlers, idempotency, serialization
- **Builders**: `Builders.cs` provides `OrderBuilder`, `AddressBuilder` for test data

### Functional Tests (`tests/Ordering.FunctionalTests/`)
- **Framework**: xUnit v3 via MTP
- **Infrastructure**: Real PostgreSQL containers via Aspire (`OrderingApiFixture`)
- **Auth bypass**: `AutoAuthorizeMiddleware` injects test identity claims
- **Coverage**: HTTP endpoint integration tests (GET/POST/PUT flows)
- **Requires**: Docker running

## Common Development Tasks

### Adding a new command
1. Create `XxxCommand.cs` record in `Application/Commands/`
2. Create `XxxCommandHandler.cs` implementing `IRequestHandler<XxxCommand, TResult>`
3. Add `XxxCommandValidator.cs` in `Application/Validations/` if needed
4. Wire up in `OrdersApi.cs` endpoint, wrap with `IdentifiedCommand` for idempotency

### Adding a new integration event
1. Define event class in `Application/IntegrationEvents/Events/`
2. For inbound: add handler in `EventHandling/`, register in `Extensions.cs` → `AddEventBusSubscriptions()`
3. For outbound: publish via `IOrderingIntegrationEventService` from a domain event handler

### Adding a new domain event
1. Define event record in `Ordering.Domain/Events/` implementing `INotification`
2. Raise it from the aggregate root method: `AddDomainEvent(new XxxDomainEvent(...))`
3. Create handler in `Application/DomainEventHandlers/`

### Modifying the database schema
1. Change entity or configuration in `Ordering.Infrastructure`
2. Generate migration: `dotnet ef migrations add <Name> --project src/Ordering.Infrastructure/ --startup-project src/Ordering.API/`
3. Migrations auto-apply on startup
