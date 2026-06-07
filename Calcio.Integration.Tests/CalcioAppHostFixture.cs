using Aspire.Hosting;
using Aspire.Hosting.Testing;

namespace Calcio.Integration.Tests;

/// <summary>
/// xUnit shared fixture that boots the Calcio AppHost using <see cref="DistributedApplicationTestingBuilder"/>
/// and tears it down after all tests in the collection have finished.
/// </summary>
/// <remarks>
/// Tests that need a running AppHost should join the <see cref="CalcioAppHostCollection"/> collection
/// and declare <c>IClassFixture&lt;CalcioAppHostFixture&gt;</c> or
/// <c>ICollectionFixture&lt;CalcioAppHostFixture&gt;</c>.
/// </remarks>
public sealed class CalcioAppHostFixture : IAsyncLifetime
{
    /// <summary>
    /// Gets the running <see cref="DistributedApplication"/> under test.
    /// Only valid between <see cref="InitializeAsync"/> and <see cref="DisposeAsync"/>.
    /// </summary>
    public DistributedApplication App { get; private set; } = null!;

    /// <summary>
    /// Gets the service provider for the running application.
    /// </summary>
    public IServiceProvider Services => App.Services;

    /// <summary>
    /// Builds and starts the Calcio distributed application for testing.
    /// </summary>
    /// <returns>A value task that completes when the application is running.</returns>
    public async ValueTask InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync(
                typeof(Projects.Calcio_AppHost),
                ["--environment=Testing"],
                CancellationToken.None);

        App = await builder.BuildAsync(CancellationToken.None);
        await App.StartAsync(CancellationToken.None);
    }

    /// <summary>
    /// Stops and disposes the distributed application after tests complete.
    /// </summary>
    /// <returns>A value task that completes when all resources have been released.</returns>
    public async ValueTask DisposeAsync()
    {
        await App.StopAsync(CancellationToken.None);
        await App.DisposeAsync();
    }
}
