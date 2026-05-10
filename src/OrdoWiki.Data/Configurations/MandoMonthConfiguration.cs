namespace OrdoWiki.Data.Configurations;

using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class MandoMonthConfiguration : IEntityTypeConfiguration<MandoMonth>
{
    // Stub Mando'a month names. Admin can rename in /admin/calendar.
    private static readonly (Guid Id, int Index, string Name)[] _seed =
    [
        (new Guid("a1c0c0a0-0001-0000-0000-000000000001"),  1, "Vhett'yc"),
        (new Guid("a1c0c0a0-0002-0000-0000-000000000002"),  2, "Beskar'yc"),
        (new Guid("a1c0c0a0-0003-0000-0000-000000000003"),  3, "Verd'yc"),
        (new Guid("a1c0c0a0-0004-0000-0000-000000000004"),  4, "Kote'yc"),
        (new Guid("a1c0c0a0-0005-0000-0000-000000000005"),  5, "Manda'yc"),
        (new Guid("a1c0c0a0-0006-0000-0000-000000000006"),  6, "Aliit'yc"),
        (new Guid("a1c0c0a0-0007-0000-0000-000000000007"),  7, "Ka'ra'yc"),
        (new Guid("a1c0c0a0-0008-0000-0000-000000000008"),  8, "Aay'han'yc"),
        (new Guid("a1c0c0a0-0009-0000-0000-000000000009"),  9, "Akaan'yc"),
        (new Guid("a1c0c0a0-0010-0000-0000-000000000010"), 10, "Mando'yc"),
        (new Guid("a1c0c0a0-0011-0000-0000-000000000011"), 11, "Resol'yc"),
        (new Guid("a1c0c0a0-0012-0000-0000-000000000012"), 12, "Buir'yc"),
    ];

    public void Configure(EntityTypeBuilder<MandoMonth> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(60).IsRequired();
        builder.HasIndex(x => x.MonthIndex).IsUnique();

        builder.HasData(_seed.Select(s => new MandoMonth
        {
            Id = s.Id, MonthIndex = s.Index, Name = s.Name,
        }));
    }
}
