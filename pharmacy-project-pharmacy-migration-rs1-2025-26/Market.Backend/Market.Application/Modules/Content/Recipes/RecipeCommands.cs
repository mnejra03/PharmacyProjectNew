namespace Market.Application.Modules.Content.Recipes;
public sealed record RecipeDto(int Id, int UserId, DateTime DateOfIssue, string DoctorFirstName, string DoctorLastName, string Status, bool HasScan);
public sealed class CreateRecipeCommand : IRequest<RecipeDto>
{ public string DoctorFirstName { get; init; } = string.Empty; public string DoctorLastName { get; init; } = string.Empty; public byte[]? Scan { get; init; } }
public sealed class CreateRecipeValidator : AbstractValidator<CreateRecipeCommand>
{ public CreateRecipeValidator() { RuleFor(x => x.DoctorFirstName).NotEmpty().MaximumLength(100); RuleFor(x => x.DoctorLastName).NotEmpty().MaximumLength(100); RuleFor(x => x.Scan).Must(x => x is null || x.Length <= 10 * 1024 * 1024).WithMessage("Sken može imati najviše 10 MB."); } }
public sealed class CreateRecipeHandler(IAppDbContext db, IAppCurrentUser user) : IRequestHandler<CreateRecipeCommand, RecipeDto>
{
    public async Task<RecipeDto> Handle(CreateRecipeCommand request, CancellationToken ct)
    {
        if (!user.IsAuthenticated || user.UserId is not int userId || !user.IsCustomer) throw new MarketConflictException("Samo prijavljeni kupci mogu poslati recept.");
        var recipe = new RecipeEntity { UserId = userId, DateOfIssue = DateTime.UtcNow, DoctorFirstName = request.DoctorFirstName.Trim(), DoctorLastName = request.DoctorLastName.Trim(), Scan = request.Scan, Status = "Pending" };
        db.Recipes.Add(recipe); await db.SaveChangesAsync(ct);
        return new(recipe.Id, recipe.UserId, recipe.DateOfIssue, recipe.DoctorFirstName, recipe.DoctorLastName, recipe.Status, recipe.Scan is not null);
    }
}
public sealed record GetRecipesQuery(bool Mine) : IRequest<IReadOnlyList<RecipeDto>>;
public sealed class GetRecipesHandler(IAppDbContext db, IAppCurrentUser user) : IRequestHandler<GetRecipesQuery, IReadOnlyList<RecipeDto>>
{
    public async Task<IReadOnlyList<RecipeDto>> Handle(GetRecipesQuery request, CancellationToken ct)
    {
        if (!user.IsAuthenticated || user.UserId is null) throw new MarketConflictException("Prijava je obavezna.");
        if (!request.Mine && !user.IsPharmacist && !user.IsAdmin) throw new MarketConflictException("Samo farmaceut može pregledati recepte.");
        var q = db.Recipes.AsNoTracking(); if (request.Mine) q = q.Where(x => x.UserId == user.UserId);
        return await q.OrderByDescending(x => x.DateOfIssue).Select(x => new RecipeDto(x.Id, x.UserId, x.DateOfIssue, x.DoctorFirstName, x.DoctorLastName, x.Status, x.Scan != null)).ToListAsync(ct);
    }
}
public sealed record UpdateRecipeStatusCommand(int Id, string Status) : IRequest;
public sealed class UpdateRecipeStatusValidator : AbstractValidator<UpdateRecipeStatusCommand>
{ public UpdateRecipeStatusValidator() { RuleFor(x => x.Id).GreaterThan(0); RuleFor(x => x.Status).Must(x => new[] { "Pending", "Approved", "Rejected" }.Contains(x)).WithMessage("Status mora biti Pending, Approved ili Rejected."); } }
public sealed class UpdateRecipeStatusHandler(IAppDbContext db, IAppCurrentUser user) : IRequestHandler<UpdateRecipeStatusCommand>
{
    public async Task Handle(UpdateRecipeStatusCommand request, CancellationToken ct)
    {
        if (!user.IsPharmacist && !user.IsAdmin) throw new MarketConflictException("Samo farmaceut može mijenjati status recepta.");
        var recipe = await db.Recipes.FirstOrDefaultAsync(x => x.Id == request.Id, ct) ?? throw new MarketNotFoundException("Recept nije pronađen.");
        recipe.Status = request.Status; await db.SaveChangesAsync(ct);
    }
}
public sealed record GetRecipeScanQuery(int Id) : IRequest<(byte[] Content, string ContentType)>;
public sealed class GetRecipeScanHandler(IAppDbContext db, IAppCurrentUser user) : IRequestHandler<GetRecipeScanQuery, (byte[] Content, string ContentType)>
{
    public async Task<(byte[] Content, string ContentType)> Handle(GetRecipeScanQuery request, CancellationToken ct)
    {
        var recipe = await db.Recipes.FirstOrDefaultAsync(x => x.Id == request.Id, ct) ?? throw new MarketNotFoundException("Recept nije pronađen.");
        if (user.UserId != recipe.UserId && !user.IsPharmacist && !user.IsAdmin) throw new MarketConflictException("Nemate pristup ovom skenu.");
        return (recipe.Scan ?? throw new MarketNotFoundException("Recept nema priložen sken."), "application/octet-stream");
    }
}
