using Calcio.Data.Contexts;
using Calcio.Data.Contexts.Base;
using Calcio.Data.Interceptors;
using Calcio.Shared.Services.BlobStorage;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using Testcontainers.PostgreSql;

namespace Calcio.IntegrationTests;

/// <summary>
/// Provides a test server configured with PostgreSQL and mocked blob storage dependencies.
/// </summary>
public class CustomApplicationFactory : WebApplicationFactory<ICalcioMarker>, IAsyncLifetime
{
    /// <summary>
    /// Hosts the PostgreSQL test container used by integration tests.
    /// </summary>
    private readonly PostgreSqlContainer _databaseContainer = new PostgreSqlBuilder("postgres:17.7")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .WithDatabase("calcioDb")
        .Build();

    /// <summary>
    /// Replaces production infrastructure with test implementations for integration tests.
    /// </summary>
    /// <param name="builder">The web host builder for configuring test services.</param>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
        => builder.ConfigureTestServices(x =>
        {
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

            // Register mock blob storage service for tests
            var blobStorageService = Substitute.For<IBlobStorageService>();

            // Configure default return values
            blobStorageService.GetSasUrl(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TimeSpan>())
                .Returns(callInfo => new Uri($"https://test.blob.core.windows.net/{callInfo.ArgAt<string>(0)}/{callInfo.ArgAt<string>(1)}?sas=token"));

            blobStorageService.DeleteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(true);

            blobStorageService.ExistsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(true);

            blobStorageService.UploadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(callInfo => new Uri($"https://test.blob.core.windows.net/{callInfo.ArgAt<string>(0)}/{callInfo.ArgAt<string>(1)}"));

            blobStorageService.DeleteByPrefixAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(0);

            x.AddSingleton(blobStorageService);

            // Register HybridCache for tests
            x.AddHybridCache();
        });

    /// <summary>
    /// Starts the PostgreSQL container before tests execute.
    /// </summary>
    /// <returns>A task that completes when the container is running.</returns>
    public async Task InitializeAsync() => await _databaseContainer.StartAsync();

    /// <summary>
    /// Stops the PostgreSQL container after tests complete.
    /// </summary>
    /// <returns>A task that completes when the container is stopped.</returns>
    public new async Task DisposeAsync() => await _databaseContainer.StopAsync();

    /// <summary>
    /// Starts asynchronous lifetime resources required by xUnit.
    /// </summary>
    /// <returns>A value task that completes when lifetime initialization finishes.</returns>
    async ValueTask IAsyncLifetime.InitializeAsync() => await _databaseContainer.StartAsync();
}
