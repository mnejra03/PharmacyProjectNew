namespace Market.Infrastructure.Database.Configurations.Communication;
public sealed class ChatMessageEntityConfiguration : IEntityTypeConfiguration<ChatMessageEntity>
{
    public void Configure(EntityTypeBuilder<ChatMessageEntity> b)
    {
        b.ToTable("ChatMessages");
        b.HasKey(x => x.Id);
        b.Property(x => x.Message).HasMaxLength(4000).IsRequired();
        b.Property(x => x.Type).HasMaxLength(40).IsRequired();
        b.Property(x => x.Status).HasMaxLength(40).IsRequired();
        b.HasIndex(x => new { x.SenderId, x.SentAtUtc });
    }
}
