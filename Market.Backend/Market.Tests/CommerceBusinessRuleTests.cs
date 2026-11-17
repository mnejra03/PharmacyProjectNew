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
    public async Task Checkout_rejects_card_payment_reference_already_used_by_another_order()
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
            UserId = 7,
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

    private sealed class TestCurrentUser(int userId) : IAppCurrentUser
    {
        public int? UserId => userId;
        public string? Email => "customer@example.com";
        public bool IsAuthenticated => true;
        public bool IsAdmin => false;
        public bool IsPharmacist => false;
        public bool IsCustomer => true;
    }

    private sealed class SuccessfulStripeGateway(string intentId, string userId, long amountMinor) : IStripePaymentGateway
    {
        public bool PaymentWasRetrieved { get; private set; }
        public bool IsConfigured => true;
        public string PublishableKey => "pk_test";

        public Task<PaymentIntentResult> CreateIntentAsync(long amount, string currency, string user, CancellationToken ct) =>
            Task.FromResult(new PaymentIntentResult(intentId, "secret", amount, currency, "succeeded", user));

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
}
