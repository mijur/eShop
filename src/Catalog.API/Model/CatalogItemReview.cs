using System.ComponentModel.DataAnnotations;

namespace eShop.Catalog.API.Model;

public class CatalogItemReview
{
    public int Id { get; set; }

    [Required]
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
    public int Rating { get; set; }

    [Required]
    [MaxLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters")]
    public string Comment { get; set; }

    [Required]
    public string UserId { get; set; }

    public DateTime ReviewDate { get; set; }

    public int CatalogItemId { get; set; }

    public CatalogItem? CatalogItem { get; set; }

    public CatalogItemReview(string userId, int catalogItemId, int rating, string comment)
    {
        UserId = userId;
        CatalogItemId = catalogItemId;
        Rating = rating;
        Comment = comment;
        ReviewDate = DateTime.UtcNow;
    }
}
