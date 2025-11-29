using Calcio.Data.Contexts;
using Calcio.IntegrationTests.Data.Contexts;
using Calcio.Services.Seasons;
using Calcio.Shared.Results;

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using OneOf.Types;

using Shouldly;

namespace Calcio.IntegrationTests.Services.Seasons;

public class SeasonServiceTests(CustomApplicationFactory factory) : BaseDbContextTests(factory)
{
    #region GetSeasonsAsync Tests

    [Fact]
    public async Task GetSeasonsAsync_WhenUserIsMember_ReturnsSeasons()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var club = await dbContext.Clubs.FirstAsync(cancellationToken);

        // Act
        var result = await service.GetSeasonsAsync(club.ClubId, cancellationToken);

        // Assert
        result.IsT0.ShouldBeTrue();
        var seasons = result.AsT0;
        seasons.ShouldNotBeEmpty();
        seasons.ShouldAllBe(s => s.SeasonId > 0);
        seasons.ShouldAllBe(s => !string.IsNullOrEmpty(s.Name));
    }

    [Fact]
    public async Task GetSeasonsAsync_WhenUserIsNotMember_ReturnsUnauthorized()
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
        var result = await service.GetSeasonsAsync(otherClub.ClubId, cancellationToken);

        // Assert
        result.IsT1.ShouldBeTrue();
        result.AsT1.ShouldBeOfType<Unauthorized>();
    }

    [Fact]
    public async Task GetSeasonsAsync_ReturnsSeasonsOrderedByStartDateDescending()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var club = await dbContext.Clubs.FirstAsync(cancellationToken);

        // Act
        var result = await service.GetSeasonsAsync(club.ClubId, cancellationToken);

        // Assert
        result.IsT0.ShouldBeTrue();
        var seasons = result.AsT0;

        if (seasons.Count > 1)
        {
            // Verify descending order by start date
            for (var i = 0; i < seasons.Count - 1; i++)
            {
                seasons[i].StartDate.ShouldBeGreaterThanOrEqualTo(seasons[i + 1].StartDate);
            }
        }
    }

    [Fact]
    public async Task GetSeasonsAsync_ReturnsOnlySeasonsForSpecifiedClub()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var club = await dbContext.Clubs.FirstAsync(cancellationToken);

        // Get count of seasons for the specific club from the database
        var expectedCount = await dbContext.Seasons
            .Where(s => s.ClubId == club.ClubId)
            .CountAsync(cancellationToken);

        // Act
        var result = await service.GetSeasonsAsync(club.ClubId, cancellationToken);

        // Assert
        result.IsT0.ShouldBeTrue();
        var seasons = result.AsT0;
        seasons.Count.ShouldBe(expectedCount);
    }

    [Fact]
    public async Task GetSeasonsAsync_ReturnsCorrectSeasonProperties()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var club = await dbContext.Clubs.FirstAsync(cancellationToken);
        var expectedSeason = await dbContext.Seasons
            .Where(s => s.ClubId == club.ClubId)
            .FirstAsync(cancellationToken);

        // Act
        var result = await service.GetSeasonsAsync(club.ClubId, cancellationToken);

        // Assert
        result.IsT0.ShouldBeTrue();
        var seasons = result.AsT0;
        var actualSeason = seasons.FirstOrDefault(s => s.SeasonId == expectedSeason.SeasonId);

        actualSeason.ShouldNotBeNull();
        actualSeason.Name.ShouldBe(expectedSeason.Name);
        actualSeason.StartDate.ShouldBe(expectedSeason.StartDate);
        actualSeason.EndDate.ShouldBe(expectedSeason.EndDate);
        actualSeason.IsComplete.ShouldBe(expectedSeason.IsComplete);
    }

    [Fact]
    public async Task GetSeasonsAsync_WhenClubDoesNotExist_ReturnsUnauthorized()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var service = CreateService(scope.ServiceProvider);

        // Act
        var result = await service.GetSeasonsAsync(999999, cancellationToken);

        // Assert
        result.IsT1.ShouldBeTrue();
        result.AsT1.ShouldBeOfType<Unauthorized>();
    }

    #endregion

    #region Helpers

    private static SeasonsService CreateService(IServiceProvider services)
    {
        var readOnlyFactory = services.GetRequiredService<IDbContextFactory<ReadOnlyDbContext>>();
        var httpContextAccessor = services.GetRequiredService<IHttpContextAccessor>();
        var logger = services.GetRequiredService<ILogger<SeasonsService>>();

        return new SeasonsService(readOnlyFactory, httpContextAccessor, logger);
    }

    #endregion
}
