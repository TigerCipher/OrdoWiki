namespace OrdoWiki.Data.Configurations;

using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class CharacterImageConfiguration : IEntityTypeConfiguration<CharacterImage>
{
    public void Configure(EntityTypeBuilder<CharacterImage> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Caption).HasMaxLength(300);

        builder.HasIndex(x => new { x.CharacterId, x.OrderIndex });

        builder.HasOne(x => x.MediaAsset)
            .WithMany()
            .HasForeignKey(x => x.MediaAssetId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
