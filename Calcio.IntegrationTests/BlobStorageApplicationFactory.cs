using Calcio.Data.Contexts;
using Calcio.Data.Contexts.Base;
using Calcio.Data.Interceptors;
using Calcio.Services.BlobStorage;
using Calcio.Shared.Services.BlobStorage;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Testcontainers.Azurite;
using Testcontainers.PostgreSql;

namespace Calcio.IntegrationTests;

/// <summary>
/// Application factory for full integration tests that use actual Azure Blob Storage emulator (Azurite).
/// Use this factory when testing actual blob upload/download operations.
/// </summary>
public class BlobStorageApplicationFactory : WebApplicationFactory<ICalcioMarker>, IAsyncLifetime
{
    /// <summary>
    /// Hosts the PostgreSQL test container used by the integration test server.
    /// </summary>
    private readonly PostgreSqlContainer _databaseContainer = new PostgreSqlBuilder("postgres:17.7")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .WithDatabase("calcioDb")
        .Build();

    /// <summary>
    /// Hosts the Azurite container used to emulate Azure Blob Storage in tests.
    /// </summary>
    private readonly AzuriteContainer _azuriteContainer = new AzuriteBuilder("mcr.microsoft.com/azure-storage/azurite:3.35.0")
        .Build();

    /// <summary>
    /// Reconfigures the test host to use containerized database and blob storage dependencies.
    /// </summary>
    /// <param name="builder">The web host builder for configuring test services.</param>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
        => builder.ConfigureTestServices(x =>
        {
            // Database setup (same as CustomApplicationFactory)
            x.Remove(x.Single(a => typeof(DbContextOptions<BaseDbContext>) == a.ServiceType));
            x.AddDbContext<BaseDbContext>((sp, a) =>
            {
                a.UseNpgsql(_databaseContainer.GetConnectionString());
                a.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
            }, ServiceLifetime.Scoped);

            x.Remove(x.Single(a => typeof(DbContextOptions<ReadWriteDbContext>) == a.ServiceType));
            x.AddDbContext<ReadWriteDbContext>((sp, a) =>
            {
                a.UseNpgsql(_databaseContainer.GetConnectionString());
                a.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
            }, ServiceLifetime.Scoped);

            x.Remove(x.Single(a => typeof(DbContextOptions<ReadOnlyDbContext>) == a.ServiceType));
            x.AddDbContext<ReadOnlyDbContext>((sp, a)
                => a.UseNpgsql(_databaseContainer.GetConnectionString()), ServiceLifetime.Scoped);

            // Replace blob storage with real Azurite-backed implementation
            x.RemoveAll<IBlobStorageService>();

            var connectionString = _azuriteContainer.GetConnectionString();
            var blobServiceClient = new Azure.Storage.Blobs.BlobServiceClient(
                connectionString,
                new Azure.Storage.Blobs.BlobClientOptions(Azure.Storage.Blobs.BlobClientOptions.ServiceVersion.V2025_11_05));

            x.AddSingleton(blobServiceClient);
            x.AddSingleton<IBlobStorageService, BlobStorageService>();

            // Register HybridCache for tests
            x.AddHybridCache();
        });

    /// <summary>
    /// Starts database and Azurite containers before tests run.
    /// </summary>
    /// <returns>A task that completes when both containers are running.</returns>
    public async Task InitializeAsync()
        => await Task.WhenAll(
            _databaseContainer.StartAsync(),
            _azuriteContainer.StartAsync());

    /// <summary>
    /// Stops database and Azurite containers after tests complete.
    /// </summary>
    /// <returns>A task that completes when both containers are stopped.</returns>
    public new async Task DisposeAsync()
        => await Task.WhenAll(
            _databaseContainer.StopAsync(),
            _azuriteContainer.StopAsync());

    /// <summary>
    /// Starts asynchronous lifetime resources required by xUnit.
    /// </summary>
    /// <returns>A value task that completes when lifetime initialization finishes.</returns>
    async ValueTask IAsyncLifetime.InitializeAsync()
        => await Task.WhenAll(
            _databaseContainer.StartAsync(),
            _azuriteContainer.StartAsync());
}
