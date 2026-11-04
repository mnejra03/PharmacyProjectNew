using Market.Domain.Entities.Commerce;

namespace Market.Application.Modules.Catalog;

public sealed record CartItemDto(int Id, int ProductId, string Name, string ImageUrl, int Quantity, decimal UnitPrice, decimal LineTotal, bool SavedForLater, int Stock);
public sealed record CartDto(IReadOnlyList<CartItemDto> Items, decimal Total, int ItemCount);
public sealed record GetCartQuery : IRequest<CartDto>;
public sealed record AddCartItemCommand(int ProductId, int Quantity = 1) : IRequest<CartDto>;
public sealed record UpdateCartItemCommand(int Id, int Quantity, bool SavedForLater) : IRequest<CartDto>;
public sealed record RemoveCartItemCommand(int Id) : IRequest;
public sealed record CheckoutCommand(string ShippingAddress, string PaymentMethod, string? PaymentReference = null) : IRequest<OrderDto>;
public sealed record CreateCartPaymentIntentCommand : IRequest<CartPaymentIntentDto>;
public sealed record CartPaymentIntentDto(string PaymentIntentId, string ClientSecret, string PublishableKey, decimal Amount, string Currency);
public sealed record OrderItemDto(int ProductId, string Name, int Quantity, decimal UnitPrice);
public sealed record OrderDto(int Id, DateTime OrderedAtUtc, string Status, decimal TotalPrice, string PaymentMethod, string ShippingAddress, string CustomerName, string CustomerEmail, IReadOnlyList<OrderItemDto> Items, bool IsSupplyOrder = false);
public sealed record GetOrdersQuery(bool All = false) : IRequest<IReadOnlyList<OrderDto>>;
public sealed record UpdateOrderStatusCommand(int Id, string Status) : IRequest;

public sealed class GetCartHandler(IAppDbContext db, IAppCurrentUser user) : IRequestHandler<GetCartQuery, CartDto>
{
    public async Task<CartDto> Handle(GetCartQuery request, CancellationToken ct)
    {
        var cart = await GetActiveCart(db, user, ct);
        return Map(cart);
    }

    internal static async Task<CartEntity> GetActiveCart(IAppDbContext db, IAppCurrentUser user, CancellationToken ct)
    {
        if (user.UserId is not int userId) throw new MarketConflictException("Prijava je obavezna.");
        var cart = await db.Carts.Include(c => c.Items).ThenInclude(i => i.Product).FirstOrDefaultAsync(c => c.UserId == userId && !c.IsCompleted, ct);
        if (cart is not null) return cart;
        cart = new CartEntity { UserId = userId };
        db.Carts.Add(cart); await db.SaveChangesAsync(ct);
        return cart;
    }

    internal static CartDto Map(CartEntity cart)
    {
        var items = cart.Items.Where(i => i.Product is not null).Select(i => new CartItemDto(i.Id, i.ProductId, i.Product!.Name, i.Product.ImageUrl, i.Quantity, CurrentPrice(i.Product), CurrentPrice(i.Product) * i.Quantity, i.SavedForLater, i.Product.QuantityInStock)).ToList();
        return new(items, items.Where(i => !i.SavedForLater).Sum(i => i.LineTotal), items.Where(i => !i.SavedForLater).Sum(i => i.Quantity));
    }

    internal static decimal CurrentPrice(ProductEntity product) => product.IsDiscounted && product.DiscountPercentage.HasValue
        ? decimal.Round(product.Price * (1 - product.DiscountPercentage.Value / 100m), 2) : product.Price;
}

public sealed class AddCartItemHandler(IAppDbContext db, IAppCurrentUser user) : IRequestHandler<AddCartItemCommand, CartDto>
{
    public async Task<CartDto> Handle(AddCartItemCommand request, CancellationToken ct)
    {
        if (request.Quantity < 1) throw new MarketConflictException("Količina mora biti veća od nule.");
        var cart = await GetCartHandler.GetActiveCart(db, user, ct);
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId, ct) ?? throw new MarketNotFoundException("Proizvod nije pronađen.");
        var item = cart.Items.FirstOrDefault(i => i.ProductId == product.Id);
        var targetQuantity = (item?.Quantity ?? 0) + request.Quantity;
        if (targetQuantity > product.QuantityInStock) throw new MarketConflictException("Nema dovoljno proizvoda na stanju.");
        if (item is null) db.CartItems.Add(new CartItemEntity { CartId = cart.Id, ProductId = product.Id, Quantity = request.Quantity }); else item.Quantity = targetQuantity;
        await db.SaveChangesAsync(ct);
        cart = await GetCartHandler.GetActiveCart(db, user, ct);
        return GetCartHandler.Map(cart);
    }
}

public sealed class UpdateCartItemHandler(IAppDbContext db, IAppCurrentUser user) : IRequestHandler<UpdateCartItemCommand, CartDto>
{
    public async Task<CartDto> Handle(UpdateCartItemCommand request, CancellationToken ct)
    {
        var cart = await GetCartHandler.GetActiveCart(db, user, ct);
        var item = cart.Items.FirstOrDefault(i => i.Id == request.Id) ?? throw new MarketNotFoundException("Stavka korpe nije pronađena.");
        if (request.Quantity < 1 || item.Product is null || request.Quantity > item.Product.QuantityInStock) throw new MarketConflictException("Količina nije dostupna na stanju.");
        item.Quantity = request.Quantity; item.SavedForLater = request.SavedForLater;
        await db.SaveChangesAsync(ct);
        return GetCartHandler.Map(await GetCartHandler.GetActiveCart(db, user, ct));
    }
}

public sealed class RemoveCartItemHandler(IAppDbContext db, IAppCurrentUser user) : IRequestHandler<RemoveCartItemCommand>
{
    public async Task Handle(RemoveCartItemCommand request, CancellationToken ct)
    {
        var cart = await GetCartHandler.GetActiveCart(db, user, ct);
        var item = cart.Items.FirstOrDefault(i => i.Id == request.Id) ?? throw new MarketNotFoundException("Stavka korpe nije pronađena.");
        db.CartItems.Remove(item); await db.SaveChangesAsync(ct);
    }
}

public sealed class CheckoutValidator : AbstractValidator<CheckoutCommand>
{
    public CheckoutValidator() { RuleFor(x => x.ShippingAddress).NotEmpty().MaximumLength(1000); RuleFor(x => x.PaymentMethod).Must(x => new[] { "Card", "CashOnDelivery" }.Contains(x)).WithMessage("Nepoznat način plaćanja."); }
}

public sealed class CreateCartPaymentIntentHandler(IAppDbContext db, IAppCurrentUser user, IStripePaymentGateway stripe) : IRequestHandler<CreateCartPaymentIntentCommand, CartPaymentIntentDto>
{
    public async Task<CartPaymentIntentDto> Handle(CreateCartPaymentIntentCommand request, CancellationToken ct)
    {
        if (user.UserId is not int userId) throw new MarketConflictException("Prijava je obavezna.");
        if (!stripe.IsConfigured) throw new MarketConflictException("Stripe test ključevi nisu podešeni. Postavite Stripe__SecretKey i Stripe__PublishableKey.");
        var cart = await GetCartHandler.GetActiveCart(db, user, ct);
        var items = cart.Items.Where(i => !i.SavedForLater).ToList();
        if (items.Count == 0) throw new MarketConflictException("Korpa je prazna.");
        foreach (var item in items)
            if (item.Product is null || item.Product.QuantityInStock < item.Quantity) throw new MarketConflictException("Jedan ili više proizvoda više nisu dostupni u traženoj količini.");
        var total = items.Sum(i => GetCartHandler.CurrentPrice(i.Product!) * i.Quantity);
        var amountMinor = decimal.ToInt64(decimal.Round(total * 100m, 0, MidpointRounding.AwayFromZero));
        var intent = await stripe.CreateIntentAsync(amountMinor, "bam", userId.ToString(), ct);
        return new(intent.Id, intent.ClientSecret, stripe.PublishableKey, total, intent.Currency);
    }
}

public sealed class CheckoutHandler(IAppDbContext db, IAppCurrentUser user, IStripePaymentGateway stripe) : IRequestHandler<CheckoutCommand, OrderDto>
{
    public async Task<OrderDto> Handle(CheckoutCommand request, CancellationToken ct)
    {
        var cart = await GetCartHandler.GetActiveCart(db, user, ct);
        var items = cart.Items.Where(i => !i.SavedForLater).ToList();
        if (items.Count == 0) throw new MarketConflictException("Korpa je prazna.");
        foreach (var item in items)
        {
            if (item.Product is null || item.Product.QuantityInStock < item.Quantity) throw new MarketConflictException($"Proizvod {item.Product?.Name ?? item.ProductId.ToString()} više nije dostupan u traženoj količini.");
        }
        var totalDue = items.Sum(i => GetCartHandler.CurrentPrice(i.Product!) * i.Quantity);
        if (request.PaymentMethod == "Card")
        {
            if (string.IsNullOrWhiteSpace(request.PaymentReference)) throw new MarketConflictException("Kartično plaćanje nije potvrđeno.");
            var payment = await stripe.GetIntentAsync(request.PaymentReference, ct);
            var expectedMinor = decimal.ToInt64(decimal.Round(totalDue * 100m, 0, MidpointRounding.AwayFromZero));
            if (payment is null || payment.Status != "succeeded" || payment.UserId != user.UserId!.Value.ToString() || payment.Currency != "bam" || payment.AmountMinor != expectedMinor)
                throw new MarketConflictException("Stripe ne potvrđuje uspješno plaćanje za ovu korpu.");
        }
        var customer = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == user.UserId, ct)
            ?? throw new MarketNotFoundException("Korisnik nije pronađen.");
        var order = new OrderEntity { UserId = user.UserId!.Value, OrderedAtUtc = DateTime.UtcNow, Status = "Pending", PaymentMethod = request.PaymentMethod, PaymentReference = request.PaymentReference, ShippingAddress = request.ShippingAddress.Trim() };
        foreach (var item in items)
        {
            var product = item.Product!; var price = GetCartHandler.CurrentPrice(product);
            order.Items.Add(new OrderItemEntity { ProductId = product.Id, ProductName = product.Name, Quantity = item.Quantity, UnitPrice = price });
            product.QuantityInStock -= item.Quantity; order.TotalPrice += price * item.Quantity;
        }
        db.Orders.Add(order); cart.IsCompleted = true;
        await db.SaveChangesAsync(ct);
        return new(order.Id, order.OrderedAtUtc, order.Status, order.TotalPrice, order.PaymentMethod, order.ShippingAddress, $"{customer.FirstName} {customer.LastName}".Trim(), customer.Email, order.Items.Select(i => new OrderItemDto(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice)).ToList());
    }
}

public sealed class GetOrdersHandler(IAppDbContext db, IAppCurrentUser user) : IRequestHandler<GetOrdersQuery, IReadOnlyList<OrderDto>>
{
    public async Task<IReadOnlyList<OrderDto>> Handle(GetOrdersQuery request, CancellationToken ct)
    {
        if (user.UserId is null) throw new MarketConflictException("Prijava je obavezna.");
        if (request.All && !user.IsAdmin && !user.IsPharmacist) throw new MarketConflictException("Nemate pravo pregledati sve narudžbe.");
        var query = db.Orders.AsNoTracking().Where(o => !o.IsSupplyOrder).Include(o => o.Items).Include(o => o.User).AsQueryable();
        if (!request.All) query = query.Where(o => o.UserId == user.UserId);
        var orders = await query.OrderByDescending(o => o.OrderedAtUtc).ToListAsync(ct);
        return orders.Select(o => new OrderDto(o.Id, o.OrderedAtUtc, o.Status, o.TotalPrice, o.PaymentMethod, o.ShippingAddress, $"{o.User?.FirstName} {o.User?.LastName}".Trim(), o.User?.Email ?? string.Empty, o.Items.Select(i => new OrderItemDto(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice)).ToList())).ToList();
    }
}

public sealed class UpdateOrderStatusHandler(IAppDbContext db, IAppCurrentUser user) : IRequestHandler<UpdateOrderStatusCommand>
{
    public async Task Handle(UpdateOrderStatusCommand request, CancellationToken ct)
    {
        if (!user.IsAdmin && !user.IsPharmacist) throw new MarketConflictException("Samo administrator ili farmaceut može mijenjati narudžbe.");
        if (!new[] { "Pending", "Processing", "Shipped", "Completed", "Cancelled" }.Contains(request.Status)) throw new MarketConflictException("Status narudžbe nije ispravan.");
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == request.Id, ct) ?? throw new MarketNotFoundException("Narudžba nije pronađena.");
        if (order.IsSupplyOrder) throw new MarketConflictException("Narudžbu zaliha ažurirajte kroz pregled nabavke.");
        order.Status = request.Status; await db.SaveChangesAsync(ct);
    }
}

public sealed record GetWishlistQuery : IRequest<IReadOnlyList<ProductDto>>;
public sealed record AddWishlistItemCommand(int ProductId) : IRequest;
public sealed record RemoveWishlistItemCommand(int ProductId) : IRequest;
public sealed class GetWishlistHandler(IAppDbContext db, IAppCurrentUser user) : IRequestHandler<GetWishlistQuery, IReadOnlyList<ProductDto>>
{
    public async Task<IReadOnlyList<ProductDto>> Handle(GetWishlistQuery request, CancellationToken ct)
    {
        if (user.UserId is null) throw new MarketConflictException("Prijava je obavezna.");
        return await db.WishlistItems.AsNoTracking().Where(w => w.Wishlist!.UserId == user.UserId).Include(w => w.Product).ThenInclude(p => p!.Category).Include(w => w.Product).ThenInclude(p => p!.Brand).Include(w => w.Product).ThenInclude(p => p!.Reviews).Select(w => w.Product!).ToListAsync(ct) is var products
            ? products.Select(ProductListHandler.ToDto).ToList() : Array.Empty<ProductDto>();
    }
}
public sealed class AddWishlistHandler(IAppDbContext db, IAppCurrentUser user) : IRequestHandler<AddWishlistItemCommand>
{
    public async Task Handle(AddWishlistItemCommand request, CancellationToken ct)
    {
        if (user.UserId is not int userId) throw new MarketConflictException("Prijava je obavezna.");
        if (!await db.Products.AnyAsync(p => p.Id == request.ProductId, ct)) throw new MarketNotFoundException("Proizvod nije pronađen.");
        var list = await db.Wishlists.Include(w => w.Items).FirstOrDefaultAsync(w => w.UserId == userId, ct);
        if (list is null) { list = new WishlistEntity { UserId = userId }; db.Wishlists.Add(list); await db.SaveChangesAsync(ct); }
        if (!list.Items.Any(i => i.ProductId == request.ProductId)) db.WishlistItems.Add(new WishlistItemEntity { WishlistId = list.Id, ProductId = request.ProductId });
        await db.SaveChangesAsync(ct);
    }
}
public sealed class RemoveWishlistHandler(IAppDbContext db, IAppCurrentUser user) : IRequestHandler<RemoveWishlistItemCommand>
{
    public async Task Handle(RemoveWishlistItemCommand request, CancellationToken ct)
    {
        if (user.UserId is null) throw new MarketConflictException("Prijava je obavezna.");
        var item = await db.WishlistItems.Include(i => i.Wishlist).FirstOrDefaultAsync(i => i.ProductId == request.ProductId && i.Wishlist!.UserId == user.UserId, ct);
        if (item is not null) { db.WishlistItems.Remove(item); await db.SaveChangesAsync(ct); }
    }
}

public sealed record ProductReviewsQuery(int ProductId) : IRequest<IReadOnlyList<ProductReviewDto>>;
public sealed record ProductReviewDto(int Id, string UserName, int Rating, string Text, DateTime CreatedAtUtc);
public sealed record AddProductReviewCommand(int ProductId, int Rating, string Text) : IRequest<ProductReviewDto>;
public sealed class AddProductReviewValidator : AbstractValidator<AddProductReviewCommand>
{ public AddProductReviewValidator() { RuleFor(x => x.ProductId).GreaterThan(0); RuleFor(x => x.Rating).InclusiveBetween(1, 5); RuleFor(x => x.Text).NotEmpty().MaximumLength(2000); } }
public sealed class ProductReviewsHandler(IAppDbContext db) : IRequestHandler<ProductReviewsQuery, IReadOnlyList<ProductReviewDto>>
{ public async Task<IReadOnlyList<ProductReviewDto>> Handle(ProductReviewsQuery request, CancellationToken ct) => await db.ProductReviews.AsNoTracking().Where(r => r.ProductId == request.ProductId).OrderByDescending(r => r.CreatedAtUtc).Select(r => new ProductReviewDto(r.Id, r.User!.FirstName + " " + r.User.LastName, r.Rating, r.Text, r.CreatedAtUtc)).ToListAsync(ct); }
public sealed class AddProductReviewHandler(IAppDbContext db, IAppCurrentUser user) : IRequestHandler<AddProductReviewCommand, ProductReviewDto>
{
    public async Task<ProductReviewDto> Handle(AddProductReviewCommand request, CancellationToken ct)
    {
        if (user.UserId is not int userId) throw new MarketConflictException("Prijava je obavezna.");
        if (!await db.Products.AnyAsync(p => p.Id == request.ProductId, ct)) throw new MarketNotFoundException("Proizvod nije pronađen.");
        if (await db.ProductReviews.AnyAsync(r => r.ProductId == request.ProductId && r.UserId == userId, ct)) throw new MarketConflictException("Već ste ocijenili ovaj proizvod.");
        var review = new ProductReviewEntity { ProductId = request.ProductId, UserId = userId, Rating = request.Rating, Text = request.Text.Trim() };
        db.ProductReviews.Add(review); await db.SaveChangesAsync(ct);
        return new(review.Id, user.Email ?? "Korisnik", review.Rating, review.Text, review.CreatedAtUtc);
    }
}
