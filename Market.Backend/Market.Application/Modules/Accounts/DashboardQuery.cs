namespace Market.Application.Modules.Accounts;
public sealed record DashboardStatsDto(int Users, int Customers, int Pharmacists, int Administrators, int Recipes, int PendingRecipes, int Notifications, int UnreadNotifications, int PharmacistUnreadNotifications);
public sealed record GetDashboardStatsQuery : IRequest<DashboardStatsDto>;
public sealed class GetDashboardStatsHandler(IAppDbContext db, IAppCurrentUser current) : IRequestHandler<GetDashboardStatsQuery, DashboardStatsDto>
{
    public async Task<DashboardStatsDto> Handle(GetDashboardStatsQuery request, CancellationToken ct)
    {
        if (!current.IsAdmin) throw new MarketConflictException("Samo administrator može vidjeti statistiku.");
        if (current.UserId is not int adminId) throw new MarketConflictException("Administrator ID is missing.");

        var unreadNotifications = await (
            from notification in db.Notifications.AsNoTracking()
            join stateRecord in db.AdminNotificationReadStates.IgnoreQueryFilters().AsNoTracking()
                .Where(x => x.AdminUserId == adminId)
                on notification.Id equals stateRecord.NotificationId into states
            from state in states.DefaultIfEmpty()
            where state == null
                ? !notification.IsRead
                : !state.IsDeleted && !state.IsRead
            select notification.Id
        ).CountAsync(ct);

        var pharmacistUnreadNotifications = await (
            from notification in db.Notifications.AsNoTracking()
            join recipient in db.Users.AsNoTracking() on notification.UserId equals recipient.Id
            where recipient.IsPharmacist && !notification.IsRead
            select notification.Id
        ).CountAsync(ct);

        return new(
            await db.Users.CountAsync(ct),
            await db.Users.CountAsync(x => x.IsCustomer, ct),
            await db.Users.CountAsync(x => x.IsPharmacist, ct),
            await db.Users.CountAsync(x => x.IsAdmin, ct),
            await db.Recipes.CountAsync(ct),
            await db.Recipes.CountAsync(x => x.Status == "Pending", ct),
            await db.Notifications.CountAsync(ct),
            unreadNotifications,
            pharmacistUnreadNotifications);
    }
}
