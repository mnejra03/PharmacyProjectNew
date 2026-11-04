using Market.Domain.Entities.Commerce;
using Market.Domain.Entities.Communication;

namespace Market.Application.Modules.Catalog;

public sealed record CreateSupplyOrderCommand(int ProductId, int Quantity) : IRequest<int>;
public sealed record GetSupplyOrdersQuery(bool All = false) : IRequest<IReadOnlyList<SupplyOrderDto>>;
public sealed record UpdateSupplyOrderStatusCommand(int Id, string Status) : IRequest;
public sealed record SupplyOrderDto(int Id, DateTime OrderedAtUtc, string Status, decimal TotalPrice, string PharmacistName, string PharmacistEmail, IReadOnlyList<OrderItemDto> Items);

public sealed class CreateSupplyOrderValidator : AbstractValidator<CreateSupplyOrderCommand>
{
    public CreateSupplyOrderValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.Quantity).InclusiveBetween(1, 100000);
    }
}

public sealed class UpdateSupplyOrderStatusValidator : AbstractValidator<UpdateSupplyOrderStatusCommand>
{
    public UpdateSupplyOrderStatusValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Status).Must(x => x is "Approved" or "Rejected")
            .WithMessage("Status nabavke mora biti Approved ili Rejected.");
    }
}

public sealed class CreateSupplyOrderHandler(IAppDbContext db, IAppCurrentUser user) : IRequestHandler<CreateSupplyOrderCommand, int>
{
    public async Task<int> Handle(CreateSupplyOrderCommand request, CancellationToken ct)
    {
        if (!user.IsPharmacist || user.UserId is not int pharmacistId)
            throw new MarketConflictException("Samo prijavljeni farmaceut može zatražiti nabavku.");

        var product = await db.Products.FirstOrDefaultAsync(x => x.Id == request.ProductId, ct)
            ?? throw new MarketNotFoundException("Proizvod nije pronađen.");

        var order = new OrderEntity
        {
            UserId = pharmacistId,
            OrderedAtUtc = DateTime.UtcNow,
            Status = "Pending",
            PaymentMethod = "Internal Transfer",
            ShippingAddress = "Apoteka Sarajevo",
            TotalPrice = product.Price * request.Quantity,
            IsSupplyOrder = true,
            Items =
            {
                new OrderItemEntity
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Quantity = request.Quantity,
                    UnitPrice = product.Price
                }
            }
        };

        db.Orders.Add(order);
        db.Notifications.Add(new NotificationEntity
        {
            UserId = pharmacistId,
            Title = "Zahtjev za nabavku je poslan",
            Message = $"Zahtjev za {request.Quantity} kom. proizvoda {product.Name} čeka odobrenje administratora.",
            Type = "supply_order_status",
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);
        return order.Id;
    }
}

public sealed class GetSupplyOrdersHandler(IAppDbContext db, IAppCurrentUser user)
    : IRequestHandler<GetSupplyOrdersQuery, IReadOnlyList<SupplyOrderDto>>
{
    public async Task<IReadOnlyList<SupplyOrderDto>> Handle(GetSupplyOrdersQuery request, CancellationToken ct)
    {
        if (user.UserId is not int userId)
            throw new MarketConflictException("Prijava je obavezna.");
        if (request.All && !user.IsAdmin)
            throw new MarketConflictException("Samo administrator može pregledati sve zahtjeve za nabavku.");
        if (!request.All && !user.IsPharmacist && !user.IsAdmin)
            throw new MarketConflictException("Nemate pravo pregledati zahtjeve za nabavku.");

        var query = db.Orders.AsNoTracking().Where(x => x.IsSupplyOrder)
            .Include(x => x.User).Include(x => x.Items).AsQueryable();
        if (!request.All && !user.IsAdmin)
            query = query.Where(x => x.UserId == userId);

        var orders = await query.OrderByDescending(x => x.OrderedAtUtc).ToListAsync(ct);
        return orders.Select(x => new SupplyOrderDto(
            x.Id, x.OrderedAtUtc, x.Status, x.TotalPrice,
            $"{x.User?.FirstName} {x.User?.LastName}".Trim(), x.User?.Email ?? string.Empty,
            x.Items.Select(i => new OrderItemDto(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice)).ToList()))
            .ToList();
    }
}

public sealed class UpdateSupplyOrderStatusHandler(IAppDbContext db, IAppCurrentUser user)
    : IRequestHandler<UpdateSupplyOrderStatusCommand>
{
    public async Task Handle(UpdateSupplyOrderStatusCommand request, CancellationToken ct)
    {
        if (!user.IsAdmin)
            throw new MarketConflictException("Samo administrator može odobriti ili odbiti nabavku.");

        var order = await db.Orders.Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.IsSupplyOrder, ct)
            ?? throw new MarketNotFoundException("Zahtjev za nabavku nije pronađen.");
        if (order.Status != "Pending")
            throw new MarketConflictException("Zahtjev je već obrađen i status se ne može mijenjati.");

        if (request.Status == "Approved")
        {
            foreach (var item in order.Items)
            {
                var product = await db.Products.FirstOrDefaultAsync(x => x.Id == item.ProductId, ct)
                    ?? throw new MarketNotFoundException($"Proizvod '{item.ProductName}' više ne postoji.");
                product.QuantityInStock = checked(product.QuantityInStock + item.Quantity);
            }
        }

        order.Status = request.Status;
        db.Notifications.Add(new NotificationEntity
        {
            UserId = order.UserId,
            Title = "Status zahtjeva za nabavku je promijenjen",
            Message = request.Status == "Approved"
                ? $"Zahtjev #{order.Id} je odobren. Stanje zaliha je ažurirano."
                : $"Zahtjev #{order.Id} je odbijen.",
            Type = "supply_order_status",
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);
    }
}
