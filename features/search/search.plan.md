# Feature: Product Search with Autocomplete - Plan

## Context
Based on: [features/search/search.findings.md](search.findings.md)

## Service Boundaries
- **Primary**: Catalog.API (search endpoints)
- **Secondary**: WebAppComponents (service client), WebApp (UI integration)
- **Communication**: REST (WebApp → Catalog.API)

## Summary
- **Total Tasks**: 14
- **Backend**: 8 tasks (4 tests, 4 implementations)
- **Frontend**: 5 tasks (2 tests, 3 implementations)
- **Validation**: 1 task
- **High-Risk Tasks**: Task 2, Task 10

---

## Tasks

### Phase 1: Backend - Tests First

#### Task 1: Create Search API Functional Tests
- **Agent**: backend-test
- **Type**: Functional Test
- **Dependencies**: None
- **Risk**: Low
- **Complexity**: Medium

**Files to Create/Modify**:
- Create: `tests/Catalog.FunctionalTests/SearchApiTests.cs`

**Acceptance Criteria**:
- [ ] Test class `SearchApiTests` created following existing pattern in `CatalogApiTests.cs`
- [ ] Test fixture setup matches existing `CatalogApiFixture` usage
- [ ] All tests initially fail (Red phase of TDD)

**Test Scenarios**:
```csharp
// 1. Search returns matching items by name (partial match)
[Fact] SearchCatalog_WithNameMatch_ReturnsItems()
// Query: /api/catalog/search?q=Alpine
// Expected: Items containing "Alpine" in name

// 2. Search returns matching items by description
[Fact] SearchCatalog_WithDescriptionMatch_ReturnsItems()
// Query: /api/catalog/search?q=hiking
// Expected: Items with "hiking" in description

// 3. Search is case-insensitive
[Fact] SearchCatalog_IsCaseInsensitive()
// Query: /api/catalog/search?q=ALPINE
// Expected: Same results as lowercase

// 4. Search with category filter
[Fact] SearchCatalog_WithCategoryFilter_ReturnsFilteredItems()
// Query: /api/catalog/search?q=boots&typeId=1
// Expected: Only items of type 1 containing "boots"

// 5. Search with empty query returns bad request
[Fact] SearchCatalog_WithEmptyQuery_ReturnsBadRequest()

// 6. Search respects limit parameter
[Fact] SearchCatalog_WithLimit_RespectsMaxResults()
// Query: /api/catalog/search?q=a&limit=5
// Expected: At most 5 items
```

**Technical Approach**:
- Follow `CatalogApiTests.cs` pattern with API versioning support
- Use `JsonSerializer` for response deserialization
- Test both API v1 and v2 where applicable

---

#### Task 2: Create Suggestions API Functional Tests
- **Agent**: backend-test
- **Type**: Functional Test
- **Dependencies**: None
- **Risk**: High (new DTO required)
- **Complexity**: Medium

**Files to Create/Modify**:
- Modify: `tests/Catalog.FunctionalTests/SearchApiTests.cs` (add suggestion tests)

**Acceptance Criteria**:
- [ ] Suggestions endpoint tests added
- [ ] Tests cover autocomplete behavior requirements
- [ ] All tests initially fail

**Test Scenarios**:
```csharp
// 1. Suggestions return matching items
[Fact] GetSuggestions_WithQuery_ReturnsSuggestions()
// Query: /api/catalog/search/suggestions?q=Alp
// Expected: Items starting with or containing "Alp"

// 2. Suggestions include item id, name, and category
[Fact] GetSuggestions_ReturnsCorrectStructure()
// Expected: { id, name, category } for each suggestion

// 3. Suggestions respect limit (default 8)
[Fact] GetSuggestions_RespectsLimit()
// Query: /api/catalog/search/suggestions?q=a&limit=5
// Expected: At most 5 items

// 4. Suggestions with short query (< 2 chars) returns empty
[Fact] GetSuggestions_WithShortQuery_ReturnsEmpty()
// Query: /api/catalog/search/suggestions?q=A
// Expected: Empty array
```

---

#### Task 3: Implement SearchSuggestion DTO
- **Agent**: backend-implement
- **Type**: Implementation
- **Dependencies**: Task 1, Task 2
- **Risk**: Low
- **Complexity**: Low

**Files to Create**:
- Create: `src/Catalog.API/Model/SearchSuggestion.cs`

**Acceptance Criteria**:
- [ ] Record type created with `Id`, `Name`, `Category` properties
- [ ] Properly serializable to JSON
- [ ] Matches expected test structure

**Implementation Details**:
```csharp
namespace eShop.Catalog.API.Model;

public record SearchSuggestion(int Id, string Name, string Category);
```

---

#### Task 4: Implement Search Endpoint
- **Agent**: backend-implement
- **Type**: Implementation
- **Dependencies**: Task 1, Task 3
- **Risk**: Medium
- **Complexity**: Medium

**Files to Modify**:
- Modify: `src/Catalog.API/Apis/CatalogApi.cs`

**Acceptance Criteria**:
- [ ] `GET /api/catalog/search` endpoint implemented
- [ ] ILIKE query for partial matching on Name and Description
- [ ] Category filter (typeId) support
- [ ] Pagination support
- [ ] Case-insensitive search
- [ ] Task 1 tests pass

**Test Scenarios Covered**: Task 1 scenarios 1-6

**Implementation Details**:
```csharp
// Add to MapCatalogApi():
api.MapGet("/search", SearchCatalog)
    .WithName("SearchCatalog")
    .WithSummary("Search catalog items")
    .WithDescription("Search for items by name or description with optional category filter")
    .WithTags("Search");

// Handler:
public static async Task<Results<Ok<PaginatedItems<CatalogItem>>, BadRequest<ProblemDetails>>> SearchCatalog(
    [AsParameters] PaginationRequest paginationRequest,
    [AsParameters] CatalogServices services,
    [Description("Search phrase"), Required, MinLength(1)] string q,
    [Description("Filter by type")] int? typeId,
    [Description("Maximum results")] int? limit)
{
    // Validate query
    if (string.IsNullOrWhiteSpace(q))
        return TypedResults.BadRequest<ProblemDetails>(new() { Detail = "Search query required" });

    var searchTerm = $"%{q}%";
    var query = services.Context.CatalogItems
        .Where(c => EF.Functions.ILike(c.Name, searchTerm) || 
                    EF.Functions.ILike(c.Description ?? "", searchTerm));
    
    if (typeId.HasValue)
        query = query.Where(c => c.CatalogTypeId == typeId);
    
    // Apply pagination...
}
```

---

#### Task 5: Implement Suggestions Endpoint
- **Agent**: backend-implement
- **Type**: Implementation
- **Dependencies**: Task 2, Task 3, Task 4
- **Risk**: Low
- **Complexity**: Low

**Files to Modify**:
- Modify: `src/Catalog.API/Apis/CatalogApi.cs`

**Acceptance Criteria**:
- [ ] `GET /api/catalog/search/suggestions` endpoint implemented
- [ ] Returns `SearchSuggestion[]` with id, name, category
- [ ] Respects limit parameter (default 8)
- [ ] Returns empty for queries < 2 characters
- [ ] Task 2 tests pass

**Test Scenarios Covered**: Task 2 scenarios 1-4

**Implementation Details**:
```csharp
// Add to MapCatalogApi():
api.MapGet("/search/suggestions", GetSearchSuggestions)
    .WithName("GetSearchSuggestions")
    .WithSummary("Get search autocomplete suggestions")
    .WithTags("Search");

// Handler:
public static async Task<Ok<List<SearchSuggestion>>> GetSearchSuggestions(
    [AsParameters] CatalogServices services,
    [Description("Search query")] string q,
    [Description("Maximum suggestions")] int limit = 8)
{
    if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
        return TypedResults.Ok(new List<SearchSuggestion>());

    var suggestions = await services.Context.CatalogItems
        .Include(c => c.CatalogType)
        .Where(c => EF.Functions.ILike(c.Name, $"{q}%") ||
                    EF.Functions.ILike(c.Name, $"% {q}%"))
        .OrderBy(c => c.Name)
        .Take(limit)
        .Select(c => new SearchSuggestion(c.Id, c.Name, c.CatalogType!.Type))
        .ToListAsync();

    return TypedResults.Ok(suggestions);
}
```

---

#### Task 6: Add Search Methods to ICatalogService
- **Agent**: backend-implement
- **Type**: Implementation
- **Dependencies**: Task 4, Task 5
- **Risk**: Low
- **Complexity**: Low

**Files to Modify**:
- Modify: `src/WebAppComponents/Services/ICatalogService.cs`
- Modify: `src/WebAppComponents/Services/CatalogService.cs`

**Acceptance Criteria**:
- [ ] `SearchCatalogItems` method added to interface and implementation
- [ ] `GetSearchSuggestions` method added to interface and implementation
- [ ] Proper URL encoding for search query
- [ ] Methods call correct API endpoints

**Implementation Details**:
```csharp
// ICatalogService.cs - Add:
Task<CatalogResult> SearchCatalogItems(string query, int? typeId, int pageIndex, int pageSize);
Task<IEnumerable<SearchSuggestion>> GetSearchSuggestions(string query, int limit = 8);

// CatalogService.cs - Add:
public async Task<CatalogResult> SearchCatalogItems(string query, int? typeId, int pageIndex, int pageSize)
{
    var uri = $"{remoteServiceBaseUrl}search?q={HttpUtility.UrlEncode(query)}&pageIndex={pageIndex}&pageSize={pageSize}";
    if (typeId.HasValue)
        uri += $"&typeId={typeId}";
    return (await httpClient.GetFromJsonAsync<CatalogResult>(uri))!;
}

public async Task<IEnumerable<SearchSuggestion>> GetSearchSuggestions(string query, int limit = 8)
{
    var uri = $"{remoteServiceBaseUrl}search/suggestions?q={HttpUtility.UrlEncode(query)}&limit={limit}";
    return (await httpClient.GetFromJsonAsync<SearchSuggestion[]>(uri))!;
}
```

---

#### Task 7: Create SearchSuggestion DTO in WebAppComponents
- **Agent**: backend-implement
- **Type**: Implementation
- **Dependencies**: Task 3
- **Risk**: Low
- **Complexity**: Low

**Files to Create**:
- Create: `src/WebAppComponents/Catalog/SearchSuggestion.cs`

**Acceptance Criteria**:
- [ ] DTO matches Catalog.API's SearchSuggestion structure
- [ ] Can be deserialized from API response

**Implementation Details**:
```csharp
namespace eShop.WebAppComponents.Catalog;

public record SearchSuggestion(int Id, string Name, string Category);
```

---

#### Task 8: Backend Integration Verification
- **Agent**: backend-implement
- **Type**: Verification
- **Dependencies**: Task 4, Task 5, Task 6, Task 7
- **Risk**: Low
- **Complexity**: Low

**Acceptance Criteria**:
- [ ] `dotnet build eShop.slnx` succeeds
- [ ] `dotnet test tests/Catalog.FunctionalTests` passes (all Task 1 & 2 tests green)
- [ ] No breaking changes to existing functionality

---

### Phase 2: Frontend - Tests First

#### Task 9: Create E2E Search Tests
- **Agent**: frontend-test
- **Type**: E2E Test
- **Dependencies**: Task 8
- **Risk**: Medium
- **Complexity**: Medium

**Files to Create**:
- Create: `e2e/SearchTest.spec.ts`

**Acceptance Criteria**:
- [ ] Playwright test file created
- [ ] Tests verify search UI behavior
- [ ] Tests initially fail (no UI yet)

**Test Scenarios**:
```typescript
// e2e/SearchTest.spec.ts
import { test, expect } from '@playwright/test';

test.describe('Catalog Search', () => {
    test('search box is visible on catalog page', async ({ page }) => {
        await page.goto('/');
        await expect(page.getByPlaceholder('Search products...')).toBeVisible();
    });

    test('typing in search shows autocomplete suggestions', async ({ page }) => {
        await page.goto('/');
        const searchBox = page.getByPlaceholder('Search products...');
        await searchBox.fill('Alp');
        // Wait for suggestions dropdown
        await expect(page.locator('.search-suggestions')).toBeVisible();
        // Should have suggestions containing "Alp"
        await expect(page.locator('.search-suggestion')).toHaveCountGreaterThan(0);
    });

    test('clicking suggestion navigates to item', async ({ page }) => {
        await page.goto('/');
        const searchBox = page.getByPlaceholder('Search products...');
        await searchBox.fill('Alpine');
        await page.locator('.search-suggestion').first().click();
        // Should navigate to item detail page
        await expect(page).toHaveURL(/\/item\/\d+/);
    });

    test('pressing enter performs search', async ({ page }) => {
        await page.goto('/');
        const searchBox = page.getByPlaceholder('Search products...');
        await searchBox.fill('boots');
        await searchBox.press('Enter');
        // Page should show filtered results
        await expect(page).toHaveURL(/[?&]q=boots/);
    });

    test('category dropdown filters search', async ({ page }) => {
        await page.goto('/');
        // Select a category from dropdown
        await page.locator('.search-category-select').selectOption({ index: 1 });
        const searchBox = page.getByPlaceholder('Search products...');
        await searchBox.fill('test');
        await searchBox.press('Enter');
        // URL should include type parameter
        await expect(page).toHaveURL(/[?&]type=\d+/);
    });

    test('no suggestions shown for single character', async ({ page }) => {
        await page.goto('/');
        const searchBox = page.getByPlaceholder('Search products...');
        await searchBox.fill('A');
        // Suggestions should not be visible
        await expect(page.locator('.search-suggestions')).not.toBeVisible();
    });
});
```

---

#### Task 10: Implement SearchBox Component
- **Agent**: frontend-implement
- **Type**: Implementation
- **Dependencies**: Task 6, Task 7, Task 9
- **Risk**: High (complex interactive component)
- **Complexity**: High

**Files to Create**:
- Create: `src/WebAppComponents/Catalog/SearchBox.razor`
- Create: `src/WebAppComponents/Catalog/SearchBox.razor.css`

**Acceptance Criteria**:
- [ ] Search input with placeholder "Search products..."
- [ ] Autocomplete dropdown showing 5-8 suggestions
- [ ] Debounce (300ms) on input
- [ ] Suggestions show after 2+ characters typed
- [ ] Highlight matching text in suggestions
- [ ] Category dropdown filter
- [ ] Enter key triggers search navigation
- [ ] Click on suggestion navigates to item detail
- [ ] Responsive design (mobile/tablet/desktop)

**UI Placement**: Dedicated section above `.catalog` div (per UI Design Guide)

**Implementation Details**:
```razor
@* SearchBox.razor *@
@rendermode InteractiveServer
@inject CatalogService CatalogService
@inject NavigationManager Nav

<div class="search-box">
    <div class="search-input-group">
        <select class="search-category-select" @bind="selectedTypeId">
            <option value="">All Categories</option>
            @if (catalogTypes is not null)
            {
                @foreach (var type in catalogTypes)
                {
                    <option value="@type.Id">@type.Type</option>
                }
            }
        </select>
        <input type="text" 
               placeholder="Search products..."
               @bind="searchText"
               @bind:event="oninput"
               @onkeydown="HandleKeyDown"
               @onfocus="ShowSuggestions"
               @onblur="HideSuggestionsDelayed" />
        <button class="search-button" @onclick="PerformSearch">
            <img src="icons/search.svg" alt="Search" />
        </button>
    </div>
    
    @if (showSuggestions && suggestions?.Any() == true)
    {
        <div class="search-suggestions">
            @foreach (var suggestion in suggestions)
            {
                <a class="search-suggestion" href="/item/@suggestion.Id">
                    <span class="suggestion-name">@HighlightMatch(suggestion.Name)</span>
                    <span class="suggestion-category">@suggestion.Category</span>
                </a>
            }
        </div>
    }
</div>

@code {
    private string searchText = "";
    private int? selectedTypeId;
    private bool showSuggestions;
    private IEnumerable<SearchSuggestion>? suggestions;
    private IEnumerable<CatalogItemType>? catalogTypes;
    private Timer? debounceTimer;
    private const int DebounceMs = 300;

    protected override async Task OnInitializedAsync()
    {
        catalogTypes = await CatalogService.GetTypes();
    }

    private void OnSearchInput()
    {
        debounceTimer?.Dispose();
        if (searchText.Length >= 2)
        {
            debounceTimer = new Timer(async _ => 
            {
                suggestions = await CatalogService.GetSearchSuggestions(searchText);
                showSuggestions = true;
                await InvokeAsync(StateHasChanged);
            }, null, DebounceMs, Timeout.Infinite);
        }
        else
        {
            suggestions = null;
            showSuggestions = false;
        }
    }

    private void HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
            PerformSearch();
    }

    private void PerformSearch()
    {
        var uri = Nav.GetUriWithQueryParameters("/", new Dictionary<string, object?>
        {
            { "q", string.IsNullOrWhiteSpace(searchText) ? null : searchText },
            { "type", selectedTypeId },
            { "page", null }
        });
        Nav.NavigateTo(uri);
    }

    private MarkupString HighlightMatch(string name)
    {
        if (string.IsNullOrEmpty(searchText))
            return (MarkupString)name;
        var highlighted = name.Replace(searchText, $"<strong>{searchText}</strong>", 
            StringComparison.OrdinalIgnoreCase);
        return (MarkupString)highlighted;
    }

    // Additional helper methods...
}
```

**CSS** (following UI Design Guide):
```css
/* SearchBox.razor.css */
.search-box {
    position: relative;
    width: 100%;
    max-width: 600px;
}

.search-input-group {
    display: flex;
    border: 1px solid #000;
    border-radius: 0.5rem;
    overflow: hidden;
}

.search-category-select {
    border: none;
    border-right: 1px solid #D2D2D2;
    padding: 0.75rem 1rem;
    background: #F7F7F7;
    min-width: 150px;
}

.search-input-group input {
    flex: 1;
    border: none;
    padding: 0.75rem 1rem;
    font-size: 1rem;
}

.search-button {
    background: #000;
    color: #FFF;
    border: none;
    padding: 0.75rem 1.5rem;
    cursor: pointer;
}

.search-suggestions {
    position: absolute;
    top: 100%;
    left: 0;
    right: 0;
    background: #FFF;
    border: 1px solid #D2D2D2;
    border-top: none;
    z-index: 100;
    max-height: 300px;
    overflow-y: auto;
}

.search-suggestion {
    display: flex;
    justify-content: space-between;
    padding: 0.75rem 1rem;
    text-decoration: none;
    color: #000;
}

.search-suggestion:hover {
    background: #F7F7F7;
}

.suggestion-category {
    color: #404040;
    font-size: 0.875rem;
}

@media only screen and (max-width: 480px) {
    .search-category-select {
        display: none;
    }
}

@media only screen and (min-width: 481px) and (max-width: 1024px) {
    .search-category-select {
        min-width: 120px;
    }
}
```

---

#### Task 11: Integrate SearchBox into Catalog Page
- **Agent**: frontend-implement
- **Type**: Implementation
- **Dependencies**: Task 10
- **Risk**: Medium
- **Complexity**: Medium

**Files to Modify**:
- Modify: `src/WebApp/Components/Pages/Catalog/Catalog.razor`
- Modify: `src/WebApp/Components/Pages/Catalog/Catalog.razor.css`

**Acceptance Criteria**:
- [ ] SearchBox component added above `.catalog` div
- [ ] Search query parameter `q` wired to page
- [ ] Results filtered by search query when present
- [ ] Category filter from SearchBox integrated with existing type filter
- [ ] Layout not broken (per UI Design Guide placement rules)

**UI Placement**: New `.search-section` div above existing `.catalog` div

**Implementation Details**:
```razor
@* Catalog.razor - Add search section *@
<SectionContent SectionName="page-header-title">Ready for a new adventure?</SectionContent>
<SectionContent SectionName="page-header-subtitle">Start the season with the latest in clothing and equipment.</SectionContent>

<div class="search-section">
    <SearchBox />
</div>

<div class="catalog">
    <!-- existing content -->
</div>

@code {
    // Add query parameter:
    [SupplyParameterFromQuery(Name = "q")]
    public string? SearchQuery { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            catalogResult = await CatalogService.SearchCatalogItems(
                SearchQuery,
                ItemTypeId,
                Page.GetValueOrDefault(1) - 1,
                PageSize);
        }
        else
        {
            catalogResult = await CatalogService.GetCatalogItems(
                Page.GetValueOrDefault(1) - 1,
                PageSize,
                BrandId,
                ItemTypeId);
        }
    }
}
```

**CSS**:
```css
/* Catalog.razor.css - Add: */
.search-section {
    padding: 2rem 10rem;
    display: flex;
    justify-content: center;
}

@media only screen and (max-width: 480px) {
    .search-section {
        padding: 1rem;
    }
}

@media only screen and (min-width: 481px) and (max-width: 1024px) {
    .search-section {
        padding: 1.5rem 3rem;
    }
}
```

---

#### Task 12: Add Search Icon Asset
- **Agent**: frontend-implement
- **Type**: Implementation
- **Dependencies**: Task 10
- **Risk**: Low
- **Complexity**: Low

**Files to Create**:
- Create: `src/WebApp/wwwroot/icons/search.svg`

**Acceptance Criteria**:
- [ ] Search icon SVG added
- [ ] Icon matches design system (black color, appropriate size)

**Implementation Details**:
```svg
<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
  <circle cx="11" cy="11" r="8"></circle>
  <line x1="21" y1="21" x2="16.65" y2="16.65"></line>
</svg>
```

---

#### Task 13: Frontend E2E Test Verification
- **Agent**: frontend-implement
- **Type**: Verification
- **Dependencies**: Task 10, Task 11, Task 12
- **Risk**: Low
- **Complexity**: Low

**Acceptance Criteria**:
- [ ] `dotnet build eShop.slnx` succeeds
- [ ] App starts without errors
- [ ] `npx playwright test e2e/SearchTest.spec.ts` passes
- [ ] Manual verification of search workflow

---

### Phase 3: Final Validation (MANDATORY)

#### Task 14: End-to-End Validation
- **Agent**: backend-implement
- **Type**: Validation
- **Dependencies**: Task 8, Task 13
- **Risk**: Low
- **Complexity**: Low

**Acceptance Criteria**:
- [ ] `dotnet build eShop.slnx` succeeds with no errors or warnings
- [ ] `dotnet test` passes (all existing + new tests)
- [ ] `dotnet run --project src/eShop.AppHost` starts successfully
- [ ] Manual test: Search for "Alpine" returns relevant products
- [ ] Manual test: Autocomplete shows suggestions after 2 characters
- [ ] Manual test: Category filter narrows search results
- [ ] Manual test: Clicking suggestion navigates to product detail
- [ ] Manual test: Search works on mobile viewport
- [ ] No regressions in existing catalog filtering (brand/type)

---

## Dependency Graph

```
Task 1 (Search Tests) ─────────────────┐
                                       ├─► Task 4 (Search Endpoint) ─┐
Task 3 (SearchSuggestion DTO API) ─────┤                             │
                                       │                             ├─► Task 6 (Service Methods)
Task 2 (Suggestions Tests) ────────────┼─► Task 5 (Suggestions EP) ──┤
                                       │                             │
                                       └─► Task 7 (DTO WebApp) ──────┘
                                                                     │
                                                                     ▼
                                                              Task 8 (Backend Verify)
                                                                     │
                                                                     ▼
                                                              Task 9 (E2E Tests)
                                                                     │
                     Task 12 (Icon) ──────────────────────────┐      │
                                                              ▼      ▼
                                                        Task 10 (SearchBox)
                                                              │
                                                              ▼
                                                        Task 11 (Integration)
                                                              │
                                                              ▼
                                                        Task 13 (Frontend Verify)
                                                              │
                                                              ▼
                                                        Task 14 (Final Validation)
```

---

## Effort Summary

| Category | Tasks | Complexity |
|----------|-------|------------|
| Backend Tests | 2 | Medium |
| Backend Implementation | 6 | Low-Medium |
| Frontend Tests | 1 | Medium |
| Frontend Implementation | 4 | High |
| Validation | 1 | Low |
| **Total** | **14** | **Medium-High** |

**High-Risk Tasks**: 
- Task 2 (new DTO pattern for suggestions)
- Task 10 (complex interactive component with debounce, autocomplete, keyboard handling)

**Estimated Duration**: 8-10 hours

---

## File Summary

### New Files (9)
| File | Task |
|------|------|
| `tests/Catalog.FunctionalTests/SearchApiTests.cs` | Task 1, 2 |
| `src/Catalog.API/Model/SearchSuggestion.cs` | Task 3 |
| `src/WebAppComponents/Catalog/SearchSuggestion.cs` | Task 7 |
| `src/WebAppComponents/Catalog/SearchBox.razor` | Task 10 |
| `src/WebAppComponents/Catalog/SearchBox.razor.css` | Task 10 |
| `src/WebApp/wwwroot/icons/search.svg` | Task 12 |
| `e2e/SearchTest.spec.ts` | Task 9 |

### Modified Files (5)
| File | Task |
|------|------|
| `src/Catalog.API/Apis/CatalogApi.cs` | Task 4, 5 |
| `src/WebAppComponents/Services/ICatalogService.cs` | Task 6 |
| `src/WebAppComponents/Services/CatalogService.cs` | Task 6 |
| `src/WebApp/Components/Pages/Catalog/Catalog.razor` | Task 11 |
| `src/WebApp/Components/Pages/Catalog/Catalog.razor.css` | Task 11 |
