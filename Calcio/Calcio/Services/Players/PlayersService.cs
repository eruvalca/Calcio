using Calcio.Data.Contexts;
using Calcio.Shared.DTOs.Players;
using Calcio.Shared.Extensions.Players;
using Calcio.Shared.Results;
using Calcio.Shared.Services.Players;

using Microsoft.EntityFrameworkCore;

namespace Calcio.Services.Players;

public partial class PlayersService(
    IDbContextFactory<ReadOnlyDbContext> readOnlyDbContextFactory,
    IHttpContextAccessor httpContextAccessor,
    ILogger<PlayersService> logger) : AuthenticatedServiceBase(httpContextAccessor), IPlayersService
{
    public async Task<ServiceResult<List<ClubPlayerDto>>> GetClubPlayersAsync(long clubId, CancellationToken cancellationToken)
    {
        // Club membership is validated by ClubMembershipFilter before this service is called.
        await using var dbContext = await readOnlyDbContextFactory.CreateDbContextAsync(cancellationToken);

        var players = await dbContext.Players
            .Where(p => p.ClubId == clubId)
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .Select(p => p.ToClubPlayerDto())
            .ToListAsync(cancellationToken);

        LogPlayersRetrieved(logger, clubId, players.Count, CurrentUserId);
        return players;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Retrieved {PlayerCount} players for club {ClubId} by user {UserId}")]
    private static partial void LogPlayersRetrieved(ILogger logger, long clubId, int playerCount, long userId);
}
