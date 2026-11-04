namespace Market.Application.Modules.Content.Advertisements;
public sealed record AdvertisementDto(int Id, string Title, string ImageUrl);
public sealed record GetAdvertisementsQuery : IRequest<IReadOnlyList<AdvertisementDto>>;
public sealed class GetAdvertisementsHandler(IAppDbContext db) : IRequestHandler<GetAdvertisementsQuery, IReadOnlyList<AdvertisementDto>>
{ public async Task<IReadOnlyList<AdvertisementDto>> Handle(GetAdvertisementsQuery request, CancellationToken ct) => await db.Advertisements.AsNoTracking().OrderBy(x => x.Id).Select(x => new AdvertisementDto(x.Id, x.Title, x.ImageUrl)).ToListAsync(ct); }
public sealed record SaveAdvertisementCommand : IRequest<AdvertisementDto>
{ public int? Id { get; init; } public string Title { get; init; } = string.Empty; public string ImageUrl { get; init; } = string.Empty; }
public sealed class SaveAdvertisementValidator : AbstractValidator<SaveAdvertisementCommand>
{ public SaveAdvertisementValidator() { RuleFor(x => x.Title).NotEmpty().MaximumLength(200); RuleFor(x => x.ImageUrl).NotEmpty().MaximumLength(500).Must(x => Uri.TryCreate(x, UriKind.Absolute, out var u) && (u.Scheme == "http" || u.Scheme == "https")).WithMessage("Unesite važeći URL slike."); } }
public sealed class SaveAdvertisementHandler(IAppDbContext db, IAppCurrentUser user) : IRequestHandler<SaveAdvertisementCommand, AdvertisementDto>
{
    public async Task<AdvertisementDto> Handle(SaveAdvertisementCommand request, CancellationToken ct)
    {
        if (!user.IsAdmin) throw new MarketConflictException("Samo administrator može uređivati oglase.");
        var item = request.Id is int id ? await db.Advertisements.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new MarketNotFoundException("Oglas nije pronađen.") : new AdvertisementEntity();
        item.Title = request.Title.Trim(); item.ImageUrl = request.ImageUrl.Trim();
        if (request.Id is null) db.Advertisements.Add(item);
        await db.SaveChangesAsync(ct); return new(item.Id, item.Title, item.ImageUrl);
    }
}
public sealed record DeleteAdvertisementCommand(int Id) : IRequest;
public sealed class DeleteAdvertisementValidator : AbstractValidator<DeleteAdvertisementCommand> { public DeleteAdvertisementValidator() => RuleFor(x => x.Id).GreaterThan(0); }
public sealed class DeleteAdvertisementHandler(IAppDbContext db, IAppCurrentUser user) : IRequestHandler<DeleteAdvertisementCommand>
{
    public async Task Handle(DeleteAdvertisementCommand request, CancellationToken ct)
    {
        if (!user.IsAdmin) throw new MarketConflictException("Samo administrator može ukloniti oglase.");
        var item = await db.Advertisements.FirstOrDefaultAsync(x => x.Id == request.Id, ct) ?? throw new MarketNotFoundException("Oglas nije pronađen.");
        db.Advertisements.Remove(item); await db.SaveChangesAsync(ct);
    }
}
