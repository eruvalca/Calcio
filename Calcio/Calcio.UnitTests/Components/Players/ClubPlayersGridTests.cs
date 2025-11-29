using Bunit;

using Calcio.Shared.DTOs.Players;
using Calcio.Shared.Enums;
using Calcio.Shared.Results;
using Calcio.Shared.Services.Players;
using Calcio.UI.Components.Players;

using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using OneOf.Types;

using Shouldly;

namespace Calcio.UnitTests.Components.Players;

/// <summary>
/// Unit tests for the ClubPlayersGrid Blazor component using bUnit.
/// </summary>
public sealed class ClubPlayersGridTests : BunitContext
{
    private readonly IPlayersService _mockPlayersService;

    public ClubPlayersGridTests()
    {
        _mockPlayersService = Substitute.For<IPlayersService>();
        Services.AddSingleton(_mockPlayersService);

        // QuickGrid uses JS interop for virtualization
        JSInterop.Mode = JSRuntimeMode.Loose;

        // Set up authorization for ClubAdmin role requirement
        var authContext = AddAuthorization();
        authContext.SetAuthorized("TestUser");
    }

    #region Helper Methods

    private static List<ClubPlayerDto> CreateTestPlayers(int count = 2)
    {
        var players = new List<ClubPlayerDto>();

        for (var i = 1; i <= count; i++)
        {
            players.Add(new ClubPlayerDto(
                PlayerId: 100 + i,
                FirstName: $"Player{i}",
                LastName: $"Test{i}",
                FullName: $"Player{i} Test{i}",
                DateOfBirth: DateOnly.FromDateTime(DateTime.Now.AddYears(-10 - i)),
                Gender: i % 2 == 0 ? Gender.Male : Gender.Female,
                JerseyNumber: i,
                TryoutNumber: 100 + i));
        }

        return players;
    }

    private IRenderedComponent<ClubPlayersGrid> RenderGrid(long clubId = 100)
        => Render<ClubPlayersGrid>(parameters => parameters
            .Add(p => p.ClubId, clubId));

    #endregion

    #region Loading State Tests

    [Fact]
    public void WhenLoading_ShouldDisplaySpinner()
    {
        // Arrange - Setup mock to never complete
        var tcs = new TaskCompletionSource<OneOf.OneOf<List<ClubPlayerDto>, Unauthorized, Error>>();
        _mockPlayersService
            .GetClubPlayersAsync(100, Arg.Any<CancellationToken>())
            .Returns(tcs.Task);

        // Act
        var cut = RenderGrid();

        // Assert
        cut.Find(".spinner-border").ShouldNotBeNull();
        cut.FindAll("table").ShouldBeEmpty();
    }

    #endregion

    #region Initial Rendering Tests

    [Fact]
    public void WhenNoPlayers_ShouldDisplayEmptyMessage()
    {
        // Arrange
        _mockPlayersService
            .GetClubPlayersAsync(100, Arg.Any<CancellationToken>())
            .Returns(new List<ClubPlayerDto>());

        // Act
        var cut = RenderGrid();

        // Wait for async load to complete
        cut.WaitForAssertion(() =>
        {
            var emptyMessage = cut.Find(".text-muted");
            emptyMessage.TextContent.ShouldBe("No players in this club.");
        });
    }

    [Fact]
    public void WhenPlayersExist_ShouldDisplayGrid()
    {
        // Arrange
        var players = CreateTestPlayers(2);
        _mockPlayersService
            .GetClubPlayersAsync(100, Arg.Any<CancellationToken>())
            .Returns(players);

        // Act
        var cut = RenderGrid();

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Find("table").ShouldNotBeNull();

            var rows = cut.FindAll("tbody tr");
            rows.Count.ShouldBe(2);

            cut.Markup.ShouldContain("Player1 Test1");
            cut.Markup.ShouldContain("Player2 Test2");
        });
    }

    [Fact]
    public void WhenPlayersExist_ShouldDisplayJerseyNumbers()
    {
        // Arrange
        var players = CreateTestPlayers(2);
        _mockPlayersService
            .GetClubPlayersAsync(100, Arg.Any<CancellationToken>())
            .Returns(players);

        // Act
        var cut = RenderGrid();

        // Assert
        cut.WaitForAssertion(() =>
        {
            var jerseyBadges = cut.FindAll(".badge.bg-primary");
            jerseyBadges.Count.ShouldBe(2);
        });
    }

    [Fact]
    public void WhenPlayersExist_ShouldDisplayTryoutNumbers()
    {
        // Arrange
        var players = CreateTestPlayers(2);
        _mockPlayersService
            .GetClubPlayersAsync(100, Arg.Any<CancellationToken>())
            .Returns(players);

        // Act
        var cut = RenderGrid();

        // Assert
        cut.WaitForAssertion(() =>
        {
            var tryoutBadges = cut.FindAll(".badge.bg-secondary");
            tryoutBadges.Count.ShouldBe(2);
        });
    }

    [Fact]
    public void WhenPlayerHasNoJerseyNumber_ShouldDisplayDash()
    {
        // Arrange
        var players = new List<ClubPlayerDto>
        {
            new(
                PlayerId: 101,
                FirstName: "Player1",
                LastName: "Test1",
                FullName: "Player1 Test1",
                DateOfBirth: DateOnly.FromDateTime(DateTime.Now.AddYears(-10)),
                Gender: Gender.Male,
                JerseyNumber: null,
                TryoutNumber: null)
        };
        _mockPlayersService
            .GetClubPlayersAsync(100, Arg.Any<CancellationToken>())
            .Returns(players);

        // Act
        var cut = RenderGrid();

        // Assert
        cut.WaitForAssertion(() =>
        {
            // Jersey and tryout columns should have dashes
            var dashes = cut.FindAll("span.text-muted");
            dashes.Count.ShouldBeGreaterThanOrEqualTo(2);
        });
    }

    [Fact]
    public void WhenPlayerHasNoGender_ShouldDisplayDash()
    {
        // Arrange
        var players = new List<ClubPlayerDto>
        {
            new(
                PlayerId: 101,
                FirstName: "Player1",
                LastName: "Test1",
                FullName: "Player1 Test1",
                DateOfBirth: DateOnly.FromDateTime(DateTime.Now.AddYears(-10)),
                Gender: null,
                JerseyNumber: 1,
                TryoutNumber: 101)
        };
        _mockPlayersService
            .GetClubPlayersAsync(100, Arg.Any<CancellationToken>())
            .Returns(players);

        // Act
        var cut = RenderGrid();

        // Assert
        cut.WaitForAssertion(() =>
        {
            // Gender column should have a dash
            cut.Markup.ShouldContain("<span class=\"text-muted\">—</span>");
        });
    }

    #endregion

    #region Error State Tests

    [Fact]
    public void WhenLoadReturnsUnauthorized_ShouldDisplayErrorMessage()
    {
        // Arrange
        _mockPlayersService
            .GetClubPlayersAsync(100, Arg.Any<CancellationToken>())
            .Returns(new Unauthorized());

        // Act
        var cut = RenderGrid();

        // Assert
        cut.WaitForAssertion(() =>
        {
            var alert = cut.Find(".alert-danger");
            alert.TextContent.ShouldContain("not authorized");
        });
    }

    [Fact]
    public void WhenLoadReturnsError_ShouldDisplayErrorMessage()
    {
        // Arrange
        _mockPlayersService
            .GetClubPlayersAsync(100, Arg.Any<CancellationToken>())
            .Returns(new Error());

        // Act
        var cut = RenderGrid();

        // Assert
        cut.WaitForAssertion(() =>
        {
            var alert = cut.Find(".alert-danger");
            alert.TextContent.ShouldContain("unexpected error");
        });
    }

    #endregion

    #region Search/Filter Tests

    [Fact]
    public void WhenSearchTermIsEmpty_ShouldDisplayAllPlayers()
    {
        // Arrange
        var players = CreateTestPlayers(3);
        _mockPlayersService
            .GetClubPlayersAsync(100, Arg.Any<CancellationToken>())
            .Returns(players);

        // Act
        var cut = RenderGrid();

        // Assert
        cut.WaitForAssertion(() =>
        {
            var rows = cut.FindAll("tbody tr");
            rows.Count.ShouldBe(3);
        });
    }

    [Fact]
    public void WhenSearchByFullName_ShouldFilterPlayers()
    {
        // Arrange
        var players = CreateTestPlayers(3);
        _mockPlayersService
            .GetClubPlayersAsync(100, Arg.Any<CancellationToken>())
            .Returns(players);

        var cut = RenderGrid();

        cut.WaitForState(() => cut.FindAll("tbody tr").Count == 3);

        // Act
        var searchInput = cut.Find("#PlayerSearch");
        searchInput.Input("Player1 Test1");

        // Assert
        cut.WaitForAssertion(() =>
        {
            var rows = cut.FindAll("tbody tr");
            rows.Count.ShouldBe(1);
            cut.Markup.ShouldContain("Player1 Test1");
            cut.Markup.ShouldNotContain("Player2 Test2");
            cut.Markup.ShouldNotContain("Player3 Test3");
        });
    }

    [Fact]
    public void WhenSearchByFirstName_ShouldFilterPlayers()
    {
        // Arrange
        var players = CreateTestPlayers(3);
        _mockPlayersService
            .GetClubPlayersAsync(100, Arg.Any<CancellationToken>())
            .Returns(players);

        var cut = RenderGrid();

        cut.WaitForState(() => cut.FindAll("tbody tr").Count == 3);

        // Act
        var searchInput = cut.Find("#PlayerSearch");
        searchInput.Input("Player2");

        // Assert
        cut.WaitForAssertion(() =>
        {
            var rows = cut.FindAll("tbody tr");
            rows.Count.ShouldBe(1);
            cut.Markup.ShouldContain("Player2 Test2");
        });
    }

    [Fact]
    public void WhenSearchByLastName_ShouldFilterPlayers()
    {
        // Arrange
        var players = CreateTestPlayers(3);
        _mockPlayersService
            .GetClubPlayersAsync(100, Arg.Any<CancellationToken>())
            .Returns(players);

        var cut = RenderGrid();

        cut.WaitForState(() => cut.FindAll("tbody tr").Count == 3);

        // Act
        var searchInput = cut.Find("#PlayerSearch");
        searchInput.Input("Test3");

        // Assert
        cut.WaitForAssertion(() =>
        {
            var rows = cut.FindAll("tbody tr");
            rows.Count.ShouldBe(1);
            cut.Markup.ShouldContain("Player3 Test3");
        });
    }

    [Fact]
    public void WhenSearchByPartialName_ShouldFilterPlayers()
    {
        // Arrange
        var players = CreateTestPlayers(3);
        _mockPlayersService
            .GetClubPlayersAsync(100, Arg.Any<CancellationToken>())
            .Returns(players);

        var cut = RenderGrid();

        cut.WaitForState(() => cut.FindAll("tbody tr").Count == 3);

        // Act
        var searchInput = cut.Find("#PlayerSearch");
        searchInput.Input("Player");

        // Assert
        cut.WaitForAssertion(() =>
        {
            var rows = cut.FindAll("tbody tr");
            rows.Count.ShouldBe(3); // All 3 players match "Player"
        });
    }

    [Fact]
    public void WhenSearchIsCaseInsensitive_ShouldFilterPlayers()
    {
        // Arrange
        var players = CreateTestPlayers(2);
        _mockPlayersService
            .GetClubPlayersAsync(100, Arg.Any<CancellationToken>())
            .Returns(players);

        var cut = RenderGrid();

        cut.WaitForState(() => cut.FindAll("tbody tr").Count == 2);

        // Act
        var searchInput = cut.Find("#PlayerSearch");
        searchInput.Input("PLAYER1");

        // Assert
        cut.WaitForAssertion(() =>
        {
            var rows = cut.FindAll("tbody tr");
            rows.Count.ShouldBe(1);
            cut.Markup.ShouldContain("Player1 Test1");
        });
    }

    [Fact]
    public void WhenSearchMatchesNoPlayers_ShouldDisplayEmptyGrid()
    {
        // Arrange
        var players = CreateTestPlayers(2);
        _mockPlayersService
            .GetClubPlayersAsync(100, Arg.Any<CancellationToken>())
            .Returns(players);

        var cut = RenderGrid();

        cut.WaitForState(() => cut.FindAll("tbody tr").Count == 2);

        // Act
        var searchInput = cut.Find("#PlayerSearch");
        searchInput.Input("nonexistent");

        // Assert
        cut.WaitForAssertion(() =>
        {
            var rows = cut.FindAll("tbody tr");
            rows.Count.ShouldBe(0);
        });
    }

    [Fact]
    public void WhenSearchCleared_ShouldDisplayAllPlayers()
    {
        // Arrange
        var players = CreateTestPlayers(3);
        _mockPlayersService
            .GetClubPlayersAsync(100, Arg.Any<CancellationToken>())
            .Returns(players);

        var cut = RenderGrid();

        cut.WaitForState(() => cut.FindAll("tbody tr").Count == 3);

        var searchInput = cut.Find("#PlayerSearch");
        searchInput.Input("Player1");

        cut.WaitForState(() => cut.FindAll("tbody tr").Count == 1);

        // Act
        searchInput.Input(string.Empty);

        // Assert
        cut.WaitForAssertion(() =>
        {
            var rows = cut.FindAll("tbody tr");
            rows.Count.ShouldBe(3); // All players displayed again
        });
    }

    #endregion
}
