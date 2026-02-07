using System.Net;

using Calcio.Client.Services.Account;
using Calcio.Shared.Endpoints;
using Calcio.Shared.Results;

using RichardSzalay.MockHttp;

using Shouldly;

namespace Calcio.UnitTests.Services.Account;

public class AccountServiceTests
{
    private const string BaseUrl = "http://localhost";

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
}
