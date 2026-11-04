namespace Market.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<MarketUserEntity> Users { get; }
    DbSet<RefreshTokenEntity> RefreshTokens { get; }
    DbSet<PasswordResetTokenEntity> PasswordResetTokens { get; }
    DbSet<ChatMessageEntity> ChatMessages { get; }
    DbSet<NotificationEntity> Notifications { get; }
    DbSet<RecipeEntity> Recipes { get; }
    DbSet<AdvertisementEntity> Advertisements { get; }
    DbSet<ProductEntity> Products { get; }
    DbSet<ProductCategoryEntity> ProductCategories { get; }
    DbSet<BrandEntity> Brands { get; }
    DbSet<ProductReviewEntity> ProductReviews { get; }
    DbSet<CartEntity> Carts { get; }
    DbSet<CartItemEntity> CartItems { get; }
    DbSet<OrderEntity> Orders { get; }
    DbSet<OrderItemEntity> OrderItems { get; }
    DbSet<WishlistEntity> Wishlists { get; }
    DbSet<WishlistItemEntity> WishlistItems { get; }
    Task<int> SaveChangesAsync(CancellationToken ct);
}
