---
paths: ["src/Ordering.Domain/**/*.cs"]
---

# Domain Model Rules for Ordering.Domain

These rules apply ONLY when working with aggregate code in `src/Ordering.Domain/`.

## Aggregate Root Pattern

**Private Setters:**
- All mutable properties MUST have private setters
- Properties are only assigned during aggregate initialization or via behavior methods
- Prevents accidental direct mutation from outside the aggregate

```csharp
// ✅ CORRECT
public class Order : IAggregateRoot
{
    public int Id { get; private set; }
    public OrderStatus Status { get; private set; }

    public void SetOrderStatus(OrderStatus newStatus)
    {
        if (!IsValidStatusTransition(Status, newStatus))
            throw new InvalidOperationException(...);

        Status = newStatus;
        AddDomainEvent(new OrderStatusChangedDomainEvent(...));
    }
}

// ❌ WRONG
public class Order : IAggregateRoot
{
    public int Id { get; set; }  // public setter!
    public OrderStatus Status { get; set; }  // direct mutation
}
```

## Named Methods for State Changes

**All mutations through explicit methods, never direct property assignment:**

- Method names should describe the business action: `SetOrderStatus()`, `AddOrderItem()`, `UpdateAddress()`
- NOT: `Status = x`, `Items.Add(x)`, `Address = x`
- Methods validate invariants before making changes

```csharp
// ✅ CORRECT
public void AddOrderItem(OrderItem item)
{
    if (item == null)
        throw new ArgumentNullException(nameof(item));

    if (_orderItems.Any(oi => oi.ProductId == item.ProductId))
        throw new InvalidOperationException("Item already in order");

    _orderItems.Add(item);
    AddDomainEvent(new OrderItemAddedDomainEvent(...));
}

// ❌ WRONG
OrderItems.Add(newItem);  // direct list mutation
```

## Domain Events

**Publishing:**
- ONLY raise domain events from within aggregate methods
- Use `AddDomainEvent()` helper method inherited from Entity base class

```csharp
// ✅ CORRECT
public void AddOrderItem(OrderItem item)
{
    _orderItems.Add(item);
    this.AddDomainEvent(new OrderItemAddedDomainEvent(this.Id, item));
}

// ❌ WRONG
public void AddOrderItem(OrderItem item)
{
    _orderItems.Add(item);
    // Forgot to add domain event
}

// ❌ WRONG (direct event from outside)
var order = orderRepository.Get(id);
var domainEventPublisher = serviceProvider.GetService<IDomainEventPublisher>();
domainEventPublisher.Publish(new OrderItemAddedDomainEvent(...));  // Wrong location
```

## Collections and Value Objects

**Private Collection Fields:**
- Use `private readonly List<T>` for collections
- Expose via `IReadOnlyCollection<T>` property to prevent external mutations

```csharp
// ✅ CORRECT
private readonly List<OrderItem> _orderItems = new();
public IReadOnlyCollection<OrderItem> OrderItems => _orderItems.AsReadOnly();

// ❌ WRONG
public List<OrderItem> OrderItems { get; set; }  // Caller can add items directly!
public HashSet<OrderItem> OrderItems { get; private set; }  // Caller can mutate
```

**Value Objects:**
- Keep immutable (all properties with private setters or init-only)
- Example: `Address`, `Money`, `OrderItemDetail`

```csharp
// ✅ CORRECT Value Object
public class Address
{
    public string Street { get; private set; }
    public string City { get; private set; }

    public Address(string street, string city) { ... }
    // No setters at all
}

// ❌ WRONG
public class Address
{
    public string Street { get; set; }  // Can be mutated after creation
    public string City { get; set; }
}
```

## Invariants

**Document and enforce business rules:**
- Order cannot be created without a Buyer
- OrderStatus transitions follow a valid state machine
- Order must have at least one OrderItem before payment

```csharp
// ✅ CORRECT
private void ValidateStateTransition(OrderStatus newStatus)
{
    if (Status == OrderStatus.Shipped && newStatus != OrderStatus.Cancelled)
        throw new InvalidOperationException("Cannot change status of shipped order");
}

public void SetOrderStatus(OrderStatus newStatus)
{
    ValidateStateTransition(newStatus);
    Status = newStatus;
    AddDomainEvent(new OrderStatusChangedDomainEvent(...));
}
```

## Factory Methods

**Use static factory methods for complex creation logic:**

```csharp
// ✅ CORRECT
public static Order NewDraft()
{
    var order = new Order
    {
        OrderDate = DateTime.UtcNow,
        OrderStatus = OrderStatus.Pending,
        _isDraft = true
    };
    return order;
}
```

## No Services in Aggregates

- Aggregates do NOT take service dependencies (repositories, event buses, etc.)
- Logic is pure domain logic, not infrastructure
- Cross-aggregate operations handled by domain services or application services

```csharp
// ❌ WRONG
public class Order : IAggregateRoot
{
    private readonly IOrderingRepository _repository;  // Wrong!
    private readonly IEventBus _eventBus;  // Wrong!

    public void PlaceOrder()
    {
        _repository.Save(this);  // Shouldn't be here
    }
}

// ✅ CORRECT (this is done by command handler)
var order = Order.NewDraft();
order.AddOrderItem(item);
await orderingRepository.AddOrderAsync(order);  // Save from handler
```

## No Constructors with Many Parameters

- Keep constructors simple
- Use factory methods or builder pattern for complex creation
- Constructors initialize invariants only

```csharp
// ✅ CORRECT
private Order() { }  // Protected/private for EF Core and factories

public static Order NewDraft()
{
    // Complex initialization logic
}

// ❌ WRONG
public Order(int id, int buyerId, DateTime orderDate, Address address,
    List<OrderItem> items, OrderStatus status, decimal total, ...)
{
    // Too many parameters
}
```

## Summary Checklist

When modifying `src/Ordering.Domain/**/*.cs`:
- [ ] All mutable properties have `private set`
- [ ] State changes use named methods (SetX, AddX, UpdateX)
- [ ] Methods validate invariants before modification
- [ ] Collections exposed as `IReadOnlyCollection<T>`
- [ ] Value objects are immutable
- [ ] Domain events raised via `AddDomainEvent()` inside methods
- [ ] No constructor with > 4 parameters
- [ ] No service dependencies injected into aggregates
- [ ] Factory methods used for complex creation logic
