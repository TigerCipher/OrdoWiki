namespace OrdoWiki.Data.Configurations;

using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class BannerConfiguration : IEntityTypeConfiguration<Banner>
{
    public void Configure(EntityTypeBuilder<Banner> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Alt).HasMaxLength(300);
        builder.Property(x => x.LinkUrl).HasMaxLength(500);

        builder.HasIndex(x => x.SlotIndex).IsUnique();

        builder.HasOne(x => x.MediaAsset)
            .WithMany()
            .HasForeignKey(x => x.MediaAssetId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.UpdatedBy)
            .WithMany()
            .HasForeignKey(x => x.UpdatedById)
            .OnDelete(DeleteBehavior.SetNull);

        // Seed 4 empty slots so the management UI always has rows to edit.
        DateTime seedAt = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        builder.HasData(
            new Banner { Id = Guid.Parse("b1000000-0000-0000-0000-000000000001"), SlotIndex = 1, UpdatedAt = seedAt },
            new Banner { Id = Guid.Parse("b1000000-0000-0000-0000-000000000002"), SlotIndex = 2, UpdatedAt = seedAt },
            new Banner { Id = Guid.Parse("b1000000-0000-0000-0000-000000000003"), SlotIndex = 3, UpdatedAt = seedAt },
            new Banner { Id = Guid.Parse("b1000000-0000-0000-0000-000000000004"), SlotIndex = 4, UpdatedAt = seedAt });
    }
}
