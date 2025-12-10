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
    private readonly PostgreSqlContainer _databaseContainer = new PostgreSqlBuilder()
        .WithImage("postgres:17.7")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .WithDatabase("calcioDb")
        .Build();

    private readonly AzuriteContainer _azuriteContainer = new AzuriteBuilder()
        .WithImage("mcr.microsoft.com/azure-storage/azurite:3.35.0")
        .Build();

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
            var blobServiceClient = new Azure.Storage.Blobs.BlobServiceClient(connectionString);

            x.AddSingleton(blobServiceClient);
            x.AddSingleton<IBlobStorageService, BlobStorageService>();

            // Register HybridCache for tests
            x.AddHybridCache();
        });

    public async Task InitializeAsync()
        => await Task.WhenAll(
            _databaseContainer.StartAsync(),
            _azuriteContainer.StartAsync());

    public new async Task DisposeAsync()
        => await Task.WhenAll(
            _databaseContainer.StopAsync(),
            _azuriteContainer.StopAsync());

    async ValueTask IAsyncLifetime.InitializeAsync()
        => await Task.WhenAll(
            _databaseContainer.StartAsync(),
            _azuriteContainer.StartAsync());
}
