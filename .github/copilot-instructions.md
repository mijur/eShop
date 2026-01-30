# eShop Coding Instructions

Essential knowledge for working in the eShop codebase.

## Documentation

### Primary References
- **[UI Design Guide](../docs/ui-design-guide.md)** - Complete visual design system, layout patterns, and component placement rules for WebApp
- **[Agent UI Guide](../docs/agent-ui-guide.md)** - How agents should use the UI Design Guide during implementation
- **This Document** - Architecture patterns, technology stack, and common gotchas

**⚠️ For UI features:** Always read the UI Design Guide before implementing frontend components to avoid placement and styling issues.

---

## Quick Reference

### Common Commands
```bash
# Build
dotnet build eShop.slnx

# Run application (requires Docker)
dotnet run --project src/eShop.AppHost/eShop.AppHost.csproj

# Run all tests
dotnet test

# Run specific test project
dotnet test tests/Catalog.FunctionalTests

# Run E2E tests (app must be running)
npx playwright test

# Add EF Core migration
dotnet ef migrations add MigrationName --project src/Catalog.API
```

### Technology Stack
| Layer | Technology |
|-------|-----------|
| Orchestration | .NET Aspire |
| Web Frontend | Blazor Server (InteractiveServer) |
| Mobile | .NET MAUI |
| APIs | ASP.NET Core Minimal APIs |
| Database | PostgreSQL + EF Core |
| Messaging | RabbitMQ |
| Service-to-Service | gRPC |

---

## Architecture

### Microservices
| Service | Purpose | Database |
|---------|---------|----------|
| Catalog.API | Product catalog, search | catalogdb |
| Basket.API | Shopping cart | Redis |
| Ordering.API | Order management | orderingdb |
| Identity.API | Authentication (OIDC) | identitydb |
| Webhooks.API | Webhook subscriptions | webhooksdb |
| WebApp | Blazor frontend | - |

### Project Structure
```
src/
├── eShop.AppHost/           # Aspire orchestration
├── eShop.ServiceDefaults/   # Shared infrastructure (health, telemetry, auth)
├── Catalog.API/
│   ├── Apis/                # Minimal API endpoints
│   ├── Infrastructure/      # EF Core DbContext, migrations
│   ├── Model/               # Domain entities
│   └── Extensions/          # DI registration
├── WebApp/
│   ├── Components/Pages/    # Routable Blazor pages
│   └── Services/            # State management
└── WebAppComponents/        # Shared Razor component library
    ├── Catalog/             # Catalog UI components
    └── Services/            # API client services
```

### Communication Patterns
- **REST**: Client → API (WebApp → Catalog.API)
- **gRPC**: Service → Service (Basket.API → Catalog.API)
- **Events**: Async coordination (RabbitMQ via IntegrationEventLogEF)

---

## Critical Patterns

### ⚠️ Options Class Naming (Silent Failure Risk)
Class name MUST match appsettings.json key exactly:
```csharp
// ✅ Correct
public class CatalogOptions { }
builder.Services.AddOptions<CatalogOptions>().BindConfiguration("CatalogOptions");

// ❌ Wrong - silently fails to bind!
public class CatalogSettings { }
builder.Services.AddOptions<CatalogSettings>().BindConfiguration("CatalogOptions");
```

### Minimal API Endpoints
Location: `src/{Service}.API/Apis/{Feature}Api.cs`
```csharp
public static class CatalogApi
{
    public static IEndpointRouteBuilder MapCatalogApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/catalog").HasApiVersion(2.0);
        
        api.MapGet("/items/{id}", GetItemById);
        api.MapPost("/items", CreateItem);
        
        return app;
    }
    
    public static async Task<Results<Ok<CatalogItem>, NotFound>> GetItemById(
        int id,
        CatalogContext context)
    {
        var item = await context.CatalogItems.FindAsync(id);
        return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
    }
}
```

### Blazor Components
- **Pages**: `src/WebApp/Components/Pages/` with `@page` directive
- **Shared**: `src/WebAppComponents/` for reusable components
- **Interactive**: Add `@rendermode InteractiveServer` for event handlers
- **Query params**: Use `[SupplyParameterFromQuery]` attribute

### Service Registration Pattern
```csharp
// In Extensions/Extensions.cs
public static IHostApplicationBuilder AddApplicationServices(this IHostApplicationBuilder builder)
{
    builder.AddNpgsqlDbContext<CatalogContext>("catalogdb");
    builder.Services.AddScoped<ICatalogService, CatalogService>();
    return builder;
}

// In Program.cs
builder.AddServiceDefaults();
builder.AddApplicationServices();
```

### Integration Events (Outbox Pattern)
Always publish events through IntegrationEventLogService to prevent data loss:
```csharp
// Save to outbox first (same transaction as domain changes)
await _eventLogService.SaveEventAsync(integrationEvent, transaction);

// Background service publishes from outbox to RabbitMQ
```

---

## Testing

### Test Project Mapping
| Source | Test Project | Framework |
|--------|-------------|-----------|
| Domain logic | `*.UnitTests` | MSTest |
| API endpoints | `*.FunctionalTests` | xUnit + Aspire |
| User workflows | `e2e/` | Playwright (TypeScript) |

### Functional Test Pattern
```csharp
public class CatalogApiTests : IClassFixture<CatalogApiFixture>
{
    [Fact]
    public async Task GetItems_ReturnsItems()
    {
        var response = await _httpClient.GetAsync("/api/catalog/items");
        response.EnsureSuccessStatusCode();
    }
}
```

### E2E Test Pattern
```typescript
// e2e/CatalogTest.spec.ts
test('can browse catalog', async ({ page }) => {
    await page.goto('/catalog');
    await expect(page.getByRole('heading', { name: 'Catalog' })).toBeVisible();
});
```

---

## Common Gotchas

| Issue | Solution |
|-------|----------|
| Blazor events not firing | Add `@rendermode InteractiveServer` |
| EF Core lazy loading fails | Use `.Include()` or `.LoadAsync()` explicitly |
| Config not binding | Check Options class name matches JSON key |
| gRPC fails | Ensure HTTP/2 is enabled for service |
| Tests need Docker | Functional tests use Aspire containers |

---

## Key Files Reference

| Pattern | Example File |
|---------|-------------|
| Minimal API | `src/Catalog.API/Apis/CatalogApi.cs` |
| DbContext | `src/Catalog.API/Infrastructure/CatalogContext.cs` |
| Service setup | `src/Catalog.API/Program.cs` |
| Blazor page | `src/WebApp/Components/Pages/Catalog/Catalog.razor` |
| API client | `src/WebAppComponents/Services/CatalogService.cs` |
| Functional test | `tests/Catalog.FunctionalTests/CatalogApiTests.cs` |
| E2E test | `e2e/CatalogTest.spec.ts` |
| ServiceDefaults | `src/eShop.ServiceDefaults/Extensions.cs` |