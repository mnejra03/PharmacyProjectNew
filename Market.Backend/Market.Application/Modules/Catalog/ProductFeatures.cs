namespace Market.Application.Modules.Catalog;

public sealed record ProductDto(int Id, string Name, string Description, decimal Price, decimal CurrentPrice, int QuantityInStock, string ImageUrl, int CategoryId, string CategoryName, int? BrandId, string? BrandName, bool IsDiscounted, decimal? DiscountPercentage, DateTime? ExpiryDate, double AverageRating, int ReviewCount);
public sealed record ProductListQuery(string? Search, int? CategoryId, int? BrandId, bool? Discounted, int Page = 1, int PageSize = 12) : IRequest<PageResult<ProductDto>>;
public sealed record ProductByIdQuery(int Id) : IRequest<ProductDto>;
public sealed record SaveProductCommand(int? Id, string Name, string Description, decimal Price, int QuantityInStock, string ImageUrl, int CategoryId, int? BrandId, bool IsDiscounted, decimal? DiscountPercentage, DateTime? ExpiryDate) : IRequest<ProductDto>;
public sealed record DeleteProductCommand(int Id) : IRequest;

public sealed class SaveProductValidator : AbstractValidator<SaveProductCommand>
{
    public SaveProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.QuantityInStock).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ImageUrl).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.CategoryId).GreaterThan(0);
        RuleFor(x => x.DiscountPercentage).InclusiveBetween(0.01m, 99.99m).When(x => x.IsDiscounted);
    }
}

public sealed class ProductListHandler(IAppDbContext db) : IRequestHandler<ProductListQuery, PageResult<ProductDto>>
{
    public async Task<PageResult<ProductDto>> Handle(ProductListQuery request, CancellationToken ct)
    {
        var query = db.Products.AsNoTracking().Include(p => p.Category).Include(p => p.Brand).Include(p => p.Reviews).AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.Search)) query = query.Where(p => p.Name.Contains(request.Search) || p.Description.Contains(request.Search));
        if (request.CategoryId.HasValue) query = query.Where(p => p.CategoryId == request.CategoryId.Value);
        if (request.BrandId.HasValue) query = query.Where(p => p.BrandId == request.BrandId.Value);
        if (request.Discounted == true) query = query.Where(p => p.IsDiscounted && p.DiscountPercentage != null);
        var page = new PageRequest { Page = Math.Max(1, request.Page), PageSize = Math.Clamp(request.PageSize, 1, 100) };
        var total = await query.CountAsync(ct);
        var products = await query.OrderBy(p => p.Name).Skip(page.SkipCount).Take(page.PageSize).ToListAsync(ct);
        return new PageResult<ProductDto> { Items = products.Select(ToDto).ToList(), PageSize = page.PageSize, CurrentPage = page.Page, IncludedTotal = true, TotalItems = total, TotalPages = (int)Math.Ceiling(total / (double)page.PageSize) };
    }

    internal static ProductDto ToDto(ProductEntity p) => new(p.Id, p.Name, p.Description, p.Price,
        p.IsDiscounted && p.DiscountPercentage.HasValue ? decimal.Round(p.Price * (1 - p.DiscountPercentage.Value / 100m), 2) : p.Price,
        p.QuantityInStock, p.ImageUrl, p.CategoryId, p.Category?.Name ?? "", p.BrandId, p.Brand?.Name,
        p.IsDiscounted, p.DiscountPercentage, p.ExpiryDate, p.Reviews.Count == 0 ? 0 : p.Reviews.Average(r => r.Rating), p.Reviews.Count);
}

public sealed class ProductByIdHandler(IAppDbContext db) : IRequestHandler<ProductByIdQuery, ProductDto>
{
    public async Task<ProductDto> Handle(ProductByIdQuery request, CancellationToken ct)
    {
        var product = await db.Products.AsNoTracking().Include(p => p.Category).Include(p => p.Brand).Include(p => p.Reviews).FirstOrDefaultAsync(p => p.Id == request.Id, ct)
            ?? throw new MarketNotFoundException("Proizvod nije pronađen.");
        return ProductListHandler.ToDto(product);
    }
}

public sealed class SaveProductHandler(IAppDbContext db, IAppCurrentUser user) : IRequestHandler<SaveProductCommand, ProductDto>
{
    public async Task<ProductDto> Handle(SaveProductCommand request, CancellationToken ct)
    {
        if (!user.IsAdmin && !user.IsPharmacist) throw new MarketConflictException("Samo administrator ili farmaceut može uređivati proizvode.");
        if (!await db.ProductCategories.AnyAsync(c => c.Id == request.CategoryId, ct)) throw new MarketNotFoundException("Kategorija nije pronađena.");
        var entity = request.Id.HasValue ? await db.Products.FirstOrDefaultAsync(p => p.Id == request.Id.Value, ct) : null;
        if (request.Id.HasValue && entity is null) throw new MarketNotFoundException("Proizvod nije pronađen.");
        entity ??= new ProductEntity();
        entity.Name = request.Name.Trim(); entity.Description = request.Description.Trim(); entity.Price = request.Price;
        entity.QuantityInStock = request.QuantityInStock; entity.ImageUrl = request.ImageUrl.Trim(); entity.CategoryId = request.CategoryId;
        entity.BrandId = request.BrandId; entity.IsDiscounted = request.IsDiscounted; entity.DiscountPercentage = request.IsDiscounted ? request.DiscountPercentage : null;
        entity.ExpiryDate = request.ExpiryDate; if (entity.AddedAtUtc == default) entity.AddedAtUtc = DateTime.UtcNow;
        if (!request.Id.HasValue) db.Products.Add(entity);
        await db.SaveChangesAsync(ct);
        return await new ProductByIdHandler(db).Handle(new(entity.Id), ct);
    }
}

public sealed class DeleteProductHandler(IAppDbContext db, IAppCurrentUser user) : IRequestHandler<DeleteProductCommand>
{
    public async Task Handle(DeleteProductCommand request, CancellationToken ct)
    {
        if (!user.IsAdmin && !user.IsPharmacist) throw new MarketConflictException("Samo administrator ili farmaceut može ukloniti proizvode.");
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == request.Id, ct) ?? throw new MarketNotFoundException("Proizvod nije pronađen.");
        db.Products.Remove(product); await db.SaveChangesAsync(ct);
    }
}

public sealed record CategoryDto(int Id, string Name);
public sealed record GetCategoriesQuery : IRequest<IReadOnlyList<CategoryDto>>;
public sealed class GetCategoriesHandler(IAppDbContext db) : IRequestHandler<GetCategoriesQuery, IReadOnlyList<CategoryDto>>
{ public async Task<IReadOnlyList<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken ct) => await db.ProductCategories.AsNoTracking().OrderBy(c => c.Name).Select(c => new CategoryDto(c.Id, c.Name)).ToListAsync(ct); }

public sealed record BrandDto(int Id, string Name, string? LogoUrl, string Description);
public sealed record GetBrandsQuery : IRequest<IReadOnlyList<BrandDto>>;
public sealed class GetBrandsHandler(IAppDbContext db) : IRequestHandler<GetBrandsQuery, IReadOnlyList<BrandDto>>
{ public async Task<IReadOnlyList<BrandDto>> Handle(GetBrandsQuery request, CancellationToken ct) => await db.Brands.AsNoTracking().OrderBy(b => b.Name).Select(b => new BrandDto(b.Id, b.Name, b.LogoUrl, b.Description)).ToListAsync(ct); }
