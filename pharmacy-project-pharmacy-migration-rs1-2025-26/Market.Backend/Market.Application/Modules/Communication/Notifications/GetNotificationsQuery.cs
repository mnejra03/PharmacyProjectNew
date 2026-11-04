namespace Market.Application.Modules.Communication.Notifications;
public sealed record NotificationDto(int Id, string Title, string Message, DateTime CreatedAt, bool IsRead, string Type, int? SenderId);
public sealed record GetNotificationsQuery : IRequest<IReadOnlyList<NotificationDto>>;
public sealed class GetNotificationsHandler(IAppDbContext db, IAppCurrentUser user) : IRequestHandler<GetNotificationsQuery, IReadOnlyList<NotificationDto>>
{
    public async Task<IReadOnlyList<NotificationDto>> Handle(GetNotificationsQuery request, CancellationToken ct) =>
        await db.Notifications.Where(x => x.UserId == user.UserId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new NotificationDto(x.Id, x.Title, x.Message, x.CreatedAt, x.IsRead, x.Type, x.SenderId))
            .ToListAsync(ct);
}
