using Calcio.Data.Contexts;
using Calcio.Shared.DTOs.Clubs;
using Calcio.Entities;
using Calcio.Shared.Enums;
using Calcio.Extensions.Clubs;
using Calcio.Shared.Results;
using Calcio.Shared.Security;
using Calcio.Shared.Services.Clubs;
using Calcio.Shared.Services.UserClubsCache;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using OneOf.Types;

namespace Calcio.Services.Clubs;

/// <summary>
/// Provides Clubs Service operations.
/// </summary>
/// <param name="readOnlyDbContextFactory">The read Only Db Context Factory.</param>
/// <param name="readWriteDbContextFactory">The read Write Db Context Factory.</param>
/// <param name="userManager">The user Manager.</param>
/// <param name="userClubsCacheService">The user Clubs Cache Service.</param>
/// <param name="httpContextAccessor">The http Context Accessor.</param>
/// <param name="httpContextAccessor">The http Context Accessor.</param>
public partial class ClubsService(
    IDbContextFactory<ReadOnlyDbContext> readOnlyDbContextFactory,
    IDbContextFactory<ReadWriteDbContext> readWriteDbContextFactory,
    UserManager<CalcioUserEntity> userManager,
    IUserClubsCacheService userClubsCacheService,
    IHttpContextAccessor httpContextAccessor,
    ILogger<ClubsService> logger) : AuthenticatedServiceBase(httpContextAccessor), IClubsService
{
    /// <summary>
    /// Executes the Get User Clubs Async operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>The operation result.</returns>
    public async Task<ServiceResult<List<BaseClubDto>>> GetUserClubsAsync(CancellationToken cancellationToken)
    {
        var clubs = await userClubsCacheService.GetClubsListAsync(CurrentUserId, cancellationToken);
        return clubs.ToList();
    }

    /// <summary>
    /// Executes the Get Club By Id Async operation.
    /// </summary>
    /// <param name="clubId">The club Id.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>The operation result.</returns>
    public async Task<ServiceResult<BaseClubDto>> GetClubByIdAsync(long clubId, CancellationToken cancellationToken)
    {
        await using var dbContext = await readOnlyDbContextFactory.CreateDbContextAsync(cancellationToken);

        var club = await dbContext.Clubs
            .Where(c => c.ClubId == clubId)
            .Select(c => c.ToClubDto())
            .FirstOrDefaultAsync(cancellationToken);

        return club is not null
            ? club
            : ServiceProblem.NotFound();
    }

    /// <summary>
    /// Executes the Get All Clubs For Browsing Async operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>The operation result.</returns>
    public async Task<ServiceResult<List<BaseClubDto>>> GetAllClubsForBrowsingAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await readOnlyDbContextFactory.CreateDbContextAsync(cancellationToken);

        var clubs = await dbContext.Clubs
            .IgnoreQueryFilters()
            .OrderBy(c => c.State)
            .ThenBy(c => c.City)
            .ThenBy(c => c.Name)
            .Select(c => c.ToClubDto())
            .ToListAsync(cancellationToken);

        return clubs;
    }

    /// <summary>
    /// Executes the Create Club Async operation.
    /// </summary>
    /// <param name="dto">The dto.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>The operation result.</returns>
    public async Task<ServiceResult<ClubCreatedDto>> CreateClubAsync(CreateClubDto dto, CancellationToken cancellationToken)
    {
        await using var dbContext = await readWriteDbContextFactory.CreateDbContextAsync(cancellationToken);

        // Check if user already belongs to a club
        var currentUser = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == CurrentUserId, cancellationToken);

        if (currentUser is null)
        {
            return ServiceProblem.NotFound();
        }

        if (currentUser.ClubId is not null)
        {
            return ServiceProblem.Conflict("You already belong to a club.");
        }

        // Check if user has a pending join request
        var pendingRequest = await dbContext.ClubJoinRequests
            .IgnoreQueryFilters()
            .AnyAsync(r => r.RequestingUserId == CurrentUserId && r.Status == RequestStatus.Pending, cancellationToken);

        if (pendingRequest)
        {
            return ServiceProblem.Conflict("You have a pending join request. Cancel it before creating a new club.");
        }

        // Delete any existing rejected join request when creating own club
        var existingRequest = await dbContext.ClubJoinRequests
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.RequestingUserId == CurrentUserId, cancellationToken);

        if (existingRequest is not null)
        {
            dbContext.Remove(existingRequest);
        }

        var club = new ClubEntity
        {
            Name = dto.Name,
            City = dto.City,
            State = dto.State,
            CreatedById = CurrentUserId
        };

        await dbContext.Clubs.AddAsync(club, cancellationToken);

        currentUser.Club = club;

        await dbContext.SaveChangesAsync(cancellationToken);

        // Get a fresh user instance from UserManager to avoid tracking conflicts
        var userForRole = await userManager.FindByIdAsync(CurrentUserId.ToString());
        if (userForRole is not null)
        {
            // Keep UserManager's entity in sync with our changes; otherwise AddToRoleAsync's
            // UpdateAsync will overwrite ClubId back to null (its stale cached value).
            userForRole.ClubId = club.ClubId;

            var roleResult = await userManager.AddToRoleAsync(userForRole, Roles.ClubAdmin);
            if (!roleResult.Succeeded)
            {
                var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                LogClubAdminRoleFailed(logger, CurrentUserId, errors);
            }
        }

        LogClubCreated(logger, club.Name, CurrentUserId);

        // Invalidate user's clubs cache since they now belong to a new club
        await userClubsCacheService.InvalidateUserClubsCacheAsync(CurrentUserId, cancellationToken);

        return new ClubCreatedDto(club.ClubId, club.Name);
    }

    /// <summary>
    /// Executes the Leave Club Async operation.
    /// </summary>
    /// <param name="clubId">The club Id.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>The operation result.</returns>
    public async Task<ServiceResult<Success>> LeaveClubAsync(long clubId, CancellationToken cancellationToken)
    {
        await using var dbContext = await readWriteDbContextFactory.CreateDbContextAsync(cancellationToken);

        var currentUser = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == CurrentUserId && u.ClubId == clubId, cancellationToken);

        if (currentUser is null)
        {
            return ServiceProblem.NotFound();
        }

        // Check if user is a ClubAdmin - they cannot leave
        var userForRoleCheck = await userManager.FindByIdAsync(CurrentUserId.ToString());
        if (userForRoleCheck is not null && await userManager.IsInRoleAsync(userForRoleCheck, Roles.ClubAdmin))
        {
            return ServiceProblem.Forbidden("ClubAdmins cannot leave the club. Transfer ownership or delete the club instead.");
        }

        currentUser.ClubId = null;
        await dbContext.SaveChangesAsync(cancellationToken);

        // Keep the UserManager-tracked user entity in sync; otherwise subsequent identity updates
        // (e.g., role changes/security stamp updates) can overwrite ClubId back to its previous value.
        userForRoleCheck?.ClubId = null;

        // Remove StandardUser role
        if (userForRoleCheck is not null)
        {
            var removeRoleResult = await userManager.RemoveFromRoleAsync(userForRoleCheck, Roles.StandardUser);
            if (!removeRoleResult.Succeeded)
            {
                var errors = string.Join(", ", removeRoleResult.Errors.Select(e => e.Description));
                LogRoleRemovalFailed(logger, CurrentUserId, Roles.StandardUser, errors);
            }
        }

        LogUserLeftClub(logger, clubId, CurrentUserId);

        // Invalidate user's clubs cache since they left a club
        await userClubsCacheService.InvalidateUserClubsCacheAsync(CurrentUserId, cancellationToken);

        return new Success();
    }

    /// <summary>
    /// Executes the Log User Left Club operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="clubId">The club Id.</param>
    /// <param name="userId">The user Id.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "User {UserId} left club {ClubId}")]
    /// <summary>
    /// Executes the log user left club operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="clubId">The club id.</param>
    /// <param name="userId">The user id.</param>
    private static partial void LogUserLeftClub(ILogger logger, long clubId, long userId);

    /// <summary>
    /// Executes the Log Role Removal Failed operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="userId">The user Id.</param>
    /// <param name="roleName">The role Name.</param>
    /// <param name="errors">The errors.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to remove {RoleName} role from user {UserId}: {Errors}")]
    /// <summary>
    /// Executes the log role removal failed operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="userId">The user id.</param>
    /// <param name="roleName">The role name.</param>
    /// <param name="errors">The errors.</param>
    private static partial void LogRoleRemovalFailed(ILogger logger, long userId, string roleName, string errors);

    /// <summary>
    /// Executes the Log Club Created operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="clubName">The club Name.</param>
    /// <param name="userId">The user Id.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Club '{ClubName}' created by user {UserId}")]
    /// <summary>
    /// Executes the log club created operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="clubName">The club name.</param>
    /// <param name="userId">The user id.</param>
    private static partial void LogClubCreated(ILogger logger, string clubName, long userId);

    /// <summary>
    /// Executes the Log Club Admin Role Failed operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="userId">The user Id.</param>
    /// <param name="errors">The errors.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to add ClubAdmin role to user {UserId}: {Errors}")]
    /// <summary>
    /// Executes the log club admin role failed operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="userId">The user id.</param>
    /// <param name="errors">The errors.</param>
    private static partial void LogClubAdminRoleFailed(ILogger logger, long userId, string errors);
}
