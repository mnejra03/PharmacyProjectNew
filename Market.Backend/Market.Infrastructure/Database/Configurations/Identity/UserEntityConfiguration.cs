namespace Market.Infrastructure.Database.Configurations.Identity;

public sealed class UserEntityConfiguration : IEntityTypeConfiguration<MarketUserEntity>
{
    public void Configure(EntityTypeBuilder<MarketUserEntity> b)
    {
        b.ToTable("Users");

        b.HasKey(x => x.Id);

        b.HasIndex(x => x.Email)
            .IsUnique();

        b.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(200);

        b.Property(x => x.PasswordHash)
            .IsRequired();

        // Roles
        b.Property(x => x.IsAdmin)
            .HasDefaultValue(false);

        b.Property(x => x.IsPharmacist)
            .HasDefaultValue(false);

        b.Property(x => x.IsCustomer).HasDefaultValue(true);
        b.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
        b.Property(x => x.LastName).HasMaxLength(100).IsRequired();
        b.Property(x => x.PhoneNumber).HasMaxLength(30);
        b.Property(x => x.ProfileImageUrl).HasMaxLength(500);

        b.Property(x => x.TokenVersion)
            .HasDefaultValue(0);

        b.Property(x => x.IsEnabled)
            .HasDefaultValue(true);

        // Navigation
        b.HasMany(x => x.RefreshTokens)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId);
    }
}
