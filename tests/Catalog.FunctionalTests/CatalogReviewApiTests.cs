using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Asp.Versioning;
using Asp.Versioning.Http;
using eShop.Catalog.API.Model;
using Microsoft.AspNetCore.Mvc.Testing;

namespace eShop.Catalog.FunctionalTests;

public sealed class CatalogReviewApiTests : IClassFixture<CatalogApiFixture>
{
    private readonly WebApplicationFactory<Program> _webApplicationFactory;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web);

    public CatalogReviewApiTests(CatalogApiFixture fixture)
    {
        _webApplicationFactory = fixture;
    }

    private HttpClient CreateHttpClient(ApiVersion apiVersion)
    {
        var handler = new ApiVersionHandler(new QueryStringApiVersionWriter(), apiVersion);
        return _webApplicationFactory.CreateDefaultClient(handler);
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(2.0)]
    public async Task CreateReview_WithValidData_CreatesReviewSuccessfully(double version)
    {
        // === ARRANGE ===
        var httpClient = CreateHttpClient(new ApiVersion(version));
        var request = new CreateReviewRequest(
            UserId: $"user-{Guid.NewGuid()}",
            Rating: 5,
            Comment: "Excellent product! Very satisfied with my purchase."
        );

        // === ACT ===
        var response = await httpClient.PostAsJsonAsync("/api/catalog/items/1/reviews", request, TestContext.Current.CancellationToken);

        // === ASSERT ===
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.StartsWith("/api/catalog/items/1/reviews/", response.Headers.Location!.ToString());
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(2.0)]
    public async Task CreateReview_WithDuplicateUserAndItem_ReturnsConflict(double version)
    {
        // === ARRANGE ===
        var httpClient = CreateHttpClient(new ApiVersion(version));
        var userId = $"user-duplicate-{Guid.NewGuid()}";
        var request = new CreateReviewRequest(
            UserId: userId,
            Rating: 4,
            Comment: "Good product"
        );

        // Create first review
        await httpClient.PostAsJsonAsync("/api/catalog/items/2/reviews", request, TestContext.Current.CancellationToken);

        // === ACT ===
        // Try to create duplicate review
        var response = await httpClient.PostAsJsonAsync("/api/catalog/items/2/reviews", request, TestContext.Current.CancellationToken);

        // === ASSERT ===
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains(userId, body);
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(2.0)]
    public async Task CreateReview_WithInvalidRating_ReturnsBadRequest(double version)
    {
        // === ARRANGE ===
        var httpClient = CreateHttpClient(new ApiVersion(version));
        var request = new CreateReviewRequest(
            UserId: $"user-{Guid.NewGuid()}",
            Rating: 6, // Invalid: must be 1-5
            Comment: "This should fail"
        );

        // === ACT ===
        var response = await httpClient.PostAsJsonAsync("/api/catalog/items/3/reviews", request, TestContext.Current.CancellationToken);

        // === ASSERT ===
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("Rating must be between 1 and 5", body);
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(2.0)]
    public async Task CreateReview_WithNonExistentItem_ReturnsNotFound(double version)
    {
        // === ARRANGE ===
        var httpClient = CreateHttpClient(new ApiVersion(version));
        var request = new CreateReviewRequest(
            UserId: $"user-{Guid.NewGuid()}",
            Rating: 4,
            Comment: "Review for non-existent item"
        );

        // === ACT ===
        var response = await httpClient.PostAsJsonAsync("/api/catalog/items/999999/reviews", request, TestContext.Current.CancellationToken);

        // === ASSERT ===
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("999999", body);
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(2.0)]
    public async Task GetReviewsForItem_WithExistingReviews_ReturnsReviews(double version)
    {
        // === ARRANGE ===
        var httpClient = CreateHttpClient(new ApiVersion(version));

        // Create some reviews first
        var review1 = new CreateReviewRequest(
            UserId: $"user-get-reviews-1-{Guid.NewGuid()}",
            Rating: 5,
            Comment: "Great product!"
        );
        var review2 = new CreateReviewRequest(
            UserId: $"user-get-reviews-2-{Guid.NewGuid()}",
            Rating: 4,
            Comment: "Good quality"
        );

        await httpClient.PostAsJsonAsync("/api/catalog/items/4/reviews", review1, TestContext.Current.CancellationToken);
        await httpClient.PostAsJsonAsync("/api/catalog/items/4/reviews", review2, TestContext.Current.CancellationToken);

        // === ACT ===
        var response = await httpClient.GetAsync("/api/catalog/items/4/reviews?pageIndex=0&pageSize=10", TestContext.Current.CancellationToken);

        // === ASSERT ===
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var result = JsonSerializer.Deserialize<PaginatedItems<CatalogItemReview>>(body, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.True(result!.Data.Count() >= 2);
        Assert.Equal(0, result.PageIndex);
        Assert.Equal(10, result.PageSize);
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(2.0)]
    public async Task GetReviewsForItem_WithNonExistentItem_ReturnsNotFound(double version)
    {
        // === ARRANGE ===
        var httpClient = CreateHttpClient(new ApiVersion(version));

        // === ACT ===
        var response = await httpClient.GetAsync("/api/catalog/items/999999/reviews?pageIndex=0&pageSize=10", TestContext.Current.CancellationToken);

        // === ASSERT ===
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("999999", body);
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(2.0)]
    public async Task GetReviewsForItem_RespectsPagination(double version)
    {
        // === ARRANGE ===
        var httpClient = CreateHttpClient(new ApiVersion(version));

        // Create 5 reviews for item 7
        for (int i = 0; i < 5; i++)
        {
            var review = new CreateReviewRequest(
                UserId: $"user-pagination-{i}-{Guid.NewGuid()}",
                Rating: 3 + (i % 3), // Ratings 3, 4, 5, 3, 4
                Comment: $"Review number {i + 1}"
            );
            await httpClient.PostAsJsonAsync("/api/catalog/items/7/reviews", review, TestContext.Current.CancellationToken);
        }

        // === ACT ===
        // Get first page with page size 2
        var response1 = await httpClient.GetAsync("/api/catalog/items/7/reviews?pageIndex=0&pageSize=2", TestContext.Current.CancellationToken);
        var body1 = await response1.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var result1 = JsonSerializer.Deserialize<PaginatedItems<CatalogItemReview>>(body1, _jsonSerializerOptions);

        // Get second page
        var response2 = await httpClient.GetAsync("/api/catalog/items/7/reviews?pageIndex=1&pageSize=2", TestContext.Current.CancellationToken);
        var body2 = await response2.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var result2 = JsonSerializer.Deserialize<PaginatedItems<CatalogItemReview>>(body2, _jsonSerializerOptions);

        // === ASSERT ===
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);

        Assert.NotNull(result1);
        Assert.Equal(2, result1!.Data.Count());
        Assert.Equal(0, result1.PageIndex);
        Assert.Equal(2, result1.PageSize);
        Assert.True(result1.Count >= 5);

        Assert.NotNull(result2);
        Assert.Equal(2, result2!.Data.Count());
        Assert.Equal(1, result2.PageIndex);
        Assert.Equal(2, result2.PageSize);

        // Verify different reviews on different pages
        var page1ReviewIds = result1.Data.Select(r => r.Id).ToList();
        var page2ReviewIds = result2.Data.Select(r => r.Id).ToList();
        Assert.Empty(page1ReviewIds.Intersect(page2ReviewIds));
    }
}
