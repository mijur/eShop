---
paths: ["tests/**/*.cs"]
---

# Testing Rules for eShop Tests

These rules apply to all test files in `tests/**/*.cs`.

## Test Framework

**Required:**
- Use **xUnit** for test structure (fact, theory, data attributes)
- Use **FluentAssertions** for all assertions (`.Should()` syntax)
- Use **NSubstitute** for mocking
- NEVER use raw MSTest Assert or Assert.That

```csharp
// ✅ CORRECT
public class OrderServiceTests
{
    [Fact]
    public async Task PlaceOrder_ValidOrder_CreatesOrderSuccessfully()
    {
        // Arrange
        var mockRepository = Substitute.For<IOrderRepository>();

        // Act
        var result = await service.PlaceOrderAsync(validOrder);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(OrderStatus.Pending);
        await mockRepository.Received(1).AddAsync(result);
    }
}

// ❌ WRONG
[TestMethod]
public void PlaceOrder_ValidOrder_CreatesOrderSuccessfully()
{
    // ...
    Assert.IsNotNull(result);  // Raw Assert
    Assert.AreEqual(result.Status, OrderStatus.Pending);  // MSTest
}
```

## Naming Convention

**Pattern: `MethodName_Scenario_ExpectedBehavior`**

```
MethodName       — Method being tested
_Scenario        — Input/context condition
_ExpectedBehavior — Expected outcome
```

**Examples:**
```csharp
PlaceOrder_WithValidItems_CreatesOrder()
PlaceOrder_WithEmptyBasket_ThrowsArgumentException()
PlaceOrder_WithInvalidPayment_ReturnsFailureResult()
GetOrder_WhenOrderNotFound_ReturnsNull()
AddOrderItem_WhenDuplicateProduct_ThrowsInvalidOperationException()
SetOrderStatus_FromPendingToShipped_UpdatesStatusAndRaisesEvent()
```

**Guidance:**
- Be specific about the scenario — not just "valid" or "invalid", but "WithEmptyBasket", "WithExpiredPayment", "WhenNotAuthorized"
- Expected behavior should name what happens — "ThrowsException", "ReturnsNull", "UpdatesDatabase", "RaisesEvent"
- Use "When" for queries (GetOrder_WhenNotFound), "With" for operations (PlaceOrder_WithInvalidItems)

```csharp
// ✅ GOOD
ValidateOrder_WithMissingBuyerId_ThrowsValidationException()
ValidateOrder_WithNegativePrice_ThrowsValidationException()
GetOrdersByBuyer_WhenBuyerHasNoOrders_ReturnsEmptyList()

// ❌ VAGUE
ValidateOrder_Invalid()
GetOrders_Works()
Test1()
TestValidation()
```

## Arrange-Act-Assert Pattern

**Three distinct sections, clearly separated:**

```csharp
[Fact]
public async Task PlaceOrder_WithValidItems_CreatesOrderInPendingStatus()
{
    // === ARRANGE ===
    var mockRepository = Substitute.For<IOrderRepository>();
    var orderService = new OrderService(mockRepository);

    var order = new Order
    {
        BuyerId = 1,
        Items = new List<OrderItem> { new() { ProductId = 1, Quantity = 1 } }
    };

    // === ACT ===
    var result = await orderService.PlaceOrderAsync(order);

    // === ASSERT ===
    result.Should().NotBeNull();
    result.Status.Should().Be(OrderStatus.Pending);
    await mockRepository.Received(1).AddAsync(Arg.Is<Order>(o => o.Id == result.Id));
}
```

**Guidance:**
- Arrange: Set up objects, mocks, dependencies
- Act: Call the method once
- Assert: Verify the outcome

## FluentAssertions Usage

**Use FluentAssertions exclusively:**

```csharp
// ✅ CORRECT FluentAssertions
result.Should().NotBeNull();
result.Status.Should().Be(OrderStatus.Pending);
result.Items.Should().HaveCount(3);
result.Items.Should().Contain(x => x.ProductId == 1);
result.Total.Should().BeGreaterThan(0);
order.Invoking(o => o.SetStatus(invalidStatus)).Should().Throw<InvalidOperationException>();

// ❌ WRONG - Raw assertions
Assert.IsNotNull(result);
Assert.AreEqual(result.Status, OrderStatus.Pending);
Assert.AreEqual(result.Items.Count, 3);
```

**Common FluentAssertions patterns:**
```csharp
value.Should().Be(expected);
collection.Should().HaveCount(5);
collection.Should().BeEmpty();
collection.Should().Contain(predicate);
collection.Should().BeInAscendingOrder(x => x.Price);
string.Should().StartWith("prefix");
string.Should().Contain("substring");
action.Should().Throw<ExceptionType>();
action.Should().NotThrow();
obj.Should().BeOfType<MyType>();
```

## Mocking with NSubstitute

**Pattern:**
```csharp
[Fact]
public async Task PlaceOrder_CallsRepositoryToSave()
{
    // === ARRANGE ===
    var mockRepository = Substitute.For<IOrderRepository>();
    var service = new OrderService(mockRepository);

    // === ACT ===
    await service.PlaceOrderAsync(order);

    // === ASSERT ===
    await mockRepository.Received(1).AddAsync(Arg.Any<Order>());
}
```

**Verification:**
```csharp
// Verify method was called
await mockRepository.Received(1).AddAsync(Arg.Any<Order>());

// Verify called with specific argument
await mockRepository.Received(1).AddAsync(Arg.Is<Order>(o => o.Id == 123));

// Verify NOT called
await mockRepository.DidNotReceive().DeleteAsync(Arg.Any<int>());

// Verify call count
mockService.Received(2).MethodName(Arg.Any<string>());
```

## Unit Test Structure

**For testing aggregates (especially Ordering.Domain):**

```csharp
public class OrderAggregateTests
{
    [Fact]
    public void AddOrderItem_WithValidItem_AddsItemToOrder()
    {
        // === ARRANGE ===
        var order = Order.NewDraft();
        var item = new OrderItem { ProductId = 1, Quantity = 1 };

        // === ACT ===
        order.AddOrderItem(item);

        // === ASSERT ===
        order.Items.Should().HaveCount(1);
        order.Items.Should().Contain(item);
    }

    [Fact]
    public void AddOrderItem_WithDuplicateProduct_ThrowsException()
    {
        // === ARRANGE ===
        var order = Order.NewDraft();
        var item = new OrderItem { ProductId = 1 };
        order.AddOrderItem(item);

        // === ACT & ASSERT ===
        order.Invoking(o => o.AddOrderItem(new OrderItem { ProductId = 1 }))
            .Should().Throw<InvalidOperationException>();
    }
}
```

**Test aggregate invariants explicitly:**
```csharp
[Fact]
public void Order_CannotChangeShippedOrderStatus_ThrowsException()
{
    // Verify the invariant: shipped orders cannot be modified
}

[Fact]
public void Order_MustHaveAtLeastOneItem_BeforePlacing()
{
    // Verify the invariant: order needs items
}
```

## Common Test Scenarios

**For Ordering service commands:**
```csharp
// Happy path
PlaceOrder_WithValidOrder_CreatesOrder()
PlaceOrder_WithValidOrder_PublishesDomainEvent()

// Validation failures
PlaceOrder_WithEmptyItems_ThrowsValidationException()
PlaceOrder_WithInvalidBuyer_ThrowsValidationException()

// State machine violations
SetOrderStatus_FromShippedToRejected_ThrowsInvalidOperationException()
```

**For Catalog/Basket CRUD:**
```csharp
GetItem_WithValidId_ReturnsItem()
GetItem_WithInvalidId_ReturnsNull()
UpdateItem_WithValidData_UpdatesSuccessfully()
DeleteItem_RemovesFromDatabase()
```

## Async Tests

```csharp
// ✅ CORRECT - use Async method names and await
[Fact]
public async Task GetOrderAsync_WithValidId_ReturnsOrder()
{
    var result = await service.GetOrderAsync(1);
    result.Should().NotBeNull();
}

// ❌ WRONG - don't block
[Fact]
public void GetOrderAsync_WithValidId_ReturnsOrder()
{
    var result = service.GetOrderAsync(1).Result;  // Can deadlock
}
```

## Functional Test Setup

**For tests in `tests/*FunctionalTests/`:**
- Use Aspire host to start services with real dependencies
- Set up test database/container fixtures
- Cleaner validation of integration between services

```csharp
public class OrderingFunctionalTests : IAsyncLifetime
{
    private DistributedApplication _app;

    public async Task InitializeAsync()
    {
        // Start Aspire application with test containers
        var appBuilder = DistributedApplication.CreateBuilder();
        // Configure services...
        _app = appBuilder.Build();
        await _app.StartAsync();
    }

    [Fact]
    public async Task PlaceOrder_WithValidData_PersistsToDatabase()
    {
        // Query real service/database
    }
}
```

## Summary Checklist

When writing tests in `tests/**/*.cs`:
- [ ] Use xUnit (Fact, Theory, InlineData, etc.)
- [ ] Use FluentAssertions exclusively (`.Should()` syntax)
- [ ] Use NSubstitute for mocking
- [ ] Follow naming: `MethodName_Scenario_ExpectedBehavior`
- [ ] Use Arrange-Act-Assert pattern with clear section comments
- [ ] Test one thing per test (single assertion focus)
- [ ] Mock external dependencies
- [ ] Test aggregate invariants (especially Ordering)
- [ ] Use async/await correctly (not `.Result`)
- [ ] No raw Assert statements
