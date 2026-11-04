namespace Market.Application.Modules.Catalog;

public sealed record RestockProductCommand(int ProductId, int Quantity) : IRequest<int>;

public sealed class RestockProductValidator : AbstractValidator<RestockProductCommand>
{
    public RestockProductValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.Quantity).InclusiveBetween(1, 100000);
    }
}

public sealed class RestockProductHandler(IAppDbContext db, IAppCurrentUser user) : IRequestHandler<RestockProductCommand, int>
{
    public async Task<int> Handle(RestockProductCommand request, CancellationToken ct)
    {
        if (!user.IsAdmin && !user.IsPharmacist) throw new MarketConflictException("Samo administrator ili farmaceut može zaprimiti zalihu.");
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId, ct)
            ?? throw new MarketNotFoundException("Proizvod nije pronađen.");
        if (product.QuantityInStock > int.MaxValue - request.Quantity) throw new MarketConflictException("Količina prelazi dozvoljeni maksimum.");
        product.QuantityInStock += request.Quantity;
        await db.SaveChangesAsync(ct);
        return product.QuantityInStock;
    }
}
