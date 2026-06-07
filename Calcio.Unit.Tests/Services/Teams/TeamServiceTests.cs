using System.Net;
using System.Net.Http.Json;

using Calcio.Client.Services.Teams;
using Calcio.Shared.DTOs.Teams;
using Calcio.Shared.Endpoints;
using Calcio.Shared.Results;

using RichardSzalay.MockHttp;

using Shouldly;

namespace Calcio.UnitTests.Services.Teams;

/// <summary>
/// Contains unit tests for T ea mS er vi ce behavior.
/// </summary>
public class TeamServiceTests
{
    /// <summary>
    /// Defines the base URL used by mocked HTTP requests in this test class.
    /// </summary>
    private const string BaseUrl = "http://localhost";

    #region GetTeamsAsync Tests
    /// <summary>
    /// Verifies the GetTeamsAsync_WhenOk_ReturnsList scenario.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>

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
    /// <summary>
    /// Verifies the GetTeamsAsync_WhenEmptyResponse_ReturnsEmptyList scenario.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>

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
    /// <summary>
    /// Verifies the GetTeamsAsync_WhenForbidden_ReturnsForbiddenProblem scenario.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>

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
    /// <summary>
    /// Verifies the GetTeamsAsync_WhenServerError_ReturnsServerErrorProblem scenario.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>

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
    /// <summary>
    /// Verifies the GetTeamsAsync_WhenNullResponse_ReturnsEmptyList scenario.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>

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
    /// <summary>
    /// Verifies the GetTeamsAsync_CorrectlyMapsTeamProperties scenario.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>

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
    /// <summary>
    /// Verifies the CreateTeamAsync_WhenCreated_ReturnsSuccess scenario.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>

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
    /// <summary>
    /// Verifies the CreateTeamAsync_WhenForbidden_ReturnsForbiddenProblem scenario.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>

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
    /// <summary>
    /// Verifies the CreateTeamAsync_WhenConflict_ReturnsConflictProblem scenario.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>

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
    /// <summary>
    /// Verifies the CreateTeamAsync_WhenServerError_ReturnsServerErrorProblem scenario.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>

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
    /// <summary>
    /// Verifies the CreateTeamAsync_WithGraduationYear_SendsCorrectPayload scenario.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>

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
        mockHttp.VerifyNoOutstandingExpectation();
    }

    #endregion
}
