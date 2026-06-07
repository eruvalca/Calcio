using Azure.Storage.Blobs;

using Calcio.Services.BlobStorage;
using Calcio.Shared.Services.BlobStorage;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Calcio.Integration.Tests;

/// <summary>
/// Extends <see cref="CustomApplicationFactory"/> for integration tests that require real
/// Azure Blob Storage operations backed by the Aspire-managed Azurite emulator.
/// </summary>
/// <remarks>
/// The base class starts the Aspire AppHost infrastructure (PostgreSQL + Azurite) and
/// exposes connection strings.  This subclass overrides blob-storage registrations so that
/// the in-process test server uses a real <see cref="BlobServiceClient"/> rather than the
/// mock registered by the base class.
/// </remarks>
public class BlobStorageApplicationFactory : CustomApplicationFactory
{
    /// <summary>
    /// Overrides the blob-storage registrations from the base class, replacing the mock
    /// <see cref="IBlobStorageService"/> with a real implementation backed by the
    /// Aspire-managed Azurite emulator.
    /// </summary>
    /// <param name="builder">The web host builder for configuring test services.</param>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Let the base class configure the database and register the mock blob storage.
        base.ConfigureWebHost(builder);

        // Replace the mock blob storage with a real Azurite-backed implementation.
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IBlobStorageService>();

            var blobServiceClient = new BlobServiceClient(
                BlobConnectionString,
                new BlobClientOptions(BlobClientOptions.ServiceVersion.V2025_11_05));

            services.AddSingleton(blobServiceClient);
            services.AddSingleton<IBlobStorageService, BlobStorageService>();
        });
    }
}

