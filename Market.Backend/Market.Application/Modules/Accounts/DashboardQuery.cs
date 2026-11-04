namespace Market.Application.Modules.Accounts;
public sealed record DashboardStatsDto(int Users, int Pharmacists, int Recipes, int PendingRecipes, int UnreadNotifications);
public sealed record GetDashboardStatsQuery : IRequest<DashboardStatsDto>;
public sealed class GetDashboardStatsHandler(IAppDbContext db, IAppCurrentUser current) : IRequestHandler<GetDashboardStatsQuery, DashboardStatsDto>
{
    public async Task<DashboardStatsDto> Handle(GetDashboardStatsQuery request, CancellationToken ct)
    {
        if (!current.IsAdmin) throw new MarketConflictException("Samo administrator može vidjeti statistiku.");
        return new(await db.Users.CountAsync(ct), await db.Users.CountAsync(x => x.IsPharmacist, ct), await db.Recipes.CountAsync(ct), await db.Recipes.CountAsync(x => x.Status == "Pending", ct), await db.Notifications.CountAsync(x => !x.IsRead, ct));
    }
}
