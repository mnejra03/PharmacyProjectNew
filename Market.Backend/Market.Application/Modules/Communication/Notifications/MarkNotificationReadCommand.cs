namespace Market.Application.Modules.Communication.Notifications;
public sealed record MarkNotificationReadCommand(int Id) : IRequest;
public sealed class MarkNotificationReadValidator : AbstractValidator<MarkNotificationReadCommand>
{ public MarkNotificationReadValidator() => RuleFor(x => x.Id).GreaterThan(0); }
public sealed class MarkNotificationReadHandler(IAppDbContext db, IAppCurrentUser user) : IRequestHandler<MarkNotificationReadCommand>
{
    public async Task Handle(MarkNotificationReadCommand request, CancellationToken ct)
    {
        var item = await db.Notifications.FirstOrDefaultAsync(x => x.Id == request.Id && x.UserId == user.UserId, ct)
            ?? throw new MarketNotFoundException("Obavijest nije pronađena.");
        item.IsRead = true;
        await db.SaveChangesAsync(ct);
    }
}
public sealed record DeleteNotificationCommand(int Id) : IRequest;
public sealed class DeleteNotificationValidator : AbstractValidator<DeleteNotificationCommand> { public DeleteNotificationValidator() => RuleFor(x => x.Id).GreaterThan(0); }
public sealed class DeleteNotificationHandler(IAppDbContext db, IAppCurrentUser user) : IRequestHandler<DeleteNotificationCommand>
{
    public async Task Handle(DeleteNotificationCommand request, CancellationToken ct)
    {
        var item = await db.Notifications.FirstOrDefaultAsync(x => x.Id == request.Id && x.UserId == user.UserId, ct) ?? throw new MarketNotFoundException("Obavijest nije pronađena.");
        db.Notifications.Remove(item); await db.SaveChangesAsync(ct);
    }
}
