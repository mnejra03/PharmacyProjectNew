using Market.Domain.Common;
namespace Market.Domain.Entities.Content;
public sealed class AdvertisementEntity : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
}
