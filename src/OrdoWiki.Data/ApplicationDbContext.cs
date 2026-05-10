namespace OrdoWiki.Data;

using Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<InviteCode> InviteCodes => Set<InviteCode>();
    public DbSet<WikiPage> WikiPages => Set<WikiPage>();
    public DbSet<PageRevision> PageRevisions => Set<PageRevision>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<Character> Characters => Set<Character>();
    public DbSet<CharacterImage> CharacterImages => Set<CharacterImage>();
    public DbSet<MandoMonth> MandoMonths => Set<MandoMonth>();
    public DbSet<MandoEra> MandoEras => Set<MandoEra>();
    public DbSet<TimelineEvent> TimelineEvents => Set<TimelineEvent>();
    public DbSet<Banner> Banners => Set<Banner>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<WikiPageTag> WikiPageTags => Set<WikiPageTag>();
    public DbSet<CharacterTag> CharacterTags => Set<CharacterTag>();
    public DbSet<MediaAssetTag> MediaAssetTags => Set<MediaAssetTag>();
    public DbSet<TimelineEventTag> TimelineEventTags => Set<TimelineEventTag>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Identity calls ToTable() inside its own OnModelCreating, which bypasses the snake_case convention.
        // Re-map the Identity tables explicitly so the schema is consistently snake_case.
        builder.Entity<ApplicationUser>().ToTable("users");
        builder.Entity<IdentityRole>().ToTable("roles");
        builder.Entity<IdentityUserRole<string>>().ToTable("user_roles");
        builder.Entity<IdentityUserClaim<string>>().ToTable("user_claims");
        builder.Entity<IdentityUserLogin<string>>().ToTable("user_logins");
        builder.Entity<IdentityRoleClaim<string>>().ToTable("role_claims");
        builder.Entity<IdentityUserToken<string>>().ToTable("user_tokens");
        builder.Entity<IdentityUserPasskey<string>>().ToTable("user_passkeys");

        // Identity also names a few indexes explicitly — rename those too.
        builder.Entity<IdentityRole>()
            .HasIndex(r => r.NormalizedName)
            .HasDatabaseName("ix_roles_normalized_name");
        builder.Entity<ApplicationUser>()
            .HasIndex(u => u.NormalizedUserName)
            .HasDatabaseName("ix_users_normalized_user_name");
        builder.Entity<ApplicationUser>()
            .HasIndex(u => u.NormalizedEmail)
            .HasDatabaseName("ix_users_normalized_email");

        builder.Entity<InviteCode>(b => { b.HasIndex(c => c.Code).IsUnique(); });

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}