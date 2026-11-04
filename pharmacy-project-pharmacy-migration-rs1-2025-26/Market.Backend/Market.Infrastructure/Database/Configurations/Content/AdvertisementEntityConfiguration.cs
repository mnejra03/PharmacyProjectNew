namespace Market.Infrastructure.Database.Configurations.Content;
public sealed class AdvertisementEntityConfiguration : IEntityTypeConfiguration<AdvertisementEntity>
{
    public void Configure(EntityTypeBuilder<AdvertisementEntity> b)
    {
        b.ToTable("Advertisements");
        b.HasKey(x => x.Id);
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.ImageUrl).HasMaxLength(500).IsRequired();
    }
}
