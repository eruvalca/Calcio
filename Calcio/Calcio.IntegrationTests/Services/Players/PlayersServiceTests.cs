using Calcio.Data.Contexts;
using Calcio.IntegrationTests.Data.Contexts;
using Calcio.Services.Players;
using Calcio.Shared.Results;

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Shouldly;

namespace Calcio.IntegrationTests.Services.Players;

public class PlayersServiceTests(CustomApplicationFactory factory) : BaseDbContextTests(factory)
{
    #region GetClubPlayersAsync Tests

    [Fact]
    public async Task GetClubPlayersAsync_WhenUserIsMember_ReturnsPlayers()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var club = await dbContext.Clubs.FirstAsync(cancellationToken);

        // Act
        var result = await service.GetClubPlayersAsync(club.ClubId, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var players = result.Value;
        players.ShouldNotBeEmpty();
        players.All(p => p.PlayerId > 0).ShouldBeTrue();
        players.All(p => !string.IsNullOrEmpty(p.FirstName)).ShouldBeTrue();
        players.All(p => !string.IsNullOrEmpty(p.LastName)).ShouldBeTrue();
        players.All(p => !string.IsNullOrEmpty(p.FullName)).ShouldBeTrue();
    }

    [Fact]
    public async Task GetClubPlayersAsync_WhenUserIsNotMember_ReturnsEmptyList()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);

        // Get the other user's club (which UserA is not a member of)
        var otherClub = await dbContext.Clubs
            .IgnoreQueryFilters()
            .Include(c => c.CalcioUsers)
            .FirstAsync(c => c.CalcioUsers.All(u => u.Id != UserAId), cancellationToken);

        // Act
        var result = await service.GetClubPlayersAsync(otherClub.ClubId, cancellationToken);

        // Assert - Global query filters return empty result for clubs user doesn't belong to
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetClubPlayersAsync_ShouldReturnPlayersOrderedByLastNameThenFirstName()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var club = await dbContext.Clubs.FirstAsync(cancellationToken);

        // Act
        var result = await service.GetClubPlayersAsync(club.ClubId, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var players = result.Value;

        if (players.Count > 1)
        {
            // Verify ordering: by last name, then by first name
            var sortedPlayers = players
                .OrderBy(p => p.LastName)
                .ThenBy(p => p.FirstName)
                .ToList();

            players.ShouldBe(sortedPlayers);
        }
    }

    [Fact]
    public async Task GetClubPlayersAsync_ShouldReturnCorrectPlayerData()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var club = await dbContext.Clubs.FirstAsync(cancellationToken);
        var expectedPlayer = await dbContext.Players.FirstAsync(p => p.ClubId == club.ClubId, cancellationToken);

        // Act
        var result = await service.GetClubPlayersAsync(club.ClubId, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var players = result.Value;

        var player = players.FirstOrDefault(p => p.PlayerId == expectedPlayer.PlayerId);
        player.ShouldNotBeNull();
        player.FirstName.ShouldBe(expectedPlayer.FirstName);
        player.LastName.ShouldBe(expectedPlayer.LastName);
        player.FullName.ShouldBe(expectedPlayer.FullName);
        player.DateOfBirth.ShouldBe(expectedPlayer.DateOfBirth);
        player.Gender.ShouldBe(expectedPlayer.Gender);
        player.JerseyNumber.ShouldBe(expectedPlayer.JerseyNumber);
        player.TryoutNumber.ShouldBe(expectedPlayer.TryoutNumber);
    }

    [Fact]
    public async Task GetClubPlayersAsync_WhenClubDoesNotExist_ReturnsEmptyList()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var service = CreateService(scope.ServiceProvider);

        // Act
        var result = await service.GetClubPlayersAsync(999999, cancellationToken);

        // Assert - Global query filters return empty result for non-existent clubs
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetClubPlayersAsync_ShouldOnlyReturnPlayersForSpecifiedClub()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var club = await dbContext.Clubs.FirstAsync(cancellationToken);

        // Act
        var result = await service.GetClubPlayersAsync(club.ClubId, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var players = result.Value;

        // Verify all returned players belong to the specified club
        // The query filter ensures this, but we can verify via the database
        var clubPlayerIds = await dbContext.Players
            .Where(p => p.ClubId == club.ClubId)
            .Select(p => p.PlayerId)
            .ToListAsync(cancellationToken);

        players.All(p => clubPlayerIds.Contains(p.PlayerId)).ShouldBeTrue();
    }

    #endregion

    #region Helpers

    private static PlayersService CreateService(IServiceProvider services)
    {
        var readOnlyFactory = services.GetRequiredService<IDbContextFactory<ReadOnlyDbContext>>();
        var httpContextAccessor = services.GetRequiredService<IHttpContextAccessor>();
        var logger = services.GetRequiredService<ILogger<PlayersService>>();

        return new PlayersService(readOnlyFactory, httpContextAccessor, logger);
    }

    #endregion
}
