using Calcio.Data.Contexts;
using Calcio.Shared.DTOs.CalcioUsers;
using Calcio.Shared.Entities;
using Calcio.Shared.Extensions.CalcioUsers;
using Calcio.Shared.Results;
using Calcio.Shared.Security;
using Calcio.Shared.Services.CalcioUsers;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using OneOf.Types;

namespace Calcio.Services.CalcioUsers;

public partial class CalcioUsersService(
    IDbContextFactory<ReadOnlyDbContext> readOnlyDbContextFactory,
    IDbContextFactory<ReadWriteDbContext> readWriteDbContextFactory,
    UserManager<CalcioUserEntity> userManager,
    IHttpContextAccessor httpContextAccessor,
    ILogger<CalcioUsersService> logger) : AuthenticatedServiceBase(httpContextAccessor), ICalcioUsersService
{
    public async Task<ServiceResult<List<ClubMemberDto>>> GetClubMembersAsync(long clubId, CancellationToken cancellationToken)
    {
        // Club membership is validated by ClubMembershipFilter before this service is called.
        await using var dbContext = await readOnlyDbContextFactory.CreateDbContextAsync(cancellationToken);

        var users = await dbContext.Users
            .Where(u => u.ClubId == clubId && u.Id != CurrentUserId)
            .ToListAsync(cancellationToken);

        var members = new List<ClubMemberDto>();
        foreach (var user in users)
        {
            var isClubAdmin = await userManager.IsInRoleAsync(user, Roles.ClubAdmin);
            members.Add(user.ToClubMemberDto(isClubAdmin));
        }

        return members.OrderByDescending(m => m.IsClubAdmin).ThenBy(m => m.FullName).ToList();
    }

    public async Task<ServiceResult<Success>> RemoveClubMemberAsync(long clubId, long userId, CancellationToken cancellationToken)
    {
        // Club membership is validated by ClubMembershipFilter before this service is called.
        if (userId == CurrentUserId)
        {
            return ServiceProblem.Forbidden("You cannot remove yourself from the club.");
        }

        await using var dbContext = await readWriteDbContextFactory.CreateDbContextAsync(cancellationToken);

        var userToRemove = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId && u.ClubId == clubId, cancellationToken);

        if (userToRemove is null)
        {
            return ServiceProblem.NotFound();
        }

        userToRemove.ClubId = null;
        await dbContext.SaveChangesAsync(cancellationToken);

        var userForRoleRemoval = await userManager.FindByIdAsync(userId.ToString());
        if (userForRoleRemoval is not null)
        {
            var removeRoleResult = await userManager.RemoveFromRoleAsync(userForRoleRemoval, Roles.StandardUser);
            if (!removeRoleResult.Succeeded)
            {
                var errors = string.Join(", ", removeRoleResult.Errors.Select(e => e.Description));
                LogRoleRemovalFailed(logger, userId, Roles.StandardUser, errors);
            }
        }

        LogMemberRemoved(logger, clubId, userId, CurrentUserId);
        return new Success();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Member {UserId} removed from club {ClubId} by user {RemovingUserId}")]
    private static partial void LogMemberRemoved(ILogger logger, long clubId, long userId, long removingUserId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to remove {RoleName} role from user {UserId}: {Errors}")]
    private static partial void LogRoleRemovalFailed(ILogger logger, long userId, string roleName, string errors);
}
