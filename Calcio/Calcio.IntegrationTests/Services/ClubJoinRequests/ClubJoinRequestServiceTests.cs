using System.Security.Claims;

using Bogus;

using Calcio.Data.Contexts;
using Calcio.IntegrationTests.Data.Contexts;
using Calcio.Services.ClubJoinRequests;
using Calcio.Shared.Enums;
using Calcio.Shared.Models.Entities;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using OneOf.Types;

using Shouldly;

namespace Calcio.IntegrationTests.Services.ClubJoinRequests;

public class ClubJoinRequestServiceTests(CustomApplicationFactory factory) : BaseDbContextTests(factory)
{
    private const long UnaffiliatedUserId = 100;

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

    #region GetPendingRequestForCurrentUserAsync Tests

    [Fact]
    public async Task GetPendingRequestForCurrentUserAsync_WhenPendingRequestExists_ReturnsRequest()
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
        dbContext.ClubJoinRequests.Add(joinRequest);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Act
        var result = await service.GetPendingRequestForCurrentUserAsync(cancellationToken);

        // Assert
        result.IsT0.ShouldBeTrue();
        var dto = result.AsT0;
        dto.ClubId.ShouldBe(club.ClubId);
        dto.RequestingUserId.ShouldBe(UnaffiliatedUserId);
        dto.Status.ShouldBe(RequestStatus.Pending);
    }

    [Fact]
    public async Task GetPendingRequestForCurrentUserAsync_WhenNoPendingRequest_ReturnsNotFound()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UnaffiliatedUserId);

        var service = CreateService(scope.ServiceProvider);

        // Act
        var result = await service.GetPendingRequestForCurrentUserAsync(cancellationToken);

        // Assert
        result.IsT1.ShouldBeTrue();
        result.AsT1.ShouldBeOfType<NotFound>();
    }

    [Fact]
    public async Task GetPendingRequestForCurrentUserAsync_WhenOnlyApprovedRequestExists_ReturnsNotFound()
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
            Status = RequestStatus.Approved,
            CreatedById = UnaffiliatedUserId
        };
        dbContext.ClubJoinRequests.Add(joinRequest);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Act
        var result = await service.GetPendingRequestForCurrentUserAsync(cancellationToken);

        // Assert
        result.IsT1.ShouldBeTrue();
    }

    #endregion

    #region CreateJoinRequestAsync Tests

    [Fact]
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
        result.IsT0.ShouldBeTrue();

        // Verify request was created
        var createdRequest = await dbContext.ClubJoinRequests
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.RequestingUserId == UnaffiliatedUserId && r.ClubId == club.ClubId, cancellationToken);

        createdRequest.ShouldNotBeNull();
        createdRequest.Status.ShouldBe(RequestStatus.Pending);
    }

    [Fact]
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
        dbContext.ClubJoinRequests.Add(existingRequest);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Act
        var result = await service.CreateJoinRequestAsync(club.ClubId, cancellationToken);

        // Assert
        result.IsT2.ShouldBeTrue(); // Conflict
    }

    [Fact]
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
        result.IsT1.ShouldBeTrue(); // NotFound
    }

    #endregion

    #region CancelJoinRequestAsync Tests

    [Fact]
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
        dbContext.ClubJoinRequests.Add(joinRequest);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Act
        var result = await service.CancelJoinRequestAsync(cancellationToken);

        // Assert
        result.IsT0.ShouldBeTrue();

        // Verify request was deleted
        var deletedRequest = await dbContext.ClubJoinRequests
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.RequestingUserId == UnaffiliatedUserId && r.Status == RequestStatus.Pending, cancellationToken);

        deletedRequest.ShouldBeNull();
    }

    [Fact]
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
        result.IsT1.ShouldBeTrue();
    }

    #endregion

    #region GetPendingRequestsForClubAsync Tests

    [Fact]
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
        dbContext.ClubJoinRequests.Add(joinRequest);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Act
        var result = await service.GetPendingRequestsForClubAsync(club.ClubId, cancellationToken);

        // Assert
        result.IsT0.ShouldBeTrue();
        var requests = result.AsT0;
        requests.ShouldNotBeEmpty();
        requests.ShouldContain(r => r.RequestingUserId == UnaffiliatedUserId);
    }

    [Fact]
    public async Task GetPendingRequestsForClubAsync_WhenUserIsNotMember_ReturnsUnauthorized()
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

        // Assert
        result.IsT1.ShouldBeTrue(); // Unauthorized
    }

    [Fact]
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
        result.IsT0.ShouldBeTrue();
        result.AsT0.ShouldBeEmpty();
    }

    #endregion

    #region ApproveJoinRequestAsync Tests

    [Fact]
    public async Task ApproveJoinRequestAsync_WhenValidRequest_ReturnsSuccessAndUpdatesUserClubAndAddsRole()
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
        dbContext.ClubJoinRequests.Add(joinRequest);
        await dbContext.SaveChangesAsync(cancellationToken);
        var requestId = joinRequest.ClubJoinRequestId;

        // Act
        var result = await service.ApproveJoinRequestAsync(club.ClubId, requestId, cancellationToken);

        // Assert
        result.IsT0.ShouldBeTrue();

        // Verify request status was updated
        dbContext.ChangeTracker.Clear();
        var updatedRequest = await dbContext.ClubJoinRequests
            .IgnoreQueryFilters()
            .FirstAsync(r => r.ClubJoinRequestId == requestId, cancellationToken);

        updatedRequest.Status.ShouldBe(RequestStatus.Approved);

        // Verify user was added to club
        var updatedUser = await dbContext.Users.IgnoreQueryFilters().FirstAsync(u => u.Id == UnaffiliatedUserId, cancellationToken);
        updatedUser.ClubId.ShouldBe(club.ClubId);

        // Verify user was assigned the StandardUser role
        var isInRole = await userManager.IsInRoleAsync(updatedUser, "StandardUser");
        isInRole.ShouldBeTrue();
    }

    [Fact]
    public async Task ApproveJoinRequestAsync_WhenRequestDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var club = await dbContext.Clubs.FirstAsync(cancellationToken);

        // Act
        var result = await service.ApproveJoinRequestAsync(club.ClubId, 999999, cancellationToken);

        // Assert
        result.IsT1.ShouldBeTrue(); // NotFound
    }

    [Fact]
    public async Task ApproveJoinRequestAsync_WhenUserIsNotMember_ReturnsUnauthorized()
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
        dbContext.ClubJoinRequests.Add(joinRequest);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Act
        var result = await service.ApproveJoinRequestAsync(otherClub.ClubId, joinRequest.ClubJoinRequestId, cancellationToken);

        // Assert
        result.IsT2.ShouldBeTrue(); // Unauthorized
    }

    [Fact]
    public async Task ApproveJoinRequestAsync_WhenRequestAlreadyApproved_ReturnsNotFound()
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
            Status = RequestStatus.Approved,
            CreatedById = UnaffiliatedUserId
        };
        dbContext.ClubJoinRequests.Add(joinRequest);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Act
        var result = await service.ApproveJoinRequestAsync(club.ClubId, joinRequest.ClubJoinRequestId, cancellationToken);

        // Assert
        result.IsT1.ShouldBeTrue(); // NotFound (because it looks for Pending status)
    }

    #endregion

    #region RejectJoinRequestAsync Tests

    [Fact]
    public async Task RejectJoinRequestAsync_WhenValidRequest_ReturnsSuccessAndUpdatesStatus()
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
        dbContext.ClubJoinRequests.Add(joinRequest);
        await dbContext.SaveChangesAsync(cancellationToken);
        var requestId = joinRequest.ClubJoinRequestId;

        // Act
        var result = await service.RejectJoinRequestAsync(club.ClubId, requestId, cancellationToken);

        // Assert
        result.IsT0.ShouldBeTrue();

        // Verify request status was updated
        dbContext.ChangeTracker.Clear();
        var updatedRequest = await dbContext.ClubJoinRequests
            .IgnoreQueryFilters()
            .FirstAsync(r => r.ClubJoinRequestId == requestId, cancellationToken);

        updatedRequest.Status.ShouldBe(RequestStatus.Rejected);
    }

    [Fact]
    public async Task RejectJoinRequestAsync_WhenRequestDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var club = await dbContext.Clubs.FirstAsync(cancellationToken);

        // Act
        var result = await service.RejectJoinRequestAsync(club.ClubId, 999999, cancellationToken);

        // Assert
        result.IsT1.ShouldBeTrue(); // NotFound
    }

    [Fact]
    public async Task RejectJoinRequestAsync_WhenUserIsNotMember_ReturnsUnauthorized()
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
        dbContext.ClubJoinRequests.Add(joinRequest);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Act
        var result = await service.RejectJoinRequestAsync(otherClub.ClubId, joinRequest.ClubJoinRequestId, cancellationToken);

        // Assert
        result.IsT2.ShouldBeTrue(); // Unauthorized
    }

    [Fact]
    public async Task RejectJoinRequestAsync_WhenRequestAlreadyRejected_ReturnsNotFound()
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
            Status = RequestStatus.Rejected,
            CreatedById = UnaffiliatedUserId
        };
        dbContext.ClubJoinRequests.Add(joinRequest);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Act
        var result = await service.RejectJoinRequestAsync(club.ClubId, joinRequest.ClubJoinRequestId, cancellationToken);

        // Assert
        result.IsT1.ShouldBeTrue(); // NotFound (because it looks for Pending status)
    }

    #endregion

    #region Helpers

    private static ClubJoinRequestService CreateService(IServiceProvider services)
    {
        var readOnlyFactory = services.GetRequiredService<IDbContextFactory<ReadOnlyDbContext>>();
        var readWriteFactory = services.GetRequiredService<IDbContextFactory<ReadWriteDbContext>>();
        var userManager = services.GetRequiredService<UserManager<CalcioUserEntity>>();
        var httpContextAccessor = services.GetRequiredService<IHttpContextAccessor>();
        var logger = services.GetRequiredService<ILogger<ClubJoinRequestService>>();

        return new ClubJoinRequestService(readOnlyFactory, readWriteFactory, userManager, httpContextAccessor, logger);
    }

    #endregion
}
