using ItAssetManagement.Services;
using Microsoft.EntityFrameworkCore;

namespace ItAssetManagement.Data;

public static class DatabaseInitializer
{
    /// <summary>
    /// Applies any pending migrations and seeds an empty database.
    /// <para>
    /// Migrating on startup is a deliberate convenience for an assessment build: it means
    /// <c>dotnet run</c> against an empty database just works, with no extra step for the
    /// reviewer. On a real deployment this belongs in the release pipeline instead, since
    /// concurrent instances would otherwise race to apply the same migration.
    /// </para>
    /// </summary>
    public static async Task MigrateAndSeedAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var provider = scope.ServiceProvider;

        var db = provider.GetRequiredService<AppDbContext>();
        var hasher = provider.GetRequiredService<IPasswordHasher>();
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DatabaseInitializer));

        await db.Database.MigrateAsync();
        await DbSeeder.SeedAsync(db, hasher, logger);
    }
}
