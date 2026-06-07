using System.Net;

using Calcio.Client.Services.Account;
using Calcio.Shared.Endpoints;
using Calcio.Shared.Results;

using RichardSzalay.MockHttp;

using Shouldly;

namespace Calcio.UnitTests.Services.Account;

/// <summary>
/// Contains unit tests for A cc ou nt Se rv ic e behavior.
/// </summary>
public class AccountServiceTests
{
    /// <summary>
    /// Defines the base URL used by mocked HTTP requests in this test class.
    /// </summary>
    private const string BaseUrl = "http://localhost";
    /// <summary>
    /// Verifies the RefreshSignInAsync_WhenNoContent_ReturnsSuccess scenario.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>

    [Fact]
    public async Task RefreshSignInAsync_WhenNoContent_ReturnsSuccess()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{BaseUrl}/{Routes.Account.ForRefreshSignIn()}")
            .Respond(HttpStatusCode.NoContent);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new AccountService(httpClient);

        // Act
        var result = await service.RefreshSignInAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }
    /// <summary>
    /// Verifies the RefreshSignInAsync_WhenNotFound_ReturnsNotFoundProblem scenario.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>

    [Fact]
    public async Task RefreshSignInAsync_WhenNotFound_ReturnsNotFoundProblem()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{BaseUrl}/{Routes.Account.ForRefreshSignIn()}")
            .Respond(HttpStatusCode.NotFound);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new AccountService(httpClient);

        // Act
        var result = await service.RefreshSignInAsync(CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.NotFound);
    }

    /// <summary>
    /// Verifies that a 403 Forbidden response maps to a Forbidden service problem.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task RefreshSignInAsync_WhenForbidden_ReturnsForbiddenProblem()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{BaseUrl}/{Routes.Account.ForRefreshSignIn()}")
            .Respond(HttpStatusCode.Forbidden);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new AccountService(httpClient);

        // Act
        var result = await service.RefreshSignInAsync(CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.Forbidden);
    }

    /// <summary>
    /// Verifies that a 500 Internal Server Error response maps to a ServerError service problem.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task RefreshSignInAsync_WhenServerError_ReturnsServerErrorProblem()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{BaseUrl}/{Routes.Account.ForRefreshSignIn()}")
            .Respond(HttpStatusCode.InternalServerError);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new AccountService(httpClient);

        // Act
        var result = await service.RefreshSignInAsync(CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.ServerError);
    }
}
