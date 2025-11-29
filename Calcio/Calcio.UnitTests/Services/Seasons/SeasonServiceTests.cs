using System.Net;
using System.Net.Http.Json;

using Calcio.Client.Services.Seasons;
using Calcio.Shared.DTOs.Seasons;
using Calcio.Shared.Results;

using RichardSzalay.MockHttp;

using Shouldly;

namespace Calcio.UnitTests.Services.Seasons;

public class SeasonServiceTests
{
    private const string BaseUrl = "http://localhost";

    #region GetSeasonsAsync Tests

    [Fact]
    public async Task GetSeasonsAsync_WhenOk_ReturnsList()
    {
        // Arrange
        var clubId = 10L;
        var expectedList = new List<SeasonDto>
        {
            new(1, "2024-2025", new DateOnly(2024, 8, 1), new DateOnly(2025, 5, 31), true),
            new(2, "2025-2026", new DateOnly(2025, 8, 1), null, false)
        };

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/api/clubs/{clubId}/seasons")
            .Respond(HttpStatusCode.OK, JsonContent.Create(expectedList));

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new SeasonsService(httpClient);

        // Act
        var result = await service.GetSeasonsAsync(clubId, CancellationToken.None);

        // Assert
        result.IsT0.ShouldBeTrue();
        var list = result.AsT0;
        list.Count.ShouldBe(2);
        list[0].Name.ShouldBe("2024-2025");
        list[0].IsComplete.ShouldBeTrue();
        list[1].Name.ShouldBe("2025-2026");
        list[1].IsComplete.ShouldBeFalse();
    }

    [Fact]
    public async Task GetSeasonsAsync_WhenEmptyResponse_ReturnsEmptyList()
    {
        // Arrange
        var clubId = 10L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/api/clubs/{clubId}/seasons")
            .Respond(HttpStatusCode.OK, JsonContent.Create(new List<SeasonDto>()));

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new SeasonsService(httpClient);

        // Act
        var result = await service.GetSeasonsAsync(clubId, CancellationToken.None);

        // Assert
        result.IsT0.ShouldBeTrue();
        result.AsT0.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetSeasonsAsync_WhenForbidden_ReturnsForbiddenProblem()
    {
        // Arrange
        var clubId = 10L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/api/clubs/{clubId}/seasons")
            .Respond(HttpStatusCode.Forbidden);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new SeasonsService(httpClient);

        // Act
        var result = await service.GetSeasonsAsync(clubId, CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.Forbidden);
    }

    [Fact]
    public async Task GetSeasonsAsync_WhenServerError_ReturnsServerErrorProblem()
    {
        // Arrange
        var clubId = 10L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/api/clubs/{clubId}/seasons")
            .Respond(HttpStatusCode.InternalServerError);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new SeasonsService(httpClient);

        // Act
        var result = await service.GetSeasonsAsync(clubId, CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.ServerError);
    }

    [Fact]
    public async Task GetSeasonsAsync_WhenNullResponse_ReturnsEmptyList()
    {
        // Arrange
        var clubId = 10L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/api/clubs/{clubId}/seasons")
            .Respond(HttpStatusCode.OK, new StringContent("null", System.Text.Encoding.UTF8, "application/json"));

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new SeasonsService(httpClient);

        // Act
        var result = await service.GetSeasonsAsync(clubId, CancellationToken.None);

        // Assert
        result.IsT0.ShouldBeTrue();
        result.AsT0.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetSeasonsAsync_CorrectlyMapsSeasonProperties()
    {
        // Arrange
        var clubId = 10L;
        var startDate = new DateOnly(2024, 8, 15);
        var endDate = new DateOnly(2025, 5, 20);
        var expectedSeason = new SeasonDto(42, "Test Season", startDate, endDate, true);

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/api/clubs/{clubId}/seasons")
            .Respond(HttpStatusCode.OK, JsonContent.Create(new List<SeasonDto> { expectedSeason }));

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new SeasonsService(httpClient);

        // Act
        var result = await service.GetSeasonsAsync(clubId, CancellationToken.None);

        // Assert
        result.IsT0.ShouldBeTrue();
        var seasons = result.AsT0;
        seasons.Count.ShouldBe(1);

        var season = seasons[0];
        season.SeasonId.ShouldBe(42);
        season.Name.ShouldBe("Test Season");
        season.StartDate.ShouldBe(startDate);
        season.EndDate.ShouldBe(endDate);
        season.IsComplete.ShouldBeTrue();
    }

    #endregion
}
