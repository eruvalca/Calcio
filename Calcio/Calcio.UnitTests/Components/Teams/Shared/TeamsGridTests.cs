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

public sealed class TeamsGridTests : BunitContext
{
    private readonly ITeamsService _mockTeamsService;

    public TeamsGridTests()
    {
        _mockTeamsService = Substitute.For<ITeamsService>();
        Services.AddSingleton(_mockTeamsService);

        // QuickGrid uses JS interop for virtualization
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    #region Helper Methods

    private static List<TeamDto> CreateTestTeams(int count = 2)
        => [
            .. Enumerable.Range(1, count)
                .Select(i => new TeamDto(
                    TeamId: i,
                    Name: $"Team {i}",
                    GraduationYear: DateTime.Today.Year + i))
        ];

    private IRenderedComponent<TeamsGrid> RenderGrid(
        long clubId = 100,
        List<TeamDto>? teams = null)
        => Render<TeamsGrid>(parameters => parameters
            .Add(p => p.ClubId, clubId)
            .Add(p => p.Teams, teams ?? []));

    #endregion

    #region Initial Rendering Tests

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

    [Fact]
    public void WhenInitialRender_ShouldDisplayNewTeamButton()
    {
        // Arrange & Act
        var cut = RenderGrid(teams: []);

        // Assert
        var button = cut.Find("button.btn-primary");
        button.TextContent.ShouldContain("New Team");
    }

    [Fact]
    public void WhenNewTeamClicked_ShouldShowCreateForm()
    {
        // Arrange
        var cut = RenderGrid(teams: []);

        // Act
        cut.Find("button.btn-primary").Click();

        // Assert
        cut.Find("form").ShouldNotBeNull();
        cut.Find("#teamName").ShouldNotBeNull();
        cut.Find("#graduationYear").ShouldNotBeNull();
    }

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

    [Fact]
    public void WhenCancelClicked_ShouldHideCreateForm()
    {
        // Arrange
        var cut = RenderGrid(teams: []);
        cut.Find("button.btn-primary").Click();

        // Act
        cut.Find("button.btn-outline-secondary").Click();

        // Assert
        cut.FindAll("form").ShouldBeEmpty();
        cut.Find("button.btn-primary").TextContent.ShouldContain("New Team");
    }

    #endregion

    #region Create Team Tests

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

        await cut.Find("form").SubmitAsync();

        // Assert
        await _mockTeamsService.Received(1)
            .CreateTeamAsync(100, Arg.Is<CreateTeamDto>(dto =>
                dto.Name == "U12 Red" &&
                dto.GraduationYear == DateTime.Today.Year + 5), Arg.Any<CancellationToken>());
    }

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
        await cut.Find("form").SubmitAsync();

        // Assert
        cut.WaitForAssertion(() =>
        {
            var alert = cut.Find(".alert-danger");
            alert.TextContent.ShouldContain("not authorized");
        });
    }

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
        await cut.Find("form").SubmitAsync();

        // Assert
        cut.WaitForAssertion(() =>
        {
            var alert = cut.Find(".alert-danger");
            alert.TextContent.ShouldContain("already exists");
        });
    }

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
        await cut.Find("form").SubmitAsync();

        // Assert
        cut.WaitForAssertion(() =>
        {
            var alert = cut.Find(".alert-danger");
            alert.TextContent.ShouldContain("unexpected error");
        });
    }

    #endregion

    #region Button State Tests

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
        var submitTask = cut.Find("form").SubmitAsync();

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
        var submitTask = cut.Find("form").SubmitAsync();

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
}
