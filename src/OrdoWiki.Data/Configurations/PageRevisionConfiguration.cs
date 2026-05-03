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
        builder.Property(r => r.EditSummary).HasMaxLength(500);
        
        builder.HasOne(r => r.Editor)
            .WithMany()
            .HasForeignKey(r => r.EditedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}