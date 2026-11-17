using Market.Domain.Common;

namespace Market.Domain.Entities.Communication;

public sealed class AdminNotificationReadStateEntity : BaseEntity
{
    public int AdminUserId { get; set; }
    public int NotificationId { get; set; }
    public bool IsRead { get; set; }
}
