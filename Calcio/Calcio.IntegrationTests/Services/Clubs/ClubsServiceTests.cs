using Bogus;

using Calcio.Data.Contexts;
using Calcio.IntegrationTests.Data.Contexts;
using Calcio.Services.Clubs;
using Calcio.Shared.DTOs.Clubs;
using Calcio.Shared.Entities;
using Calcio.Shared.Enums;

using Calcio.Shared.Results;
using Calcio.Shared.Security;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Shouldly;

namespace Calcio.IntegrationTests.Services.Clubs;

public class ClubsServiceTests(CustomApplicationFactory factory) : BaseDbContextTests(factory)
{
    private const long UnaffiliatedUserId = 200;

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();

        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<CalcioUserEntity>>();

        // Create an unaffiliated user (no club) for club creation testing using UserManager
        var existingUser = await dbContext.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == UnaffiliatedUserId);
        if (existingUser is null)
        {
            var userFaker = new Faker<CalcioUserEntity>()
                .RuleFor(u => u.FirstName, f => f.Name.FirstName())
                .RuleFor(u => u.LastName, f => f.Name.LastName())
                .RuleFor(u => u.UserName, f => f.Internet.Email())
                .RuleFor(u => u.Email, (f, u) => u.UserName);

            var unaffiliatedUser = userFaker.Generate();
            unaffiliatedUser.Id = UnaffiliatedUserId;
            unaffiliatedUser.ClubId = null;

            var result = await userManager.CreateAsync(unaffiliatedUser, "TestPassword123!");
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Failed to create unaffiliated user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
        else
        {
            // Reset user state: remove from club and remove ClubAdmin role
            if (existingUser.ClubId is not null)
            {
                // Delete any clubs created by this user (test clubs)
                var testClubs = await dbContext.Clubs
                    .IgnoreQueryFilters()
                    .Where(c => c.CreatedById == UnaffiliatedUserId)
                    .ToListAsync();

                if (testClubs.Count > 0)
                {
                    dbContext.Clubs.RemoveRange(testClubs);
                }

                existingUser.ClubId = null;
                await dbContext.SaveChangesAsync();

                // Remove ClubAdmin role if assigned
                var userForRole = await userManager.FindByIdAsync(UnaffiliatedUserId.ToString());
                if (userForRole is not null && await userManager.IsInRoleAsync(userForRole, Roles.ClubAdmin))
                {
                    await userManager.RemoveFromRoleAsync(userForRole, Roles.ClubAdmin);
                }
            }
        }

        // Clean up any join requests for the unaffiliated user
        var existingRequests = await dbContext.ClubJoinRequests
            .IgnoreQueryFilters()
            .Where(r => r.RequestingUserId == UnaffiliatedUserId)
            .ToListAsync();

        if (existingRequests.Count > 0)
        {
            dbContext.ClubJoinRequests.RemoveRange(existingRequests);
            await dbContext.SaveChangesAsync();
        }
    }

    #region GetUserClubsAsync Tests

    [Fact]
    public async Task GetUserClubsAsync_WhenUserBelongsToClub_ReturnsClubs()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var service = CreateService(scope.ServiceProvider);

        // Act
        var result = await service.GetUserClubsAsync(cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeEmpty();
        result.Value.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetUserClubsAsync_WhenUserHasNoClub_ReturnsEmptyList()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UnaffiliatedUserId);

        var service = CreateService(scope.ServiceProvider);

        // Act
        var result = await service.GetUserClubsAsync(cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }

    #endregion

    #region GetAllClubsForBrowsingAsync Tests

    [Fact]
    public async Task GetAllClubsForBrowsingAsync_ReturnsAllClubs()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UnaffiliatedUserId);

        var service = CreateService(scope.ServiceProvider);

        // Act
        var result = await service.GetAllClubsForBrowsingAsync(cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeEmpty();
        // Should return all clubs (at least 2 from base seeding)
        result.Value.Count.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetAllClubsForBrowsingAsync_IgnoresQueryFilters()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId); // User with club membership

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);

        // Get total club count ignoring filters
        var totalClubCount = await dbContext.Clubs.IgnoreQueryFilters().CountAsync(cancellationToken);

        // Act
        var result = await service.GetAllClubsForBrowsingAsync(cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(totalClubCount);
    }

    #endregion

    #region GetClubByIdAsync Tests

    [Fact]
    public async Task GetClubByIdAsync_WhenUserIsMember_ReturnsClub()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);

        // Get a club that UserA belongs to
        var userClub = await dbContext.Clubs.FirstOrDefaultAsync(cancellationToken);
        userClub.ShouldNotBeNull();

        // Act
        var result = await service.GetClubByIdAsync(userClub.ClubId, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(userClub.ClubId);
        result.Value.Name.ShouldBe(userClub.Name);
    }

    [Fact]
    public async Task GetClubByIdAsync_WhenUserIsNotMember_ReturnsNotFound()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);

        // Get a club that UserA does NOT belong to (UserB's club)
        var otherClub = await dbContext.Clubs
            .IgnoreQueryFilters()
            .Where(c => !c.CalcioUsers.Any(u => u.Id == UserAId))
            .FirstOrDefaultAsync(cancellationToken);
        otherClub.ShouldNotBeNull("Test requires at least one club that UserA is not a member of");

        // Act
        var result = await service.GetClubByIdAsync(otherClub.ClubId, cancellationToken);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.NotFound);
    }

    [Fact]
    public async Task GetClubByIdAsync_WhenClubDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var service = CreateService(scope.ServiceProvider);

        // Act
        var result = await service.GetClubByIdAsync(99999, cancellationToken);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.NotFound);
    }

    #endregion

    #region CreateClubAsync Tests

    [Fact]
    public async Task CreateClubAsync_WhenValidUser_ReturnsSuccessAndCreatesClub()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UnaffiliatedUserId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<CalcioUserEntity>>();
        var service = CreateService(scope.ServiceProvider);

        var createDto = new CreateClubDto("Test Club", "Test City", "TX");

        // Act
        var result = await service.CreateClubAsync(createDto, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Test Club");
        result.Value.ClubId.ShouldBeGreaterThan(0);

        // Verify club was created in database
        dbContext.ChangeTracker.Clear();
        var createdClub = await dbContext.Clubs
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.ClubId == result.Value.ClubId, cancellationToken);

        createdClub.ShouldNotBeNull();
        createdClub.Name.ShouldBe("Test Club");
        createdClub.City.ShouldBe("Test City");
        createdClub.State.ShouldBe("TX");

        // Verify user was added to club
        var updatedUser = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstAsync(u => u.Id == UnaffiliatedUserId, cancellationToken);
        updatedUser.ClubId.ShouldBe(result.Value.ClubId);

        // Verify user was assigned ClubAdmin role
        var isInRole = await userManager.IsInRoleAsync(updatedUser, Roles.ClubAdmin);
        isInRole.ShouldBeTrue();
    }

    [Fact]
    public async Task CreateClubAsync_WhenUserAlreadyHasClub_ReturnsConflict()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId); // User already in a club

        var service = CreateService(scope.ServiceProvider);

        var createDto = new CreateClubDto("Another Club", "Some City", "CA");

        // Act
        var result = await service.CreateClubAsync(createDto, cancellationToken);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.Conflict);
    }

    [Fact]
    public async Task CreateClubAsync_WhenUserHasPendingJoinRequest_ReturnsConflict()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UnaffiliatedUserId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();
        var service = CreateService(scope.ServiceProvider);

        // Create pending join request
        var club = await dbContext.Clubs.IgnoreQueryFilters().FirstAsync(cancellationToken);
        var joinRequest = new ClubJoinRequestEntity
        {
            ClubId = club.ClubId,
            RequestingUserId = UnaffiliatedUserId,
            Status = RequestStatus.Pending,
            CreatedById = UnaffiliatedUserId
        };
        await dbContext.ClubJoinRequests.AddAsync(joinRequest, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var createDto = new CreateClubDto("My Club", "My City", "NY");

        // Act
        var result = await service.CreateClubAsync(createDto, cancellationToken);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.Conflict);

        // Clean up
        dbContext.ClubJoinRequests.Remove(joinRequest);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    [Fact]
    public async Task CreateClubAsync_WhenUserHasRejectedJoinRequest_DeletesRequestAndCreatesClub()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UnaffiliatedUserId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();
        var service = CreateService(scope.ServiceProvider);

        // Create rejected join request
        var club = await dbContext.Clubs.IgnoreQueryFilters().FirstAsync(cancellationToken);
        var rejectedRequest = new ClubJoinRequestEntity
        {
            ClubId = club.ClubId,
            RequestingUserId = UnaffiliatedUserId,
            Status = RequestStatus.Rejected,
            CreatedById = UnaffiliatedUserId
        };
        await dbContext.ClubJoinRequests.AddAsync(rejectedRequest, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        var rejectedRequestId = rejectedRequest.ClubJoinRequestId;

        var createDto = new CreateClubDto("Fresh Start Club", "New City", "FL");

        // Act
        var result = await service.CreateClubAsync(createDto, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Fresh Start Club");

        // Verify rejected request was deleted
        dbContext.ChangeTracker.Clear();
        var deletedRequest = await dbContext.ClubJoinRequests
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.ClubJoinRequestId == rejectedRequestId, cancellationToken);
        deletedRequest.ShouldBeNull();
    }

    #endregion

    #region LeaveClubAsync Tests

    [Fact]
    public async Task LeaveClubAsync_WhenMemberAndNotClubAdmin_RemovesClubAndReturnsSuccess()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UnaffiliatedUserId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<CalcioUserEntity>>();
        var service = CreateService(scope.ServiceProvider);

        var club = await dbContext.Clubs.IgnoreQueryFilters().FirstAsync(cancellationToken);

        var user = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstAsync(u => u.Id == UnaffiliatedUserId, cancellationToken);

        user.ClubId = club.ClubId;
        await dbContext.SaveChangesAsync(cancellationToken);
        dbContext.ChangeTracker.Clear();

        var userForRole = await userManager.FindByIdAsync(UnaffiliatedUserId.ToString());
        userForRole.ShouldNotBeNull();

        // Ensure user is not a ClubAdmin and has StandardUser role to exercise role removal.
        if (await userManager.IsInRoleAsync(userForRole, Roles.ClubAdmin))
        {
            await userManager.RemoveFromRoleAsync(userForRole, Roles.ClubAdmin);
        }

        if (!await userManager.IsInRoleAsync(userForRole, Roles.StandardUser))
        {
            await userManager.AddToRoleAsync(userForRole, Roles.StandardUser);
        }

        // Act
        var result = await service.LeaveClubAsync(club.ClubId, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        dbContext.ChangeTracker.Clear();
        var updatedUser = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstAsync(u => u.Id == UnaffiliatedUserId, cancellationToken);

        updatedUser.ClubId.ShouldBeNull();

        var updatedUserForRole = await userManager.FindByIdAsync(UnaffiliatedUserId.ToString());
        updatedUserForRole.ShouldNotBeNull();
        (await userManager.IsInRoleAsync(updatedUserForRole, Roles.StandardUser)).ShouldBeFalse();
    }

    [Fact]
    public async Task LeaveClubAsync_WhenUserNotInClub_ReturnsNotFound()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UnaffiliatedUserId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var club = await dbContext.Clubs.IgnoreQueryFilters().FirstAsync(cancellationToken);
        var user = await dbContext.Users.IgnoreQueryFilters().FirstAsync(u => u.Id == UnaffiliatedUserId, cancellationToken);

        // Ensure user is not in this club
        user.ClubId = null;
        await dbContext.SaveChangesAsync(cancellationToken);
        dbContext.ChangeTracker.Clear();

        // Act
        var result = await service.LeaveClubAsync(club.ClubId, cancellationToken);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.NotFound);
    }

    [Fact]
    public async Task LeaveClubAsync_WhenUserIsClubAdmin_ReturnsForbiddenAndDoesNotLeave()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UnaffiliatedUserId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<CalcioUserEntity>>();
        var service = CreateService(scope.ServiceProvider);

        var club = await dbContext.Clubs.IgnoreQueryFilters().FirstAsync(cancellationToken);
        var user = await dbContext.Users.IgnoreQueryFilters().FirstAsync(u => u.Id == UnaffiliatedUserId, cancellationToken);
        user.ClubId = club.ClubId;
        await dbContext.SaveChangesAsync(cancellationToken);
        dbContext.ChangeTracker.Clear();

        var userForRole = await userManager.FindByIdAsync(UnaffiliatedUserId.ToString());
        userForRole.ShouldNotBeNull();

        if (!await userManager.IsInRoleAsync(userForRole, Roles.ClubAdmin))
        {
            await userManager.AddToRoleAsync(userForRole, Roles.ClubAdmin);
        }

        // Act
        var result = await service.LeaveClubAsync(club.ClubId, cancellationToken);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.Forbidden);

        dbContext.ChangeTracker.Clear();
        var updatedUser = await dbContext.Users.IgnoreQueryFilters().FirstAsync(u => u.Id == UnaffiliatedUserId, cancellationToken);
        updatedUser.ClubId.ShouldBe(club.ClubId);
    }

    #endregion

    #region Helpers

    private static ClubsService CreateService(IServiceProvider services)
    {
        var readOnlyFactory = services.GetRequiredService<IDbContextFactory<ReadOnlyDbContext>>();
        var readWriteFactory = services.GetRequiredService<IDbContextFactory<ReadWriteDbContext>>();
        var userManager = services.GetRequiredService<UserManager<CalcioUserEntity>>();
        var httpContextAccessor = services.GetRequiredService<IHttpContextAccessor>();
        var logger = services.GetRequiredService<ILogger<ClubsService>>();

        return new ClubsService(readOnlyFactory, readWriteFactory, userManager, httpContextAccessor, logger);
    }

    #endregion
}
