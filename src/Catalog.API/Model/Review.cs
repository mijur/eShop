using System.ComponentModel.DataAnnotations;

namespace eShop.Catalog.API.Model;

public class Review
{
    public int Id { get; set; }

    [Required]
    [Range(1, 5)]
    public int Rating { get; set; }

    [Required]
    [MinLength(10)]
    [MaxLength(1000)]
    public string Comment { get; set; }

    [Required]
    [MaxLength(100)]
    public string ReviewerName { get; set; }

    public DateTime CreatedAt { get; set; }

    public int CatalogItemId { get; set; }

    public CatalogItem? CatalogItem { get; set; }

    [Required]
    [MaxLength(50)]
    public string UserId { get; set; }

    public Review(string comment, string reviewerName, string userId)
    {
        Comment = comment;
        ReviewerName = reviewerName;
        UserId = userId;
        CreatedAt = DateTime.UtcNow;
    }
}
