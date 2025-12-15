using System.Net;
using System.Net.Http.Json;

using Calcio.Client.Services.Teams;
using Calcio.Shared.DTOs.Teams;
using Calcio.Shared.Endpoints;
using Calcio.Shared.Results;

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
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/{Routes.Teams.ForClub(clubId)}")
            .Respond(HttpStatusCode.OK, JsonContent.Create(expectedList));

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new TeamsService(httpClient);

        // Act
        var result = await service.GetTeamsAsync(clubId, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var list = result.Value;
        list.Count.ShouldBe(2);
        list[0].Name.ShouldBe("U12 Red");
        list[0].GraduationYear.ShouldBe(2012);
        list[1].Name.ShouldBe("U14 Blue");
        list[1].GraduationYear.ShouldBe(2010);
    }

    [Fact]
    public async Task GetTeamsAsync_WhenEmptyResponse_ReturnsEmptyList()
    {
        // Arrange
        var clubId = 10L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/{Routes.Teams.ForClub(clubId)}")
            .Respond(HttpStatusCode.OK, JsonContent.Create(new List<TeamDto>()));

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new TeamsService(httpClient);

        // Act
        var result = await service.GetTeamsAsync(clubId, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetTeamsAsync_WhenForbidden_ReturnsForbiddenProblem()
    {
        // Arrange
        var clubId = 10L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/{Routes.Teams.ForClub(clubId)}")
            .Respond(HttpStatusCode.Forbidden);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new TeamsService(httpClient);

        // Act
        var result = await service.GetTeamsAsync(clubId, CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.Forbidden);
    }

    [Fact]
    public async Task GetTeamsAsync_WhenServerError_ReturnsServerErrorProblem()
    {
        // Arrange
        var clubId = 10L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/{Routes.Teams.ForClub(clubId)}")
            .Respond(HttpStatusCode.InternalServerError);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new TeamsService(httpClient);

        // Act
        var result = await service.GetTeamsAsync(clubId, CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.ServerError);
    }

    [Fact]
    public async Task GetTeamsAsync_WhenNullResponse_ReturnsEmptyList()
    {
        // Arrange
        var clubId = 10L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/{Routes.Teams.ForClub(clubId)}")
            .Respond(HttpStatusCode.OK, new StringContent("null", System.Text.Encoding.UTF8, "application/json"));

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new TeamsService(httpClient);

        // Act
        var result = await service.GetTeamsAsync(clubId, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetTeamsAsync_CorrectlyMapsTeamProperties()
    {
        // Arrange
        var clubId = 10L;
        var expectedTeam = new TeamDto(42, "Test Team", 2015);

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/{Routes.Teams.ForClub(clubId)}")
            .Respond(HttpStatusCode.OK, JsonContent.Create(new List<TeamDto> { expectedTeam }));

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new TeamsService(httpClient);

        // Act
        var result = await service.GetTeamsAsync(clubId, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var teams = result.Value;
        teams.Count.ShouldBe(1);

        var team = teams[0];
        team.TeamId.ShouldBe(42);
        team.Name.ShouldBe("Test Team");
        team.GraduationYear.ShouldBe(2015);
    }

    #endregion

    #region CreateTeamAsync Tests

    [Fact]
    public async Task CreateTeamAsync_WhenCreated_ReturnsSuccess()
    {
        // Arrange
        var clubId = 10L;
        var dto = new CreateTeamDto("U12 Red", 2030);

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{BaseUrl}/{Routes.Teams.ForClub(clubId)}")
            .Respond(HttpStatusCode.Created);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new TeamsService(httpClient);

        // Act
        var result = await service.CreateTeamAsync(clubId, dto, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task CreateTeamAsync_WhenForbidden_ReturnsForbiddenProblem()
    {
        // Arrange
        var clubId = 10L;
        var dto = new CreateTeamDto("U12 Red", 2030);

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{BaseUrl}/{Routes.Teams.ForClub(clubId)}")
            .Respond(HttpStatusCode.Forbidden);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new TeamsService(httpClient);

        // Act
        var result = await service.CreateTeamAsync(clubId, dto, CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.Forbidden);
    }

    [Fact]
    public async Task CreateTeamAsync_WhenConflict_ReturnsConflictProblem()
    {
        // Arrange
        var clubId = 10L;
        var dto = new CreateTeamDto("U12 Red", 2030);

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{BaseUrl}/{Routes.Teams.ForClub(clubId)}")
            .Respond(HttpStatusCode.Conflict);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new TeamsService(httpClient);

        // Act
        var result = await service.CreateTeamAsync(clubId, dto, CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.Conflict);
    }

    [Fact]
    public async Task CreateTeamAsync_WhenServerError_ReturnsServerErrorProblem()
    {
        // Arrange
        var clubId = 10L;
        var dto = new CreateTeamDto("U12 Red", 2030);

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{BaseUrl}/{Routes.Teams.ForClub(clubId)}")
            .Respond(HttpStatusCode.InternalServerError);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new TeamsService(httpClient);

        // Act
        var result = await service.CreateTeamAsync(clubId, dto, CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.ServerError);
    }

    [Fact]
    public async Task CreateTeamAsync_WithGraduationYear_SendsCorrectPayload()
    {
        // Arrange
        var clubId = 10L;
        var graduationYear = 2030;
        var dto = new CreateTeamDto("U12 Red", graduationYear);

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{BaseUrl}/{Routes.Teams.ForClub(clubId)}")
            .With(request =>
            {
                var content = request.Content?.ReadAsStringAsync().Result;
                return content is not null &&
                       content.Contains("U12 Red") &&
                       content.Contains(graduationYear.ToString());
            })
            .Respond(HttpStatusCode.Created);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new TeamsService(httpClient);

        // Act
        var result = await service.CreateTeamAsync(clubId, dto, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    #endregion
}
