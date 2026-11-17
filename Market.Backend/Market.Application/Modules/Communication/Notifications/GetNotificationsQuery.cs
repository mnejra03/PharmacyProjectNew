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

public sealed record AdminNotificationDto(int Id, string Title, string Message, DateTime CreatedAt, bool IsRead, bool RecipientIsRead, string Type, int RecipientId, string RecipientName, string RecipientEmail, int? SenderId, string? SenderName, string? SenderEmail);
public sealed record GetAllNotificationsForAdminQuery : IRequest<IReadOnlyList<AdminNotificationDto>>;
public sealed class GetAllNotificationsForAdminHandler(IAppDbContext db, IAppCurrentUser user) : IRequestHandler<GetAllNotificationsForAdminQuery, IReadOnlyList<AdminNotificationDto>>
{
    public async Task<IReadOnlyList<AdminNotificationDto>> Handle(GetAllNotificationsForAdminQuery request, CancellationToken ct)
    {
        if (!user.IsAdmin || user.UserId is not int adminId) throw new MarketConflictException("Samo administrator može pregledati sve obavijesti.");

        var query =
            from notification in db.Notifications.AsNoTracking()
            join recipient in db.Users.AsNoTracking() on notification.UserId equals recipient.Id
            join senderRecord in db.Users.AsNoTracking() on notification.SenderId equals (int?)senderRecord.Id into senders
            from sender in senders.DefaultIfEmpty()
            join stateRecord in db.AdminNotificationReadStates.AsNoTracking().Where(x => x.AdminUserId == adminId && !x.IsDeleted) on notification.Id equals stateRecord.NotificationId into states
            from state in states.DefaultIfEmpty()
            orderby notification.CreatedAt descending
            select new AdminNotificationDto(
                notification.Id, notification.Title, notification.Message, notification.CreatedAt, state == null ? notification.IsRead : state.IsRead, notification.IsRead, notification.Type,
                recipient.Id, recipient.FirstName + " " + recipient.LastName, recipient.Email,
                sender == null ? null : sender.Id, sender == null ? null : sender.FirstName + " " + sender.LastName, sender == null ? null : sender.Email);

        return await query.ToListAsync(ct);
    }
}

public sealed record SetAdminNotificationReadStateCommand(int NotificationId, bool IsRead) : IRequest;
public sealed class SetAdminNotificationReadStateValidator : AbstractValidator<SetAdminNotificationReadStateCommand>
{
    public SetAdminNotificationReadStateValidator() => RuleFor(x => x.NotificationId).GreaterThan(0);
}

public sealed class SetAdminNotificationReadStateHandler(IAppDbContext db, IAppCurrentUser user) : IRequestHandler<SetAdminNotificationReadStateCommand>
{
    public async Task Handle(SetAdminNotificationReadStateCommand request, CancellationToken ct)
    {
        if (!user.IsAdmin || user.UserId is not int adminId) throw new MarketConflictException("Samo administrator može mijenjati svoj status obavijesti.");
        if (!await db.Notifications.AnyAsync(x => x.Id == request.NotificationId, ct)) throw new MarketNotFoundException("Obavijest nije pronađena.");

        var state = await db.AdminNotificationReadStates.FirstOrDefaultAsync(x => x.AdminUserId == adminId && x.NotificationId == request.NotificationId, ct);
        if (state is null)
        {
            db.AdminNotificationReadStates.Add(new AdminNotificationReadStateEntity { AdminUserId = adminId, NotificationId = request.NotificationId, IsRead = request.IsRead });
        }
        else
        {
            state.IsRead = request.IsRead;
            state.ModifiedAtUtc = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
    }
}
