namespace OrdoWiki.Data.Configurations;

using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
{
    public void Configure(EntityTypeBuilder<MediaAsset> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.StoragePath).HasMaxLength(300).IsRequired();
        builder.Property(x => x.OriginalName).HasMaxLength(260).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.UploadedById).IsRequired();

        builder.HasIndex(x => x.UploadedById);
        builder.HasIndex(x => x.UploadedAt);
        builder.HasIndex(x => new { x.SourceType, x.UploadedAt });

        builder.HasOne(x => x.UploadedBy)
            .WithMany()
            .HasForeignKey(x => x.UploadedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
