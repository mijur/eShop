using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace eShop.Catalog.API.Model;

public record CreateReviewRequest(
    [property: Description("The ID of the user submitting the review")]
    [property: Required]
    string UserId,

    [property: Description("Rating from 1 to 5 stars")]
    [property: Required]
    [property: Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
    int Rating,

    [property: Description("Review comment text")]
    [property: Required]
    [property: MaxLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters")]
    string Comment
);
