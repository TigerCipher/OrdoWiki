namespace OrdoWiki.Data.Configurations;

using Calendars;
using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class MandoEraConfiguration : IEntityTypeConfiguration<MandoEra>
{
    public void Configure(EntityTypeBuilder<MandoEra> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(60).IsRequired();
        builder.Property(x => x.ShortCode).HasMaxLength(8).IsRequired();
        builder.HasIndex(x => x.ShortCode).IsUnique();
        builder.HasIndex(x => x.AnchorYear);

        builder.HasData(
            new MandoEra
            {
                Id = new Guid("e1a0e1a0-0000-0000-0000-000000000001"),
                Name = "Before Civil War",
                ShortCode = "BCW",
                AnchorYear = 0,
                Direction = EraDirection.Backward,
                SortOrder = 0,
            },
            new MandoEra
            {
                Id = new Guid("e1a0e1a0-0000-0000-0000-000000000002"),
                Name = "After Civil War",
                ShortCode = "ACW",
                AnchorYear = 0,
                Direction = EraDirection.Forward,
                SortOrder = 1,
            });
    }
}
