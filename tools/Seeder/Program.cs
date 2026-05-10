// Load-test data seeder. Run repeatedly:
//   dotnet run --project tools/Seeder
//   dotnet run --project tools/Seeder -- 100 200      (100 events, 200 logs)
//
// Reads the connection string from src/OrdoWiki.Web/appsettings.Development.json
// (override with ORDOWIKI_CONNECTION env var). Picks the first user found as the
// creator/editor. Slugs are suffixed with a short random token so re-runs don't collide.

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrdoWiki.Data;
using OrdoWiki.Data.Calendars;
using OrdoWiki.Data.Entities;

int eventCount = args.Length > 0 && int.TryParse(args[0], out int e) ? e : 50;
int logCount = args.Length > 1 && int.TryParse(args[1], out int l) ? l : 50;

string connectionString = ResolveConnectionString();

// Mirror the web app's Identity registration so the model validator is happy with
// the passkey/identity tables — otherwise EF refuses to materialize the model.
ServiceCollection services = new();
services.AddLogging();
services.AddDbContext<ApplicationDbContext>(opt =>
    opt.UseNpgsql(connectionString).UseSnakeCaseNamingConvention());
services.AddIdentityCore<ApplicationUser>(opt =>
    {
        opt.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

await using ServiceProvider provider = services.BuildServiceProvider();
await using AsyncServiceScope scope = provider.CreateAsyncScope();
ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

string? creatorId = await db.Users.OrderBy(u => u.UserName).Select(u => u.Id).FirstOrDefaultAsync();
if (creatorId is null)
{
    Console.Error.WriteLine("No users found. Register a user first, then re-run the seeder.");
    return 1;
}

Console.WriteLine($"Seeding as user {creatorId}");

Random rng = new();

if (eventCount > 0) await SeedTimelineEventsAsync(db, creatorId, eventCount, rng);
if (logCount > 0) await SeedLogsAsync(db, creatorId, logCount, rng);

Console.WriteLine("Done.");
return 0;

static async Task SeedTimelineEventsAsync(ApplicationDbContext db, string creatorId, int count, Random rng)
{
    DateTime now = DateTime.UtcNow;
    List<TimelineEvent> events = new(count);

    for (int i = 0; i < count; i++)
    {
        // Random date across a wide span — mostly fully specified, some fuzzy.
        int year = rng.Next(-200, 200);
        int? month = rng.Next(0, 10) < 8 ? rng.Next(1, MandoCalendar.MonthsPerYear + 1) : null;
        int? day = month is not null && rng.Next(0, 10) < 8 ? rng.Next(1, MandoCalendar.DaysPerMonth + 1) : null;

        events.Add(new TimelineEvent
        {
            Id = Guid.NewGuid(),
            Title = $"{Pick(rng, WordBanks.EventVerbs)} {Pick(rng, WordBanks.EventNouns)} ({RandomToken(rng, 4)})",
            MarkdownBody = BuildBody(rng),
            MandoYear = year,
            MandoMonth = month,
            MandoDay = day,
            EpochDayNumber = MandoCalendar.ToEpochDay(new MandoDate(year, month, day)),
            DisplayOverride = null,
            CreatedById = creatorId,
            CreatedAt = now,
            UpdatedAt = now,
        });
    }

    db.TimelineEvents.AddRange(events);
    await db.SaveChangesAsync();
    Console.WriteLine($"Inserted {events.Count} timeline events.");
}

static async Task SeedLogsAsync(ApplicationDbContext db, string creatorId, int count, Random rng)
{
    DateTime now = DateTime.UtcNow;
    List<WikiPage> pages = new(count);
    List<PageRevision> revisions = new(count);

    for (int i = 0; i < count; i++)
    {
        Guid pageId = Guid.NewGuid();
        Guid revisionId = Guid.NewGuid();
        string slug = $"{Pick(rng, WordBanks.LogTopics).ToLowerInvariant()}-{RandomToken(rng, 6)}";

        WikiPage page = new()
        {
            Id = pageId,
            Slug = slug,
            Title = $"{Pick(rng, WordBanks.LogTopics)}: {Pick(rng, WordBanks.EventNouns)}",
            Summary = BuildSummary(rng),
            CurrentRevisionId = revisionId,
            CreatedAt = now,
            CreatedById = creatorId,
        };

        PageRevision revision = new()
        {
            Id = revisionId,
            PageId = pageId,
            MarkdownBody = BuildBody(rng),
            EditSummary = "Seeded entry",
            EditedAt = now,
            EditedById = creatorId,
        };

        pages.Add(page);
        revisions.Add(revision);
    }

    // Insert pages first (without CurrentRevisionId), then revisions, then update FK —
    // skipping the deferred-constraint dance keeps EF happy when it generates the SQL.
    db.WikiPages.AddRange(pages.Select(p => new WikiPage
    {
        Id = p.Id, Slug = p.Slug, Title = p.Title, Summary = p.Summary,
        CreatedAt = p.CreatedAt, CreatedById = p.CreatedById,
    }));
    db.PageRevisions.AddRange(revisions);
    await db.SaveChangesAsync();

    foreach (WikiPage p in pages)
    {
        WikiPage tracked = await db.WikiPages.SingleAsync(x => x.Id == p.Id);
        tracked.CurrentRevisionId = p.CurrentRevisionId;
    }
    await db.SaveChangesAsync();

    Console.WriteLine($"Inserted {pages.Count} log pages.");
}

static string ResolveConnectionString()
{
    string? envCs = Environment.GetEnvironmentVariable("ORDOWIKI_CONNECTION");
    if (!string.IsNullOrWhiteSpace(envCs)) return envCs;

    string repoRoot = FindRepoRoot();
    string settingsPath = Path.Combine(repoRoot, "src", "OrdoWiki.Web", "appsettings.Development.json");
    IConfigurationRoot config = new ConfigurationBuilder()
        .AddJsonFile(settingsPath, optional: false)
        .Build();

    return config.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("DefaultConnection not found in appsettings.Development.json.");
}

static string FindRepoRoot()
{
    DirectoryInfo? dir = new(AppContext.BaseDirectory);
    while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "OrdoWiki.slnx")))
        dir = dir.Parent;
    return dir?.FullName ?? throw new InvalidOperationException("Couldn't locate repo root (OrdoWiki.slnx).");
}

static string RandomToken(Random rng, int length)
{
    const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
    return new string(Enumerable.Range(0, length).Select(_ => chars[rng.Next(chars.Length)]).ToArray());
}

static string Pick(Random rng, string[] pool) => pool[rng.Next(pool.Length)];

static string BuildSummary(Random rng) =>
    $"{Pick(rng, WordBanks.EventVerbs)} {Pick(rng, WordBanks.EventNouns)} during the {Pick(rng, WordBanks.EventNouns).ToLowerInvariant()} campaign.";

static string BuildBody(Random rng)
{
    int paragraphs = rng.Next(1, 4);
    return string.Join("\n\n", Enumerable.Range(0, paragraphs).Select(_ =>
    {
        int sentences = rng.Next(2, 5);
        return string.Join(" ", Enumerable.Range(0, sentences).Select(_ =>
            $"{Pick(rng, WordBanks.EventVerbs)} {Pick(rng, WordBanks.EventNouns).ToLowerInvariant()} on {Pick(rng, WordBanks.Locations)}."));
    }));
}

// Word banks. Boring but unique enough for load-test variety.

static class WordBanks
{
    public static readonly string[] EventVerbs =
    [
        "Liberated", "Sieged", "Held", "Lost", "Reclaimed", "Sabotaged", "Founded",
        "Disbanded", "Brokered", "Ambushed", "Fortified", "Razed", "Negotiated",
        "Defended", "Recovered", "Forged",
    ];

    public static readonly string[] EventNouns =
    [
        "Mandalore", "Sundari", "Concord Dawn", "Kalevala", "Krownest", "Vizsla",
        "Beskar Forge", "Death Watch", "True Mandalorians", "Clan Ordo",
        "Outer Rim Holdout", "Civil War", "Reformation", "Treaty", "Council",
        "Banner", "Beskad", "Verpine Outpost",
    ];

    public static readonly string[] LogTopics =
    [
        "Operation", "Campaign", "Council", "Skirmish", "Treaty", "Foundry",
        "Patrol", "Raid", "Audience", "Trial", "Banquet", "Vigil",
    ];

    public static readonly string[] Locations =
    [
        "Mandalore", "Concordia", "Concord Dawn", "Sundari", "Kalevala",
        "Krownest", "Tatooine", "Naboo", "Coruscant", "Jakku", "Hoth",
    ];
}
