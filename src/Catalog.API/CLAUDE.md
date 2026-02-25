# Catalog.API — Simple CRUD Service

This service is intentionally kept simple. It provides product catalog data with minimal abstraction.

## Architecture

**Keep It Simple:**
- Direct EF Core DbContext usage — no repositories, no mediators, no domain services
- Simple entity models in `Model/` (CatalogItem, CatalogBrand, CatalogType)
- Endpoints defined as minimal APIs in `Apis/` folder
- PostgreSQL with pgvector for semantic search

**Folder Structure:**
- `Model/` — EF Core entity models (CatalogItem, CatalogBrand, CatalogType)
- `Infrastructure/` — EF Core DbContext (CatalogContext)
- `Apis/` — Minimal API endpoints (MapGet, MapPost, MapPut, MapDelete)
- `Services/` — Optional business logic (seeding, indexing) — keep thin
- `Extensions/` — DI configuration

## Data Access Pattern

**Direct DbContext Access:**
```csharp
app.MapGet("/api/v1/catalog/items", async (CatalogContext context) =>
{
    return await context.CatalogItems
        .Include(i => i.CatalogType)
        .Include(i => i.CatalogBrand)
        .ToListAsync();
});
```

**DO NOT create:**
- Repository layer (`IItemRepository`, `ItemRepository`)
- Service layer (`ICatalogService`, `CatalogService`)
- Command/Query handlers (no MediatR)
- Complex abstractions

**Reasoning:** This adds unnecessary complexity. CRUD operations are straightforward; the overhead of abstraction patterns doesn't justify the benefit here.

## Endpoints

**Location:** `Apis/` folder

**Guidelines:**
- Use minimal APIs (MapGet, MapPost, MapPut, MapDelete)
- Keep handlers simple — validation, data access, return response
- Use `TypedResults` for type-safe responses

**Example:**
```csharp
app.MapGet("/api/v1/catalog/items/{id:int}", async (int id, CatalogContext context) =>
    await context.CatalogItems.FindAsync(id) is CatalogItem item
        ? Results.Ok(item)
        : Results.NotFound());
```

## Validation

- Use `FluentValidation` on requests (if needed — many endpoints don't need it)
- Validate in the endpoint handler before database access
- Return 400 BadRequest with validation errors

## Models

**Entity Design:**
```csharp
public class CatalogItem
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public int CatalogTypeId { get; set; }
    public CatalogType CatalogType { get; set; }
    // ... other properties
}
```

- Simple entity models — no complex value objects
- Direct property access — no encapsulation needed here
- EF Core conventions handle most mapping

## Database

**Seeding:**
- Catalog data loaded from `Setup/catalog.json` at startup
- Images stored in `Pics/` folder
- Use `CatalogContextSeed.SeedAsync()` to populate initial data

**Migrations:**
```bash
dotnet ef migrations add DescriptionOfChange --project src/Catalog.API --startup-project src/eShop.AppHost
dotnet ef database update --project src/Catalog.API --startup-project src/eShop.AppHost
```

## Integration Events

- Catalog publishes events like `ProductPriceChangedIntegrationEvent`
- Events sent via EventBus to RabbitMQ
- Other services (Basket, Ordering) subscribe to catalog changes

## Guidelines

- **Direct DbContext** — no repository abstraction
- **Minimal APIs** — no Controllers
- **Simple models** — straightforward entity mapping
- **Thin services** — seeding and basic utilities only
- **Stateless handlers** — each endpoint is independent

## Anti-Patterns (DO NOT DO)

❌ Create repository interface `ICatalogRepository`
❌ Create service layer `CatalogService`
❌ Add MediatR commands/queries
❌ Complex abstractions or generics
❌ Project references to this service — consume via HTTP client only

## Common Tasks

**Add a new endpoint:**
```bash
1. Create: src/Catalog.API/Apis/MyNewEndpoint.cs
2. Add minimal API: app.MapGet("/api/v1/...", handler)
3. Add validation if needed in handler
4. Test in tests/Catalog.FunctionalTests/ or e2e tests
```

**Modify the data model:**
```bash
1. Update entity in src/Catalog.API/Model/
2. Update CatalogContext.OnModelCreating() if needed
3. Create migration: dotnet ef migrations add Description --project src/Catalog.API
4. Update catalog.json seeding if schema changed
```

**Add semantic search (pgvector):**
```bash
1. EF Core 9 includes pgvector support via .AddPostgres()
2. Use Vector type on model: public Vector Embedding { get; set; }
3. Query with: context.CatalogItems.Where(i => i.Embedding.L2Distance(embedding) < threshold)
```
