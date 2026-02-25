# Ordering.API — Domain-Driven Design Service

This service implements DDD with CQRS pattern. The Order aggregate is the heart of the domain.

## Architecture

**Folder Structure:**
- `Application/Commands/` — Commands that modify state, each with a handler
- `Application/Queries/` — Queries that read state, each with a handler
- `Application/Validations/` — FluentValidation validators for commands
- `Application/DomainEventHandlers/` — Handlers for domain events published by aggregates
- `Application/IntegrationEvents/` — Events for other services (published to RabbitMQ)

**Domain Layer** (in `Ordering.Domain`):
- `AggregatesModel/OrderAggregate/` — Order aggregate root, OrderItem, Address, OrderStatus
- `AggregatesModel/BuyerAggregate/` — Buyer aggregate root
- Domain events are raised inside aggregates and published by OrderingContext

## CQRS + MediatR Pattern

**Commands (state changes):**
1. Create command class in `Application/Commands/`
2. Create FluentValidation validator in `Application/Validations/` with same name + `Validator`
3. Create handler class implementing `ICommandHandler<TCommand, TResponse>`
4. Handler receives injected `IOrderingRepository` and calls aggregate methods
5. Aggregate methods validate and raise domain events via `AddDomainEvent()`
6. Handler publishes integration events to EventBus

**Example Command Flow:**
```
CreateOrderCommand
  → CreateOrderCommandValidator (validation)
  → CreateOrderCommandHandler.Handle()
    → IOrderingRepository.AddOrderAsync(order)
    → order.AddDomainEvent(new OrderStartedDomainEvent(...))
    → dbContext.SaveChangesAsync() dispatches domain events
    → integration event published to RabbitMQ
```

**Queries (read-only):**
1. Create query class in `Application/Queries/`
2. Create handler implementing `IQueryHandler<TQuery, TResponse>`
3. Handler queries `IOrderingRepository` or reads directly from OrderingContext

## Domain Aggregates

**Order Aggregate (in `Ordering.Domain`):**
- Private setters for all mutable state — no direct property assignment from outside
- State changes ONLY through named methods (e.g., `SetBuyerId()`, `SetOrderStatus()`, `AddOrderItem()`)
- Each method validates invariants and raises domain events with `AddDomainEvent()`
- Read-only collections expose internal state: `IReadOnlyCollection<OrderItem> OrderItems => _orderItems.AsReadOnly()`

**Key Invariants:**
- Order must have a valid Buyer
- Order must have at least one OrderItem
- OrderStatus transitions follow allowed state machine (e.g., Pending → Paid → Shipped)
- Order total = sum of OrderItem prices

## Domain Events

**Publishing in Aggregates:**
```csharp
this.AddDomainEvent(new OrderStartedDomainEvent(
    order.Id, order.BuyerId, order.OrderDate));
```

**Dispatching via SaveChangesAsync:**
- `OrderingContext.SaveChangesAsync()` publishes all domain events
- Domain event handlers in `Application/DomainEventHandlers/` process these events
- Domain event handlers can publish integration events to EventBus

## Integration Events

- Published from domain event handlers or command handlers
- Sent to RabbitMQ via EventBus interface
- Other services (Basket, Catalog, WebApp) subscribe and react
- Maintain loose coupling: never call other services via HTTP

## Guidelines

- **ALWAYS validate commands** — create a FluentValidation validator for every command
- **Private setters** on all aggregate properties — state changes through methods only
- **Named methods** — `SetOrderStatus()` not `order.OrderStatus = status`
- **Raise domain events** — aggregate methods raise events, don't publish directly
- **Immutable value objects** — Address, OrderItem prices are immutable
- **Repository pattern** — use `IOrderingRepository` to persist aggregates, never DbContext directly in handlers

## Testing

- See parent CLAUDE.md for testing conventions
- Test aggregate invariants in `tests/Ordering.UnitTests/`
- Test command handlers with mock repository in `tests/Ordering.UnitTests/`
- Test functional integration with Aspire in `tests/Ordering.FunctionalTests/`

## Common Tasks

**Add a new command:**
```bash
1. Create: src/Ordering.API/Application/Commands/MyNewCommand.cs
2. Create: src/Ordering.API/Application/Validations/MyNewCommandValidator.cs
3. Create: src/Ordering.API/Application/Commands/MyNewCommandHandler.cs
4. Add aggregate method in Ordering.Domain/AggregatesModel/OrderAggregate/Order.cs
5. Test in tests/Ordering.UnitTests/
```

**Modify Order aggregate:**
```bash
1. Add private field or property
2. Add public method to modify it (validate invariants, raise domain event)
3. Update any relevant domain event
4. Update OrderingContext OnModelCreating if persistence changes
5. Create EF migration: dotnet ef migrations add DescriptionOfChange --project src/Ordering.Infrastructure
```
