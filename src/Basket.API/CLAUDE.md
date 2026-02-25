# Basket.API — Redis + gRPC Service

This service manages shopping baskets using Redis for fast access and gRPC for inter-service communication.

## Architecture

**Data Store:** Redis (not SQL)
- Customer baskets stored as JSON in Redis
- Key pattern: `basket_{customerId}`
- No EF Core, no PostgreSQL — Redis only

**Communication Patterns:**
- **HTTP REST** for WebApp and mobile clients
- **gRPC** for service-to-service communication with Identity.API

**Folder Structure:**
- `Model/` — BasketItem, CustomerBasket models
- `Grpc/` — gRPC service definition and handlers
- `Proto/` — Protocol Buffer definitions (`.proto` files)
- `Repositories/` — BasketRepository (Redis access layer)
- `IntegrationEvents/` — Events published when basket changes

## Data Model

**Simple Models:**
```csharp
public class CustomerBasket
{
    public string BuyerId { get; set; }
    public List<BasketItem> Items { get; set; } = new();
    public decimal Total => Items.Sum(x => x.Quantity * x.UnitPrice);
}

public class BasketItem
{
    public string ProductId { get; set; }
    public string ProductName { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}
```

- No complex value objects or aggregates
- Direct serialization to/from Redis JSON
- No domain logic — just data containers

## Redis Access

**Via IBasketRepository:**
```csharp
public interface IBasketRepository
{
    Task<CustomerBasket> GetBasketAsync(string customerId);
    Task<CustomerBasket> UpdateBasketAsync(CustomerBasket basket);
    Task<bool> DeleteBasketAsync(string customerId);
}
```

**Implementation:**
- Use `IConnectionMultiplexer` from StackExchange.Redis
- Store as JSON: `redis.StringSet(key, JsonSerializer.Serialize(basket))`
- Retrieve and deserialize: `JsonSerializer.Deserialize<CustomerBasket>(value)`
- No ORM, no abstraction layers

**TTL/Expiration:**
- Baskets expire after inactivity (configured in settings)
- Use `redis.StringSet(key, value, TimeSpan.FromHours(24))`

## gRPC Communication

**With Identity.API:**
- Verify JWT tokens and user identity
- Get user claims (UserId, Email) for basket operations

**Grpc Folder:**
- `BasketService.cs` — implements gRPC service methods
- Proto definition in `Proto/basket.proto`

**Example gRPC Service:**
```csharp
public class BasketService : Basket.BasketBase
{
    private readonly IBasketRepository _repository;

    public override async Task<GetBasketResponse> GetBasket(
        GetBasketRequest request,
        ServerCallContext context)
    {
        var basket = await _repository.GetBasketAsync(request.CustomerId);
        return new GetBasketResponse { /* map basket */ };
    }
}
```

## Endpoints

**HTTP REST Endpoints:**
```
GET    /api/v1/basket/{customerId}      — Get basket
POST   /api/v1/basket                   — Create/update basket
DELETE /api/v1/basket/{customerId}      — Clear basket
POST   /api/v1/basket/{id}/items        — Add item
DELETE /api/v1/basket/{id}/items/{itemId} — Remove item
```

- Minimal APIs, direct Redis access
- No complex service layer
- Authentication via JWT token

## Integration Events

- `OrderStartedIntegrationEvent` — clear basket after checkout
- `ProductPriceChangedIntegrationEvent` — update item prices in baskets
- Published to RabbitMQ EventBus

## Guidelines

- **Redis only** — no EF Core, no SQL database
- **gRPC for service-to-service** — not HTTP
- **Simple models** — no domain logic, no aggregates
- **Direct repository access** — minimal abstraction
- **JSON serialization** — standard dotnet serialization

## Anti-Patterns (DO NOT DO)

❌ Add EF Core or SQL database
❌ Create aggregate/domain patterns
❌ Service layer or mediators
❌ Complex validation — keep it simple
❌ Direct Redis access from endpoints — use IBasketRepository

## Common Tasks

**Add a basket endpoint:**
```bash
1. Create minimal API in Program.cs: app.MapGet("/api/v1/basket/{id}", handler)
2. Handler receives IBasketRepository injected
3. Query Redis via repository
4. Return TypedResults response
```

**Modify basket model:**
```bash
1. Update CustomerBasket or BasketItem in Model/
2. Ensure JSON serialization/deserialization works
3. Update gRPC proto if needed: edit Proto/basket.proto
4. Regenerate gRPC code with dotnet build (protoc runs automatically)
```

**Update gRPC service:**
```bash
1. Edit Proto/basket.proto to change messages or methods
2. Run: dotnet build (protoc auto-generates C# code)
3. Implement changes in Grpc/BasketService.cs
4. Recompile and test
```

## Caching Strategy

- Basket is the cache — Redis stores the source of truth
- No multi-level caching needed
- TTL set at basket level, not item level
- Distributed cache for multi-instance scenarios

## Performance Notes

- Redis operations are fast — no N+1 queries possible
- Basket fits in memory easily (typical < 100KB per customer)
- Horizontal scaling: share Redis instance or use Redis Cluster
