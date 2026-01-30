using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Asp.Versioning;
using Asp.Versioning.Http;
using eShop.Catalog.API.Model;
using Microsoft.AspNetCore.Mvc.Testing;

namespace eShop.Catalog.FunctionalTests;

/// <summary>
/// Functional tests for the Catalog Search API endpoints.
/// Tests cover search and autocomplete suggestion functionality.
/// </summary>
public sealed class SearchApiTests : IClassFixture<CatalogApiFixture>
{
    private readonly WebApplicationFactory<Program> _webApplicationFactory;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web);

    public SearchApiTests(CatalogApiFixture fixture)
    {
        _webApplicationFactory = fixture;
    }

    private HttpClient CreateHttpClient(ApiVersion apiVersion)
    {
        var handler = new ApiVersionHandler(new QueryStringApiVersionWriter(), apiVersion);
        return _webApplicationFactory.CreateDefaultClient(handler);
    }

    #region Search Endpoint Tests

    /// <summary>
    /// Verifies that search returns items matching the query by name (partial match).
    /// </summary>
    [Fact]
    public async Task Search_ByName_ReturnsMatchingItems()
    {
        // Arrange
        var httpClient = CreateHttpClient(new ApiVersion(2.0));

        // Act
        var response = await httpClient.GetAsync("/api/catalog/search?q=Alpine", TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var result = JsonSerializer.Deserialize<PaginatedItems<CatalogItem>>(body, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.True(result.Count > 0, "Expected at least one matching item");
        Assert.All(result.Data, item =>
            Assert.True(
                item.Name.Contains("Alpine", StringComparison.OrdinalIgnoreCase) ||
                (item.Description?.Contains("Alpine", StringComparison.OrdinalIgnoreCase) ?? false),
                $"Item '{item.Name}' does not contain 'Alpine' in name or description"));
    }

    /// <summary>
    /// Verifies that search is case-insensitive.
    /// </summary>
    [Theory]
    [InlineData("ALPINE")]
    [InlineData("alpine")]
    [InlineData("AlPiNe")]
    public async Task Search_IsCaseInsensitive(string searchTerm)
    {
        // Arrange
        var httpClient = CreateHttpClient(new ApiVersion(2.0));

        // Act
        var response = await httpClient.GetAsync($"/api/catalog/search?q={searchTerm}", TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var result = JsonSerializer.Deserialize<PaginatedItems<CatalogItem>>(body, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.True(result.Count > 0, $"Expected results for case-insensitive search term '{searchTerm}'");
    }

    /// <summary>
    /// Verifies that search returns items matching the query in description.
    /// </summary>
    [Fact]
    public async Task Search_ByDescription_ReturnsMatchingItems()
    {
        // Arrange
        var httpClient = CreateHttpClient(new ApiVersion(2.0));
        // "hiking" appears in descriptions of hiking boots/gear
        var searchTerm = "hiking";

        // Act
        var response = await httpClient.GetAsync($"/api/catalog/search?q={searchTerm}", TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var result = JsonSerializer.Deserialize<PaginatedItems<CatalogItem>>(body, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.True(result.Count > 0, "Expected at least one matching item by description");
        // Items should match in name OR description
        Assert.All(result.Data, item =>
            Assert.True(
                item.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                (item.Description?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false),
                $"Item '{item.Name}' does not contain '{searchTerm}' in name or description"));
    }

    /// <summary>
    /// Verifies that search returns empty results when no items match the query.
    /// </summary>
    [Fact]
    public async Task Search_WithNoMatches_ReturnsEmptyResults()
    {
        // Arrange
        var httpClient = CreateHttpClient(new ApiVersion(2.0));
        var nonExistentTerm = "zzzznonexistentproductxyz";

        // Act
        var response = await httpClient.GetAsync($"/api/catalog/search?q={nonExistentTerm}", TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var result = JsonSerializer.Deserialize<PaginatedItems<CatalogItem>>(body, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.Equal(0, result.Count);
        Assert.Empty(result.Data);
    }

    /// <summary>
    /// Verifies that search can filter results by category type.
    /// </summary>
    [Fact]
    public async Task Search_WithCategoryTypeFilter_ReturnsFilteredItems()
    {
        // Arrange
        var httpClient = CreateHttpClient(new ApiVersion(2.0));
        // Type 1 is typically a specific product category
        var typeId = 1;
        var searchTerm = "a"; // Broad search to get results, then filter by type

        // Act
        var response = await httpClient.GetAsync($"/api/catalog/search?q={searchTerm}&type={typeId}", TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var result = JsonSerializer.Deserialize<PaginatedItems<CatalogItem>>(body, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.All(result.Data, item =>
            Assert.Equal(typeId, item.CatalogTypeId));
    }

    /// <summary>
    /// Verifies that search respects the pageSize limit parameter.
    /// </summary>
    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task Search_RespectsPageSizeLimit(int pageSize)
    {
        // Arrange
        var httpClient = CreateHttpClient(new ApiVersion(2.0));
        var searchTerm = "a"; // Broad search to get many results

        // Act
        var response = await httpClient.GetAsync($"/api/catalog/search?q={searchTerm}&pageSize={pageSize}", TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var result = JsonSerializer.Deserialize<PaginatedItems<CatalogItem>>(body, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.Equal(pageSize, result.PageSize);
        Assert.True(result.Data.Count() <= pageSize, $"Expected at most {pageSize} items");
    }

    /// <summary>
    /// Verifies that search returns paginated results correctly.
    /// </summary>
    [Fact]
    public async Task Search_ReturnsPaginatedResultsCorrectly()
    {
        // Arrange
        var httpClient = CreateHttpClient(new ApiVersion(2.0));
        var searchTerm = "a"; // Broad search
        var pageSize = 5;

        // Act - Get first page
        var response1 = await httpClient.GetAsync($"/api/catalog/search?q={searchTerm}&pageSize={pageSize}&pageIndex=0", TestContext.Current.CancellationToken);
        response1.EnsureSuccessStatusCode();
        var body1 = await response1.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var page1 = JsonSerializer.Deserialize<PaginatedItems<CatalogItem>>(body1, _jsonSerializerOptions);

        // Act - Get second page
        var response2 = await httpClient.GetAsync($"/api/catalog/search?q={searchTerm}&pageSize={pageSize}&pageIndex=1", TestContext.Current.CancellationToken);
        response2.EnsureSuccessStatusCode();
        var body2 = await response2.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var page2 = JsonSerializer.Deserialize<PaginatedItems<CatalogItem>>(body2, _jsonSerializerOptions);

        // Assert
        Assert.NotNull(page1);
        Assert.NotNull(page2);
        Assert.Equal(0, page1.PageIndex);
        Assert.Equal(1, page2.PageIndex);
        Assert.Equal(pageSize, page1.PageSize);
        Assert.Equal(pageSize, page2.PageSize);

        // Verify pages contain different items (if there are enough results)
        if (page1.Count > pageSize)
        {
            var page1Ids = page1.Data.Select(i => i.Id).ToHashSet();
            var page2Ids = page2.Data.Select(i => i.Id).ToHashSet();
            Assert.False(page1Ids.SetEquals(page2Ids), "Page 1 and Page 2 should contain different items");
        }
    }

    /// <summary>
    /// Verifies that search handles empty query string gracefully with bad request.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Search_WithEmptyQuery_ReturnsBadRequest(string query)
    {
        // Arrange
        var httpClient = CreateHttpClient(new ApiVersion(2.0));

        // Act
        var response = await httpClient.GetAsync($"/api/catalog/search?q={Uri.EscapeDataString(query)}", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Verifies that search without query parameter returns bad request.
    /// </summary>
    [Fact]
    public async Task Search_WithoutQueryParameter_ReturnsBadRequest()
    {
        // Arrange
        var httpClient = CreateHttpClient(new ApiVersion(2.0));

        // Act
        var response = await httpClient.GetAsync("/api/catalog/search", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Suggestions Endpoint Tests

    /// <summary>
    /// Verifies that suggestions endpoint returns matching items.
    /// </summary>
    [Fact]
    public async Task Suggestions_WithQuery_ReturnsSuggestions()
    {
        // Arrange
        var httpClient = CreateHttpClient(new ApiVersion(2.0));

        // Act
        var response = await httpClient.GetAsync("/api/catalog/search/suggestions?q=Alp", TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var result = JsonSerializer.Deserialize<List<SearchSuggestion>>(body, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.True(result.Count > 0, "Expected at least one suggestion");
        Assert.All(result, suggestion =>
            Assert.Contains("Alp", suggestion.Name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Verifies that suggestions respect the limit parameter.
    /// </summary>
    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(8)]
    public async Task Suggestions_RespectsLimit(int limit)
    {
        // Arrange
        var httpClient = CreateHttpClient(new ApiVersion(2.0));
        var searchTerm = "a"; // Broad search to get many suggestions

        // Act
        var response = await httpClient.GetAsync($"/api/catalog/search/suggestions?q={searchTerm}&limit={limit}", TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var result = JsonSerializer.Deserialize<List<SearchSuggestion>>(body, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.True(result.Count <= limit, $"Expected at most {limit} suggestions");
    }

    /// <summary>
    /// Verifies that suggestions returns empty for queries under 2 characters.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("A")]
    public async Task Suggestions_WithShortQuery_ReturnsEmpty(string query)
    {
        // Arrange
        var httpClient = CreateHttpClient(new ApiVersion(2.0));

        // Act
        var response = await httpClient.GetAsync($"/api/catalog/search/suggestions?q={query}", TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var result = JsonSerializer.Deserialize<List<SearchSuggestion>>(body, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    /// <summary>
    /// Verifies that suggestions returns distinct results (no duplicates).
    /// </summary>
    [Fact]
    public async Task Suggestions_ReturnsDistinctResults()
    {
        // Arrange
        var httpClient = CreateHttpClient(new ApiVersion(2.0));

        // Act
        var response = await httpClient.GetAsync("/api/catalog/search/suggestions?q=Al&limit=20", TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var result = JsonSerializer.Deserialize<List<SearchSuggestion>>(body, _jsonSerializerOptions);

        Assert.NotNull(result);
        var distinctIds = result.Select(s => s.Id).Distinct().Count();
        Assert.Equal(distinctIds, result.Count);
    }

    /// <summary>
    /// Verifies that suggestion response has correct structure (Id, Name, Category).
    /// </summary>
    [Fact]
    public async Task Suggestions_ReturnsCorrectStructure()
    {
        // Arrange
        var httpClient = CreateHttpClient(new ApiVersion(2.0));

        // Act
        var response = await httpClient.GetAsync("/api/catalog/search/suggestions?q=Alpine", TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var result = JsonSerializer.Deserialize<List<SearchSuggestion>>(body, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.True(result.Count > 0, "Expected at least one suggestion for structure validation");

        var firstSuggestion = result.First();
        Assert.True(firstSuggestion.Id > 0, "Suggestion should have a positive Id");
        Assert.False(string.IsNullOrWhiteSpace(firstSuggestion.Name), "Suggestion should have a Name");
        Assert.False(string.IsNullOrWhiteSpace(firstSuggestion.Category), "Suggestion should have a Category");
    }

    /// <summary>
    /// Verifies that suggestions uses default limit when not specified.
    /// </summary>
    [Fact]
    public async Task Suggestions_UsesDefaultLimitWhenNotSpecified()
    {
        // Arrange
        var httpClient = CreateHttpClient(new ApiVersion(2.0));
        var searchTerm = "a"; // Broad search
        var expectedDefaultLimit = 5; // As per requirements

        // Act
        var response = await httpClient.GetAsync($"/api/catalog/search/suggestions?q={searchTerm}", TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var result = JsonSerializer.Deserialize<List<SearchSuggestion>>(body, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.True(result.Count <= expectedDefaultLimit, $"Expected at most {expectedDefaultLimit} suggestions with default limit");
    }

    #endregion
}

/// <summary>
/// DTO for search suggestion response.
/// Matches the expected API response structure.
/// </summary>
public record SearchSuggestion(int Id, string Name, string Category);
