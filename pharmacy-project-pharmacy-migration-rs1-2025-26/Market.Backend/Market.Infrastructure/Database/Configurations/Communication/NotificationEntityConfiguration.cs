namespace Market.Infrastructure.Database.Configurations.Communication;
public sealed class NotificationEntityConfiguration : IEntityTypeConfiguration<NotificationEntity>
{
    public void Configure(EntityTypeBuilder<NotificationEntity> b)
    {
        b.ToTable("Notifications");
        b.HasKey(x => x.Id);
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.Message).HasMaxLength(2000).IsRequired();
        b.Property(x => x.Type).HasMaxLength(80).IsRequired();
        b.HasIndex(x => new { x.UserId, x.IsRead, x.CreatedAt });
    }
}
