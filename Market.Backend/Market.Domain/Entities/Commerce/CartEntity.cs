using Market.Domain.Common;
using Market.Domain.Entities.Catalog;
using Market.Domain.Entities.Identity;

namespace Market.Domain.Entities.Commerce;

public sealed class CartEntity : BaseEntity
{
    public int UserId { get; set; }
    public MarketUserEntity? User { get; set; }
    public bool IsCompleted { get; set; }
    public ICollection<CartItemEntity> Items { get; set; } = new List<CartItemEntity>();
}

public sealed class CartItemEntity : BaseEntity
{
    public int CartId { get; set; }
    public CartEntity? Cart { get; set; }
    public int ProductId { get; set; }
    public ProductEntity? Product { get; set; }
    public int Quantity { get; set; }
    public bool SavedForLater { get; set; }
}

public sealed class OrderEntity : BaseEntity
{
    public int UserId { get; set; }
    public MarketUserEntity? User { get; set; }
    public DateTime OrderedAtUtc { get; set; }
    public string Status { get; set; } = "Pending";
    public decimal TotalPrice { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? PaymentReference { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;
    public bool IsSupplyOrder { get; set; }
    public ICollection<OrderItemEntity> Items { get; set; } = new List<OrderItemEntity>();
}

public sealed class OrderItemEntity : BaseEntity
{
    public int OrderId { get; set; }
    public OrderEntity? Order { get; set; }
    public int ProductId { get; set; }
    public ProductEntity? Product { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public sealed class WishlistEntity : BaseEntity
{
    public int UserId { get; set; }
    public MarketUserEntity? User { get; set; }
    public ICollection<WishlistItemEntity> Items { get; set; } = new List<WishlistItemEntity>();
}

public sealed class WishlistItemEntity : BaseEntity
{
    public int WishlistId { get; set; }
    public WishlistEntity? Wishlist { get; set; }
    public int ProductId { get; set; }
    public ProductEntity? Product { get; set; }
}
