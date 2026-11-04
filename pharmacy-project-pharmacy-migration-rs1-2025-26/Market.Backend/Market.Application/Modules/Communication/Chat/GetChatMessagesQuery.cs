namespace Market.Application.Modules.Communication.Chat;
public sealed record ChatMessageDto(int Id, int SenderId, int? ReceiverId, string Message, string Type, string Status, bool IsResponse, DateTime SentAtUtc);
public sealed record GetChatMessagesQuery(int? OtherUserId) : IRequest<IReadOnlyList<ChatMessageDto>>;
public sealed class GetChatMessagesHandler(IAppDbContext db, IAppCurrentUser user) : IRequestHandler<GetChatMessagesQuery, IReadOnlyList<ChatMessageDto>>
{
    public async Task<IReadOnlyList<ChatMessageDto>> Handle(GetChatMessagesQuery request, CancellationToken ct)
    {
        var q = db.ChatMessages.AsNoTracking().Where(x => x.SenderId == user.UserId || x.ReceiverId == user.UserId);
        if (request.OtherUserId is int other) q = q.Where(x => x.SenderId == other || x.ReceiverId == other);
        return await q.OrderBy(x => x.SentAtUtc).Select(x => new ChatMessageDto(x.Id, x.SenderId, x.ReceiverId, x.Message, x.Type, x.Status, x.IsResponse, x.SentAtUtc)).ToListAsync(ct);
    }
}
