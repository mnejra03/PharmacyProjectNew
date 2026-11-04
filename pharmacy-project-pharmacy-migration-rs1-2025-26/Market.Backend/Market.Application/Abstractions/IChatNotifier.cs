namespace Market.Application.Abstractions;
public interface IChatNotifier
{
    Task SendMessageAsync(int userId, object message, CancellationToken ct);
    Task SendNotificationAsync(int userId, object notification, CancellationToken ct);
}
