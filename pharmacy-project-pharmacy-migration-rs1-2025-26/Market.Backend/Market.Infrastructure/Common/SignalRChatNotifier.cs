using Microsoft.AspNetCore.SignalR;
namespace Market.Infrastructure.Common;
public sealed class SignalRChatNotifier(IHubContext<ChatHub> hub) : IChatNotifier
{
    public Task SendMessageAsync(int userId, object message, CancellationToken ct) => hub.Clients.Group(userId.ToString()).SendAsync("ReceiveMessage", message, ct);
    public Task SendNotificationAsync(int userId, object notification, CancellationToken ct) => hub.Clients.Group(userId.ToString()).SendAsync("ReceiveNotification", notification, ct);
}
