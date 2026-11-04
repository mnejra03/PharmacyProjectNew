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

        var notification = new NotificationEntity
        {
            UserId = receiver.Id,
            SenderId = sender.Id,
            Title = user.IsCustomer ? "Nova poruka od kupca" : "Nova poruka od farmaceuta",
            Message = $"Nova poruka od {sender.FirstName} {sender.LastName}: {item.Message}",
            Type = "new_message",
            CreatedAt = sentAt
        };
        db.Notifications.Add(notification);
        await db.SaveChangesAsync(ct);

        var messageDto = new ChatMessageDto(item.Id, item.SenderId, item.ReceiverId, item.Message, item.Type, item.Status, item.IsResponse, item.SentAtUtc);
        await notifier.SendMessageAsync(receiver.Id, messageDto, ct);
        await notifier.SendNotificationAsync(receiver.Id,
            new NotificationDto(notification.Id, notification.Title, notification.Message, notification.CreatedAt, notification.IsRead, notification.Type, notification.SenderId), ct);
        return messageDto;
    }
}
