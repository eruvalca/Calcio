using Bogus;

using Calcio.Data.Contexts;
using Calcio.IntegrationTests.Data.Contexts;
using Calcio.Services.Teams;
using Calcio.Shared.DTOs.Teams;
using Calcio.Shared.Entities;
using Calcio.Shared.Security;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Shouldly;

namespace Calcio.IntegrationTests.Services.Teams;

public class TeamServiceTests(CustomApplicationFactory factory) : BaseDbContextTests(factory)
{
    private const long StandardMemberUserId = 400;

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
    #region GetTeamsAsync Tests

    [Fact]
    public async Task GetTeamsAsync_WhenUserIsMember_ReturnsTeams()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var club = await dbContext.Clubs.FirstAsync(cancellationToken);

        // Act
        var result = await service.GetTeamsAsync(club.ClubId, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var teams = result.Value;
        teams.ShouldNotBeEmpty();
        teams.ShouldAllBe(t => t.TeamId > 0);
        teams.ShouldAllBe(t => !string.IsNullOrEmpty(t.Name));
    }

    [Fact]
    public async Task GetTeamsAsync_WhenUserIsNotMember_ReturnsEmptyList()
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
        var result = await service.GetTeamsAsync(otherClub.ClubId, cancellationToken);

        // Assert - Global query filters return empty result for clubs user doesn't belong to
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetTeamsAsync_ReturnsTeamsOrderedByName()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var club = await dbContext.Clubs.FirstAsync(cancellationToken);

        // Act
        var result = await service.GetTeamsAsync(club.ClubId, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var teams = result.Value;

        if (teams.Count > 1)
        {
            // Verify ascending order by name
            for (var i = 0; i < teams.Count - 1; i++)
            {
                string.Compare(teams[i].Name, teams[i + 1].Name, StringComparison.Ordinal)
                    .ShouldBeLessThanOrEqualTo(0);
            }
        }
    }

    [Fact]
    public async Task GetTeamsAsync_ReturnsOnlyTeamsForSpecifiedClub()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var club = await dbContext.Clubs.FirstAsync(cancellationToken);

        // Get count of teams for the specific club from the database
        var expectedCount = await dbContext.Teams
            .Where(t => t.ClubId == club.ClubId)
            .CountAsync(cancellationToken);

        // Act
        var result = await service.GetTeamsAsync(club.ClubId, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var teams = result.Value;
        teams.Count.ShouldBe(expectedCount);
    }

    [Fact]
    public async Task GetTeamsAsync_ReturnsCorrectTeamProperties()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var club = await dbContext.Clubs.FirstAsync(cancellationToken);
        var expectedTeam = await dbContext.Teams
            .Where(t => t.ClubId == club.ClubId)
            .FirstAsync(cancellationToken);

        // Act
        var result = await service.GetTeamsAsync(club.ClubId, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var teams = result.Value;
        var actualTeam = teams.FirstOrDefault(t => t.TeamId == expectedTeam.TeamId);

        actualTeam.ShouldNotBeNull();
        actualTeam.Name.ShouldBe(expectedTeam.Name);
        actualTeam.GraduationYear.ShouldBe(expectedTeam.GraduationYear);
    }

    [Fact]
    public async Task GetTeamsAsync_WhenClubDoesNotExist_ReturnsEmptyList()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var service = CreateService(scope.ServiceProvider);

        // Act
        var result = await service.GetTeamsAsync(999999, cancellationToken);

        // Assert - Global query filters return empty result for non-existent clubs
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }

    #endregion

    #region CreateTeamAsync Tests

    [Fact]
    public async Task CreateTeamAsync_WhenValidInput_CreatesTeamSuccessfully()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var readOnlyDbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var club = await readOnlyDbContext.Clubs.FirstAsync(cancellationToken);
        var dto = new CreateTeamDto("New Test Team", 2030);

        // Act
        var result = await service.CreateTeamAsync(club.ClubId, dto, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        // Verify the team was created in the database
        var createdTeam = await readOnlyDbContext.Teams
            .FirstOrDefaultAsync(t => t.Name == "New Test Team" && t.ClubId == club.ClubId, cancellationToken);

        createdTeam.ShouldNotBeNull();
        createdTeam.Name.ShouldBe(dto.Name);
        createdTeam.GraduationYear.ShouldBe(dto.GraduationYear);
        createdTeam.CreatedById.ShouldBe(UserAId);
    }

    [Fact]
    public async Task CreateTeamAsync_SetsCreatedByIdToCurrentUser()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var readOnlyDbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var club = await readOnlyDbContext.Clubs.FirstAsync(cancellationToken);
        var dto = new CreateTeamDto("Team With CreatedBy", 2028);

        // Act
        var result = await service.CreateTeamAsync(club.ClubId, dto, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        var createdTeam = await readOnlyDbContext.Teams
            .FirstOrDefaultAsync(t => t.Name == "Team With CreatedBy" && t.ClubId == club.ClubId, cancellationToken);

        createdTeam.ShouldNotBeNull();
        createdTeam.CreatedById.ShouldBe(UserAId);
    }

    [Fact]
    public async Task CreateTeamAsync_WithGraduationYear_StoresGraduationYearCorrectly()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var readOnlyDbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var club = await readOnlyDbContext.Clubs.FirstAsync(cancellationToken);
        var graduationYear = DateTime.Today.Year + 5;
        var dto = new CreateTeamDto("Team With Year", graduationYear);

        // Act
        var result = await service.CreateTeamAsync(club.ClubId, dto, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        var createdTeam = await readOnlyDbContext.Teams
            .FirstOrDefaultAsync(t => t.Name == "Team With Year" && t.ClubId == club.ClubId, cancellationToken);

        createdTeam.ShouldNotBeNull();
        createdTeam.GraduationYear.ShouldBe(graduationYear);
    }

    [Fact]
    public async Task CreateTeamAsync_WhenRegularMemberNotAdmin_ReturnsCreatedTeam()
    {
        // Arrange - Use non-admin member to verify authorization changes allow regular members to create teams
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, StandardMemberUserId);

        var readOnlyDbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var club = await readOnlyDbContext.Clubs.FirstAsync(cancellationToken);
        var dto = new CreateTeamDto("Member Created Team", 2029);

        // Act
        var result = await service.CreateTeamAsync(club.ClubId, dto, cancellationToken);

        // Assert - Regular members can now create teams
        result.IsSuccess.ShouldBeTrue();

        var createdTeam = await readOnlyDbContext.Teams
            .FirstOrDefaultAsync(t => t.Name == "Member Created Team" && t.ClubId == club.ClubId, cancellationToken);

        createdTeam.ShouldNotBeNull();
        createdTeam.Name.ShouldBe(dto.Name);
        createdTeam.GraduationYear.ShouldBe(dto.GraduationYear);
        createdTeam.CreatedById.ShouldBe(StandardMemberUserId);
    }

    #endregion

    #region Helpers

    private static TeamsService CreateService(IServiceProvider services)
    {
        var readOnlyFactory = services.GetRequiredService<IDbContextFactory<ReadOnlyDbContext>>();
        var readWriteFactory = services.GetRequiredService<IDbContextFactory<ReadWriteDbContext>>();
        var httpContextAccessor = services.GetRequiredService<IHttpContextAccessor>();
        var logger = services.GetRequiredService<ILogger<TeamsService>>();

        return new TeamsService(readOnlyFactory, readWriteFactory, httpContextAccessor, logger);
    }

    #endregion
}
