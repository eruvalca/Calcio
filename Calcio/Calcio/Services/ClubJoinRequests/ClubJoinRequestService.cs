using Calcio.Data.Contexts;
using Calcio.Shared.DTOs.ClubJoinRequests;
using Calcio.Shared.Enums;
using Calcio.Shared.Extensions.ClubJoinRequests;
using Calcio.Shared.Models.Entities;
using Calcio.Shared.Results;
using Calcio.Shared.Services.ClubJoinRequests;

using Microsoft.EntityFrameworkCore;

using OneOf;
using OneOf.Types;

namespace Calcio.Services.ClubJoinRequests;

public partial class ClubJoinRequestService(
    IDbContextFactory<ReadOnlyDbContext> readOnlyDbContextFactory,
    IDbContextFactory<ReadWriteDbContext> readWriteDbContextFactory,
    IHttpContextAccessor httpContextAccessor,
    ILogger<ClubJoinRequestService> logger) : AuthenticatedServiceBase(httpContextAccessor), IClubJoinRequestService
{
    public async Task<OneOf<ClubJoinRequestDto, NotFound, Unauthorized, Error>> GetPendingRequestForCurrentUserAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await readOnlyDbContextFactory.CreateDbContextAsync(cancellationToken);

        var request = await dbContext.ClubJoinRequests
            .Where(request => request.RequestingUserId == CurrentUserId && request.Status == RequestStatus.Pending)
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
            .AnyAsync(r => r.RequestingUserId == CurrentUserId && r.Status == RequestStatus.Pending, cancellationToken);

        if (existingRequest)
        {
            return new Conflict();
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

        joinRequest.Status = RequestStatus.Approved;
        joinRequest.RequestingUser.ClubId = joinRequest.ClubId;

        await dbContext.SaveChangesAsync(cancellationToken);

        LogJoinRequestApproved(logger, joinRequest.ClubId, joinRequest.RequestingUserId, CurrentUserId);
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

        joinRequest.Status = RequestStatus.Rejected;

        await dbContext.SaveChangesAsync(cancellationToken);

        LogJoinRequestRejected(logger, joinRequest.ClubId, joinRequest.RequestingUserId, CurrentUserId);
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
}
