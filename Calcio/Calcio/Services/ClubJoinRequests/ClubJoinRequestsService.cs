using Calcio.Data.Contexts;
using Calcio.Shared.DTOs.ClubJoinRequests;
using Calcio.Entities;
using Calcio.Shared.Enums;
using Calcio.Extensions.ClubJoinRequests;
using Calcio.Shared.Results;
using Calcio.Shared.Security;
using Calcio.Shared.Services.ClubJoinRequests;
using Calcio.Shared.Services.UserClubsCache;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using OneOf.Types;

namespace Calcio.Services.ClubJoinRequests;

/// <summary>
/// Provides Club Join Requests Service operations.
/// </summary>
/// <param name="readOnlyDbContextFactory">The read Only Db Context Factory.</param>
/// <param name="readWriteDbContextFactory">The read Write Db Context Factory.</param>
/// <param name="userManager">The user Manager.</param>
/// <param name="userClubsCacheService">The user Clubs Cache Service.</param>
/// <param name="httpContextAccessor">The http Context Accessor.</param>
/// <param name="httpContextAccessor">The http Context Accessor.</param>
public partial class ClubJoinRequestsService(
    IDbContextFactory<ReadOnlyDbContext> readOnlyDbContextFactory,
    IDbContextFactory<ReadWriteDbContext> readWriteDbContextFactory,
    UserManager<CalcioUserEntity> userManager,
    IUserClubsCacheService userClubsCacheService,
    IHttpContextAccessor httpContextAccessor,
    ILogger<ClubJoinRequestsService> logger) : AuthenticatedServiceBase(httpContextAccessor), IClubJoinRequestsService
{
    /// <summary>
    /// Executes the Get Request For Current User Async operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>The operation result.</returns>
    public async Task<ServiceResult<ClubJoinRequestDto>> GetRequestForCurrentUserAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await readOnlyDbContextFactory.CreateDbContextAsync(cancellationToken);

        // Return both pending and rejected requests so UI can show rejection status
        var request = await dbContext.ClubJoinRequests
            .Where(request => request.RequestingUserId == CurrentUserId &&
                (request.Status == RequestStatus.Pending || request.Status == RequestStatus.Rejected))
            .Select(request => request.ToClubJoinRequestDto())
            .FirstOrDefaultAsync(cancellationToken);

        return request is null
            ? ServiceProblem.NotFound()
            : request;
    }

    /// <summary>
    /// Executes the Create Join Request Async operation.
    /// </summary>
    /// <param name="clubId">The club Id.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>The operation result.</returns>
    public async Task<ServiceResult<Success>> CreateJoinRequestAsync(long clubId, CancellationToken cancellationToken)
    {
        await using var dbContext = await readWriteDbContextFactory.CreateDbContextAsync(cancellationToken);

        var existingRequest = await dbContext.ClubJoinRequests
            .FirstOrDefaultAsync(r => r.RequestingUserId == CurrentUserId, cancellationToken);

        if (existingRequest is not null)
        {
            if (existingRequest.Status == RequestStatus.Pending)
            {
                return ServiceProblem.Conflict("You already have a pending request to join a club.");
            }

            // Delete rejected request to allow new request
            dbContext.Remove(existingRequest);
        }

        var clubExists = await dbContext.Clubs
            .IgnoreQueryFilters()
            .AnyAsync(c => c.ClubId == clubId, cancellationToken);

        if (!clubExists)
        {
            return ServiceProblem.NotFound();
        }

        var joinRequest = new ClubJoinRequestEntity
        {
            ClubId = clubId,
            RequestingUserId = CurrentUserId,
            Status = RequestStatus.Pending,
            CreatedById = CurrentUserId
        };

        await dbContext.AddAsync(joinRequest, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        LogJoinRequestCreated(logger, clubId, CurrentUserId);
        return new Success();
    }

    /// <summary>
    /// Executes the Cancel Join Request Async operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>The operation result.</returns>
    public async Task<ServiceResult<Success>> CancelJoinRequestAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await readWriteDbContextFactory.CreateDbContextAsync(cancellationToken);

        var joinRequest = await dbContext.ClubJoinRequests
            .FirstOrDefaultAsync(r => r.RequestingUserId == CurrentUserId && r.Status == RequestStatus.Pending, cancellationToken);

        if (joinRequest is null)
        {
            return ServiceProblem.NotFound();
        }

        dbContext.Remove(joinRequest);
        await dbContext.SaveChangesAsync(cancellationToken);

        LogJoinRequestCanceled(logger, joinRequest.ClubId, CurrentUserId);
        return new Success();
    }

    /// <summary>
    /// Executes the Get Pending Requests For Club Async operation.
    /// </summary>
    /// <param name="clubId">The club Id.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>The operation result.</returns>
    public async Task<ServiceResult<List<ClubJoinRequestWithUserDto>>> GetPendingRequestsForClubAsync(long clubId, CancellationToken cancellationToken)
    {
        // Club membership is validated by ClubMembershipFilter before this service is called.
        await using var dbContext = await readOnlyDbContextFactory.CreateDbContextAsync(cancellationToken);

        var requests = await dbContext.ClubJoinRequests
            .Include(r => r.RequestingUser)
            .Where(r => r.ClubId == clubId && r.Status == RequestStatus.Pending)
            .OrderBy(r => r.CreatedAt)
            .Select(r => r.ToClubJoinRequestWithUserDto())
            .ToListAsync(cancellationToken);

        return requests;
    }

    /// <summary>
    /// Executes the Update Join Request Status Async operation.
    /// </summary>
    /// <param name="clubId">The club Id.</param>
    /// <param name="requestId">The request Id.</param>
    /// <param name="status">The status.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>The operation result.</returns>
    public async Task<ServiceResult<Success>> UpdateJoinRequestStatusAsync(long clubId, long requestId, RequestStatus status, CancellationToken cancellationToken)
        => status switch
        {
            RequestStatus.Approved => await ApproveJoinRequestInternalAsync(clubId, requestId, cancellationToken),
            RequestStatus.Rejected => await RejectJoinRequestInternalAsync(clubId, requestId, cancellationToken),
            _ => ServiceProblem.BadRequest("Invalid status. Only 'Approved' or 'Rejected' transitions are allowed.")
        };

    /// <summary>
    /// Executes the Approve Join Request Internal Async operation.
    /// </summary>
    /// <param name="clubId">The club Id.</param>
    /// <param name="requestId">The request Id.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>The operation result.</returns>
    private async Task<ServiceResult<Success>> ApproveJoinRequestInternalAsync(long clubId, long requestId, CancellationToken cancellationToken)
    {
        // Club membership is validated by ClubMembershipFilter before this service is called.
        await using var dbContext = await readWriteDbContextFactory.CreateDbContextAsync(cancellationToken);

        var joinRequest = await dbContext.ClubJoinRequests
            .Include(r => r.RequestingUser)
            .Include(r => r.Club)
            .FirstOrDefaultAsync(r => r.ClubJoinRequestId == requestId && r.ClubId == clubId && r.Status == RequestStatus.Pending, cancellationToken);

        if (joinRequest is null)
        {
            return ServiceProblem.NotFound();
        }

        var requestingUserId = joinRequest.RequestingUserId;
        var requestingUser = joinRequest.RequestingUser;

        // Assign user to club and remove the join request record
        // (allows user to submit new requests in future if removed from club)
        requestingUser.ClubId = joinRequest.ClubId;
        dbContext.Remove(joinRequest);

        await dbContext.SaveChangesAsync(cancellationToken);

        // Add StandardUser role to the newly approved user
        // Fetch fresh user via UserManager to avoid entity tracking conflicts
        var userForRoleAssignment = await userManager.FindByIdAsync(requestingUserId.ToString());
        if (userForRoleAssignment is not null)
        {
            var roleResult = await userManager.AddToRoleAsync(userForRoleAssignment, Roles.StandardUser);
            if (!roleResult.Succeeded)
            {
                var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                LogStandardUserRoleFailed(logger, requestingUserId, errors);
            }
        }

        LogJoinRequestApproved(logger, joinRequest.ClubId, requestingUserId, CurrentUserId);

        // Invalidate the approved user's clubs cache since they now belong to a new club
        await userClubsCacheService.InvalidateUserClubsCacheAsync(requestingUserId, cancellationToken);

        return new Success();
    }

    /// <summary>
    /// Executes the Reject Join Request Internal Async operation.
    /// </summary>
    /// <param name="clubId">The club Id.</param>
    /// <param name="requestId">The request Id.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>The operation result.</returns>
    private async Task<ServiceResult<Success>> RejectJoinRequestInternalAsync(long clubId, long requestId, CancellationToken cancellationToken)
    {
        // Club membership is validated by ClubMembershipFilter before this service is called.
        await using var dbContext = await readWriteDbContextFactory.CreateDbContextAsync(cancellationToken);

        var joinRequest = await dbContext.ClubJoinRequests
            .FirstOrDefaultAsync(r => r.ClubJoinRequestId == requestId && r.ClubId == clubId && r.Status == RequestStatus.Pending, cancellationToken);

        if (joinRequest is null)
        {
            return ServiceProblem.NotFound();
        }

        // Mark as rejected - record will be deleted when user requests to join another club or creates their own
        joinRequest.Status = RequestStatus.Rejected;

        await dbContext.SaveChangesAsync(cancellationToken);

        LogJoinRequestRejected(logger, clubId, joinRequest.RequestingUserId, CurrentUserId);
        return new Success();
    }

    /// <summary>
    /// Executes the Log Join Request Created operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="clubId">The club Id.</param>
    /// <param name="userId">The user Id.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Join request created for club {ClubId} by user {UserId}")]
    /// <summary>
    /// Executes the log join request created operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="clubId">The club id.</param>
    /// <param name="userId">The user id.</param>
    private static partial void LogJoinRequestCreated(ILogger logger, long clubId, long userId);

    /// <summary>
    /// Executes the Log Join Request Canceled operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="clubId">The club Id.</param>
    /// <param name="userId">The user Id.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Join request canceled for club {ClubId} by user {UserId}")]
    /// <summary>
    /// Executes the log join request canceled operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="clubId">The club id.</param>
    /// <param name="userId">The user id.</param>
    private static partial void LogJoinRequestCanceled(ILogger logger, long clubId, long userId);

    /// <summary>
    /// Executes the Log Join Request Approved operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="clubId">The club Id.</param>
    /// <param name="requestingUserId">The requesting User Id.</param>
    /// <param name="approvingUserId">The approving User Id.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Join request for club {ClubId} from user {RequestingUserId} approved by user {ApprovingUserId}")]
    /// <summary>
    /// Executes the log join request approved operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="clubId">The club id.</param>
    /// <param name="requestingUserId">The requesting user id.</param>
    /// <param name="approvingUserId">The approving user id.</param>
    private static partial void LogJoinRequestApproved(ILogger logger, long clubId, long requestingUserId, long approvingUserId);

    /// <summary>
    /// Executes the Log Join Request Rejected operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="clubId">The club Id.</param>
    /// <param name="requestingUserId">The requesting User Id.</param>
    /// <param name="rejectingUserId">The rejecting User Id.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Join request for club {ClubId} from user {RequestingUserId} rejected by user {RejectingUserId}")]
    /// <summary>
    /// Executes the log join request rejected operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="clubId">The club id.</param>
    /// <param name="requestingUserId">The requesting user id.</param>
    /// <param name="rejectingUserId">The rejecting user id.</param>
    private static partial void LogJoinRequestRejected(ILogger logger, long clubId, long requestingUserId, long rejectingUserId);

    /// <summary>
    /// Executes the Log Standard User Role Failed operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="userId">The user Id.</param>
    /// <param name="errors">The errors.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to add StandardUser role to user {UserId}: {Errors}")]
    /// <summary>
    /// Executes the log standard user role failed operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="userId">The user id.</param>
    /// <param name="errors">The errors.</param>
    private static partial void LogStandardUserRoleFailed(ILogger logger, long userId, string errors);
}
