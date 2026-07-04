namespace OrdoWiki.Data.Configurations;

using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class TimelineEventConfiguration : IEntityTypeConfiguration<TimelineEvent>
{
    public void Configure(EntityTypeBuilder<TimelineEvent> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Summary).HasMaxLength(500);
        builder.Property(x => x.DisplayOverride).HasMaxLength(120);
        builder.Property(x => x.ContentFormat).HasDefaultValue(ContentFormat.Markdown);
        builder.Property(x => x.CreatedById).IsRequired();

        builder.HasIndex(x => x.EpochDayNumber);

        builder.Property(x => x.SearchVector)
            .HasColumnType("tsvector")
            .HasComputedColumnSql(
                "setweight(to_tsvector('english', title), 'A') || " +
                "setweight(to_tsvector('english', coalesce(summary, '')), 'B') || " +
                "setweight(to_tsvector('english', regexp_replace(coalesce(markdown_body, ''), '<[^>]+>', ' ', 'g')), 'C')",
                stored: true);

        builder.HasIndex(x => x.SearchVector)
            .HasMethod("GIN");

        builder.HasOne(x => x.CreatedBy)
            .WithMany()
            .HasForeignKey(x => x.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
