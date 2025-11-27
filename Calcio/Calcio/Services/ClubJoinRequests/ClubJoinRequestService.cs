using Calcio.Data.Contexts;
using Calcio.Shared.DTOs.ClubJoinRequests;
using Calcio.Shared.Enums;
using Calcio.Shared.Extensions.ClubJoinRequests;
using Calcio.Shared.Models.Entities;
using Calcio.Shared.Results;
using Calcio.Shared.Services.ClubJoinRequests;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using OneOf;
using OneOf.Types;

namespace Calcio.Services.ClubJoinRequests;

public partial class ClubJoinRequestService(
    IDbContextFactory<ReadOnlyDbContext> readOnlyDbContextFactory,
    IDbContextFactory<ReadWriteDbContext> readWriteDbContextFactory,
    UserManager<CalcioUserEntity> userManager,
    IHttpContextAccessor httpContextAccessor,
    ILogger<ClubJoinRequestService> logger) : AuthenticatedServiceBase(httpContextAccessor), IClubJoinRequestService
{
    public async Task<OneOf<ClubJoinRequestDto, NotFound, Unauthorized, Error>> GetRequestForCurrentUserAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await readOnlyDbContextFactory.CreateDbContextAsync(cancellationToken);

        // Return both pending and rejected requests so UI can show rejection status
        var request = await dbContext.ClubJoinRequests
            .Where(request => request.RequestingUserId == CurrentUserId &&
                (request.Status == RequestStatus.Pending || request.Status == RequestStatus.Rejected))
            .Select(request => request.ToClubJoinRequestDto())
            .FirstOrDefaultAsync(cancellationToken);

        return request is null
            ? new NotFound()
            : request;
    }

    public async Task<OneOf<Success, NotFound, Conflict, Unauthorized, Error>> CreateJoinRequestAsync(long clubId, CancellationToken cancellationToken)
    {
        await using var dbContext = await readWriteDbContextFactory.CreateDbContextAsync(cancellationToken);

        var existingRequest = await dbContext.ClubJoinRequests
            .FirstOrDefaultAsync(r => r.RequestingUserId == CurrentUserId, cancellationToken);

        if (existingRequest is not null)
        {
            if (existingRequest.Status == RequestStatus.Pending)
            {
                return new Conflict();
            }

            // Delete rejected request to allow new request
            dbContext.Remove(existingRequest);
        }

        var clubExists = await dbContext.Clubs
            .IgnoreQueryFilters()
            .AnyAsync(c => c.ClubId == clubId, cancellationToken);

        if (!clubExists)
        {
            return new NotFound();
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

    public async Task<OneOf<Success, NotFound, Unauthorized, Error>> CancelJoinRequestAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await readWriteDbContextFactory.CreateDbContextAsync(cancellationToken);

        var joinRequest = await dbContext.ClubJoinRequests
            .FirstOrDefaultAsync(r => r.RequestingUserId == CurrentUserId && r.Status == RequestStatus.Pending, cancellationToken);

        if (joinRequest is null)
        {
            return new NotFound();
        }

        dbContext.Remove(joinRequest);
        await dbContext.SaveChangesAsync(cancellationToken);

        LogJoinRequestCanceled(logger, joinRequest.ClubId, CurrentUserId);
        return new Success();
    }

    public async Task<OneOf<List<ClubJoinRequestWithUserDto>, Unauthorized, Error>> GetPendingRequestsForClubAsync(long clubId, CancellationToken cancellationToken)
    {
        await using var dbContext = await readOnlyDbContextFactory.CreateDbContextAsync(cancellationToken);

        var isClubMember = await dbContext.Clubs
            .AnyAsync(c => c.ClubId == clubId, cancellationToken);

        if (!isClubMember)
        {
            return new Unauthorized();
        }

        var requests = await dbContext.ClubJoinRequests
            .Include(r => r.RequestingUser)
            .Where(r => r.ClubId == clubId && r.Status == RequestStatus.Pending)
            .OrderBy(r => r.CreatedAt)
            .Select(r => r.ToClubJoinRequestWithUserDto())
            .ToListAsync(cancellationToken);

        return requests;
    }

    public async Task<OneOf<Success, NotFound, Unauthorized, Error>> ApproveJoinRequestAsync(long clubId, long requestId, CancellationToken cancellationToken)
    {
        await using var dbContext = await readWriteDbContextFactory.CreateDbContextAsync(cancellationToken);

        // Check club membership first (authorization before data access)
        var isClubMember = await dbContext.Clubs
            .AnyAsync(c => c.ClubId == clubId, cancellationToken);

        if (!isClubMember)
        {
            return new Unauthorized();
        }

        var joinRequest = await dbContext.ClubJoinRequests
            .Include(r => r.RequestingUser)
            .Include(r => r.Club)
            .FirstOrDefaultAsync(r => r.ClubJoinRequestId == requestId && r.ClubId == clubId && r.Status == RequestStatus.Pending, cancellationToken);

        if (joinRequest is null)
        {
            return new NotFound();
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
            var roleResult = await userManager.AddToRoleAsync(userForRoleAssignment, "StandardUser");
            if (!roleResult.Succeeded)
            {
                var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                LogStandardUserRoleFailed(logger, requestingUserId, errors);
            }
        }

        LogJoinRequestApproved(logger, joinRequest.ClubId, requestingUserId, CurrentUserId);
        return new Success();
    }

    public async Task<OneOf<Success, NotFound, Unauthorized, Error>> RejectJoinRequestAsync(long clubId, long requestId, CancellationToken cancellationToken)
    {
        await using var dbContext = await readWriteDbContextFactory.CreateDbContextAsync(cancellationToken);

        // Check club membership first (authorization before data access)
        var isClubMember = await dbContext.Clubs
            .AnyAsync(c => c.ClubId == clubId, cancellationToken);

        if (!isClubMember)
        {
            return new Unauthorized();
        }

        var joinRequest = await dbContext.ClubJoinRequests
            .FirstOrDefaultAsync(r => r.ClubJoinRequestId == requestId && r.ClubId == clubId && r.Status == RequestStatus.Pending, cancellationToken);

        if (joinRequest is null)
        {
            return new NotFound();
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
