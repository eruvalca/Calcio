using System.Net;
using System.Net.Http.Json;

using Calcio.Client.Services.Teams;
using Calcio.Shared.DTOs.Teams;
using Calcio.Shared.Results;

using OneOf.Types;

using RichardSzalay.MockHttp;

using Shouldly;

namespace Calcio.UnitTests.Services.Teams;

public class TeamServiceTests
{
    private const string BaseUrl = "http://localhost";

    #region GetTeamsAsync Tests

    [Fact]
    public async Task GetTeamsAsync_WhenOk_ReturnsList()
    {
        // Arrange
        var clubId = 10L;
        var expectedList = new List<TeamDto>
        {
            new(1, "U12 Red", 2012),
            new(2, "U14 Blue", 2010)
        };

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/api/clubs/{clubId}/teams")
            .Respond(HttpStatusCode.OK, JsonContent.Create(expectedList));

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new TeamService(httpClient);

        // Act
        var result = await service.GetTeamsAsync(clubId, CancellationToken.None);

        // Assert
        result.IsT0.ShouldBeTrue();
        var list = result.AsT0;
        list.Count.ShouldBe(2);
        list[0].Name.ShouldBe("U12 Red");
        list[0].BirthYear.ShouldBe(2012);
        list[1].Name.ShouldBe("U14 Blue");
        list[1].BirthYear.ShouldBe(2010);
    }

    [Fact]
    public async Task GetTeamsAsync_WhenEmptyResponse_ReturnsEmptyList()
    {
        // Arrange
        var clubId = 10L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/api/clubs/{clubId}/teams")
            .Respond(HttpStatusCode.OK, JsonContent.Create(new List<TeamDto>()));

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new TeamService(httpClient);

        // Act
        var result = await service.GetTeamsAsync(clubId, CancellationToken.None);

        // Assert
        result.IsT0.ShouldBeTrue();
        result.AsT0.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetTeamsAsync_WhenUnauthorized_ReturnsUnauthorized()
    {
        // Arrange
        var clubId = 10L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/api/clubs/{clubId}/teams")
            .Respond(HttpStatusCode.Unauthorized);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new TeamService(httpClient);

        // Act
        var result = await service.GetTeamsAsync(clubId, CancellationToken.None);

        // Assert
        result.IsT1.ShouldBeTrue();
        result.AsT1.ShouldBeOfType<Unauthorized>();
    }

    [Fact]
    public async Task GetTeamsAsync_WhenServerError_ReturnsError()
    {
        // Arrange
        var clubId = 10L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/api/clubs/{clubId}/teams")
            .Respond(HttpStatusCode.InternalServerError);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new TeamService(httpClient);

        // Act
        var result = await service.GetTeamsAsync(clubId, CancellationToken.None);

        // Assert
        result.IsT2.ShouldBeTrue();
        result.AsT2.ShouldBeOfType<Error>();
    }

    [Fact]
    public async Task GetTeamsAsync_WhenNullResponse_ReturnsEmptyList()
    {
        // Arrange
        var clubId = 10L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/api/clubs/{clubId}/teams")
            .Respond(HttpStatusCode.OK, new StringContent("null", System.Text.Encoding.UTF8, "application/json"));

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new TeamService(httpClient);

        // Act
        var result = await service.GetTeamsAsync(clubId, CancellationToken.None);

        // Assert
        result.IsT0.ShouldBeTrue();
        result.AsT0.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetTeamsAsync_CorrectlyMapsTeamProperties()
    {
        // Arrange
        var clubId = 10L;
        var expectedTeam = new TeamDto(42, "Test Team", 2015);

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/api/clubs/{clubId}/teams")
            .Respond(HttpStatusCode.OK, JsonContent.Create(new List<TeamDto> { expectedTeam }));

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new TeamService(httpClient);

        // Act
        var result = await service.GetTeamsAsync(clubId, CancellationToken.None);

        // Assert
        result.IsT0.ShouldBeTrue();
        var teams = result.AsT0;
        teams.Count.ShouldBe(1);

        var team = teams[0];
        team.TeamId.ShouldBe(42);
        team.Name.ShouldBe("Test Team");
        team.BirthYear.ShouldBe(2015);
    }

    [Fact]
    public async Task GetTeamsAsync_WhenBirthYearNull_ReturnsTeamWithNullBirthYear()
    {
        // Arrange
        var clubId = 10L;
        var expectedTeam = new TeamDto(42, "Test Team", null);

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/api/clubs/{clubId}/teams")
            .Respond(HttpStatusCode.OK, JsonContent.Create(new List<TeamDto> { expectedTeam }));

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new TeamService(httpClient);

        // Act
        var result = await service.GetTeamsAsync(clubId, CancellationToken.None);

        // Assert
        result.IsT0.ShouldBeTrue();
        var teams = result.AsT0;
        teams.Count.ShouldBe(1);
        teams[0].BirthYear.ShouldBeNull();
    }

    #endregion
}
