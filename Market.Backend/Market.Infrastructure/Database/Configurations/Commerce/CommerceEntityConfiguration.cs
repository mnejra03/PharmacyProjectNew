using Market.Domain.Entities.Catalog;
using Market.Domain.Entities.Commerce;
using Market.Domain.Entities.Identity;

namespace Market.Infrastructure.Database.Configurations.Commerce;

public sealed class CartEntityConfiguration : IEntityTypeConfiguration<CartEntity>
{
    public void Configure(EntityTypeBuilder<CartEntity> b)
    {
        b.ToTable("Carts");
        b.HasKey(x => x.Id);
        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.UserId, x.IsCompleted });
    }
}

public sealed class CartItemEntityConfiguration : IEntityTypeConfiguration<CartItemEntity>
{
    public void Configure(EntityTypeBuilder<CartItemEntity> b)
    {
        b.ToTable("CartItems");
        b.HasKey(x => x.Id);
        b.HasOne(x => x.Cart).WithMany(x => x.Items).HasForeignKey(x => x.CartId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.CartId, x.ProductId }).IsUnique();
    }
}

public sealed class OrderEntityConfiguration : IEntityTypeConfiguration<OrderEntity>
{
    public void Configure(EntityTypeBuilder<OrderEntity> b)
    {
        b.ToTable("Orders");
        b.HasKey(x => x.Id);
        b.Property(x => x.Status).HasMaxLength(40).IsRequired();
        b.Property(x => x.PaymentMethod).HasMaxLength(40).IsRequired();
        b.Property(x => x.PaymentReference).HasMaxLength(200);
        b.Property(x => x.ShippingAddress).HasMaxLength(1000).IsRequired();
        b.Property(x => x.TotalPrice).HasPrecision(18, 2);
        b.Property(x => x.IsSupplyOrder).HasDefaultValue(false);
        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class OrderItemEntityConfiguration : IEntityTypeConfiguration<OrderItemEntity>
{
    public void Configure(EntityTypeBuilder<OrderItemEntity> b)
    {
        b.ToTable("OrderItems");
        b.HasKey(x => x.Id);
        b.Property(x => x.ProductName).HasMaxLength(200).IsRequired();
        b.Property(x => x.UnitPrice).HasPrecision(18, 2);
        b.HasOne(x => x.Order).WithMany(x => x.Items).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class WishlistEntityConfiguration : IEntityTypeConfiguration<WishlistEntity>
{
    public void Configure(EntityTypeBuilder<WishlistEntity> b)
    {
        b.ToTable("Wishlists");
        b.HasKey(x => x.Id);
        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => x.UserId).IsUnique();
    }
}

public sealed class WishlistItemEntityConfiguration : IEntityTypeConfiguration<WishlistItemEntity>
{
    public void Configure(EntityTypeBuilder<WishlistItemEntity> b)
    {
        b.ToTable("WishlistItems");
        b.HasKey(x => x.Id);
        b.HasOne(x => x.Wishlist).WithMany(x => x.Items).HasForeignKey(x => x.WishlistId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.WishlistId, x.ProductId }).IsUnique();
    }
}
