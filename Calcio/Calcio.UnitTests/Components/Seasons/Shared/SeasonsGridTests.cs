using Bunit;

using Calcio.Shared.DTOs.Seasons;
using Calcio.Shared.Results;
using Calcio.Shared.Services.Seasons;
using Calcio.UI.Components.Seasons.Shared;

using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using OneOf.Types;

using Shouldly;

namespace Calcio.UnitTests.Components.Seasons.Shared;

/// <summary>
/// Unit tests for the SeasonsGrid Blazor component using bUnit.
/// </summary>
public sealed class SeasonsGridTests : BunitContext
{
    private readonly ISeasonsService _mockSeasonsService;

    public SeasonsGridTests()
    {
        _mockSeasonsService = Substitute.For<ISeasonsService>();
        Services.AddSingleton(_mockSeasonsService);

        // QuickGrid uses JS interop for virtualization
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    #region Helper Methods

    private static List<SeasonDto> CreateTestSeasons(int count = 2)
        => [.. Enumerable.Range(1, count)
            .Select(i => new SeasonDto(
                SeasonId: i,
                Name: $"Season {i}",
                StartDate: DateOnly.FromDateTime(DateTime.Today.AddDays(i * 30)),
                EndDate: DateOnly.FromDateTime(DateTime.Today.AddDays((i * 30) + 90)),
                IsComplete: false))];

    private IRenderedComponent<SeasonsGrid> RenderGrid(
        long clubId = 100,
        List<SeasonDto>? seasons = null)
        => Render<SeasonsGrid>(parameters => parameters
            .Add(p => p.ClubId, clubId)
            .Add(p => p.Seasons, seasons ?? []));

    #endregion

    #region Initial Rendering Tests

    [Fact]
    public void WhenNoSeasons_ShouldDisplayEmptyMessage()
    {
        // Arrange & Act
        var cut = RenderGrid(seasons: []);

        // Assert
        var emptyMessage = cut.Find(".text-muted");
        emptyMessage.TextContent.ShouldBe("No seasons found.");

        cut.FindAll("table").ShouldBeEmpty();
    }

    [Fact]
    public void WhenSeasonsExist_ShouldDisplayGrid()
    {
        // Arrange
        var seasons = CreateTestSeasons(2);

        // Act
        var cut = RenderGrid(seasons: seasons);

        // Assert
        cut.Find("table").ShouldNotBeNull();

        var rows = cut.FindAll("tbody tr");
        rows.Count.ShouldBe(2);

        cut.Markup.ShouldContain("Season 1");
        cut.Markup.ShouldContain("Season 2");
    }

    [Fact]
    public void WhenSeasonIsComplete_ShouldDisplayCompleteBadge()
    {
        // Arrange
        var seasons = new List<SeasonDto>
        {
            new(
                SeasonId: 1,
                Name: "Completed Season",
                StartDate: DateOnly.FromDateTime(DateTime.Today.AddDays(-180)),
                EndDate: DateOnly.FromDateTime(DateTime.Today.AddDays(-90)),
                IsComplete: true)
        };

        // Act
        var cut = RenderGrid(seasons: seasons);

        // Assert
        var badge = cut.Find(".badge.bg-secondary");
        badge.TextContent.ShouldBe("Complete");
    }

    [Fact]
    public void WhenSeasonIsActive_ShouldDisplayActiveBadge()
    {
        // Arrange
        var seasons = new List<SeasonDto>
        {
            new(
                SeasonId: 1,
                Name: "Active Season",
                StartDate: DateOnly.FromDateTime(DateTime.Today),
                EndDate: DateOnly.FromDateTime(DateTime.Today.AddDays(90)),
                IsComplete: false)
        };

        // Act
        var cut = RenderGrid(seasons: seasons);

        // Assert
        var badge = cut.Find(".badge.bg-success");
        badge.TextContent.ShouldBe("Active");
    }

    [Fact]
    public void WhenSeasonHasNotStarted_ShouldDisplayInactiveBadge()
    {
        // Arrange
        var seasons = new List<SeasonDto>
        {
            new(
                SeasonId: 1,
                Name: "Future Season",
                StartDate: DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
                EndDate: DateOnly.FromDateTime(DateTime.Today.AddDays(120)),
                IsComplete: false)
        };

        // Act
        var cut = RenderGrid(seasons: seasons);

        // Assert
        var badge = cut.Find(".badge.bg-warning");
        badge.TextContent.ShouldBe("Inactive");
    }

    [Fact]
    public void WhenSeasonHasEnded_ShouldDisplayInactiveBadge()
    {
        // Arrange
        var seasons = new List<SeasonDto>
        {
            new(
                SeasonId: 1,
                Name: "Past Season",
                StartDate: DateOnly.FromDateTime(DateTime.Today.AddDays(-90)),
                EndDate: DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
                IsComplete: false)
        };

        // Act
        var cut = RenderGrid(seasons: seasons);

        // Assert
        var badge = cut.Find(".badge.bg-warning");
        badge.TextContent.ShouldBe("Inactive");
    }

    [Fact]
    public void WhenSeasonHasNoEndDate_ShouldDisplayDash()
    {
        // Arrange
        var seasons = new List<SeasonDto>
        {
            new(
                SeasonId: 1,
                Name: "Open Season",
                StartDate: DateOnly.FromDateTime(DateTime.Today),
                EndDate: null,
                IsComplete: false)
        };

        // Act
        var cut = RenderGrid(seasons: seasons);

        // Assert
        cut.Markup.ShouldContain("—");
    }

    #endregion

    #region Create Form Tests

    [Fact]
    public void WhenNewSeasonClicked_ShouldShowCreateForm()
    {
        // Arrange
        var cut = RenderGrid(seasons: []);

        // Act
        cut.Find("button.btn-primary").Click();

        // Assert
        var form = cut.Find(".card-body form");
        form.ShouldNotBeNull();

        cut.FindAll(".card-header button.btn-primary").ShouldBeEmpty(); // New Season button should be hidden
        cut.Find("#seasonName").ShouldNotBeNull();
        cut.Find("#startDate").ShouldNotBeNull();
        cut.Find("#endDate").ShouldNotBeNull();
    }

    [Fact]
    public void WhenCancelClicked_ShouldHideCreateForm()
    {
        // Arrange
        var cut = RenderGrid(seasons: []);
        cut.Find("button.btn-primary").Click();

        // Act
        cut.Find("button.btn-outline-secondary").Click();

        // Assert
        cut.FindAll(".card-body.border-bottom form").ShouldBeEmpty();
        cut.Find(".card-header button.btn-primary").ShouldNotBeNull(); // New Season button should be visible again
    }

    [Fact]
    public async Task WhenCreateSeasonSubmitted_ShouldCallService()
    {
        // Arrange
        var cut = RenderGrid(clubId: 100, seasons: []);

        _mockSeasonsService
            .CreateSeasonAsync(100, Arg.Any<CreateSeasonDto>(), Arg.Any<CancellationToken>())
            .Returns(new ServiceResult<Success>(new Success()));

        // Open form
        cut.Find("button.btn-primary").Click();

        // Fill in the form
        cut.Find("#seasonName").Change("Spring 2025");

        // Act
        await cut.Find(".card-body form").SubmitAsync();

        // Assert
        await _mockSeasonsService.Received(1)
            .CreateSeasonAsync(100, Arg.Any<CreateSeasonDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WhenCreateReturnsForbidden_ShouldDisplayError()
    {
        // Arrange
        var cut = RenderGrid(clubId: 100, seasons: []);

        _mockSeasonsService
            .CreateSeasonAsync(100, Arg.Any<CreateSeasonDto>(), Arg.Any<CancellationToken>())
            .Returns(new ServiceResult<Success>(ServiceProblem.Forbidden()));

        cut.Find("button.btn-primary").Click();
        cut.Find("#seasonName").Change("Spring 2025");

        // Act
        await cut.Find(".card-body form").SubmitAsync();

        // Assert
        cut.WaitForAssertion(() =>
        {
            var alert = cut.Find(".alert-danger");
            alert.TextContent.ShouldContain("not authorized");
        });
    }

    [Fact]
    public async Task WhenCreateReturnsConflict_ShouldDisplayError()
    {
        // Arrange
        var cut = RenderGrid(clubId: 100, seasons: []);

        _mockSeasonsService
            .CreateSeasonAsync(100, Arg.Any<CreateSeasonDto>(), Arg.Any<CancellationToken>())
            .Returns(new ServiceResult<Success>(ServiceProblem.Conflict()));

        cut.Find("button.btn-primary").Click();
        cut.Find("#seasonName").Change("Spring 2025");

        // Act
        await cut.Find(".card-body form").SubmitAsync();

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
        var cut = RenderGrid(clubId: 100, seasons: []);

        _mockSeasonsService
            .CreateSeasonAsync(100, Arg.Any<CreateSeasonDto>(), Arg.Any<CancellationToken>())
            .Returns(new ServiceResult<Success>(ServiceProblem.ServerError()));

        cut.Find("button.btn-primary").Click();
        cut.Find("#seasonName").Change("Spring 2025");

        // Act
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

    [Fact]
    public async Task WhenCreating_ShouldDisableButtons()
    {
        // Arrange
        var cut = RenderGrid(clubId: 100, seasons: []);

        var tcs = new TaskCompletionSource<ServiceResult<Success>>();
        _mockSeasonsService
            .CreateSeasonAsync(100, Arg.Any<CreateSeasonDto>(), Arg.Any<CancellationToken>())
            .Returns(tcs.Task);

        cut.Find("button.btn-primary").Click();
        cut.Find("#seasonName").Change("Spring 2025");

        // Act
        var submitTask = cut.Find(".card-body form").SubmitAsync();

        // Assert
        cut.WaitForAssertion(() =>
        {
            var createButton = cut.Find("button.btn-success");
            createButton.HasAttribute("disabled").ShouldBeTrue();

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
        var cut = RenderGrid(clubId: 100, seasons: []);

        var tcs = new TaskCompletionSource<ServiceResult<Success>>();
        _mockSeasonsService
            .CreateSeasonAsync(100, Arg.Any<CreateSeasonDto>(), Arg.Any<CancellationToken>())
            .Returns(tcs.Task);

        cut.Find("button.btn-primary").Click();
        cut.Find("#seasonName").Change("Spring 2025");

        // Act
        var submitTask = cut.Find(".card-body form").SubmitAsync();

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.FindAll(".spinner-border").Count.ShouldBe(1);
        });

        // Cleanup
        tcs.SetResult(new ServiceResult<Success>(new Success()));
        await submitTask;
    }

    #endregion

    #region Multiple Seasons Tests

    [Fact]
    public void WhenMultipleSeasons_ShouldDisplayAllInOrder()
    {
        // Arrange
        var seasons = CreateTestSeasons(3);

        // Act
        var cut = RenderGrid(seasons: seasons);

        // Assert
        var rows = cut.FindAll("tbody tr");
        rows.Count.ShouldBe(3);

        cut.Markup.ShouldContain("Season 1");
        cut.Markup.ShouldContain("Season 2");
        cut.Markup.ShouldContain("Season 3");
    }

    #endregion

    #region Search/Filter Tests

    [Fact]
    public void WhenSearchTermIsEmpty_ShouldDisplayAllSeasons()
    {
        // Arrange
        var seasons = CreateTestSeasons(3);

        // Act
        var cut = RenderGrid(seasons: seasons);

        // Assert
        var rows = cut.FindAll("tbody tr");
        rows.Count.ShouldBe(3);
    }

    [Fact]
    public void WhenSearchByName_ShouldFilterSeasons()
    {
        // Arrange
        var seasons = CreateTestSeasons(3);
        var cut = RenderGrid(seasons: seasons);

        // Act
        var searchInput = cut.Find("#SeasonSearch");
        searchInput.Input("Season 1");

        // Assert
        cut.WaitForAssertion(() =>
        {
            var rows = cut.FindAll("tbody tr");
            rows.Count.ShouldBe(1);
            cut.Markup.ShouldContain("Season 1");
            cut.Markup.ShouldNotContain("Season 2");
            cut.Markup.ShouldNotContain("Season 3");
        });
    }

    [Fact]
    public void WhenSearchByPartialName_ShouldFilterSeasons()
    {
        // Arrange
        var seasons = CreateTestSeasons(3);
        var cut = RenderGrid(seasons: seasons);

        // Act
        var searchInput = cut.Find("#SeasonSearch");
        searchInput.Input("Season");

        // Assert
        cut.WaitForAssertion(() =>
        {
            var rows = cut.FindAll("tbody tr");
            rows.Count.ShouldBe(3); // All 3 seasons match "Season"
        });
    }

    [Fact]
    public void WhenSearchIsCaseInsensitive_ShouldFilterSeasons()
    {
        // Arrange
        var seasons = CreateTestSeasons(2);
        var cut = RenderGrid(seasons: seasons);

        // Act
        var searchInput = cut.Find("#SeasonSearch");
        searchInput.Input("SEASON 1");

        // Assert
        cut.WaitForAssertion(() =>
        {
            var rows = cut.FindAll("tbody tr");
            rows.Count.ShouldBe(1);
            cut.Markup.ShouldContain("Season 1");
        });
    }

    [Fact]
    public void WhenSearchMatchesNoSeasons_ShouldDisplayEmptyGrid()
    {
        // Arrange
        var seasons = CreateTestSeasons(2);
        var cut = RenderGrid(seasons: seasons);

        // Act
        var searchInput = cut.Find("#SeasonSearch");
        searchInput.Input("nonexistent");

        // Assert
        cut.WaitForAssertion(() =>
        {
            var rows = cut.FindAll("tbody tr");
            rows.Count.ShouldBe(0);
        });
    }

    [Fact]
    public void WhenSearchCleared_ShouldDisplayAllSeasons()
    {
        // Arrange
        var seasons = CreateTestSeasons(3);
        var cut = RenderGrid(seasons: seasons);

        var searchInput = cut.Find("#SeasonSearch");
        searchInput.Input("Season 1");

        cut.WaitForState(() => cut.FindAll("tbody tr").Count == 1);

        // Act
        searchInput.Input(string.Empty);

        // Assert
        cut.WaitForAssertion(() =>
        {
            var rows = cut.FindAll("tbody tr");
            rows.Count.ShouldBe(3); // All seasons displayed again
        });
    }

    #endregion
}
