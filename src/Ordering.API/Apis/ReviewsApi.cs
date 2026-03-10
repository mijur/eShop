public static class ReviewsApi
{
    public static RouteGroupBuilder MapReviewsApiV1(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/reviews").HasApiVersion(1.0);

        api.MapGet("{orderId:int}", GetReviewsByOrderId);

        return api;
    }

    public static IEnumerable<ReviewSummary> GetReviewsByOrderId(int orderId)
    {
        // TODO: Implement with proper CQRS query (IReviewQueries) backed by data access
        throw new NotImplementedException("Reviews feature is not yet implemented.");
    }
}

public record ReviewSummary(int Id, int OrderId, string ReviewerName, int Rating, string Comment, DateTime CreatedAt);
