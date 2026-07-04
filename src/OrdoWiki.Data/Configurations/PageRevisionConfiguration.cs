namespace OrdoWiki.Data.Configurations;

using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PageRevisionConfiguration : IEntityTypeConfiguration<PageRevision>
{
    public void Configure(EntityTypeBuilder<PageRevision> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.MarkdownBody).IsRequired();
        builder.Property(r => r.ContentFormat).HasDefaultValue(ContentFormat.Markdown);
        builder.Property(r => r.EditSummary).HasMaxLength(500);

        // markdown_body may hold either markdown or HTML per ContentFormat. Strip
        // tag-shaped tokens before indexing so HTML content doesn't pollute the
        // tsvector with words like "strong" or "span". Markdown surface syntax
        // (# * _) passes through the parser fine as regular text.
        builder.Property(r => r.SearchVector)
            .HasColumnType("tsvector")
            .HasComputedColumnSql(
                "to_tsvector('english', regexp_replace(markdown_body, '<[^>]+>', ' ', 'g'))",
                stored: true);

        builder.HasIndex(r => r.SearchVector)
            .HasMethod("GIN");

        builder.HasOne(r => r.Editor)
            .WithMany()
            .HasForeignKey(r => r.EditedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}