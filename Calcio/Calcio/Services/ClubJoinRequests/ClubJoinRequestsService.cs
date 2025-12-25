using Calcio.Data.Contexts;
using Calcio.Shared.DTOs.ClubJoinRequests;
using Calcio.Shared.Enums;
using Calcio.Shared.Extensions.ClubJoinRequests;
using Calcio.Shared.Models.Entities;
using Calcio.Shared.Results;
using Calcio.Shared.Security;
using Calcio.Shared.Services.ClubJoinRequests;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using OneOf.Types;

namespace Calcio.Services.ClubJoinRequests;

public partial class ClubJoinRequestsService(
    IDbContextFactory<ReadOnlyDbContext> readOnlyDbContextFactory,
    IDbContextFactory<ReadWriteDbContext> readWriteDbContextFactory,
    UserManager<CalcioUserEntity> userManager,
    IHttpContextAccessor httpContextAccessor,
    ILogger<ClubJoinRequestsService> logger) : AuthenticatedServiceBase(httpContextAccessor), IClubJoinRequestsService
{
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

        dbContext.Add(joinRequest);
        await dbContext.SaveChangesAsync(cancellationToken);

        LogJoinRequestCreated(logger, clubId, CurrentUserId);
        return new Success();
    }

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

    public async Task<ServiceResult<Success>> UpdateJoinRequestStatusAsync(long clubId, long requestId, RequestStatus status, CancellationToken cancellationToken)
        => status switch
        {
            RequestStatus.Approved => await ApproveJoinRequestInternalAsync(clubId, requestId, cancellationToken),
            RequestStatus.Rejected => await RejectJoinRequestInternalAsync(clubId, requestId, cancellationToken),
            _ => ServiceProblem.BadRequest("Invalid status. Only 'Approved' or 'Rejected' transitions are allowed.")
        };

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
        return new Success();
    }

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

    [LoggerMessage(Level = LogLevel.Information, Message = "Join request created for club {ClubId} by user {UserId}")]
    private static partial void LogJoinRequestCreated(ILogger logger, long clubId, long userId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Join request canceled for club {ClubId} by user {UserId}")]
    private static partial void LogJoinRequestCanceled(ILogger logger, long clubId, long userId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Join request for club {ClubId} from user {RequestingUserId} approved by user {ApprovingUserId}")]
    private static partial void LogJoinRequestApproved(ILogger logger, long clubId, long requestingUserId, long approvingUserId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Join request for club {ClubId} from user {RequestingUserId} rejected by user {RejectingUserId}")]
    private static partial void LogJoinRequestRejected(ILogger logger, long clubId, long requestingUserId, long rejectingUserId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to add StandardUser role to user {UserId}: {Errors}")]
    private static partial void LogStandardUserRoleFailed(ILogger logger, long userId, string errors);
}
