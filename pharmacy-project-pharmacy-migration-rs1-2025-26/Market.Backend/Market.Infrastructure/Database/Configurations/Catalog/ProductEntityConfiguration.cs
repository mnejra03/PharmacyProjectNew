using Market.Domain.Entities.Catalog;

namespace Market.Infrastructure.Database.Configurations.Catalog;

public sealed class ProductEntityConfiguration : IEntityTypeConfiguration<ProductEntity>
{
    public void Configure(EntityTypeBuilder<ProductEntity> b)
    {
        b.ToTable("Products");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Description).HasMaxLength(4000).IsRequired();
        b.Property(x => x.ImageUrl).HasMaxLength(1000).IsRequired();
        b.Property(x => x.Price).HasPrecision(18, 2);
        b.Property(x => x.DiscountPercentage).HasPrecision(5, 2);
        b.HasIndex(x => x.Name);
        b.HasOne(x => x.Category).WithMany(x => x.Products).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Brand).WithMany(x => x.Products).HasForeignKey(x => x.BrandId).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class ProductCategoryEntityConfiguration : IEntityTypeConfiguration<ProductCategoryEntity>
{
    public void Configure(EntityTypeBuilder<ProductCategoryEntity> b)
    {
        b.ToTable("ProductCategories");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(120).IsRequired();
        b.HasIndex(x => x.Name).IsUnique();
    }
}

public sealed class BrandEntityConfiguration : IEntityTypeConfiguration<BrandEntity>
{
    public void Configure(EntityTypeBuilder<BrandEntity> b)
    {
        b.ToTable("Brands");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(160).IsRequired();
        b.Property(x => x.LogoUrl).HasMaxLength(1000);
        b.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        b.HasIndex(x => x.Name).IsUnique();
    }
}

public sealed class ProductReviewEntityConfiguration : IEntityTypeConfiguration<ProductReviewEntity>
{
    public void Configure(EntityTypeBuilder<ProductReviewEntity> b)
    {
        b.ToTable("ProductReviews");
        b.HasKey(x => x.Id);
        b.Property(x => x.Text).HasMaxLength(2000).IsRequired();
        b.HasOne(x => x.Product).WithMany(x => x.Reviews).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.ProductId, x.UserId }).IsUnique();
    }
}
