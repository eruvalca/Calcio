using Bunit;

using Calcio.Shared.DTOs.Teams;
using Calcio.Shared.Results;
using Calcio.Shared.Services.Teams;
using Calcio.UI.Components.Teams.Shared;

using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using OneOf.Types;

using Shouldly;

namespace Calcio.UnitTests.Components.Teams.Shared;

/// <summary>
/// Contains unit tests for T ea ms Gr id behavior.
/// </summary>
public sealed class TeamsGridTests : BunitContext
{
    /// <summary>
    /// Stores test state for m oc kt ea ms se rv ic e.
    /// </summary>
    private readonly ITeamsService _mockTeamsService;
    /// <summary>
    /// Initializes a new instance of the <see cref="TeamsGridTests"/> class.
    /// </summary>

    public TeamsGridTests()
    {
        _mockTeamsService = Substitute.For<ITeamsService>();
        Services.AddSingleton(_mockTeamsService);

        // QuickGrid uses JS interop for virtualization
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    #region Helper Methods

    /// <summary>
    /// Creates a collection of team DTOs for rendering and filter test cases.
    /// </summary>
    /// <param name="count">The number of teams to generate.</param>
    /// <returns>A generated list of <see cref="TeamDto"/> values.</returns>
    private static List<TeamDto> CreateTestTeams(int count = 2)
        => [
            .. Enumerable.Range(1, count)
                .Select(i => new TeamDto(
                    TeamId: i,
                    Name: $"Team {i}",
                    GraduationYear: DateTime.Today.Year + i))
        ];

    /// <summary>
    /// Renders the <see cref="TeamsGrid"/> component with the provided test parameters.
    /// </summary>
    /// <param name="clubId">The club identifier supplied to the component.</param>
    /// <param name="teams">The list of teams passed to the component.</param>
    /// <returns>The rendered component under test.</returns>
    private IRenderedComponent<TeamsGrid> RenderGrid(
        long clubId = 100,
        List<TeamDto>? teams = null)
        => Render<TeamsGrid>(parameters => parameters
            .Add(p => p.ClubId, clubId)
            .Add(p => p.Teams, teams ?? []));

    #endregion

    #region Initial Rendering Tests
    /// <summary>
    /// Verifies the WhenNoTeams_ShouldDisplayEmptyMessage scenario.
    /// </summary>

    [Fact]
    public void WhenNoTeams_ShouldDisplayEmptyMessage()
    {
        // Arrange & Act
        var cut = RenderGrid(teams: []);

        // Assert
        var emptyMessage = cut.Find(".text-muted");
        emptyMessage.TextContent.ShouldBe("No teams found.");

        cut.FindAll("table").ShouldBeEmpty();
    }
    /// <summary>
    /// Verifies the WhenTeamsExist_ShouldDisplayGrid scenario.
    /// </summary>

    [Fact]
    public void WhenTeamsExist_ShouldDisplayGrid()
    {
        // Arrange
        var teams = CreateTestTeams(2);

        // Act
        var cut = RenderGrid(teams: teams);

        // Assert
        cut.Find("table").ShouldNotBeNull();

        cut.Markup.ShouldContain("Team 1");
        cut.Markup.ShouldContain("Team 2");
    }

    #endregion

    #region Create Form Toggle Tests
    /// <summary>
    /// Verifies the WhenInitialRender_ShouldDisplayNewTeamButton scenario.
    /// </summary>

    [Fact]
    public void WhenInitialRender_ShouldDisplayNewTeamButton()
    {
        // Arrange & Act
        var cut = RenderGrid(teams: []);

        // Assert
        var button = cut.Find("button.btn-primary");
        button.TextContent.ShouldContain("New Team");
    }
    /// <summary>
    /// Verifies the WhenNewTeamClicked_ShouldShowCreateForm scenario.
    /// </summary>

    [Fact]
    public void WhenNewTeamClicked_ShouldShowCreateForm()
    {
        // Arrange
        var cut = RenderGrid(teams: []);

        // Act
        cut.Find("button.btn-primary").Click();

        // Assert
        cut.Find(".card-body form").ShouldNotBeNull();
        cut.Find("#teamName").ShouldNotBeNull();
        cut.Find("#graduationYear").ShouldNotBeNull();
    }
    /// <summary>
    /// Verifies the WhenCreateFormShown_ShouldHideNewTeamButton scenario.
    /// </summary>

    [Fact]
    public void WhenCreateFormShown_ShouldHideNewTeamButton()
    {
        // Arrange
        var cut = RenderGrid(teams: []);

        // Act
        cut.Find("button.btn-primary").Click();

        // Assert
        cut.FindAll(".card-header button.btn-primary").ShouldBeEmpty();
    }
    /// <summary>
    /// Verifies the WhenCancelClicked_ShouldHideCreateForm scenario.
    /// </summary>

    [Fact]
    public void WhenCancelClicked_ShouldHideCreateForm()
    {
        // Arrange
        var cut = RenderGrid(teams: []);
        cut.Find("button.btn-primary").Click();

        // Act
        cut.Find("button.btn-outline-secondary").Click();

        // Assert
        cut.FindAll(".card-body.border-bottom form").ShouldBeEmpty();
        cut.Find(".card-header button.btn-primary").TextContent.ShouldContain("New Team");
    }

    #endregion

    #region Create Team Tests
    /// <summary>
    /// Verifies the WhenCreateFormSubmitted_ShouldCallService scenario.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>

    [Fact]
    public async Task WhenCreateFormSubmitted_ShouldCallService()
    {
        // Arrange
        var cut = RenderGrid(clubId: 100, teams: []);

        _mockTeamsService
            .CreateTeamAsync(100, Arg.Any<CreateTeamDto>(), Arg.Any<CancellationToken>())
            .Returns(new ServiceResult<Success>(new Success()));

        cut.Find("button.btn-primary").Click();

        // Act
        var teamNameInput = cut.Find("#teamName");
        var graduationYearInput = cut.Find("#graduationYear");

        await teamNameInput.ChangeAsync(new() { Value = "U12 Red" });
        await graduationYearInput.ChangeAsync(new() { Value = (DateTime.Today.Year + 5).ToString() });

        await cut.Find(".card-body form").SubmitAsync();

        // Assert
        await _mockTeamsService.Received(1)
            .CreateTeamAsync(100, Arg.Is<CreateTeamDto>(dto =>
                dto.Name == "U12 Red" &&
                dto.GraduationYear == DateTime.Today.Year + 5), Arg.Any<CancellationToken>());
    }
    /// <summary>
    /// Verifies the WhenCreateReturnsForbidden_ShouldDisplayError scenario.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>

    [Fact]
    public async Task WhenCreateReturnsForbidden_ShouldDisplayError()
    {
        // Arrange
        var cut = RenderGrid(clubId: 100, teams: []);

        _mockTeamsService
            .CreateTeamAsync(100, Arg.Any<CreateTeamDto>(), Arg.Any<CancellationToken>())
            .Returns(new ServiceResult<Success>(ServiceProblem.Forbidden()));

        cut.Find("button.btn-primary").Click();

        // Act
        await cut.Find("#teamName").ChangeAsync(new() { Value = "U12 Red" });
        await cut.Find("#graduationYear").ChangeAsync(new() { Value = (DateTime.Today.Year + 5).ToString() });
        await cut.Find(".card-body form").SubmitAsync();

        // Assert
        cut.WaitForAssertion(() =>
        {
            var alert = cut.Find(".alert-danger");
            alert.TextContent.ShouldContain("not authorized");
        });
    }
    /// <summary>
    /// Verifies the WhenCreateReturnsConflict_ShouldDisplayConflictError scenario.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>

    [Fact]
    public async Task WhenCreateReturnsConflict_ShouldDisplayConflictError()
    {
        // Arrange
        var cut = RenderGrid(clubId: 100, teams: []);

        _mockTeamsService
            .CreateTeamAsync(100, Arg.Any<CreateTeamDto>(), Arg.Any<CancellationToken>())
            .Returns(new ServiceResult<Success>(ServiceProblem.Conflict()));

        cut.Find("button.btn-primary").Click();

        // Act
        await cut.Find("#teamName").ChangeAsync(new() { Value = "U12 Red" });
        await cut.Find("#graduationYear").ChangeAsync(new() { Value = (DateTime.Today.Year + 5).ToString() });
        await cut.Find(".card-body form").SubmitAsync();

        // Assert
        cut.WaitForAssertion(() =>
        {
            var alert = cut.Find(".alert-danger");
            alert.TextContent.ShouldContain("already exists");
        });
    }
    /// <summary>
    /// Verifies the WhenCreateReturnsServerError_ShouldDisplayGenericError scenario.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>

    [Fact]
    public async Task WhenCreateReturnsServerError_ShouldDisplayGenericError()
    {
        // Arrange
        var cut = RenderGrid(clubId: 100, teams: []);

        _mockTeamsService
            .CreateTeamAsync(100, Arg.Any<CreateTeamDto>(), Arg.Any<CancellationToken>())
            .Returns(new ServiceResult<Success>(ServiceProblem.ServerError()));

        cut.Find("button.btn-primary").Click();

        // Act
        await cut.Find("#teamName").ChangeAsync(new() { Value = "U12 Red" });
        await cut.Find("#graduationYear").ChangeAsync(new() { Value = (DateTime.Today.Year + 5).ToString() });
        await cut.Find(".card-body form").SubmitAsync();

        // Assert
        cut.WaitForAssertion(() =>
        {
            var alert = cut.Find(".alert-danger");
            alert.TextContent.ShouldContain("unexpected error");
        });
    }

    #endregion

    #region Button State Tests
    /// <summary>
    /// Verifies the WhenCreating_ShouldDisableButtons scenario.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>

    [Fact]
    public async Task WhenCreating_ShouldDisableButtons()
    {
        // Arrange
        var cut = RenderGrid(clubId: 100, teams: []);

        var tcs = new TaskCompletionSource<ServiceResult<Success>>();
        _mockTeamsService
            .CreateTeamAsync(100, Arg.Any<CreateTeamDto>(), Arg.Any<CancellationToken>())
            .Returns(tcs.Task);

        cut.Find("button.btn-primary").Click();

        await cut.Find("#teamName").ChangeAsync(new() { Value = "U12 Red" });
        await cut.Find("#graduationYear").ChangeAsync(new() { Value = (DateTime.Today.Year + 5).ToString() });

        // Act
        var submitTask = cut.Find(".card-body form").SubmitAsync();

        // Assert
        cut.WaitForAssertion(() =>
        {
            var submitButton = cut.Find("button.btn-success");
            submitButton.HasAttribute("disabled").ShouldBeTrue();

            var cancelButton = cut.Find("button.btn-outline-secondary");
            cancelButton.HasAttribute("disabled").ShouldBeTrue();
        });

        // Cleanup
        tcs.SetResult(new ServiceResult<Success>(new Success()));
        await submitTask;
    }
    /// <summary>
    /// Verifies the WhenCreating_ShouldShowSpinner scenario.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>

    [Fact]
    public async Task WhenCreating_ShouldShowSpinner()
    {
        // Arrange
        var cut = RenderGrid(clubId: 100, teams: []);

        var tcs = new TaskCompletionSource<ServiceResult<Success>>();
        _mockTeamsService
            .CreateTeamAsync(100, Arg.Any<CreateTeamDto>(), Arg.Any<CancellationToken>())
            .Returns(tcs.Task);

        cut.Find("button.btn-primary").Click();

        await cut.Find("#teamName").ChangeAsync(new() { Value = "U12 Red" });
        await cut.Find("#graduationYear").ChangeAsync(new() { Value = (DateTime.Today.Year + 5).ToString() });

        // Act
        var submitTask = cut.Find(".card-body form").SubmitAsync();

        // Assert
        cut.WaitForAssertion(() =>
        {
            var spinner = cut.Find("button.btn-success .spinner-border");
            spinner.ShouldNotBeNull();
        });

        // Cleanup
        tcs.SetResult(new ServiceResult<Success>(new Success()));
        await submitTask;
    }

    #endregion

    #region Graduation Year Default Value Tests
    /// <summary>
    /// Verifies the WhenCreateFormOpened_ShouldDefaultGraduationYearToCurrentYear scenario.
    /// </summary>

    [Fact]
    public void WhenCreateFormOpened_ShouldDefaultGraduationYearToCurrentYear()
    {
        // Arrange
        var cut = RenderGrid(teams: []);

        // Act
        cut.Find("button.btn-primary").Click();

        // Assert
        var graduationYearInput = cut.Find("#graduationYear");
        graduationYearInput.GetAttribute("value").ShouldBe(DateTime.Today.Year.ToString());
    }

    #endregion

    #region Multiple Teams Tests
    /// <summary>
    /// Verifies the WhenMultipleTeams_ShouldDisplayAll scenario.
    /// </summary>

    [Fact]
    public void WhenMultipleTeams_ShouldDisplayAll()
    {
        // Arrange
        var teams = CreateTestTeams(3);

        // Act
        var cut = RenderGrid(teams: teams);

        // Assert
        cut.Markup.ShouldContain("Team 1");
        cut.Markup.ShouldContain("Team 2");
        cut.Markup.ShouldContain("Team 3");
    }

    #endregion

    #region Search/Filter Tests
    /// <summary>
    /// Verifies the WhenSearchTermIsEmpty_ShouldDisplayAllTeams scenario.
    /// </summary>

    [Fact]
    public void WhenSearchTermIsEmpty_ShouldDisplayAllTeams()
    {
        // Arrange
        var teams = CreateTestTeams(3);

        // Act
        var cut = RenderGrid(teams: teams);

        // Assert
        var rows = cut.FindAll("tbody tr");
        rows.Count.ShouldBe(3);
    }
    /// <summary>
    /// Verifies the WhenSearchByName_ShouldFilterTeams scenario.
    /// </summary>

    [Fact]
    public void WhenSearchByName_ShouldFilterTeams()
    {
        // Arrange
        var teams = CreateTestTeams(3);
        var cut = RenderGrid(teams: teams);

        // Act
        var searchInput = cut.Find("#TeamSearch");
        searchInput.Input("Team 1");

        // Assert
        cut.WaitForAssertion(() =>
        {
            var rows = cut.FindAll("tbody tr");
            rows.Count.ShouldBe(1);
            cut.Markup.ShouldContain("Team 1");
            cut.Markup.ShouldNotContain("Team 2");
            cut.Markup.ShouldNotContain("Team 3");
        });
    }
    /// <summary>
    /// Verifies the WhenSearchByPartialName_ShouldFilterTeams scenario.
    /// </summary>

    [Fact]
    public void WhenSearchByPartialName_ShouldFilterTeams()
    {
        // Arrange
        var teams = CreateTestTeams(3);
        var cut = RenderGrid(teams: teams);

        // Act
        var searchInput = cut.Find("#TeamSearch");
        searchInput.Input("Team");

        // Assert
        cut.WaitForAssertion(() =>
        {
            var rows = cut.FindAll("tbody tr");
            rows.Count.ShouldBe(3); // All 3 teams match "Team"
        });
    }
    /// <summary>
    /// Verifies the WhenSearchIsCaseInsensitive_ShouldFilterTeams scenario.
    /// </summary>

    [Fact]
    public void WhenSearchIsCaseInsensitive_ShouldFilterTeams()
    {
        // Arrange
        var teams = CreateTestTeams(2);
        var cut = RenderGrid(teams: teams);

        // Act
        var searchInput = cut.Find("#TeamSearch");
        searchInput.Input("TEAM 1");

        // Assert
        cut.WaitForAssertion(() =>
        {
            var rows = cut.FindAll("tbody tr");
            rows.Count.ShouldBe(1);
            cut.Markup.ShouldContain("Team 1");
        });
    }
    /// <summary>
    /// Verifies the WhenSearchMatchesNoTeams_ShouldDisplayEmptyGrid scenario.
    /// </summary>

    [Fact]
    public void WhenSearchMatchesNoTeams_ShouldDisplayEmptyGrid()
    {
        // Arrange
        var teams = CreateTestTeams(2);
        var cut = RenderGrid(teams: teams);

        // Act
        var searchInput = cut.Find("#TeamSearch");
        searchInput.Input("nonexistent");

        // Assert
        cut.WaitForAssertion(() =>
        {
            var rows = cut.FindAll("tbody tr");
            rows.Count.ShouldBe(0);
        });
    }
    /// <summary>
    /// Verifies the WhenSearchCleared_ShouldDisplayAllTeams scenario.
    /// </summary>

    [Fact]
    public void WhenSearchCleared_ShouldDisplayAllTeams()
    {
        // Arrange
        var teams = CreateTestTeams(3);
        var cut = RenderGrid(teams: teams);

        var searchInput = cut.Find("#TeamSearch");
        searchInput.Input("Team 1");

        cut.WaitForState(() => cut.FindAll("tbody tr").Count == 1);

        // Act
        searchInput.Input(string.Empty);

        // Assert
        cut.WaitForAssertion(() =>
        {
            var rows = cut.FindAll("tbody tr");
            rows.Count.ShouldBe(3); // All teams displayed again
        });
    }

    #endregion
}
