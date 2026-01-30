# Feature: Product Search with Autocomplete

## Feasibility: HIGH

The eShop codebase has a well-structured architecture that supports adding search functionality. The existing patterns for filtering (by brand/type), API endpoints, and frontend components provide a solid foundation.

---

## Summary of Existing Architecture

### Data Model

**CatalogItem** ([src/Catalog.API/Model/CatalogItem.cs](../../src/Catalog.API/Model/CatalogItem.cs)):
- `Name` (string, required, max 50 chars) - indexed
- `Description` (string, nullable) - NOT indexed
- `CatalogTypeId` / `CatalogBrandId` - foreign keys for category filtering
- `Embedding` (Vector) - used for AI semantic search
- **No tags field exists** - would need to be added if required

**Categories**:
- `CatalogType` - product types (e.g., "Mug", "T-Shirt")
- `CatalogBrand` - product brands (e.g., ".NET", "Visual Studio")

### Existing Query Patterns

From [CatalogApi.cs](../../src/Catalog.API/Apis/CatalogApi.cs):

```csharp
// Current name filtering - prefix match only
root = root.Where(c => c.Name.StartsWith(name));

// Brand/Type filtering
root = root.Where(c => c.CatalogTypeId == type);
root = root.Where(c => c.CatalogBrandId == brand);
```

**Limitation**: Current search uses `StartsWith` (prefix match), not partial/substring matching.

### Frontend Architecture

- **Filter Sidebar**: [CatalogSearch.razor](../../src/WebAppComponents/Catalog/CatalogSearch.razor) - 2-column flex layout with catalog items
- **API Client**: [CatalogService.cs](../../src/WebAppComponents/Services/CatalogService.cs) - HttpClient-based service
- **State**: URL query parameters (`?brand=1&type=2&page=1`)

---

## Recommended Solution: PostgreSQL Full-Text Search + Dedicated Search Endpoint

### Backend Architecture

#### 1. New Search API Endpoint

Add to [CatalogApi.cs](../../src/Catalog.API/Apis/CatalogApi.cs):

```csharp
// GET /api/catalog/search?q={phrase}&typeId={id}&limit={n}
api.MapGet("/search", SearchCatalog)
    .WithName("SearchCatalog")
    .WithSummary("Search catalog items")
    .WithDescription("Search for items by name or description with optional category filter")
    .WithTags("Search");

// GET /api/catalog/search/suggestions?q={phrase}&limit={n}
api.MapGet("/search/suggestions", GetSearchSuggestions)
    .WithName("GetSearchSuggestions")
    .WithSummary("Get search autocomplete suggestions")
    .WithTags("Search");
```

#### 2. Search Implementation Options

**Option A: PostgreSQL ILIKE (Simple, Recommended for MVP)**

```csharp
public static async Task<Ok<PaginatedItems<CatalogItem>>> SearchCatalog(
    [AsParameters] PaginationRequest paginationRequest,
    [AsParameters] CatalogServices services,
    [Description("Search phrase")] string q,
    [Description("Filter by type")] int? typeId,
    [Description("Maximum results")] int? limit)
{
    var searchTerm = $"%{q}%";
    
    var query = services.Context.CatalogItems
        .Where(c => EF.Functions.ILike(c.Name, searchTerm) || 
                    EF.Functions.ILike(c.Description ?? "", searchTerm));
    
    if (typeId.HasValue)
        query = query.Where(c => c.CatalogTypeId == typeId);
    
    // ... pagination logic
}
```

**Pros**: Simple, no schema changes, built-in to PostgreSQL
**Cons**: Slower on large datasets (table scan)

**Option B: PostgreSQL Full-Text Search (Better Performance)**

```csharp
// Requires GIN index on tsvector column
var query = services.Context.CatalogItems
    .Where(c => EF.Functions.ToTsVector("english", c.Name + " " + c.Description)
        .Matches(EF.Functions.PlainToTsQuery("english", q)));
```

**Pros**: Leverages PostgreSQL FTS, better ranking, stemming
**Cons**: Requires migration to add tsvector column and GIN index

#### 3. Autocomplete Suggestions Endpoint

```csharp
public static async Task<Ok<List<SearchSuggestion>>> GetSearchSuggestions(
    [AsParameters] CatalogServices services,
    string q,
    int limit = 8)
{
    var suggestions = await services.Context.CatalogItems
        .Where(c => EF.Functions.ILike(c.Name, $"{q}%") ||
                    EF.Functions.ILike(c.Name, $"% {q}%"))
        .OrderBy(c => c.Name)
        .Take(limit)
        .Select(c => new SearchSuggestion(c.Id, c.Name, c.CatalogType!.Type))
        .ToListAsync();
    
    return TypedResults.Ok(suggestions);
}

public record SearchSuggestion(int Id, string Name, string Category);
```

### Frontend Architecture

#### 1. New Search Components

Create in `src/WebAppComponents/Catalog/`:

| Component | Purpose |
|-----------|---------|
| `SearchBox.razor` | Input with debounced search, autocomplete dropdown |
| `SearchBox.razor.css` | Scoped styles following design system |
| `SearchSuggestion.razor` | Individual suggestion item (optional) |

#### 2. SearchBox Component Structure

```razor
@* SearchBox.razor *@
<div class="search-box">
    <input type="text" 
           @bind="searchText" 
           @bind:event="oninput"
           @onkeyup="OnSearchInput"
           placeholder="Search products..." />
    
    @if (showSuggestions && suggestions.Any())
    {
        <div class="search-suggestions">
            @foreach (var suggestion in suggestions)
            {
                <a href="/item/@suggestion.Id" class="search-suggestion">
                    <span class="suggestion-name">@HighlightMatch(suggestion.Name)</span>
                    <span class="suggestion-category">@suggestion.Category</span>
                </a>
            }
        </div>
    }
</div>

@code {
    private Timer? debounceTimer;
    private const int DebounceMs = 300;
    
    private async Task OnSearchInput()
    {
        debounceTimer?.Dispose();
        debounceTimer = new Timer(async _ => await FetchSuggestions(), null, DebounceMs, Timeout.Infinite);
    }
}
```

#### 3. Placement (Per UI Design Guide)

**Option 1**: Dedicated section above catalog (RECOMMENDED)
```razor
@* Catalog.razor *@
<div class="search-section">
    <SearchBox OnSearch="HandleSearch" />
</div>

<div class="catalog">
    <CatalogSearch BrandId="@BrandId" ItemTypeId="@ItemTypeId" />
    <!-- existing content -->
</div>
```

**Option 2**: Inside CatalogSearch sidebar (alternative)
```razor
@* CatalogSearch.razor *@
<div class="catalog-search">
    <SearchBox />  <!-- At top of filter panel -->
    <!-- existing filters -->
</div>
```

#### 4. Service Extension

Add to [ICatalogService.cs](../../src/WebAppComponents/Services/ICatalogService.cs):

```csharp
Task<CatalogResult> SearchCatalogItems(string query, int? typeId, int pageIndex, int pageSize);
Task<List<SearchSuggestion>> GetSearchSuggestions(string query, int limit = 8);
```

### Database Changes

#### Option A (MVP - No Migration)
No changes needed. Use ILIKE queries on existing columns.

#### Option B (Full-Text Search - Requires Migration)

```csharp
// Migration: Add full-text search support
public partial class AddFullTextSearch : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Add tsvector column
        migrationBuilder.Sql(
            @"ALTER TABLE ""Catalog"" ADD COLUMN ""SearchVector"" tsvector 
              GENERATED ALWAYS AS (to_tsvector('english', coalesce(""Name"", '') || ' ' || coalesce(""Description"", ''))) STORED;");
        
        // Add GIN index
        migrationBuilder.Sql(
            @"CREATE INDEX ""IX_Catalog_SearchVector"" ON ""Catalog"" USING GIN (""SearchVector"");");
    }
}
```

---

## Implementation Details

### Services Affected
- **Catalog.API** - New search endpoints
- **WebAppComponents** - New SearchBox component, CatalogService extension
- **WebApp** - Integration with Catalog page

### Communication
- **REST**: WebApp → Catalog.API (search endpoints)

### Data Changes
- **MVP**: None
- **Full-Text**: Migration for tsvector column + GIN index

---

## Risks

| Risk | Mitigation |
|------|------------|
| ILIKE performance on large catalogs | Start with ILIKE for MVP (~100 items is fine); migrate to FTS if needed |
| Autocomplete latency | Cache suggestions in browser (session storage), implement request cancellation |
| Search ranking quality | Use PostgreSQL `ts_rank` for relevance scoring with FTS |
| Partial match accuracy | Combine prefix match (`lap%`) with word-boundary match (`% lap%`) |
| Category filter confusion | Use CatalogType (not Brand) for category dropdown since it's more product-focused |
| Mobile keyboard experience | Auto-focus search input, show suggestions above keyboard |

---

## Alternatives Considered

<details>
<summary>Other options (click to expand)</summary>

- **ElasticSearch/Meilisearch**: Not chosen because adds infrastructure complexity; PostgreSQL FTS is sufficient for catalog size
- **Client-side search**: Not chosen because requires loading all items; doesn't scale
- **AI Semantic Search only**: Already exists (`GetItemsBySemanticRelevance`) but requires AI service; phrase search should be standard feature
- **Redis caching**: Could be added later for suggestion caching; not needed for MVP

</details>

---

## Files to Modify

| File | Changes |
|------|---------|
| [src/Catalog.API/Apis/CatalogApi.cs](../../src/Catalog.API/Apis/CatalogApi.cs) | Add `SearchCatalog` and `GetSearchSuggestions` endpoints |
| [src/WebAppComponents/Services/ICatalogService.cs](../../src/WebAppComponents/Services/ICatalogService.cs) | Add search methods |
| [src/WebAppComponents/Services/CatalogService.cs](../../src/WebAppComponents/Services/CatalogService.cs) | Implement search methods |
| [src/WebApp/Components/Pages/Catalog/Catalog.razor](../../src/WebApp/Components/Pages/Catalog/Catalog.razor) | Add search section, wire up SearchBox |
| [src/WebApp/Components/Pages/Catalog/Catalog.razor.css](../../src/WebApp/Components/Pages/Catalog/Catalog.razor.css) | Add `.search-section` styles |

## New Files to Create

| File | Purpose |
|------|---------|
| `src/WebAppComponents/Catalog/SearchBox.razor` | Search input with autocomplete |
| `src/WebAppComponents/Catalog/SearchBox.razor.css` | Scoped styles |
| `src/WebAppComponents/Catalog/SearchSuggestion.cs` | DTO for suggestions |

---

## Validation Checklist

- [ ] Build succeeds (`dotnet build eShop.slnx`)
- [ ] Existing tests pass (`dotnet test`)
- [ ] App runs (`dotnet run --project src/eShop.AppHost`)
- [ ] Search returns relevant results
- [ ] Autocomplete triggers at 2+ characters
- [ ] Debounce prevents excessive API calls
- [ ] Category filter works with search
- [ ] Responsive design on mobile/tablet

---

## Estimated Effort

| Phase | Tasks | Estimate |
|-------|-------|----------|
| Backend | Search endpoint + suggestions endpoint | 2-3 hours |
| Frontend | SearchBox component + integration | 3-4 hours |
| Testing | Unit tests + manual QA | 2 hours |
| **Total** | | **7-9 hours**
