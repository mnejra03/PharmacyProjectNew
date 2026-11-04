using Market.Domain.Common;
namespace Market.Domain.Entities.Communication;
public sealed class NotificationEntity : BaseEntity
{
    public int UserId { get; set; }
    public int? SenderId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
}
