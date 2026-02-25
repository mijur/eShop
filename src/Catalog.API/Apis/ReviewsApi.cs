using System.ComponentModel;
using eShop.ServiceDefaults;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace eShop.Catalog.API;

public static class ReviewsApi
{
    public static IEndpointRouteBuilder MapReviewsApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/catalog/items");

        api.MapPost("/{itemId:int}/reviews", CreateReview)
            .WithName("CreateReview")
            .WithSummary("Create a review for a catalog item")
            .WithDescription("Create a new review for the specified catalog item. Requires authentication.")
            .WithTags("Reviews")
            .RequireAuthorization();

        api.MapGet("/{itemId:int}/reviews", GetReviews)
            .WithName("GetReviews")
            .WithSummary("Get reviews for a catalog item")
            .WithDescription("Get a paginated list of reviews for the specified catalog item.")
            .WithTags("Reviews");

        return app;
    }

    public static async Task<Results<Created<ReviewDto>, NotFound<ProblemDetails>, BadRequest<ProblemDetails>>> CreateReview(
        HttpContext httpContext,
        [AsParameters] CatalogServices services,
        [Description("The catalog item id")] int itemId,
        CreateReviewRequest request)
    {
        // Extract user information from claims
        var userName = httpContext.User.GetUserName();
        var userId = httpContext.User.GetUserId();

        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(userId))
        {
            return TypedResults.BadRequest<ProblemDetails>(new()
            {
                Detail = "User information could not be determined from authentication token."
            });
        }

        // Validate catalog item exists
        var catalogItem = await services.Context.CatalogItems.FindAsync(itemId);
        if (catalogItem == null)
        {
            return TypedResults.NotFound<ProblemDetails>(new()
            {
                Detail = $"Catalog item with id {itemId} not found."
            });
        }

        // Create review entity
        var review = new Review(request.Comment, userName, userId)
        {
            Rating = request.Rating,
            CatalogItemId = itemId
        };

        services.Context.Reviews.Add(review);
        await services.Context.SaveChangesAsync();

        // Create response DTO
        var reviewDto = new ReviewDto(
            review.Id,
            review.Rating,
            review.Comment,
            review.ReviewerName,
            review.CreatedAt,
            review.CatalogItemId,
            review.UserId
        );

        return TypedResults.Created($"/api/catalog/items/{itemId}/reviews/{review.Id}", reviewDto);
    }

    public static async Task<Results<Ok<PaginatedItems<ReviewDto>>, NotFound<ProblemDetails>>> GetReviews(
        [AsParameters] CatalogServices services,
        [AsParameters] PaginationRequest paginationRequest,
        [Description("The catalog item id")] int itemId)
    {
        // Validate catalog item exists
        var catalogItem = await services.Context.CatalogItems.FindAsync(itemId);
        if (catalogItem == null)
        {
            return TypedResults.NotFound<ProblemDetails>(new()
            {
                Detail = $"Catalog item with id {itemId} not found."
            });
        }

        var pageSize = paginationRequest.PageSize;
        var pageIndex = paginationRequest.PageIndex;

        // Query reviews for this catalog item
        var totalItems = await services.Context.Reviews
            .Where(r => r.CatalogItemId == itemId)
            .LongCountAsync();

        var reviews = await services.Context.Reviews
            .Where(r => r.CatalogItemId == itemId)
            .OrderByDescending(r => r.CreatedAt)
            .Skip(pageSize * pageIndex)
            .Take(pageSize)
            .ToListAsync();

        // Map to DTOs
        var reviewDtos = reviews.Select(r => new ReviewDto(
            r.Id,
            r.Rating,
            r.Comment,
            r.ReviewerName,
            r.CreatedAt,
            r.CatalogItemId,
            r.UserId
        ));

        return TypedResults.Ok(new PaginatedItems<ReviewDto>(pageIndex, pageSize, totalItems, reviewDtos));
    }
}
