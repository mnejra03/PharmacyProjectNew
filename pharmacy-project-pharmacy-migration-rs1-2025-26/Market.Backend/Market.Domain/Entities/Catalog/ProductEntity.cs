using Market.Domain.Common;
using Market.Domain.Entities.Identity;

namespace Market.Domain.Entities.Catalog;

public sealed class ProductEntity : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int QuantityInStock { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public ProductCategoryEntity? Category { get; set; }
    public int? BrandId { get; set; }
    public BrandEntity? Brand { get; set; }
    public bool IsDiscounted { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime AddedAtUtc { get; set; }
    public ICollection<ProductReviewEntity> Reviews { get; set; } = new List<ProductReviewEntity>();
}

public sealed class ProductCategoryEntity : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public ICollection<ProductEntity> Products { get; set; } = new List<ProductEntity>();
}

public sealed class BrandEntity : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string Description { get; set; } = string.Empty;
    public ICollection<ProductEntity> Products { get; set; } = new List<ProductEntity>();
}

public sealed class ProductReviewEntity : BaseEntity
{
    public int ProductId { get; set; }
    public ProductEntity? Product { get; set; }
    public int UserId { get; set; }
    public MarketUserEntity? User { get; set; }
    public int Rating { get; set; }
    public string Text { get; set; } = string.Empty;
}
