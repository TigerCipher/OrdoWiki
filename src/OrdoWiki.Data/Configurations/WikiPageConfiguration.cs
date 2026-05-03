namespace OrdoWiki.Data.Configurations;

using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class WikiPageConfiguration : IEntityTypeConfiguration<WikiPage>
{
    public void Configure(EntityTypeBuilder<WikiPage> builder)
    {
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => p.Slug).IsUnique();
        builder.Property(p => p.Title).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Slug).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Summary).HasMaxLength(500);
        
        builder.HasOne(p => p.CurrentRevision)
            .WithOne()
            .HasForeignKey<WikiPage>(p => p.CurrentRevisionId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasMany(p => p.Revisions)
            .WithOne(p => p.Page)
            .HasForeignKey(r => r.PageId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(p => p.Creator)
            .WithMany()
            .HasForeignKey(p => p.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}