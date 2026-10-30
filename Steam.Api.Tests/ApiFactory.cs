using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Steam.Api;

namespace Steam.Api.Tests;

public class ApiFactory : WebApplicationFactory<Program>
{
    public bool EnableDemoPurchases { get; init; }
    private readonly string _database = Path.Combine(Path.GetTempPath(), $"steam-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            var sql = Environment.GetEnvironmentVariable("STEAM_TEST_SQL_CONNECTION");
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["UseSqliteDatabase"] = string.IsNullOrEmpty(sql) ? "true" : "false",
                ["ConnectionStrings:SqliteConnection"] = $"Data Source={_database}",
                ["ConnectionStrings:SqlServerConnection"] = sql,
                ["ApplyMigrations"] = string.IsNullOrEmpty(sql) ? "false" : "true",
                ["JwtSettings:Key"] = "integration_tests_only_signing_key_at_least_32_bytes",
                ["Logging:LogLevel:Default"] = "Warning",
                ["Checkout:EnableDemoPurchases"] = EnableDemoPurchases ? "true" : "false"
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            foreach (var suffix in new[] { "", "-shm", "-wal" })
                File.Delete(_database + suffix);
        }
    }
}
