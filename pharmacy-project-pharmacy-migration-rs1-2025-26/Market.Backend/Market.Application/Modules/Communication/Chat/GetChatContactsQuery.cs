namespace Market.Application.Modules.Communication.Chat;
public sealed record ChatContactDto(int Id, string FirstName, string LastName, string Email);
public sealed record GetChatContactsQuery : IRequest<IReadOnlyList<ChatContactDto>>;
public sealed class GetChatContactsHandler(IAppDbContext db, IAppCurrentUser current) : IRequestHandler<GetChatContactsQuery, IReadOnlyList<ChatContactDto>>
{
    public async Task<IReadOnlyList<ChatContactDto>> Handle(GetChatContactsQuery request, CancellationToken ct)
    {
        if (current.UserId is not int id) throw new MarketConflictException("Prijava je obavezna.");
        if (current.IsCustomer)
            return await db.Users.AsNoTracking().Where(x => x.IsPharmacist && x.IsEnabled).OrderBy(x => x.FirstName).Select(x => new ChatContactDto(x.Id, x.FirstName, x.LastName, x.Email)).ToListAsync(ct);
        if (!current.IsPharmacist && !current.IsAdmin) throw new MarketConflictException("Ova uloga ne može koristiti chat.");
        var customerIds = await db.ChatMessages.Where(x => x.ReceiverId == id && !x.IsResponse).Select(x => x.SenderId).Distinct().ToListAsync(ct);
        return await db.Users.AsNoTracking().Where(x => customerIds.Contains(x.Id) && x.IsCustomer).OrderBy(x => x.FirstName).Select(x => new ChatContactDto(x.Id, x.FirstName, x.LastName, x.Email)).ToListAsync(ct);
    }
}
