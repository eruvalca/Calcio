using Bogus;

using Calcio.Data.Contexts;
using Calcio.IntegrationTests.Data.Contexts;
using Calcio.Services.ClubJoinRequests;
using Calcio.Entities;
using Calcio.Shared.Enums;

using Calcio.Shared.Results;
using Calcio.Shared.Security;
using Calcio.Shared.Services.UserClubsCache;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Shouldly;

namespace Calcio.IntegrationTests.Services.ClubJoinRequests;

/// <summary>
/// Contains integration tests for club join request service behavior.
/// </summary>
/// <param name="factory">Provides dependencies used to build the integration test host.</param>
public class ClubJoinRequestServiceTests(CustomApplicationFactory factory) : BaseDbContextTests(factory)
{
    /// <summary>
    /// Stores the unaffiliated user id value used by this test suite.
    /// </summary>
    private const long UnaffiliatedUserId = 100;

    /// <summary>
    /// Initializes shared test data required by this test suite.
    /// </summary>
    /// <returns>A value task that represents the asynchronous initialization operation.</returns>
    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();

        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<CalcioUserEntity>>();

        // Create an unaffiliated user (no club) for join request testing using UserManager
        // This ensures the user has proper security stamp for role assignments
        if (!await dbContext.Users.IgnoreQueryFilters().AnyAsync(u => u.Id == UnaffiliatedUserId))
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

        // Clean up any existing join requests from previous test runs
        var existingRequests = await dbContext.ClubJoinRequests
            .IgnoreQueryFilters()
            .ToListAsync();

        if (existingRequests.Count > 0)
        {
            dbContext.ClubJoinRequests.RemoveRange(existingRequests);
            await dbContext.SaveChangesAsync();
        }
    }

    #region GetRequestForCurrentUserAsync Tests

    [Fact]
    /// <summary>
    /// Verifies that get request for current user async when pending request exists returns request.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task GetRequestForCurrentUserAsync_WhenPendingRequestExists_ReturnsRequest()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UnaffiliatedUserId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();
        var service = CreateService(scope.ServiceProvider);

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

        // Act
        var result = await service.GetRequestForCurrentUserAsync(cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var dto = result.Value;
        dto.ClubId.ShouldBe(club.ClubId);
        dto.RequestingUserId.ShouldBe(UnaffiliatedUserId);
        dto.Status.ShouldBe(RequestStatus.Pending);
    }

    [Fact]
    /// <summary>
    /// Verifies that get request for current user async when rejected request exists returns request.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task GetRequestForCurrentUserAsync_WhenRejectedRequestExists_ReturnsRequest()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UnaffiliatedUserId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var club = await dbContext.Clubs.IgnoreQueryFilters().FirstAsync(cancellationToken);
        var joinRequest = new ClubJoinRequestEntity
        {
            ClubId = club.ClubId,
            RequestingUserId = UnaffiliatedUserId,
            Status = RequestStatus.Rejected,
            CreatedById = UnaffiliatedUserId
        };
        await dbContext.ClubJoinRequests.AddAsync(joinRequest, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Act
        var result = await service.GetRequestForCurrentUserAsync(cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var dto = result.Value;
        dto.ClubId.ShouldBe(club.ClubId);
        dto.Status.ShouldBe(RequestStatus.Rejected);
    }

    [Fact]
    /// <summary>
    /// Verifies that get request for current user async when no request returns not found.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task GetRequestForCurrentUserAsync_WhenNoRequest_ReturnsNotFound()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UnaffiliatedUserId);

        var service = CreateService(scope.ServiceProvider);

        // Act
        var result = await service.GetRequestForCurrentUserAsync(cancellationToken);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.NotFound);
    }

    #endregion

    #region CreateJoinRequestAsync Tests

    [Fact]
    /// <summary>
    /// Verifies that create join request async when club exists and no pending request returns success.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task CreateJoinRequestAsync_WhenClubExistsAndNoPendingRequest_ReturnsSuccess()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UnaffiliatedUserId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var club = await dbContext.Clubs.IgnoreQueryFilters().FirstAsync(cancellationToken);

        // Act
        var result = await service.CreateJoinRequestAsync(club.ClubId, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        // Verify request was created
        var createdRequest = await dbContext.ClubJoinRequests
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.RequestingUserId == UnaffiliatedUserId && r.ClubId == club.ClubId, cancellationToken);

        createdRequest.ShouldNotBeNull();
        createdRequest.Status.ShouldBe(RequestStatus.Pending);
    }

    [Fact]
    /// <summary>
    /// Verifies that create join request async when pending request already exists returns conflict.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task CreateJoinRequestAsync_WhenPendingRequestAlreadyExists_ReturnsConflict()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UnaffiliatedUserId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var club = await dbContext.Clubs.IgnoreQueryFilters().FirstAsync(cancellationToken);

        // Create existing pending request
        var existingRequest = new ClubJoinRequestEntity
        {
            ClubId = club.ClubId,
            RequestingUserId = UnaffiliatedUserId,
            Status = RequestStatus.Pending,
            CreatedById = UnaffiliatedUserId
        };
        await dbContext.ClubJoinRequests.AddAsync(existingRequest, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Act
        var result = await service.CreateJoinRequestAsync(club.ClubId, cancellationToken);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.Conflict);
    }

    [Fact]
    /// <summary>
    /// Verifies that create join request async when club does not exist returns not found.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task CreateJoinRequestAsync_WhenClubDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UnaffiliatedUserId);

        var service = CreateService(scope.ServiceProvider);

        // Act
        var result = await service.CreateJoinRequestAsync(999999, cancellationToken);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.NotFound);
    }

    [Fact]
    /// <summary>
    /// Verifies that create join request async when rejected request exists deletes old and creates new.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task CreateJoinRequestAsync_WhenRejectedRequestExists_DeletesOldAndCreatesNew()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UnaffiliatedUserId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var clubs = await dbContext.Clubs.IgnoreQueryFilters().Take(2).ToListAsync(cancellationToken);
        var firstClub = clubs[0];
        var secondClub = clubs[1];

        // Create existing rejected request for first club
        var existingRequest = new ClubJoinRequestEntity
        {
            ClubId = firstClub.ClubId,
            RequestingUserId = UnaffiliatedUserId,
            Status = RequestStatus.Rejected,
            CreatedById = UnaffiliatedUserId
        };
        await dbContext.ClubJoinRequests.AddAsync(existingRequest, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        var oldRequestId = existingRequest.ClubJoinRequestId;

        // Act - request to join second club
        var result = await service.CreateJoinRequestAsync(secondClub.ClubId, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        // Verify old rejected request was deleted
        dbContext.ChangeTracker.Clear();
        var deletedRequest = await dbContext.ClubJoinRequests
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.ClubJoinRequestId == oldRequestId, cancellationToken);
        deletedRequest.ShouldBeNull();

        // Verify new request was created
        var newRequest = await dbContext.ClubJoinRequests
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.RequestingUserId == UnaffiliatedUserId && r.ClubId == secondClub.ClubId, cancellationToken);
        newRequest.ShouldNotBeNull();
        newRequest.Status.ShouldBe(RequestStatus.Pending);
    }

    #endregion

    #region CancelJoinRequestAsync Tests

    [Fact]
    /// <summary>
    /// Verifies that cancel join request async when pending request exists returns success and deletes request.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task CancelJoinRequestAsync_WhenPendingRequestExists_ReturnsSuccessAndDeletesRequest()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UnaffiliatedUserId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();
        var service = CreateService(scope.ServiceProvider);

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

        // Act
        var result = await service.CancelJoinRequestAsync(cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        // Verify request was deleted
        var deletedRequest = await dbContext.ClubJoinRequests
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.RequestingUserId == UnaffiliatedUserId && r.Status == RequestStatus.Pending, cancellationToken);

        deletedRequest.ShouldBeNull();
    }

    [Fact]
    /// <summary>
    /// Verifies that cancel join request async when no pending request returns not found.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task CancelJoinRequestAsync_WhenNoPendingRequest_ReturnsNotFound()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UnaffiliatedUserId);

        var service = CreateService(scope.ServiceProvider);

        // Act
        var result = await service.CancelJoinRequestAsync(cancellationToken);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.NotFound);
    }

    #endregion

    #region GetPendingRequestsForClubAsync Tests

    [Fact]
    /// <summary>
    /// Verifies that get pending requests for club async when user is member returns requests.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task GetPendingRequestsForClubAsync_WhenUserIsMember_ReturnsRequests()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var club = await dbContext.Clubs.FirstAsync(cancellationToken);
        _ = await dbContext.Users.IgnoreQueryFilters().FirstAsync(u => u.Id == UnaffiliatedUserId, cancellationToken);

        var joinRequest = new ClubJoinRequestEntity
        {
            ClubId = club.ClubId,
            RequestingUserId = UnaffiliatedUserId,
            Status = RequestStatus.Pending,
            CreatedById = UnaffiliatedUserId
        };
        await dbContext.ClubJoinRequests.AddAsync(joinRequest, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Act
        var result = await service.GetPendingRequestsForClubAsync(club.ClubId, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var requests = result.Value;
        requests.ShouldNotBeEmpty();
        requests.ShouldContain(r => r.RequestingUserId == UnaffiliatedUserId);
    }

    [Fact]
    /// <summary>
    /// Verifies that get pending requests for club async when user is not member returns empty list.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task GetPendingRequestsForClubAsync_WhenUserIsNotMember_ReturnsEmptyList()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();
        var service = CreateService(scope.ServiceProvider);

        // Get the other user's club (which UserA is not a member of)
        var otherClub = await dbContext.Clubs
            .IgnoreQueryFilters()
            .Include(c => c.CalcioUsers)
            .FirstAsync(c => c.CalcioUsers.All(u => u.Id != UserAId), cancellationToken);

        // Act
        var result = await service.GetPendingRequestsForClubAsync(otherClub.ClubId, cancellationToken);

        // Assert - Global query filters return empty result for clubs user doesn't belong to
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }

    [Fact]
    /// <summary>
    /// Verifies that get pending requests for club async when no requests returns empty list.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task GetPendingRequestsForClubAsync_WhenNoRequests_ReturnsEmptyList()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var club = await dbContext.Clubs.FirstAsync(cancellationToken);

        // Act
        var result = await service.GetPendingRequestsForClubAsync(club.ClubId, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }

    #endregion

    #region UpdateJoinRequestStatusAsync Tests - Approve

    [Fact]
    /// <summary>
    /// Verifies that update join request status async when approving returns success and updates user club and adds role.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task UpdateJoinRequestStatusAsync_WhenApproving_ReturnsSuccessAndUpdatesUserClubAndAddsRole()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<CalcioUserEntity>>();
        var service = CreateService(scope.ServiceProvider);

        var club = await dbContext.Clubs.FirstAsync(cancellationToken);
        var joinRequest = new ClubJoinRequestEntity
        {
            ClubId = club.ClubId,
            RequestingUserId = UnaffiliatedUserId,
            Status = RequestStatus.Pending,
            CreatedById = UnaffiliatedUserId
        };
        await dbContext.ClubJoinRequests.AddAsync(joinRequest, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        var requestId = joinRequest.ClubJoinRequestId;

        // Act
        var result = await service.UpdateJoinRequestStatusAsync(club.ClubId, requestId, RequestStatus.Approved, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        // Verify request was deleted (not just status changed)
        dbContext.ChangeTracker.Clear();
        var deletedRequest = await dbContext.ClubJoinRequests
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.ClubJoinRequestId == requestId, cancellationToken);

        deletedRequest.ShouldBeNull();

        // Verify user was added to club
        var updatedUser = await dbContext.Users.IgnoreQueryFilters().FirstAsync(u => u.Id == UnaffiliatedUserId, cancellationToken);
        updatedUser.ClubId.ShouldBe(club.ClubId);

        // Verify user was assigned the StandardUser role
        var isInRole = await userManager.IsInRoleAsync(updatedUser, Roles.StandardUser);
        isInRole.ShouldBeTrue();
    }

    [Fact]
    /// <summary>
    /// Verifies that update join request status async when approving and request does not exist returns not found.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task UpdateJoinRequestStatusAsync_WhenApprovingAndRequestDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var club = await dbContext.Clubs.FirstAsync(cancellationToken);

        // Act
        var result = await service.UpdateJoinRequestStatusAsync(club.ClubId, 999999, RequestStatus.Approved, cancellationToken);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.NotFound);
    }

    [Fact]
    /// <summary>
    /// Verifies that update join request status async when approving and user is not member returns not found.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task UpdateJoinRequestStatusAsync_WhenApprovingAndUserIsNotMember_ReturnsNotFound()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();
        var service = CreateService(scope.ServiceProvider);

        // Get the other user's club
        var otherClub = await dbContext.Clubs
            .IgnoreQueryFilters()
            .Include(c => c.CalcioUsers)
            .FirstAsync(c => c.CalcioUsers.All(u => u.Id != UserAId), cancellationToken);

        var joinRequest = new ClubJoinRequestEntity
        {
            ClubId = otherClub.ClubId,
            RequestingUserId = UnaffiliatedUserId,
            Status = RequestStatus.Pending,
            CreatedById = UnaffiliatedUserId
        };
        await dbContext.ClubJoinRequests.AddAsync(joinRequest, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Act
        var result = await service.UpdateJoinRequestStatusAsync(otherClub.ClubId, joinRequest.ClubJoinRequestId, RequestStatus.Approved, cancellationToken);

        // Assert - Global query filters hide the join request, so it appears as NotFound
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.NotFound);
    }

    #endregion

    #region UpdateJoinRequestStatusAsync Tests - Reject

    [Fact]
    /// <summary>
    /// Verifies that update join request status async when rejecting returns success and updates status.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task UpdateJoinRequestStatusAsync_WhenRejecting_ReturnsSuccessAndUpdatesStatus()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var club = await dbContext.Clubs.FirstAsync(cancellationToken);
        var joinRequest = new ClubJoinRequestEntity
        {
            ClubId = club.ClubId,
            RequestingUserId = UnaffiliatedUserId,
            Status = RequestStatus.Pending,
            CreatedById = UnaffiliatedUserId
        };
        await dbContext.ClubJoinRequests.AddAsync(joinRequest, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        var requestId = joinRequest.ClubJoinRequestId;

        // Act
        var result = await service.UpdateJoinRequestStatusAsync(club.ClubId, requestId, RequestStatus.Rejected, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        // Verify request status was updated to Rejected (not deleted)
        dbContext.ChangeTracker.Clear();
        var updatedRequest = await dbContext.ClubJoinRequests
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.ClubJoinRequestId == requestId, cancellationToken);

        updatedRequest.ShouldNotBeNull();
        updatedRequest.Status.ShouldBe(RequestStatus.Rejected);
    }

    [Fact]
    /// <summary>
    /// Verifies that update join request status async when rejecting and request does not exist returns not found.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task UpdateJoinRequestStatusAsync_WhenRejectingAndRequestDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var club = await dbContext.Clubs.FirstAsync(cancellationToken);

        // Act
        var result = await service.UpdateJoinRequestStatusAsync(club.ClubId, 999999, RequestStatus.Rejected, cancellationToken);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.NotFound);
    }

    [Fact]
    /// <summary>
    /// Verifies that update join request status async when rejecting and user is not member returns not found.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task UpdateJoinRequestStatusAsync_WhenRejectingAndUserIsNotMember_ReturnsNotFound()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();
        var service = CreateService(scope.ServiceProvider);

        // Get the other user's club
        var otherClub = await dbContext.Clubs
            .IgnoreQueryFilters()
            .Include(c => c.CalcioUsers)
            .FirstAsync(c => c.CalcioUsers.All(u => u.Id != UserAId), cancellationToken);

        var joinRequest = new ClubJoinRequestEntity
        {
            ClubId = otherClub.ClubId,
            RequestingUserId = UnaffiliatedUserId,
            Status = RequestStatus.Pending,
            CreatedById = UnaffiliatedUserId
        };
        await dbContext.ClubJoinRequests.AddAsync(joinRequest, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Act
        var result = await service.UpdateJoinRequestStatusAsync(otherClub.ClubId, joinRequest.ClubJoinRequestId, RequestStatus.Rejected, cancellationToken);

        // Assert - Global query filters hide the join request, so it appears as NotFound
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.NotFound);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Creates the service under test using dependencies from the current scope.
    /// </summary>
    /// <param name="services">Specifies the services value for this scenario.</param>
    /// <returns>The club join requests service produced by the operation.</returns>
    private static ClubJoinRequestsService CreateService(IServiceProvider services)
    {
        var readOnlyFactory = services.GetRequiredService<IDbContextFactory<ReadOnlyDbContext>>();
        var readWriteFactory = services.GetRequiredService<IDbContextFactory<ReadWriteDbContext>>();
        var userManager = services.GetRequiredService<UserManager<CalcioUserEntity>>();
        var userClubsCacheService = services.GetRequiredService<IUserClubsCacheService>();
        var httpContextAccessor = services.GetRequiredService<IHttpContextAccessor>();
        var logger = services.GetRequiredService<ILogger<ClubJoinRequestsService>>();

        return new ClubJoinRequestsService(readOnlyFactory, readWriteFactory, userManager, userClubsCacheService, httpContextAccessor, logger);
    }

    #endregion
}
