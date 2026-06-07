using System.Net;

using Aspire.Hosting.Testing;

using Shouldly;

namespace Calcio.Integration.Tests;

/// <summary>
/// Smoke tests that verify the Calcio AppHost can be built and that expected
/// resources can run through Aspire's testing host.
/// </summary>
/// <param name="fixture">The shared AppHost fixture for resource-driven smoke tests.</param>
[Collection(CalcioAppHostCollection.Name)]
public sealed class AppHostSmokeTests(CalcioAppHostFixture fixture)
{
    /// <summary>
    /// The shared AppHost fixture used by smoke tests.
    /// </summary>
    private readonly CalcioAppHostFixture _fixture = fixture;

    /// <summary>
    /// Verifies that the full Calcio resource starts under Aspire and serves its root page.
    /// </summary>
    [Fact]
    public async Task AppHost_StartsCalcioResource_ReturnsOk()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await _fixture.App.ResourceNotifications
            .WaitForResourceHealthyAsync("calcio", cancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(60), cancellationToken);

        using var httpClient = _fixture.App.CreateHttpClient("calcio");
        using var response = await httpClient.GetAsync("/", cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
