using Calcio.Data.Contexts;
using Calcio.Shared.DTOs.Clubs;
using Calcio.Shared.Enums;
using Calcio.Shared.Extensions.Clubs;
using Calcio.Shared.Models.Entities;
using Calcio.Shared.Results;
using Calcio.Shared.Services.Clubs;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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
        await using var dbContext = await readOnlyDbContextFactory.CreateDbContextAsync(cancellationToken);

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
            CreatedById = CurrentUserId,
            CalcioUsers = [currentUser]
        };

        dbContext.Add(club);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Get a fresh user instance from UserManager to avoid tracking conflicts
        var userForRole = await userManager.FindByIdAsync(CurrentUserId.ToString());
        if (userForRole is not null)
        {
            var roleResult = await userManager.AddToRoleAsync(userForRole, "ClubAdmin");
            if (!roleResult.Succeeded)
            {
                var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                LogClubAdminRoleFailed(logger, CurrentUserId, errors);
            }
        }

        LogClubCreated(logger, club.Name, CurrentUserId);

        return new ClubCreatedDto(club.ClubId, club.Name);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Club '{ClubName}' created by user {UserId}")]
    private static partial void LogClubCreated(ILogger logger, string clubName, long userId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to add ClubAdmin role to user {UserId}: {Errors}")]
    private static partial void LogClubAdminRoleFailed(ILogger logger, long userId, string errors);
}
