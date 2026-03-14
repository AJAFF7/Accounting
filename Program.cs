using Microsoft.EntityFrameworkCore;
using Serilog;
using SoftMax.Accounting;
using SoftMax.Accounting.Components;
using SoftMax.Core;
using SoftMax.Core.Services;
using System.Reflection;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var AppVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
    var LocalTime = DateTimeOffset.Now.ToString("yyyy-MM-dd hh:mm tt");

    Log.Information("Starting application - Version: {AppVersion}, Local Time: {LocalTime}", AppVersion, LocalTime);

    var builder = WebApplication.CreateBuilder(args);
    Log.Information("Web application builder created successfully");

    // Configure Serilog from configuration
    Log.Information("Configuring Serilog from appsettings.json");
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    // Configure services using extension methods
    Log.Information("Configuring application services");

    builder.Services
        .AddBlazorServices()
        .AddMudServices()
        .AddApplicationServices()
        .AddDataProtectionServices()
        .AddNotificationServices();

    // Add Controllers for API support
    builder.Services.AddControllers();
    builder.Services.AddHttpClient();

    await builder.Services.AddRedisServicesAsync();

    // Configure database and related services
    builder.Services
        .AddDatabaseServices()
        .AddIdentityServices()
        .AddOpenIdConnectServices("SoftMax.Accounting")
        .AddHealthCheckServices();

    // Configure localization and other core services
    builder.Services
        .AddLocalizationServices()
        .AddBackgroundServices()
        .AddConnectionTrackingServices()
        .AddRepositoryServices()
        .AddRequestMonitoringServices();

    // Configure API authorization policies
    //Log.Information("Configuring API authorization policies");
    //builder.Services.AddApiAuthorizationPolicy(auth => auth.ConfigurePolicies = options =>
    //{
    //    options.AddPolicy("TopCare_Accounting", policy => policy.RequireAuthenticatedUser());
    //});

    // Configure DbContexts with enum seeding and other services
    Log.Information("Configuring database contexts and seeding enums");
    builder.Services.ConfigureDbContext<AccountingDbContext>(builder, (context, enums) =>
    {
        var DEFAULT_LOG_PREFIX = $"{context.Schema}.Startup";
        foreach (var enumItem in enums)
        {
            var existingLookup = context.Lookups.FirstOrDefault(l => l.Id == enumItem.Id && !l.IsDeleted);
            if (existingLookup == null)
            {
                context.Lookups.Add(new()
                {
                    Id = enumItem.Id,
                    Name = enumItem.Name,
                    Type = enumItem.Type,
                    Sort = enumItem.Value
                });
                Log.Information("[{Prefix}] Added {Type} enum: {Name} with ID: {Id}", DEFAULT_LOG_PREFIX, enumItem.Type, enumItem.Name, enumItem.Id);
            }
            else
            {
                existingLookup.Name = enumItem.Name;
                existingLookup.Type = enumItem.Type;
                existingLookup.Sort = enumItem.Value;

                if (existingLookup.Name != enumItem.Name || existingLookup.Type != enumItem.Type || existingLookup.Sort != enumItem.Value)
                    existingLookup.ModifiedDate = DateTimeOffset.UtcNow;
            }
        }

        var changesCount = context.SaveChanges();
        if (changesCount > 0)
            Log.Information("[{Prefix}] Successfully seeded {Count} enum lookup items", DEFAULT_LOG_PREFIX, changesCount);
        else
            Log.Information("[{Prefix}] No new enum lookup items to seed", DEFAULT_LOG_PREFIX);

        var SyncSqlScripts = Environment.GetEnvironmentVariable("SyncSqlScripts").ToBool() ?? false;
        if (!SyncSqlScripts)
        {
            Log.Information("[{Prefix}] SyncSqlScripts is disabled, skipping SQL script execution.", DEFAULT_LOG_PREFIX);
            return;
        }

        var currentDirectory = Directory.GetCurrentDirectory();
        var sqlScriptsPath = Path.Combine(currentDirectory, "..", "SoftMax.Students", "SqlScripts");
        sqlScriptsPath = Path.GetFullPath(sqlScriptsPath);

        if (Directory.Exists(sqlScriptsPath))
        {
            var sqlFiles = Directory.GetFiles(sqlScriptsPath, "*.sql", SearchOption.TopDirectoryOnly)
                                   .OrderBy(f => Path.GetFileName(f))
                                   .ToArray();

            Log.Information("[{Prefix}] Found {Count} SQL scripts to execute in path: {SqlScriptsPath}",
                DEFAULT_LOG_PREFIX, sqlFiles.Length, sqlScriptsPath);

            foreach (var sqlFile in sqlFiles)
            {
                var fileName = Path.GetFileName(sqlFile);
                try
                {
                    var sqlContent = File.ReadAllText(sqlFile);

                    if (!string.IsNullOrWhiteSpace(sqlContent))
                    {
                        Log.Information("[{Prefix}] Executing SQL script: {FileName}", DEFAULT_LOG_PREFIX, fileName);

                        // Execute the SQL script
                        var affectedRows = context.Database.ExecuteSqlRaw(sqlContent);

                        Log.Information("[{Prefix}] Successfully executed SQL script: {FileName} (Affected rows: {AffectedRows})",
                            DEFAULT_LOG_PREFIX, fileName, affectedRows);
                    }
                    else
                    {
                        Log.Warning("[{Prefix}] SQL script {FileName} is empty, skipping", DEFAULT_LOG_PREFIX, fileName);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "[{Prefix}] Failed to execute SQL script: {FileName}. Error: {ErrorMessage}",
                        DEFAULT_LOG_PREFIX, fileName, ex.Message);

                    // Continue with other scripts even if one fails
                    continue;
                }
            }
        }
        else
        {
            Log.Warning("[{Prefix}] SQL scripts directory not found: {SqlScriptsPath}", DEFAULT_LOG_PREFIX, sqlScriptsPath);
        }
    });

    Log.Information("Service configuration completed successfully");

    // Build the application
    var app = builder.Build();
    Log.Information("Application built successfully");

    // Seed database if required
    var seedSecurity = ConfigurationHelper.GetBool("SEED_SECURITY_ENABLED", false);

    var seedData = ConfigurationHelper.GetBool("SEED_SECURITY_DATA", false);

    // Configure middleware and endpoints
    app.ConfigureMiddleware().ConfigureRazorComponents<App>(typeof(AdminDbContext).Assembly).ConfigureEndpoints();

    // Map API Controllers
    app.MapControllers();

    // Configure localization
    var languages = ClientStartup.GetLanguages(app);
    app.ConfigureRequestLocalization(languages);

    Log.Information("Application configuration completed successfully");
    Log.Information("Starting application with {LanguageCount} supported languages", languages.Length);

    app.MapGet("/", () =>
    {
        var defaultUrl = ConfigurationHelper.GetString("DEFAULT_URL", "/Modules");
        return Results.Redirect(defaultUrl);
    }).AllowAnonymous();

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.Information("Application shutdown completed");
    Log.CloseAndFlush();
}