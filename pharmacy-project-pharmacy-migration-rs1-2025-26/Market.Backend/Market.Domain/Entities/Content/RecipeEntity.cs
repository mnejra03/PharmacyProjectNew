using Market.Domain.Common;
namespace Market.Domain.Entities.Content;
public sealed class RecipeEntity : BaseEntity
{
    public int UserId { get; set; }
    public DateTime DateOfIssue { get; set; }
    public string DoctorFirstName { get; set; } = string.Empty;
    public string DoctorLastName { get; set; } = string.Empty;
    public string? ScanUrl { get; set; }
    public byte[]? Scan { get; set; }
    public string Status { get; set; } = "Pending";
}
