using System.ComponentModel.DataAnnotations;

namespace eShop.Catalog.API.Model;

public record CreateReviewRequest(
    [Required]
    [Range(1, 5)]
    int Rating,

    [Required]
    [MinLength(10)]
    [MaxLength(1000)]
    string Comment
);

public record ReviewDto(
    int Id,
    int Rating,
    string Comment,
    string ReviewerName,
    DateTime CreatedAt,
    int CatalogItemId,
    string UserId
);
