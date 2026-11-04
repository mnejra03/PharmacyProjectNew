namespace Market.Application.Modules.Communication.Chat;
using Market.Application.Modules.Communication.Notifications;
public sealed class SendChatMessageCommand : IRequest<ChatMessageDto>
{ public int ReceiverId { get; init; } public string Message { get; init; } = string.Empty; }
public sealed class SendChatMessageValidator : AbstractValidator<SendChatMessageCommand>
{ public SendChatMessageValidator() { RuleFor(x => x.Message).NotEmpty().MaximumLength(4000); RuleFor(x => x.ReceiverId).GreaterThan(0); } }
public sealed class SendChatMessageHandler(IAppDbContext db, IAppCurrentUser user, IChatNotifier notifier) : IRequestHandler<SendChatMessageCommand, ChatMessageDto>
{
    public async Task<ChatMessageDto> Handle(SendChatMessageCommand request, CancellationToken ct)
    {
        if (user.UserId is not int senderId || (!user.IsCustomer && !user.IsPharmacist)) throw new MarketConflictException("Samo kupci i farmaceuti mogu slati poruke.");
        var sender = await db.Users.FirstOrDefaultAsync(x => x.Id == senderId, ct) ?? throw new MarketNotFoundException("Korisnik nije pronađen.");
        var receiver = await db.Users.FirstOrDefaultAsync(x => x.Id == request.ReceiverId && x.IsEnabled, ct) ?? throw new MarketNotFoundException("Primaoc nije pronađen.");
        if ((user.IsCustomer && !receiver.IsPharmacist) || (user.IsPharmacist && !receiver.IsCustomer)) throw new MarketConflictException("Poruka mora biti poslana kupcu ili farmaceutu.");

        var sentAt = DateTime.UtcNow;
        var item = new ChatMessageEntity { SenderId = sender.Id, ReceiverId = receiver.Id, Message = request.Message.Trim(), Type = user.IsPharmacist ? "response" : "question", Status = "new", IsResponse = user.IsPharmacist, SentAtUtc = sentAt };
        db.ChatMessages.Add(item);

        var notifications = new List<NotificationEntity>();
        if (user.IsCustomer)
        {
            var pharmacists = await db.Users.Where(x => x.IsPharmacist && x.IsEnabled).ToListAsync(ct);
            foreach (var pharmacist in pharmacists)
            {
                var notification = new NotificationEntity { UserId = pharmacist.Id, SenderId = sender.Id, Title = "Nova poruka od korisnika", Message = $"Nova poruka od korisnika {sender.FirstName} {sender.LastName}: {item.Message}", Type = "new_message", CreatedAt = sentAt };
                db.Notifications.Add(notification); notifications.Add(notification);
            }
        }
        await db.SaveChangesAsync(ct);

        var messageDto = new ChatMessageDto(item.Id, item.SenderId, item.ReceiverId, item.Message, item.Type, item.Status, item.IsResponse, item.SentAtUtc);
        await notifier.SendMessageAsync(receiver.Id, messageDto, ct);
        foreach (var notification in notifications)
        {
            var dto = new NotificationDto(notification.Id, notification.Title, notification.Message, notification.CreatedAt, notification.IsRead, notification.Type, notification.SenderId);
            await notifier.SendNotificationAsync(notification.UserId, dto, ct);
        }
        return messageDto;
    }
}
