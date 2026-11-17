namespace Market.Infrastructure.Database.Configurations.Communication;

public sealed class AdminNotificationReadStateEntityConfiguration : IEntityTypeConfiguration<AdminNotificationReadStateEntity>
{
    public void Configure(EntityTypeBuilder<AdminNotificationReadStateEntity> b)
    {
        b.ToTable("AdminNotificationReadStates");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.AdminUserId, x.NotificationId }).IsUnique();
        b.HasOne<MarketUserEntity>().WithMany().HasForeignKey(x => x.AdminUserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<NotificationEntity>().WithMany().HasForeignKey(x => x.NotificationId).OnDelete(DeleteBehavior.Cascade);
    }
}
