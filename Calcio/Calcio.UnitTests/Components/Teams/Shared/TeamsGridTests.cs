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

    private IRenderedComponent<TeamsGrid> RenderGrid(long clubId = 100, List<TeamDto>? teams = null)
    {
        _mockTeamsService
            .GetTeamsAsync(clubId, Arg.Any<CancellationToken>())
            .Returns(new ServiceResult<List<TeamDto>>(teams ?? []));

        return Render<TeamsGrid>(parameters => parameters
            .Add(p => p.ClubId, clubId));
    }

    #endregion

    #region Initial Loading Tests

    [Fact]
    public void WhenLoading_ShouldDisplaySpinner()
    {
        // Arrange
        var tcs = new TaskCompletionSource<ServiceResult<List<TeamDto>>>();
        _mockTeamsService
            .GetTeamsAsync(100, Arg.Any<CancellationToken>())
            .Returns(tcs.Task);

        // Act
        var cut = Render<TeamsGrid>(parameters => parameters
            .Add(p => p.ClubId, 100));

        // Assert
        cut.FindAll(".spinner-border").Count.ShouldBe(1);

        // Cleanup
        tcs.SetResult(new ServiceResult<List<TeamDto>>(new List<TeamDto>()));
    }

    [Fact]
    public void WhenNoTeams_ShouldDisplayEmptyMessage()
    {
        // Arrange & Act
        var cut = RenderGrid(teams: []);

        // Assert
        cut.WaitForAssertion(() =>
        {
            var emptyMessage = cut.Find(".text-muted");
            emptyMessage.TextContent.ShouldBe("No teams found.");
            cut.FindAll("table").ShouldBeEmpty();
        });
    }

    [Fact]
    public void WhenTeamsExist_ShouldDisplayGrid()
    {
        // Arrange
        var teams = CreateTestTeams(2);

        // Act
        var cut = RenderGrid(teams: teams);

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Find("table").ShouldNotBeNull();
            cut.Markup.ShouldContain("Team 1");
            cut.Markup.ShouldContain("Team 2");
        });
    }

    [Fact]
    public void WhenLoadFails_ShouldDisplayErrorMessage()
    {
        // Arrange
        _mockTeamsService
            .GetTeamsAsync(100, Arg.Any<CancellationToken>())
            .Returns(new ServiceResult<List<TeamDto>>(ServiceProblem.Forbidden()));

        // Act
        var cut = Render<TeamsGrid>(parameters => parameters
            .Add(p => p.ClubId, 100));

        // Assert
        cut.WaitForAssertion(() =>
        {
            var alert = cut.Find(".alert-danger");
            alert.TextContent.ShouldContain("not authorized");
        });
    }

    #endregion

    #region Create Form Toggle Tests

    [Fact]
    public void WhenInitialRender_ShouldDisplayNewTeamButton()
    {
        // Arrange & Act
        var cut = RenderGrid(teams: []);

        // Assert
        cut.WaitForAssertion(() =>
        {
            var button = cut.Find("button.btn-primary");
            button.TextContent.ShouldContain("New Team");
        });
    }

    [Fact]
    public void WhenNewTeamClicked_ShouldShowCreateForm()
    {
        // Arrange
        var cut = RenderGrid(teams: []);

        // Act
        cut.WaitForAssertion(() =>
        {
            cut.Find("button.btn-primary").Click();
        });

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Find("form").ShouldNotBeNull();
            cut.Find("#teamName").ShouldNotBeNull();
            cut.Find("#graduationYear").ShouldNotBeNull();
        });
    }

    [Fact]
    public void WhenCreateFormShown_ShouldHideNewTeamButton()
    {
        // Arrange
        var cut = RenderGrid(teams: []);

        // Act
        cut.WaitForAssertion(() =>
        {
            cut.Find("button.btn-primary").Click();
        });

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.FindAll(".card-header button.btn-primary").ShouldBeEmpty();
        });
    }

    [Fact]
    public void WhenCancelClicked_ShouldHideCreateForm()
    {
        // Arrange
        var cut = RenderGrid(teams: []);

        cut.WaitForAssertion(() =>
        {
            cut.Find("button.btn-primary").Click();
        });

        // Act
        cut.WaitForAssertion(() =>
        {
            cut.Find("button.btn-outline-secondary").Click();
        });

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.FindAll("form").ShouldBeEmpty();
            cut.Find("button.btn-primary").TextContent.ShouldContain("New Team");
        });
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

        cut.WaitForAssertion(() =>
        {
            cut.Find("button.btn-primary").Click();
        });

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
    public async Task WhenCreateSucceeds_ShouldHideFormAndReloadTeams()
    {
        // Arrange
        var initialTeams = CreateTestTeams(1);
        var cut = RenderGrid(clubId: 100, teams: initialTeams);

        _mockTeamsService
            .CreateTeamAsync(100, Arg.Any<CreateTeamDto>(), Arg.Any<CancellationToken>())
            .Returns(new ServiceResult<Success>(new Success()));

        var updatedTeams = new List<TeamDto>
        {
            new(1, "Team 1", DateTime.Today.Year + 1),
            new(2, "U12 Red", DateTime.Today.Year + 5)
        };

        cut.WaitForAssertion(() =>
        {
            cut.Find("button.btn-primary").Click();
        });

        // Act
        await cut.Find("#teamName").ChangeAsync(new() { Value = "U12 Red" });
        await cut.Find("#graduationYear").ChangeAsync(new() { Value = (DateTime.Today.Year + 5).ToString() });

        // Setup to return updated teams after create
        _mockTeamsService
            .GetTeamsAsync(100, Arg.Any<CancellationToken>())
            .Returns(new ServiceResult<List<TeamDto>>(updatedTeams));

        await cut.Find("form").SubmitAsync();

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.FindAll("form").ShouldBeEmpty();
            cut.Find("button.btn-primary").TextContent.ShouldContain("New Team");
        });

        // Verify GetTeamsAsync was called again to reload
        await _mockTeamsService.Received(2)
            .GetTeamsAsync(100, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WhenCreateFails_ShouldDisplayError()
    {
        // Arrange
        var cut = RenderGrid(clubId: 100, teams: []);

        _mockTeamsService
            .CreateTeamAsync(100, Arg.Any<CreateTeamDto>(), Arg.Any<CancellationToken>())
            .Returns(new ServiceResult<Success>(ServiceProblem.Forbidden()));

        cut.WaitForAssertion(() =>
        {
            cut.Find("button.btn-primary").Click();
        });

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

        cut.WaitForAssertion(() =>
        {
            cut.Find("button.btn-primary").Click();
        });

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

        cut.WaitForAssertion(() =>
        {
            cut.Find("button.btn-primary").Click();
        });

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

        cut.WaitForAssertion(() =>
        {
            cut.Find("button.btn-primary").Click();
        });

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

        cut.WaitForAssertion(() =>
        {
            cut.Find("button.btn-primary").Click();
        });

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
        cut.WaitForAssertion(() =>
        {
            cut.Find("button.btn-primary").Click();
        });

        // Assert
        cut.WaitForAssertion(() =>
        {
            var graduationYearInput = cut.Find("#graduationYear");
            graduationYearInput.GetAttribute("value").ShouldBe(DateTime.Today.Year.ToString());
        });
    }

    #endregion
}
