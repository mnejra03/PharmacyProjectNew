using Market.Domain.Common;
namespace Market.Domain.Entities.Communication;
public sealed class ChatMessageEntity : BaseEntity
{
    public int SenderId { get; set; }
    public int? ReceiverId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "question";
    public string Status { get; set; } = "new";
    public bool IsResponse { get; set; }
    public DateTime SentAtUtc { get; set; }
}
