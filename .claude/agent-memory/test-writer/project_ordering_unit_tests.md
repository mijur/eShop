---
name: Ordering.UnitTests conventions
description: Naming, assertion style, builders, test runner invocation, and Domain entity quirks for the Ordering.UnitTests project
type: project
---

## Project location
`tests/Ordering.UnitTests/` — uses `MSTest.Sdk 4.0.2` via `global.json`. NSubstitute for mocking.

## Builders (Builders.cs in tests/Ordering.UnitTests/)
- `AddressBuilder` — `.Build()` returns `new Address("street","city","state","country","zipcode")`
- `OrderBuilder(address)` — constructs a new Order in Submitted status; `.AddOne(productId, name, price, discount, pictureUrl, units=1)` chains items; `.Build()` returns the Order

## Naming conventions
Tests use a mix of `MethodName_Scenario_ExpectedBehavior` (PascalCase) and `snake_case_description`. Both are accepted. Prefer the PascalCase form for new tests.

## Assertion style
- `Assert.AreEqual`, `Assert.IsNotNull`, `Assert.IsTrue`, `Assert.HasCount`, `Assert.IsInstanceOfType<T>`, `Assert.Contains`
- `Assert.ThrowsExactly<ExceptionType>(() => ...)` — NOT `Assert.ThrowsException`
- NEVER use FluentAssertions
- Analyzer MSTEST0037 enforces: use `Assert.Contains` instead of `Assert.IsTrue(s.Contains(...))`, and `Assert.HasCount` instead of `Assert.AreEqual(n, collection.Count)`

## DomainEvents quirk on Entity
`Entity._domainEvents` is null until first `AddDomainEvent` is called. `ClearDomainEvents()` calls `.Clear()` on the list but does NOT null it out — so `DomainEvents` returns an empty (non-null) collection after clearing. When asserting no events after a clear, use `Assert.AreEqual(0, order.DomainEvents?.Count ?? 0)` rather than `Assert.IsNull(order.DomainEvents)`.

## Order status machine summary
- Construction → Submitted (raises OrderStartedDomainEvent)
- Submitted → AwaitingValidation (raises OrderStatusChangedToAwaitingValidationDomainEvent; no-op if not Submitted)
- AwaitingValidation → StockConfirmed (raises OrderStatusChangedToStockConfirmedDomainEvent; no-op if not AwaitingValidation)
- StockConfirmed → Paid (raises OrderStatusChangedToPaidDomainEvent; no-op if not StockConfirmed)
- Paid → Shipped (raises OrderShippedDomainEvent; THROWS if not Paid)
- Any non-Paid/non-Shipped → Cancelled (raises OrderCancelledDomainEvent; THROWS if Paid or Shipped)
- AwaitingValidation → Cancelled via SetCancelledStatusWhenStockIsRejected (no domain event raised; no-op if not AwaitingValidation)

## Parallelization
`[assembly: Parallelize(Workers = 0, Scope = ExecutionScope.MethodLevel)]` in GlobalUsings.cs — tests are parallelized at method level.

## GlobalUsings.cs
All common namespaces are in `tests/Ordering.UnitTests/GlobalUsings.cs` — no need to add `using` directives in individual test files for MediatR, MSTest, NSubstitute, or core Ordering domain/app namespaces.
