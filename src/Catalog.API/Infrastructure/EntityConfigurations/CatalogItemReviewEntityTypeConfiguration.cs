namespace eShop.Catalog.API.Infrastructure.EntityConfigurations;

class CatalogItemReviewEntityTypeConfiguration
    : IEntityTypeConfiguration<CatalogItemReview>
{
    public void Configure(EntityTypeBuilder<CatalogItemReview> builder)
    {
        builder.ToTable("CatalogItemReview");

        builder.Property(r => r.Comment)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(r => r.UserId)
            .IsRequired();

        builder.Property(r => r.Rating)
            .IsRequired();

        builder.Property(r => r.ReviewDate)
            .IsRequired();

        builder.HasOne(r => r.CatalogItem)
            .WithMany()
            .HasForeignKey(r => r.CatalogItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique composite index to enforce one review per user per item
        builder.HasIndex(r => new { r.UserId, r.CatalogItemId })
            .IsUnique();

        // Index on CatalogItemId for efficient queries
        builder.HasIndex(r => r.CatalogItemId);
    }
}
