namespace Market.Infrastructure.Database.Configurations.Content;
public sealed class RecipeEntityConfiguration : IEntityTypeConfiguration<RecipeEntity>
{
    public void Configure(EntityTypeBuilder<RecipeEntity> b)
    {
        b.ToTable("Recipes");
        b.HasKey(x => x.Id);
        b.Property(x => x.DoctorFirstName).HasMaxLength(100).IsRequired();
        b.Property(x => x.DoctorLastName).HasMaxLength(100).IsRequired();
        b.Property(x => x.Status).HasMaxLength(40).IsRequired();
        b.Property(x => x.ScanUrl).HasMaxLength(500);
        b.Property(x => x.Scan).HasColumnType("varbinary(max)");
        b.HasIndex(x => new { x.UserId, x.DateOfIssue });
    }
}
