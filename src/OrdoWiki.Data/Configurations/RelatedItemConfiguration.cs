namespace OrdoWiki.Data.Configurations;

using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class RelatedItemConfiguration : IEntityTypeConfiguration<RelatedItem>
{
    public void Configure(EntityTypeBuilder<RelatedItem> builder)
    {
        builder.HasKey(x => x.Id);

        // Enum stored as int — small, fast, and easy to read.
        builder.Property(x => x.SourceKind).HasConversion<int>();
        builder.Property(x => x.TargetKind).HasConversion<int>();

        // Prevents duplicate canonical rows.
        builder.HasIndex(x => new { x.SourceKind, x.SourceId, x.TargetKind, x.TargetId })
            .IsUnique();

        // Read side: GetForAsync queries by either side, so both directions need an index.
        builder.HasIndex(x => new { x.TargetKind, x.TargetId });
    }
}
