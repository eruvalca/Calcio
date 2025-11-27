using Calcio.Data.Contexts;
using Calcio.Shared.DTOs.CalcioUsers;
using Calcio.Shared.Extensions.CalcioUsers;
using Calcio.Shared.Models.Entities;
using Calcio.Shared.Results;
using Calcio.Shared.Services.CalcioUsers;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using OneOf;
using OneOf.Types;

namespace Calcio.Services.CalcioUsers;

public partial class CalcioUsersService(
    IDbContextFactory<ReadOnlyDbContext> readOnlyDbContextFactory,
    IDbContextFactory<ReadWriteDbContext> readWriteDbContextFactory,
    UserManager<CalcioUserEntity> userManager,
    IHttpContextAccessor httpContextAccessor,
    ILogger<CalcioUsersService> logger) : AuthenticatedServiceBase(httpContextAccessor), ICalcioUsersService
{
    public async Task<OneOf<List<ClubMemberDto>, Unauthorized, Error>> GetClubMembersAsync(long clubId, CancellationToken cancellationToken)
    {
        await using var dbContext = await readOnlyDbContextFactory.CreateDbContextAsync(cancellationToken);

        var isClubMember = await dbContext.Clubs
            .AnyAsync(c => c.ClubId == clubId, cancellationToken);

        if (!isClubMember)
        {
            return new Unauthorized();
        }

        var users = await dbContext.Users
            .Where(u => u.ClubId == clubId)
            .ToListAsync(cancellationToken);

        var members = new List<ClubMemberDto>();
        foreach (var user in users)
        {
            var isClubAdmin = await userManager.IsInRoleAsync(user, "ClubAdmin");
            members.Add(user.ToClubMemberDto(isClubAdmin));
        }

        return members.OrderByDescending(m => m.IsClubAdmin).ThenBy(m => m.FullName).ToList();
    }

    public async Task<OneOf<Success, NotFound, Unauthorized, Error>> RemoveClubMemberAsync(long clubId, long userId, CancellationToken cancellationToken)
    {
        if (userId == CurrentUserId)
        {
            return new Unauthorized();
        }

        await using var dbContext = await readWriteDbContextFactory.CreateDbContextAsync(cancellationToken);

        var isClubMember = await dbContext.Clubs
            .AnyAsync(c => c.ClubId == clubId, cancellationToken);

        if (!isClubMember)
        {
            return new Unauthorized();
        }

        var userToRemove = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId && u.ClubId == clubId, cancellationToken);

        if (userToRemove is null)
        {
            return new NotFound();
        }

        userToRemove.ClubId = null;
        await dbContext.SaveChangesAsync(cancellationToken);

        var userForRoleRemoval = await userManager.FindByIdAsync(userId.ToString());
        if (userForRoleRemoval is not null)
        {
            var removeRoleResult = await userManager.RemoveFromRoleAsync(userForRoleRemoval, "StandardUser");
            if (!removeRoleResult.Succeeded)
            {
                var errors = string.Join(", ", removeRoleResult.Errors.Select(e => e.Description));
                LogRoleRemovalFailed(logger, userId, "StandardUser", errors);
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
