using Market.Application.Abstractions;
using Market.Application.Common.Exceptions;
using Market.Application.Modules.Catalog;
using Market.Domain.Entities.Catalog;
using Market.Domain.Entities.Commerce;
using Market.Domain.Entities.Identity;
using Market.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Market.Tests;

public sealed class CommerceBusinessRuleTests
{
    [Fact]
    public void Save_product_validator_rejects_discount_without_percentage()
    {
        var command = new SaveProductCommand(
            Id: null,
            Name: "Vitamin C",
            Description: "Vitamin C 500 mg",
            Price: 12m,
            QuantityInStock: 10,
            ImageUrl: "/images/vitamin-c.jpg",
            CategoryId: 1,
            BrandId: null,
            IsDiscounted: true,
            DiscountPercentage: null,
            ExpiryDate: null);

        var result = new SaveProductValidator().Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(command.DiscountPercentage));
    }

    [Fact]
    public async Task Checkout_rejects_card_payment_reference_owned_by_another_customer()
    {
        var options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new DatabaseContext(options, TimeProvider.System);
        var product = new ProductEntity { Id = 15, Name = "Vitamin C", Price = 10m, QuantityInStock = 5, CategoryId = 1 };
        var cart = new CartEntity
        {
            Id = 20,
            UserId = 7,
            Items = new List<CartItemEntity>
            {
                new() { Id = 21, CartId = 20, ProductId = product.Id, Product = product, Quantity = 1 }
            }
        };
        db.Users.Add(new MarketUserEntity { Id = 7, Email = "customer@example.com", FirstName = "Test", LastName = "Customer" });
        db.Carts.Add(cart);
        db.Orders.Add(new OrderEntity
        {
            Id = 30,
            UserId = 99,
            PaymentMethod = "Card",
            PaymentReference = "pi_already_used",
            ShippingAddress = "Ulica 1",
            TotalPrice = 10m
        });
        await db.SaveChangesAsync(CancellationToken.None);

        var stripe = new SuccessfulStripeGateway("pi_already_used", "7", 1000);
        var handler = new CheckoutHandler(db, new TestCurrentUser(7), stripe, new NoOpChatNotifier());

        await Assert.ThrowsAsync<MarketConflictException>(() => handler.Handle(
            new CheckoutCommand("Ulica 2", "Card", "pi_already_used"), CancellationToken.None));

        Assert.Equal(1, await db.Orders.CountAsync());
        Assert.False(cart.IsCompleted);
        Assert.Equal(5, product.QuantityInStock);
        Assert.False(stripe.PaymentWasRetrieved);
    }

    [Fact]
    public async Task Checkout_retry_returns_original_order_without_recharging_or_decrementing_stock()
    {
        var options = new DbContextOptionsBuilder<DatabaseContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new DatabaseContext(options, TimeProvider.System);
        var customer = new MarketUserEntity { Id = 7, Email = "customer@example.com", FirstName = "Test", LastName = "Customer" };
        var product = new ProductEntity { Id = 15, Name = "Vitamin C", Price = 10m, QuantityInStock = 4, CategoryId = 1 };
        db.Users.Add(customer);
        db.Orders.Add(new OrderEntity { Id = 30, UserId = 7, User = customer, PaymentMethod = "Card", PaymentReference = "pi_succeeded", ShippingAddress = "Ulica 1", TotalPrice = 10m, Status = "Pending", Items = new List<OrderItemEntity> { new() { ProductId = 15, ProductName = "Vitamin C", Quantity = 1, UnitPrice = 10m } } });
        db.Carts.Add(new CartEntity { Id = 20, UserId = 7, Items = new List<CartItemEntity> { new() { Id = 21, CartId = 20, ProductId = 15, Product = product, Quantity = 1 } } });
        await db.SaveChangesAsync(CancellationToken.None);
        var stripe = new SuccessfulStripeGateway("pi_succeeded", "7", 1000);
        var handler = new CheckoutHandler(db, new TestCurrentUser(7), stripe, new NoOpChatNotifier());

        var result = await handler.Handle(new CheckoutCommand("Ulica 1", "Card", "pi_succeeded"), CancellationToken.None);

        Assert.Equal(30, result.Id);
        Assert.Equal(1, await db.Orders.CountAsync());
        Assert.Equal(4, product.QuantityInStock);
        Assert.False(stripe.PaymentWasRetrieved);
    }

    [Fact]
    public async Task Card_checkout_attaches_reserved_payment_to_created_order()
    {
        var options = new DbContextOptionsBuilder<DatabaseContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new DatabaseContext(options, TimeProvider.System);
        var product = new ProductEntity { Id = 15, Name = "Vitamin C", Price = 10m, QuantityInStock = 5, CategoryId = 1 };
        var cart = new CartEntity { Id = 20, UserId = 7, Items = new List<CartItemEntity> { new() { Id = 21, CartId = 20, ProductId = 15, Product = product, Quantity = 1 } } };
        db.Users.Add(new MarketUserEntity { Id = 7, Email = "customer@example.com", FirstName = "Test", LastName = "Customer" });
        db.Carts.Add(cart);
        await db.SaveChangesAsync(CancellationToken.None);
        var stripe = new SuccessfulStripeGateway("pi_reserved", "7", 1000);
        var user = new TestCurrentUser(7);
        await new CreateCartPaymentIntentHandler(db, user, stripe).Handle(new CreateCartPaymentIntentCommand(), CancellationToken.None);

        var order = await new CheckoutHandler(db, user, stripe, new NoOpChatNotifier()).Handle(new CheckoutCommand("Ulica 1", "Card", "pi_reserved"), CancellationToken.None);

        var reservation = await db.PaymentIntentReservations.SingleAsync();
        Assert.Equal(order.Id, reservation.OrderId);
        Assert.Equal(20, reservation.CartId);
        Assert.Equal("pi_reserved", (await db.Orders.SingleAsync()).PaymentReference);
    }

    [Fact]
    public async Task Save_product_handler_rejects_unknown_brand_without_persisting_product()
    {
        var options = new DbContextOptionsBuilder<DatabaseContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new DatabaseContext(options, TimeProvider.System);
        db.ProductCategories.Add(new ProductCategoryEntity { Id = 1, Name = "Vitamini" });
        await db.SaveChangesAsync(CancellationToken.None);
        var handler = new SaveProductHandler(db, new TestCurrentUser(7, isAdmin: true));
        var command = new SaveProductCommand(null, "Vitamin C", "Opis", 10m, 4, "/vitamin.jpg", 1, 999, false, null, null);

        await Assert.ThrowsAsync<MarketNotFoundException>(() => handler.Handle(command, CancellationToken.None));

        Assert.Empty(await db.Products.ToListAsync());
    }

    [Fact]
    public async Task Save_product_handler_rejects_discount_without_percentage_and_keeps_existing_price()
    {
        var options = new DbContextOptionsBuilder<DatabaseContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new DatabaseContext(options, TimeProvider.System);
        db.ProductCategories.Add(new ProductCategoryEntity { Id = 1, Name = "Vitamini" });
        var product = new ProductEntity { Id = 15, Name = "Vitamin C", Description = "Opis", Price = 10m, QuantityInStock = 4, ImageUrl = "/vitamin.jpg", CategoryId = 1, IsDiscounted = false };
        db.Products.Add(product);
        await db.SaveChangesAsync(CancellationToken.None);
        var handler = new SaveProductHandler(db, new TestCurrentUser(7, isAdmin: true));
        var command = new SaveProductCommand(15, "Vitamin C", "Izmijenjen opis", 12m, 4, "/vitamin.jpg", 1, null, true, null, null);

        await Assert.ThrowsAsync<MarketConflictException>(() => handler.Handle(command, CancellationToken.None));

        Assert.Equal(10m, (await db.Products.SingleAsync()).Price);
        Assert.False(product.IsDiscounted);
    }

    [Fact]
    public async Task Save_product_handler_persists_discount_and_returns_reduced_current_price()
    {
        var options = new DbContextOptionsBuilder<DatabaseContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new DatabaseContext(options, TimeProvider.System);
        db.ProductCategories.Add(new ProductCategoryEntity { Id = 1, Name = "Vitamini" });
        await db.SaveChangesAsync(CancellationToken.None);
        var handler = new SaveProductHandler(db, new TestCurrentUser(7, isAdmin: true));

        var result = await handler.Handle(new SaveProductCommand(null, "Vitamin C", "Opis", 10m, 4, "/vitamin.jpg", 1, null, true, 20m, null), CancellationToken.None);

        Assert.Equal(8m, result.CurrentPrice);
        Assert.True(result.IsDiscounted);
        Assert.Equal(20m, result.DiscountPercentage);
    }

    [Fact]
    public async Task Checkout_rejects_expired_product_without_changing_stock_or_cart()
    {
        var options = new DbContextOptionsBuilder<DatabaseContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new DatabaseContext(options, TimeProvider.System);
        var product = new ProductEntity { Id = 15, Name = "Istekli vitamin", Price = 10m, QuantityInStock = 5, CategoryId = 1, ExpiryDate = DateTime.UtcNow.Date.AddDays(-1) };
        var cart = new CartEntity { Id = 20, UserId = 7, Items = new List<CartItemEntity> { new() { Id = 21, CartId = 20, ProductId = 15, Product = product, Quantity = 1 } } };
        db.Users.Add(new MarketUserEntity { Id = 7, Email = "customer@example.com", FirstName = "Test", LastName = "Customer" });
        db.Carts.Add(cart);
        await db.SaveChangesAsync(CancellationToken.None);
        var handler = new CheckoutHandler(db, new TestCurrentUser(7), new SuccessfulStripeGateway("pi", "7", 1000), new NoOpChatNotifier());

        var exception = await Assert.ThrowsAsync<MarketConflictException>(() => handler.Handle(new CheckoutCommand("Ulica 1", "CashOnDelivery"), CancellationToken.None));

        Assert.Contains("Istekli vitamin", exception.Message);
        Assert.Empty(await db.Orders.ToListAsync());
        Assert.False(cart.IsCompleted);
        Assert.Equal(5, product.QuantityInStock);
    }

    [Fact]
    public async Task Payment_intent_rejects_expired_product_before_calling_gateway()
    {
        var options = new DbContextOptionsBuilder<DatabaseContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new DatabaseContext(options, TimeProvider.System);
        var product = new ProductEntity { Id = 15, Name = "Istekli vitamin", Price = 10m, QuantityInStock = 5, CategoryId = 1, ExpiryDate = DateTime.UtcNow.Date.AddDays(-1) };
        db.Carts.Add(new CartEntity { Id = 20, UserId = 7, Items = new List<CartItemEntity> { new() { Id = 21, CartId = 20, ProductId = 15, Product = product, Quantity = 1 } } });
        await db.SaveChangesAsync(CancellationToken.None);
        var stripe = new SuccessfulStripeGateway("pi", "7", 1000);
        var handler = new CreateCartPaymentIntentHandler(db, new TestCurrentUser(7), stripe);

        await Assert.ThrowsAsync<MarketConflictException>(() => handler.Handle(new CreateCartPaymentIntentCommand(), CancellationToken.None));

        Assert.False(stripe.IntentWasCreated);
    }

    [Fact]
    public async Task Checkout_returns_saved_order_even_when_realtime_notification_delivery_fails()
    {
        var options = new DbContextOptionsBuilder<DatabaseContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new DatabaseContext(options, TimeProvider.System);
        var product = new ProductEntity { Id = 15, Name = "Vitamin C", Price = 10m, QuantityInStock = 5, CategoryId = 1 };
        var cart = new CartEntity { Id = 20, UserId = 7, Items = new List<CartItemEntity> { new() { Id = 21, CartId = 20, ProductId = 15, Product = product, Quantity = 1 } } };
        db.Users.Add(new MarketUserEntity { Id = 7, Email = "customer@example.com", FirstName = "Test", LastName = "Customer" });
        db.Carts.Add(cart);
        await db.SaveChangesAsync(CancellationToken.None);
        var handler = new CheckoutHandler(db, new TestCurrentUser(7), new SuccessfulStripeGateway("pi", "7", 1000), new ThrowingChatNotifier());

        var result = await handler.Handle(new CheckoutCommand("Ulica 1", "CashOnDelivery"), CancellationToken.None);

        Assert.True(result.Id > 0);
        var notice = await db.Notifications.SingleAsync();
        Assert.Contains($"#{result.Id}", notice.Message);
        Assert.Equal(1, await db.Orders.CountAsync());
        Assert.Equal(4, product.QuantityInStock);
    }

    [Fact]
    public async Task Order_cancellation_restores_inventory_once_and_repeated_status_is_noop()
    {
        var options = new DbContextOptionsBuilder<DatabaseContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new DatabaseContext(options, TimeProvider.System);
        var product = new ProductEntity { Id = 15, Name = "Vitamin C", Price = 10m, QuantityInStock = 4, CategoryId = 1 };
        var order = new OrderEntity { Id = 30, UserId = 7, Status = "Pending", PaymentMethod = "CashOnDelivery", ShippingAddress = "Ulica 1", Items = new List<OrderItemEntity> { new() { Id = 31, OrderId = 30, ProductId = 15, Product = product, ProductName = product.Name, Quantity = 2, UnitPrice = 10m } } };
        db.Users.Add(new MarketUserEntity { Id = 7, Email = "customer@example.com", FirstName = "Test", LastName = "Customer" });
        db.Products.Add(product);
        db.Orders.Add(order);
        await db.SaveChangesAsync(CancellationToken.None);
        var handler = new UpdateOrderStatusHandler(db, new TestCurrentUser(8, isAdmin: true), new NoOpChatNotifier());

        await handler.Handle(new UpdateOrderStatusCommand(30, "Cancelled"), CancellationToken.None);
        await handler.Handle(new UpdateOrderStatusCommand(30, "Cancelled"), CancellationToken.None);

        Assert.Equal("Cancelled", order.Status);
        Assert.Equal(6, product.QuantityInStock);
        Assert.Equal(1, await db.Notifications.CountAsync());
    }

    [Fact]
    public async Task Order_status_rejects_invalid_transition_without_changing_order()
    {
        var options = new DbContextOptionsBuilder<DatabaseContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new DatabaseContext(options, TimeProvider.System);
        var order = new OrderEntity { Id = 30, UserId = 7, Status = "Pending", PaymentMethod = "CashOnDelivery", ShippingAddress = "Ulica 1" };
        db.Orders.Add(order);
        await db.SaveChangesAsync(CancellationToken.None);
        var handler = new UpdateOrderStatusHandler(db, new TestCurrentUser(8, isAdmin: true), new NoOpChatNotifier());

        await Assert.ThrowsAsync<MarketConflictException>(() => handler.Handle(new UpdateOrderStatusCommand(30, "Completed"), CancellationToken.None));

        Assert.Equal("Pending", order.Status);
        Assert.Empty(await db.Notifications.ToListAsync());
    }

    private sealed class TestCurrentUser(int userId, bool isAdmin = false, bool isPharmacist = false) : IAppCurrentUser
    {
        public int? UserId => userId;
        public string? Email => "customer@example.com";
        public bool IsAuthenticated => true;
        public bool IsAdmin => isAdmin;
        public bool IsPharmacist => isPharmacist;
        public bool IsCustomer => true;
    }

    private sealed class SuccessfulStripeGateway(string intentId, string userId, long amountMinor) : IStripePaymentGateway
    {
        public bool PaymentWasRetrieved { get; private set; }
        public bool IntentWasCreated { get; private set; }
        public bool IsConfigured => true;
        public string PublishableKey => "pk_test";

        public Task<PaymentIntentResult> CreateIntentAsync(long amount, string currency, string user, CancellationToken ct)
        {
            IntentWasCreated = true;
            return Task.FromResult(new PaymentIntentResult(intentId, "secret", amount, currency, "succeeded", user));
        }

        public Task<PaymentIntentResult?> GetIntentAsync(string paymentIntentId, CancellationToken ct)
        {
            PaymentWasRetrieved = true;
            return Task.FromResult<PaymentIntentResult?>(new PaymentIntentResult(intentId, "secret", amountMinor, "bam", "succeeded", userId));
        }
    }

    private sealed class NoOpChatNotifier : IChatNotifier
    {
        public Task SendMessageAsync(int userId, object message, CancellationToken ct) => Task.CompletedTask;
        public Task SendNotificationAsync(int userId, object notification, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class ThrowingChatNotifier : IChatNotifier
    {
        public Task SendMessageAsync(int userId, object message, CancellationToken ct) => throw new InvalidOperationException("Realtime is unavailable.");
        public Task SendNotificationAsync(int userId, object notification, CancellationToken ct) => throw new InvalidOperationException("Realtime is unavailable.");
    }
}
