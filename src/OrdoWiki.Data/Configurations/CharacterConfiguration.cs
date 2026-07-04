namespace OrdoWiki.Data.Configurations;

using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class CharacterConfiguration : IEntityTypeConfiguration<Character>
{
    public void Configure(EntityTypeBuilder<Character> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Slug).HasMaxLength(160).IsRequired();
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Summary).HasMaxLength(500);
        builder.Property(x => x.ContentFormat).HasDefaultValue(ContentFormat.Markdown);
        builder.Property(x => x.OwnerId).IsRequired();

        builder.HasIndex(x => x.OwnerId);

        builder.Property(x => x.SearchVector)
            .HasColumnType("tsvector")
            .HasComputedColumnSql(
                "setweight(to_tsvector('english', name), 'A') || " +
                "setweight(to_tsvector('english', coalesce(summary, '')), 'B') || " +
                "setweight(to_tsvector('english', coalesce(markdown_body, '')), 'C')",
                stored: true);

        builder.HasIndex(x => x.SearchVector)
            .HasMethod("GIN");

        builder.HasOne(x => x.Owner)
            .WithMany()
            .HasForeignKey(x => x.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Images)
            .WithOne(i => i.Character)
            .HasForeignKey(i => i.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
