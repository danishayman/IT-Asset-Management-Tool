using ItAssetManagement.Data;
using ItAssetManagement.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

// A bootstrap logger, so that a failure during startup itself still gets recorded.
// It is replaced by the fully configured logger as soon as configuration has been read.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
            "Connection string 'DefaultConnection' is not configured. See README.md for setup.");

    builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

    builder.Services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();

    builder.Services.AddControllersWithViews();

    var app = builder.Build();

    await app.MigrateAndSeedAsync();

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");

        // HTTPS enforcement is deliberately production-only. Locally the app serves plain
        // HTTP on a fixed port so a reviewer does not have to trust a dev certificate first.
        app.UseHsts();
        app.UseHttpsRedirection();
    }

    app.UseStaticFiles();
    app.UseSerilogRequestLogging();

    app.UseRouting();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    await app.RunAsync();
    return 0;
}
// HostAbortedException is excluded deliberately: `dotnet ef` builds the host and then
// aborts it on purpose to get at the DbContext, so catching it here would report every
// design-time command as a fatal startup crash.
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly during startup.");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}
