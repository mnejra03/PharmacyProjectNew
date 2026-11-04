using Market.Domain.Common;

namespace Market.Domain.Entities.Identity;

public sealed class PasswordResetTokenEntity : BaseEntity
{
    public int UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }
}
