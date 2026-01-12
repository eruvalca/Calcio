using Calcio.Data.Contexts;
using Calcio.IntegrationTests.Data.Contexts;
using Calcio.Services.Seasons;
using Calcio.Shared.DTOs.Seasons;
using Calcio.Shared.Entities;

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
        result.IsSuccess.ShouldBeTrue();
        var seasons = result.Value;
        seasons.ShouldNotBeEmpty();
        seasons.ShouldAllBe(s => s.SeasonId > 0);
        seasons.ShouldAllBe(s => !string.IsNullOrEmpty(s.Name));
    }

    [Fact]
    public async Task GetSeasonsAsync_WhenUserIsNotMember_ReturnsEmptyList()
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

        // Assert - Global query filters return empty result for clubs user doesn't belong to
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
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
        result.IsSuccess.ShouldBeTrue();
        var seasons = result.Value;

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
        result.IsSuccess.ShouldBeTrue();
        var seasons = result.Value;
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
        result.IsSuccess.ShouldBeTrue();
        var seasons = result.Value;
        var actualSeason = seasons.FirstOrDefault(s => s.SeasonId == expectedSeason.SeasonId);

        actualSeason.ShouldNotBeNull();
        actualSeason.Name.ShouldBe(expectedSeason.Name);
        actualSeason.StartDate.ShouldBe(expectedSeason.StartDate);
        actualSeason.EndDate.ShouldBe(expectedSeason.EndDate);
        actualSeason.IsComplete.ShouldBe(expectedSeason.IsComplete);
    }

    [Fact]
    public async Task GetSeasonsAsync_WhenClubDoesNotExist_ReturnsEmptyList()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var service = CreateService(scope.ServiceProvider);

        // Act
        var result = await service.GetSeasonsAsync(999999, cancellationToken);

        // Assert - Global query filters return empty result for non-existent clubs
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }

    #endregion

    #region CreateSeasonAsync Tests

    [Fact]
    public async Task CreateSeasonAsync_WhenValidInput_CreatesSeasonSuccessfully()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var readOnlyDbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var club = await readOnlyDbContext.Clubs.FirstAsync(cancellationToken);
        var dto = new CreateSeasonDto("New Test Season", DateOnly.FromDateTime(DateTime.Today.AddDays(1)), DateOnly.FromDateTime(DateTime.Today.AddMonths(3)));

        // Act
        var result = await service.CreateSeasonAsync(club.ClubId, dto, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        // Verify the season was created in the database
        var createdSeason = await readOnlyDbContext.Seasons
            .FirstOrDefaultAsync(s => s.Name == "New Test Season" && s.ClubId == club.ClubId, cancellationToken);

        createdSeason.ShouldNotBeNull();
        createdSeason.Name.ShouldBe(dto.Name);
        createdSeason.StartDate.ShouldBe(dto.StartDate);
        createdSeason.EndDate.ShouldBe(dto.EndDate);
        createdSeason.CreatedById.ShouldBe(UserAId);
    }

    [Fact]
    public async Task CreateSeasonAsync_WithoutEndDate_CreatesSeasonSuccessfully()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var readOnlyDbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var club = await readOnlyDbContext.Clubs.FirstAsync(cancellationToken);
        var dto = new CreateSeasonDto("Open-Ended Season", DateOnly.FromDateTime(DateTime.Today.AddDays(1)));

        // Act
        var result = await service.CreateSeasonAsync(club.ClubId, dto, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        var createdSeason = await readOnlyDbContext.Seasons
            .FirstOrDefaultAsync(s => s.Name == "Open-Ended Season" && s.ClubId == club.ClubId, cancellationToken);

        createdSeason.ShouldNotBeNull();
        createdSeason.EndDate.ShouldBeNull();
        createdSeason.IsComplete.ShouldBeFalse();
    }

    [Fact]
    public async Task CreateSeasonAsync_SetsCreatedByIdToCurrentUser()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var readOnlyDbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var club = await readOnlyDbContext.Clubs.FirstAsync(cancellationToken);
        var dto = new CreateSeasonDto("Season With CreatedBy", DateOnly.FromDateTime(DateTime.Today.AddDays(1)));

        // Act
        var result = await service.CreateSeasonAsync(club.ClubId, dto, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        var createdSeason = await readOnlyDbContext.Seasons
            .FirstOrDefaultAsync(s => s.Name == "Season With CreatedBy" && s.ClubId == club.ClubId, cancellationToken);

        createdSeason.ShouldNotBeNull();
        createdSeason.CreatedById.ShouldBe(UserAId);
    }

    [Fact]
    public async Task CreateSeasonAsync_WithEndDateInPast_SeasonIsComplete()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var readOnlyDbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var readWriteDbContext = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var club = await readOnlyDbContext.Clubs.FirstAsync(cancellationToken);

        // Create a season directly with a past end date for testing IsComplete logic
        var pastSeason = new SeasonEntity
        {
            Name = "Past Season",
            StartDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(-6)),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
            ClubId = club.ClubId,
            CreatedById = UserAId
        };

        await readWriteDbContext.Seasons.AddAsync(pastSeason, cancellationToken);
        await readWriteDbContext.SaveChangesAsync(cancellationToken);

        // Act - Retrieve the season and verify IsComplete
        var retrievedSeason = await readOnlyDbContext.Seasons
            .FirstOrDefaultAsync(s => s.Name == "Past Season" && s.ClubId == club.ClubId, cancellationToken);

        // Assert
        retrievedSeason.ShouldNotBeNull();
        retrievedSeason.IsComplete.ShouldBeTrue();
    }

    [Fact]
    public async Task CreateSeasonAsync_WithEndDateInFuture_SeasonIsNotComplete()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var readOnlyDbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var club = await readOnlyDbContext.Clubs.FirstAsync(cancellationToken);
        var dto = new CreateSeasonDto("Future End Season", DateOnly.FromDateTime(DateTime.Today.AddDays(1)), DateOnly.FromDateTime(DateTime.Today.AddMonths(6)));

        // Act
        var result = await service.CreateSeasonAsync(club.ClubId, dto, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        var createdSeason = await readOnlyDbContext.Seasons
            .FirstOrDefaultAsync(s => s.Name == "Future End Season" && s.ClubId == club.ClubId, cancellationToken);

        createdSeason.ShouldNotBeNull();
        createdSeason.EndDate.ShouldNotBeNull();
        createdSeason.IsComplete.ShouldBeFalse();
    }

    #endregion

    #region Helpers

    private static SeasonsService CreateService(IServiceProvider services)
    {
        var readOnlyFactory = services.GetRequiredService<IDbContextFactory<ReadOnlyDbContext>>();
        var readWriteFactory = services.GetRequiredService<IDbContextFactory<ReadWriteDbContext>>();
        var httpContextAccessor = services.GetRequiredService<IHttpContextAccessor>();
        var logger = services.GetRequiredService<ILogger<SeasonsService>>();

        return new SeasonsService(readOnlyFactory, readWriteFactory, httpContextAccessor, logger);
    }

    #endregion
}
