using ItAssetManagement.Constants;
using ItAssetManagement.Domain;
using ItAssetManagement.Services;
using Microsoft.EntityFrameworkCore;

namespace ItAssetManagement.Data;

/// <summary>
/// Populates an empty database with a realistic demo dataset.
/// <para>
/// This runs at startup rather than through EF's <c>HasData</c> because seeding needs
/// values that are not compile-time constants: BCrypt generates a fresh random salt on
/// every call, so <c>HasData</c> would see a different hash each time the model was built
/// and emit a spurious migration on every <c>dotnet ef migrations add</c>. Everything
/// else is kept deterministic with a fixed RNG seed and a fixed base date, so repeated
/// seeds of a fresh database produce identical data.
/// </para>
/// </summary>
public static class DbSeeder
{
    private static readonly DateTime BaseUtc = new(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);

    // Demo passwords. These are seeded, documented in the README, and only ever stored as
    // BCrypt hashes. They exist so a reviewer can sign in, nothing more.
    public const string AdminPassword = "Admin@123";
    public const string UserPassword = "User@123";

    private record AssetSpec(string Name, string Category, AssetStatus Status);

    public static async Task SeedAsync(AppDbContext db, IPasswordHasher hasher, ILogger logger)
    {
        // Guard on Users rather than on any table: it is the first thing written, so its
        // presence is a reliable signal that a previous seed ran to completion.
        if (await db.Users.AnyAsync())
        {
            logger.LogInformation("Database already seeded; skipping.");
            return;
        }

        logger.LogInformation("Seeding database with demo data.");

        var users = new List<User>
        {
            new() { Username = "admin", FullName = "Aisyah Rahman", Role = Roles.Admin, IsActive = true, PasswordHash = hasher.Hash(AdminPassword), CreatedAt = BaseUtc.AddDays(-400) },
            new() { Username = "jlim",  FullName = "Jason Lim",     Role = Roles.User,  IsActive = true, PasswordHash = hasher.Hash(UserPassword),  CreatedAt = BaseUtc.AddDays(-360) },
            new() { Username = "schan", FullName = "Sarah Chan",    Role = Roles.User,  IsActive = true, PasswordHash = hasher.Hash(UserPassword),  CreatedAt = BaseUtc.AddDays(-300) }
        };
        db.Users.AddRange(users);

        var categoryNames = new[] { "Laptop", "Desktop", "Monitor", "Mobile Phone", "Printer", "Networking" };
        var categories = categoryNames.Select(n => new Category { Name = n }).ToList();
        db.Categories.AddRange(categories);

        // Saved first so the assets below can reference real primary keys.
        await db.SaveChangesAsync();

        var admin = users[0];
        var staff = users.Where(u => u.Role == Roles.User).ToList();
        var categoryIdByName = categories.ToDictionary(c => c.Name, c => c.Id);

        var specs = BuildAssetSpecs();
        var locations = new[] { "HQ Level 3", "HQ Level 5", "Penang Branch", "Store Room A", "Remote / WFH" };

        // Fixed seed: the demo dataset comes out identical on every fresh install, which
        // keeps screenshots, seed.sql and any bug report reproducible.
        var rng = new Random(12345);

        var assets = new List<Asset>();
        for (var i = 0; i < specs.Count; i++)
        {
            var spec = specs[i];
            var purchase = new DateOnly(2022, 1, 1).AddDays(rng.Next(0, 1200));
            var createdAt = BaseUtc.AddDays(-rng.Next(30, 365));
            var serialSuffix = (char)('A' + rng.Next(0, 26));

            assets.Add(new Asset
            {
                AssetTag = $"ITAM-{i + 1:D4}",
                Name = spec.Name,
                CategoryId = categoryIdByName[spec.Category],
                SerialNumber = $"SN{rng.Next(100000, 999999)}{serialSuffix}",
                Status = spec.Status,
                // Only hardware actually in use is held by somebody; anything available,
                // in for repair, or retired sits unassigned.
                AssignedToUserId = spec.Status == AssetStatus.InUse ? staff[i % staff.Count].Id : null,
                PurchaseDate = purchase,
                WarrantyExpiry = purchase.AddYears(3),
                PurchaseCost = Math.Round((decimal)(rng.NextDouble() * 6500 + 350), 2),
                Location = locations[rng.Next(locations.Length)],
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            });
        }

        db.Assets.AddRange(assets);
        await db.SaveChangesAsync();

        // Backfill the audit trail so the activity log page is not empty on first run.
        var logs = new List<ActivityLog>();
        foreach (var asset in assets)
        {
            logs.Add(new ActivityLog
            {
                AssetId = asset.Id,
                PerformedByUserId = admin.Id,
                Action = ActivityAction.Create,
                Details = $"Created asset {asset.AssetTag} ({asset.Name}).",
                Timestamp = asset.CreatedAt
            });

            if (asset.AssignedToUserId is { } assigneeId)
            {
                var assignee = users.First(u => u.Id == assigneeId);
                logs.Add(new ActivityLog
                {
                    AssetId = asset.Id,
                    PerformedByUserId = admin.Id,
                    Action = ActivityAction.Assign,
                    Details = $"Assigned {asset.AssetTag} to {assignee.FullName}.",
                    Timestamp = asset.CreatedAt.AddHours(2)
                });
            }
        }
        db.ActivityLogs.AddRange(logs);
        db.Tickets.AddRange(BuildTickets(assets, staff));

        await db.SaveChangesAsync();

        logger.LogInformation(
            "Seed complete: {Users} users, {Categories} categories, {Assets} assets, {Logs} activity rows.",
            users.Count, categories.Count, assets.Count, logs.Count);
    }

    private static List<Ticket> BuildTickets(List<Asset> assets, List<User> staff)
    {
        // Point the hardware-fault tickets at assets that are genuinely in for repair,
        // so the ticket list and the asset statuses tell a consistent story.
        var faulty = assets.Where(a => a.Status == AssetStatus.Repair).ToList();
        int? FaultyId(int index) => faulty.ElementAtOrDefault(index)?.Id;

        return
        [
            new Ticket
            {
                RaisedByUserId = staff[0].Id,
                AssetId = FaultyId(0),
                Title = "Laptop will not power on",
                Description = "Dead after the weekend. No lights, no fan. Tried a different charger and wall socket.",
                Priority = TicketPriority.High,
                Status = TicketStatus.Open,
                CreatedAt = BaseUtc.AddDays(-12)
            },
            new Ticket
            {
                RaisedByUserId = staff[1].Id,
                AssetId = FaultyId(1),
                Title = "Screen flickering on external display",
                Description = "Flickers roughly every ten minutes over USB-C. Fine on HDMI.",
                Priority = TicketPriority.Medium,
                Status = TicketStatus.InProgress,
                CreatedAt = BaseUtc.AddDays(-9)
            },
            new Ticket
            {
                RaisedByUserId = staff[0].Id,
                AssetId = FaultyId(2),
                Title = "Printer jamming on duplex jobs",
                Description = "Jams in the duplex unit on anything over five pages. Single-sided is unaffected.",
                Priority = TicketPriority.Low,
                Status = TicketStatus.Open,
                CreatedAt = BaseUtc.AddDays(-7)
            },
            new Ticket
            {
                RaisedByUserId = staff[1].Id,
                AssetId = null,
                Title = "Request a second monitor",
                Description = "Working across three spreadsheets daily. A second monitor would help considerably.",
                Priority = TicketPriority.Low,
                Status = TicketStatus.Open,
                CreatedAt = BaseUtc.AddDays(-5)
            },
            new Ticket
            {
                RaisedByUserId = staff[0].Id,
                AssetId = null,
                Title = "VPN drops every few minutes",
                Description = "Disconnects roughly every five minutes from home. The office network is fine.",
                Priority = TicketPriority.Critical,
                Status = TicketStatus.InProgress,
                CreatedAt = BaseUtc.AddDays(-3)
            },
            new Ticket
            {
                RaisedByUserId = staff[1].Id,
                AssetId = null,
                Title = "Keyboard key sticking",
                Description = "The E key needs a hard press to register. Cleaned it with compressed air, no improvement.",
                Priority = TicketPriority.Low,
                Status = TicketStatus.Closed,
                CreatedAt = BaseUtc.AddDays(-20)
            }
        ];
    }

    /// <summary>
    /// 28 assets with a deliberate spread across all four statuses, so the dashboard, the
    /// status filter and the repair count all have something meaningful to show.
    /// </summary>
    private static List<AssetSpec> BuildAssetSpecs() =>
    [
        new("Dell Latitude 5540",        "Laptop",       AssetStatus.InUse),
        new("Lenovo ThinkPad T14 Gen 4", "Laptop",       AssetStatus.InUse),
        new("HP EliteBook 840 G10",      "Laptop",       AssetStatus.InUse),
        new("MacBook Pro 14 M3",         "Laptop",       AssetStatus.InUse),
        new("Dell Latitude 7440",        "Laptop",       AssetStatus.Available),
        new("Lenovo ThinkPad X1 Carbon", "Laptop",       AssetStatus.Repair),
        new("HP ProBook 450 G9",         "Laptop",       AssetStatus.Retired),
        new("Dell OptiPlex 7010",        "Desktop",      AssetStatus.InUse),
        new("HP ProDesk 400 G9",         "Desktop",      AssetStatus.InUse),
        new("Lenovo ThinkCentre M70q",   "Desktop",      AssetStatus.Available),
        new("Dell OptiPlex 3000",        "Desktop",      AssetStatus.Retired),
        new("Dell UltraSharp U2723QE",   "Monitor",      AssetStatus.InUse),
        new("LG 27UP850-W",              "Monitor",      AssetStatus.InUse),
        new("HP E24 G5",                 "Monitor",      AssetStatus.Available),
        new("Samsung ViewFinity S6",     "Monitor",      AssetStatus.Available),
        new("AOC 24B2XH",                "Monitor",      AssetStatus.Repair),
        new("Apple iPhone 14",           "Mobile Phone", AssetStatus.InUse),
        new("Samsung Galaxy S23",        "Mobile Phone", AssetStatus.InUse),
        new("Google Pixel 7a",           "Mobile Phone", AssetStatus.Available),
        new("Apple iPhone 12",           "Mobile Phone", AssetStatus.Retired),
        new("HP LaserJet Pro M404dn",    "Printer",      AssetStatus.InUse),
        new("Brother HL-L2350DW",        "Printer",      AssetStatus.Repair),
        new("Epson EcoTank L3250",       "Printer",      AssetStatus.Available),
        new("Canon imageCLASS MF445dw",  "Printer",      AssetStatus.Available),
        new("Cisco Catalyst 2960-X",     "Networking",   AssetStatus.InUse),
        new("Ubiquiti UniFi U6 Pro",     "Networking",   AssetStatus.InUse),
        new("TP-Link ER605 Router",      "Networking",   AssetStatus.Repair),
        new("Netgear GS308 Switch",      "Networking",   AssetStatus.Available)
    ];
}
