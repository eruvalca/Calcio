using Bunit;

using Calcio.Shared.DTOs.Players;
using Calcio.Shared.Enums;
using Calcio.UI.Components.Players.Shared;

using Shouldly;

namespace Calcio.UnitTests.Components.Players.Shared;

/// <summary>
/// Unit tests for the ClubPlayersGrid Blazor component using bUnit.
/// </summary>
public sealed class ClubPlayersGridTests : BunitContext
{
    public ClubPlayersGridTests()
    {
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

    private IRenderedComponent<ClubPlayersGrid> RenderGrid(List<ClubPlayerDto> players)
        => Render<ClubPlayersGrid>(parameters => parameters
            .Add(p => p.Players, players));

    #endregion

    #region Initial Rendering Tests

    [Fact]
    public void WhenNoPlayers_ShouldDisplayEmptyMessage()
    {
        // Arrange & Act
        var cut = RenderGrid([]);

        // Assert
        var emptyMessage = cut.Find(".text-muted");
        emptyMessage.TextContent.ShouldBe("No players in this club.");
    }

    [Fact]
    public void WhenPlayersExist_ShouldDisplayGrid()
    {
        // Arrange
        var players = CreateTestPlayers(2);

        // Act
        var cut = RenderGrid(players);

        // Assert
        cut.Find("table").ShouldNotBeNull();

        var rows = cut.FindAll("tbody tr");
        rows.Count.ShouldBe(2);

        cut.Markup.ShouldContain("Player1 Test1");
        cut.Markup.ShouldContain("Player2 Test2");
    }

    [Fact]
    public void WhenPlayersExist_ShouldDisplayJerseyNumbers()
    {
        // Arrange
        var players = CreateTestPlayers(2);

        // Act
        var cut = RenderGrid(players);

        // Assert
        var jerseyBadges = cut.FindAll(".badge.bg-primary");
        jerseyBadges.Count.ShouldBe(2);
    }

    [Fact]
    public void WhenPlayersExist_ShouldDisplayTryoutNumbers()
    {
        // Arrange
        var players = CreateTestPlayers(2);

        // Act
        var cut = RenderGrid(players);

        // Assert
        var tryoutBadges = cut.FindAll(".badge.bg-secondary");
        tryoutBadges.Count.ShouldBe(2);
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

        // Act
        var cut = RenderGrid(players);

        // Assert
        // Jersey and tryout columns should have dashes
        var dashes = cut.FindAll("span.text-muted");
        dashes.Count.ShouldBeGreaterThanOrEqualTo(2);
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

        // Act
        var cut = RenderGrid(players);

        // Assert
        // Gender column should have a dash
        cut.Markup.ShouldContain("<span class=\"text-muted\">—</span>");
    }

    #endregion

    #region Search/Filter Tests

    [Fact]
    public void WhenSearchTermIsEmpty_ShouldDisplayAllPlayers()
    {
        // Arrange
        var players = CreateTestPlayers(3);

        // Act
        var cut = RenderGrid(players);

        // Assert
        var rows = cut.FindAll("tbody tr");
        rows.Count.ShouldBe(3);
    }

    [Fact]
    public void WhenSearchByFullName_ShouldFilterPlayers()
    {
        // Arrange
        var players = CreateTestPlayers(3);
        var cut = RenderGrid(players);

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
        var cut = RenderGrid(players);

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
        var cut = RenderGrid(players);

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
        var cut = RenderGrid(players);

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
        var cut = RenderGrid(players);

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
        var cut = RenderGrid(players);

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
        var cut = RenderGrid(players);

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
