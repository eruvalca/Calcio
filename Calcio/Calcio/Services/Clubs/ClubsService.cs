using Calcio.Data.Contexts;
using Calcio.Shared.DTOs.Clubs;
using Calcio.Shared.Entities;
using Calcio.Shared.Enums;
using Calcio.Shared.Extensions.Clubs;
using Calcio.Shared.Results;
using Calcio.Shared.Security;
using Calcio.Shared.Services.Clubs;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using OneOf.Types;

namespace Calcio.Services.Clubs;

public partial class ClubsService(
    IDbContextFactory<ReadOnlyDbContext> readOnlyDbContextFactory,
    IDbContextFactory<ReadWriteDbContext> readWriteDbContextFactory,
    UserManager<CalcioUserEntity> userManager,
    IHttpContextAccessor httpContextAccessor,
    ILogger<ClubsService> logger) : AuthenticatedServiceBase(httpContextAccessor), IClubsService
{
    public async Task<ServiceResult<List<BaseClubDto>>> GetUserClubsAsync(CancellationToken cancellationToken)
    {
        var userId = CurrentUserId;

        await using var dbContext = await readOnlyDbContextFactory.CreateDbContextAsync(cancellationToken);

        var user = await dbContext.Users
            .Where(u => u.Id == userId)
            .Include(u => u.Club)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(cancellationToken);

        var allClubs = await dbContext.Clubs.ToListAsync(cancellationToken);
        var allClubsWithUsers = await dbContext.Clubs
            .Include(c => c.CalcioUsers)
            .ToListAsync(cancellationToken);

        var allClubsNoFilters = await dbContext.Clubs
            .IgnoreQueryFilters()
            .ToListAsync(cancellationToken);
        var allClubsWithUsersNoFilters = await dbContext.Clubs
            .Include(c => c.CalcioUsers)
            .IgnoreQueryFilters()
            .ToListAsync(cancellationToken);

        var clubs = await dbContext.Clubs
            .OrderBy(c => c.Name)
            .Select(c => c.ToClubDto())
            .ToListAsync(cancellationToken);

        return clubs;
    }

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

        return new ClubCreatedDto(club.ClubId, club.Name);
    }

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
        return new Success();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "User {UserId} left club {ClubId}")]
    private static partial void LogUserLeftClub(ILogger logger, long clubId, long userId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to remove {RoleName} role from user {UserId}: {Errors}")]
    private static partial void LogRoleRemovalFailed(ILogger logger, long userId, string roleName, string errors);

    [LoggerMessage(Level = LogLevel.Information, Message = "Club '{ClubName}' created by user {UserId}")]
    private static partial void LogClubCreated(ILogger logger, string clubName, long userId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to add ClubAdmin role to user {UserId}: {Errors}")]
    private static partial void LogClubAdminRoleFailed(ILogger logger, long userId, string errors);
}
