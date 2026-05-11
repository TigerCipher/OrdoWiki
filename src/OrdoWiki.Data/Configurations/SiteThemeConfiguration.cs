namespace OrdoWiki.Data.Configurations;

using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class SiteThemeConfiguration : IEntityTypeConfiguration<SiteTheme>
{
    public void Configure(EntityTypeBuilder<SiteTheme> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.LightPaletteJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.DarkPaletteJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.CustomValuesJson).HasColumnType("jsonb").IsRequired();

        builder.HasOne(x => x.LightBackgroundAsset)
            .WithMany()
            .HasForeignKey(x => x.LightBackgroundAssetId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.DarkBackgroundAsset)
            .WithMany()
            .HasForeignKey(x => x.DarkBackgroundAssetId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.UpdatedBy)
            .WithMany()
            .HasForeignKey(x => x.UpdatedById)
            .OnDelete(DeleteBehavior.SetNull);

        DateTime seedAt = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        builder.HasData(new SiteTheme
        {
            Id = SiteTheme.SingletonId,
            LightPaletteJson = "{}",
            DarkPaletteJson = "{}",
            CustomValuesJson = "{}",
            UpdatedAt = seedAt,
        });
    }
}

public class CustomThemeVariableConfiguration : IEntityTypeConfiguration<CustomThemeVariable>
{
    public void Configure(EntityTypeBuilder<CustomThemeVariable> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(300);
        builder.HasIndex(x => x.Name).IsUnique();
    }
}
