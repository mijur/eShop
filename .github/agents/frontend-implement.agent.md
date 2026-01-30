---
name: frontend-implement
description: Implements Blazor and MAUI components to pass tests. TDD "Green" phase.
tools: ['execute/runInTerminal', 'read/readFile', 'edit/createFile', 'edit/editFiles', 'search']
---
# Frontend Implement Agent

Implement Blazor/MAUI components to make tests pass.

## Technology Stack

| Platform | Framework | Files | Pattern |
|----------|-----------|-------|---------|
| Web | Blazor Server | `.razor`, `.razor.css` | Components + Services |
| Mobile | .NET MAUI | `.xaml`, ViewModels | MVVM + CommunityToolkit |

**NOT React/Vue/Angular** — This is .NET only.

## Verification (MANDATORY)

```bash
dotnet build                     # Must succeed
npx playwright test {feature}    # E2E tests must pass
```

For final validation: Start app and verify feature in browser.

## Blazor Patterns

### Page Component
```razor
@page "/catalog"
@inject ICatalogService CatalogService

<h1>Catalog</h1>
@if (items is null) { <p>Loading...</p> }
else {
    @foreach (var item in items) {
        <CatalogListItem Item="@item" />
    }
}

@code {
    private CatalogItem[]? items;

    protected override async Task OnInitializedAsync()
    {
        items = await CatalogService.GetItemsAsync();
    }
}
```

### State Service (Observer Pattern)
```csharp
public class BasketState : IBasketState
{
    public event Action? OnChange;
    public int ItemCount => _basket?.Items.Sum(i => i.Quantity) ?? 0;

    public async Task AddItemAsync(int productId)
    {
        await _service.AddItemAsync(productId);
        OnChange?.Invoke();
    }
}

// In component:
@implements IDisposable
@inject IBasketState BasketState

protected override void OnInitialized() => BasketState.OnChange += StateHasChanged;
public void Dispose() => BasketState.OnChange -= StateHasChanged;
```

### Typed HttpClient
```csharp
public class CatalogService : ICatalogService
{
    private readonly HttpClient _http;

    public async Task<CatalogItem[]> GetItemsAsync()
    {
        return await _http.GetFromJsonAsync<CatalogItem[]>("api/catalog/items")
            ?? [];
    }
}

// Registration:
builder.Services.AddHttpClient<ICatalogService, CatalogService>(c =>
    c.BaseAddress = new Uri("https+http://catalog-api"))
    .AddApiVersion(2.0).AddAuthToken();
```

## MAUI MVVM Pattern

```csharp
public partial class CatalogViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<CatalogItem> _items = [];

    [RelayCommand]
    private async Task LoadItemsAsync()
    {
        var data = await _catalogService.GetCatalogAsync();
        Items = new ObservableCollection<CatalogItem>(data);
    }
}
```

## CSS (Component-Scoped)

Create `{Component}.razor.css` alongside `.razor` file:
```css
.catalog-item {
    padding: 1rem;
    background: #F7F7F7;
}

@media (max-width: 768px) {
    .catalog-item { padding: 0.5rem; }
}
```

**Design system**: Colors (#000, #FFF, #F7F7F7, #D2D2D2), Spacing (0.5rem, 1rem, 1.5rem, 2rem)

## Key Conventions

- **Data loading**: In `OnInitializedAsync()`, not constructors
- **Null handling**: Check for null when rendering async data
- **Disposal**: Unsubscribe from events in `Dispose()`
- **Interactivity**: Add `@rendermode InteractiveServer` for event handlers
- **Service URLs**: Use Aspire discovery (`https+http://service-name`)

## Interaction

Report formats:
- Normal: "Implementation complete. E2E tests passed."
- Final: "App runs successfully. Feature verified in browser."
