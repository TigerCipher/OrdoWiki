namespace OrdoWiki.Data.Configurations;

using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class HomePageConfiguration : IEntityTypeConfiguration<HomePage>
{
    public void Configure(EntityTypeBuilder<HomePage> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.BioMarkdown).IsRequired();

        builder.HasOne(x => x.FeaturedLog)
            .WithMany()
            .HasForeignKey(x => x.FeaturedLogId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.UpdatedBy)
            .WithMany()
            .HasForeignKey(x => x.UpdatedById)
            .OnDelete(DeleteBehavior.SetNull);

        DateTime seedAt = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        builder.HasData(new HomePage
        {
            Id = HomePage.SingletonId,
            BioMarkdown = string.Empty,
            UpdatedAt = seedAt,
        });
    }
}
