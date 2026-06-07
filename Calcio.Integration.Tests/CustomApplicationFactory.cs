using Aspire.Hosting;
using Aspire.Hosting.Testing;

using Calcio.AppHost;
using Calcio.Data.Contexts;
using Calcio.Data.Contexts.Base;
using Calcio.Data.Interceptors;
using Calcio.Shared.Services.BlobStorage;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Npgsql;

using NSubstitute;

namespace Calcio.Integration.Tests;

/// <summary>
/// Provides a test server that uses Aspire-managed infrastructure (PostgreSQL and Azure Blob
/// Storage emulator) instead of direct container orchestration, while keeping the Calcio application hosted
/// in-process via <see cref="WebApplicationFactory{TEntryPoint}"/> so tests can resolve
/// services directly from the DI container.
/// </summary>
/// <remarks>
/// On <see cref="IAsyncLifetime.InitializeAsync"/>, this fixture starts the Aspire AppHost
/// in <c>Testing</c> mode minus the <c>calcio</c> project resource, which is hosted
/// in-process, then creates a uniquely named database so each test class receives an isolated
/// schema. The database is dropped in <see cref="DisposeAsync"/> to avoid accumulation.
/// Blob storage is replaced with a mock by default; override <see cref="ConfigureWebHost"/>
/// in <see cref="BlobStorageApplicationFactory"/> to use the real Azurite-backed client.
/// </remarks>
public class CustomApplicationFactory : WebApplicationFactory<ICalcioMarker>, IAsyncLifetime
{
    /// <summary>
    /// The running Aspire distributed application that manages the PostgreSQL and Azurite
    /// infrastructure containers for the duration of the test class.
    /// </summary>
    private DistributedApplication _aspireApp = null!;

    /// <summary>
    /// The unique database name allocated for this fixture instance.  Stored so the database
    /// can be dropped during <see cref="DisposeAsync"/>.
    /// </summary>
    private string _uniqueDbName = null!;

    /// <summary>
    /// The connection string pointing to the PostgreSQL server (not to any database) that is
    /// used when dropping the unique test database during disposal.
    /// </summary>
    private string _serverConnectionString = null!;

    /// <summary>
    /// Gets the PostgreSQL connection string for the unique per-fixture database, available
    /// after <see cref="IAsyncLifetime.InitializeAsync"/> completes.
    /// </summary>
    protected string DbConnectionString { get; private set; } = null!;

    /// <summary>
    /// Gets the Azure Blob Storage connection string retrieved from the Aspire-managed
    /// <c>blobs</c> emulator resource, available after
    /// <see cref="IAsyncLifetime.InitializeAsync"/> completes.
    /// </summary>
    protected string BlobConnectionString { get; private set; } = null!;

    /// <summary>
    /// Replaces production database and blob-storage registrations with Aspire-provided
    /// connection strings and test implementations.  Override in a derived class to swap
    /// the mock blob-storage registration for a real one.
    /// </summary>
    /// <param name="builder">The web host builder for configuring test services.</param>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
        => builder.ConfigureTestServices(services =>
        {
            services.Remove(services.Single(a => typeof(DbContextOptions<BaseDbContext>) == a.ServiceType));
            services.AddDbContext<BaseDbContext>((sp, options) =>
            {
                options.UseNpgsql(DbConnectionString);
                options.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
            }, ServiceLifetime.Scoped);

            services.Remove(services.Single(a => typeof(DbContextOptions<ReadWriteDbContext>) == a.ServiceType));
            services.AddDbContext<ReadWriteDbContext>((sp, options) =>
            {
                options.UseNpgsql(DbConnectionString);
                options.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
            }, ServiceLifetime.Scoped);

            services.Remove(services.Single(a => typeof(DbContextOptions<ReadOnlyDbContext>) == a.ServiceType));
            services.AddDbContext<ReadOnlyDbContext>((sp, options) =>
                options.UseNpgsql(DbConnectionString), ServiceLifetime.Scoped);

            // Default: mock blob storage so DB-focused tests do not require real blob operations.
            // BlobStorageApplicationFactory overrides this with a real Azurite-backed client.
            var blobStorageService = Substitute.For<IBlobStorageService>();

            blobStorageService
                .GetSasUrl(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TimeSpan>())
                .Returns(callInfo => new Uri(
                    $"https://test.blob.core.windows.net/{callInfo.ArgAt<string>(0)}/{callInfo.ArgAt<string>(1)}?sas=token"));

            blobStorageService
                .DeleteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(true);

            blobStorageService
                .ExistsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(true);

            blobStorageService
                .UploadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(callInfo => new Uri(
                    $"https://test.blob.core.windows.net/{callInfo.ArgAt<string>(0)}/{callInfo.ArgAt<string>(1)}"));

            blobStorageService
                .DeleteByPrefixAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(0);

            services.AddSingleton(blobStorageService);

            services.AddHybridCache();
        });

    /// <summary>
    /// Starts the Aspire AppHost's infrastructure resources, allocates a unique per-fixture
    /// PostgreSQL database to prevent cross-class state pollution, and retrieves connection
    /// strings that <see cref="ConfigureWebHost"/> uses to configure the in-process server.
    /// </summary>
    /// <returns>A task that completes when the AppHost infrastructure is ready.</returns>
    public async Task InitializeAsync()
    {
        var appBuilder = await DistributedApplicationTestingBuilder.CreateAsync(
            typeof(ICalcioAppHostMarker),
            ["--environment=Testing"],
            CancellationToken.None);

        // Remove the Calcio project resource: the app is hosted in-process by
        // WebApplicationFactory, so we only need the infrastructure containers.
        var calcioResource = appBuilder.Resources.FirstOrDefault(r => r.Name == "calcio");
        if (calcioResource is not null)
        {
            appBuilder.Resources.Remove(calcioResource);
        }

        _aspireApp = await appBuilder.BuildAsync(CancellationToken.None);
        await _aspireApp.StartAsync(CancellationToken.None);

        var rawDbConnectionString = await _aspireApp.GetConnectionStringAsync("calcioDb")
            ?? throw new InvalidOperationException(
                "The 'calcioDb' connection string was not available from the Aspire AppHost. " +
                "Ensure the postgres resource started successfully.");

        // Allocate a unique database for this fixture instance so parallel test classes do
        // not share state when xUnit runs them concurrently.
        _uniqueDbName = $"calcio_test_{Guid.NewGuid():N}";
        var connStrBuilder = new NpgsqlConnectionStringBuilder(rawDbConnectionString)
        {
            Database = _uniqueDbName
        };
        DbConnectionString = connStrBuilder.ConnectionString;

        // Keep a server-level connection string (no specific database) for cleanup.
        connStrBuilder.Database = "postgres";
        _serverConnectionString = connStrBuilder.ConnectionString;

        BlobConnectionString = await _aspireApp.GetConnectionStringAsync("blobs")
            ?? throw new InvalidOperationException(
                "The 'blobs' connection string was not available from the Aspire AppHost. " +
                "Ensure the Azure Storage emulator started successfully.");
    }

    /// <summary>
    /// Drops the unique test database created for this fixture instance, then disposes the
    /// Aspire distributed application.
    /// </summary>
    /// <returns>A task that completes when cleanup has finished.</returns>
    public new async Task DisposeAsync()
    {
        // Drop the unique test database so it does not accumulate between test runs.
        if (!string.IsNullOrEmpty(_serverConnectionString) && !string.IsNullOrEmpty(_uniqueDbName))
        {
            await using var connection = new NpgsqlConnection(_serverConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();

            // Terminate active connections before dropping so the command does not block.
            command.CommandText = "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = @databaseName AND pid <> pg_backend_pid();";
            command.Parameters.AddWithValue("databaseName", _uniqueDbName);
            await command.ExecuteNonQueryAsync();

            command.Parameters.Clear();
            var quotedDatabaseName = $"\"{_uniqueDbName.Replace("\"", "\"\"")}\"";
            command.CommandText = $"DROP DATABASE IF EXISTS {quotedDatabaseName};";
            await command.ExecuteNonQueryAsync();
        }

        await _aspireApp.DisposeAsync();
    }

    /// <summary>
    /// Satisfies <see cref="IAsyncLifetime.InitializeAsync"/> for xUnit v3, delegating to
    /// the public <see cref="InitializeAsync"/> method.
    /// </summary>
    /// <returns>A value task that completes when initialization has finished.</returns>
    async ValueTask IAsyncLifetime.InitializeAsync() => await InitializeAsync();
}
