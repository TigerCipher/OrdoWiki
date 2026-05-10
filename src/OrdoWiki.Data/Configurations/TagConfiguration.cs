namespace OrdoWiki.Data.Configurations;

using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Slug).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(80).IsRequired();
        builder.HasIndex(x => x.Slug).IsUnique();

        builder.HasOne(x => x.CreatedBy)
            .WithMany()
            .HasForeignKey(x => x.CreatedById)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class WikiPageTagConfiguration : IEntityTypeConfiguration<WikiPageTag>
{
    public void Configure(EntityTypeBuilder<WikiPageTag> builder)
    {
        builder.HasKey(x => new { x.PageId, x.TagId });
        builder.HasIndex(x => x.TagId);

        builder.HasOne(x => x.Page)
            .WithMany()
            .HasForeignKey(x => x.PageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Tag)
            .WithMany()
            .HasForeignKey(x => x.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class CharacterTagConfiguration : IEntityTypeConfiguration<CharacterTag>
{
    public void Configure(EntityTypeBuilder<CharacterTag> builder)
    {
        builder.HasKey(x => new { x.CharacterId, x.TagId });
        builder.HasIndex(x => x.TagId);

        builder.HasOne(x => x.Character)
            .WithMany()
            .HasForeignKey(x => x.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Tag)
            .WithMany()
            .HasForeignKey(x => x.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class MediaAssetTagConfiguration : IEntityTypeConfiguration<MediaAssetTag>
{
    public void Configure(EntityTypeBuilder<MediaAssetTag> builder)
    {
        builder.HasKey(x => new { x.MediaAssetId, x.TagId });
        builder.HasIndex(x => x.TagId);

        builder.HasOne(x => x.MediaAsset)
            .WithMany()
            .HasForeignKey(x => x.MediaAssetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Tag)
            .WithMany()
            .HasForeignKey(x => x.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class TimelineEventTagConfiguration : IEntityTypeConfiguration<TimelineEventTag>
{
    public void Configure(EntityTypeBuilder<TimelineEventTag> builder)
    {
        builder.HasKey(x => new { x.TimelineEventId, x.TagId });
        builder.HasIndex(x => x.TagId);

        builder.HasOne(x => x.TimelineEvent)
            .WithMany()
            .HasForeignKey(x => x.TimelineEventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Tag)
            .WithMany()
            .HasForeignKey(x => x.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
