namespace eShop.Catalog.API.Infrastructure.EntityConfigurations;

class ReviewEntityTypeConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("Review");

        builder.Property(r => r.Comment)
            .HasMaxLength(1000);

        builder.Property(r => r.ReviewerName)
            .HasMaxLength(100);

        builder.Property(r => r.UserId)
            .HasMaxLength(50);

        builder.HasOne(r => r.CatalogItem)
            .WithMany()
            .HasForeignKey(r => r.CatalogItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => r.CatalogItemId);
        builder.HasIndex(r => r.CreatedAt);
        builder.HasIndex(r => r.UserId);
    }
}
