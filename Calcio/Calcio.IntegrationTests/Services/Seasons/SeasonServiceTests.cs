using Bogus;

using Calcio.Data.Contexts;
using Calcio.IntegrationTests.Data.Contexts;
using Calcio.Services.Seasons;
using Calcio.Shared.DTOs.Seasons;
using Calcio.Shared.Entities;
using Calcio.Shared.Security;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Shouldly;

namespace Calcio.IntegrationTests.Services.Seasons;

public class SeasonServiceTests(CustomApplicationFactory factory) : BaseDbContextTests(factory)
{
    private const long StandardMemberUserId = 500;

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();

        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<CalcioUserEntity>>();

        var club = await dbContext.Clubs.FirstAsync();

        // Create or reset the standard member user in UserA's club for testing
        var existingUser = await dbContext.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == StandardMemberUserId);
        if (existingUser is null)
        {
            var userFaker = new Faker<CalcioUserEntity>()
                .RuleFor(u => u.FirstName, f => f.Name.FirstName())
                .RuleFor(u => u.LastName, f => f.Name.LastName())
                .RuleFor(u => u.UserName, f => f.Internet.Email())
                .RuleFor(u => u.Email, (f, u) => u.UserName);

            var standardMember = userFaker.Generate();
            standardMember.Id = StandardMemberUserId;
            standardMember.ClubId = club.ClubId;

            var result = await userManager.CreateAsync(standardMember, "TestPassword123!");
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Failed to create standard member user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            // Add StandardUser role only (not ClubAdmin)
            await userManager.AddToRoleAsync(standardMember, Roles.StandardUser);
        }
        else if (existingUser.ClubId != club.ClubId)
        {
            // Reset the user's club membership if needed
            existingUser.ClubId = club.ClubId;
            await dbContext.SaveChangesAsync();
        }
    }
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

    [Fact]
    public async Task CreateSeasonAsync_WhenRegularMemberNotAdmin_ReturnsCreatedSeason()
    {
        // Arrange - Use non-admin member to verify authorization changes allow regular members to create seasons
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, StandardMemberUserId);

        var readOnlyDbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var club = await readOnlyDbContext.Clubs.FirstAsync(cancellationToken);
        var dto = new CreateSeasonDto("Member Created Season", DateOnly.FromDateTime(DateTime.Today.AddDays(1)));

        // Act
        var result = await service.CreateSeasonAsync(club.ClubId, dto, cancellationToken);

        // Assert - Regular members can now create seasons
        result.IsSuccess.ShouldBeTrue();

        var createdSeason = await readOnlyDbContext.Seasons
            .FirstOrDefaultAsync(s => s.Name == "Member Created Season" && s.ClubId == club.ClubId, cancellationToken);

        createdSeason.ShouldNotBeNull();
        createdSeason.Name.ShouldBe(dto.Name);
        createdSeason.StartDate.ShouldBe(dto.StartDate);
        createdSeason.CreatedById.ShouldBe(StandardMemberUserId);
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
