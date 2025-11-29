using Calcio.Data.Contexts;
using Calcio.Shared.DTOs.Players;
using Calcio.Shared.Extensions.Players;
using Calcio.Shared.Results;
using Calcio.Shared.Services.Players;

using Microsoft.EntityFrameworkCore;

using OneOf;
using OneOf.Types;

namespace Calcio.Services.Players;

public partial class PlayersService(
    IDbContextFactory<ReadOnlyDbContext> readOnlyDbContextFactory,
    IHttpContextAccessor httpContextAccessor,
    ILogger<PlayersService> logger) : AuthenticatedServiceBase(httpContextAccessor), IPlayersService
{
    public async Task<OneOf<List<ClubPlayerDto>, Unauthorized, Error>> GetClubPlayersAsync(long clubId, CancellationToken cancellationToken)
    {
        await using var dbContext = await readOnlyDbContextFactory.CreateDbContextAsync(cancellationToken);

        var isClubMember = await dbContext.Clubs
            .AnyAsync(c => c.ClubId == clubId, cancellationToken);

        if (!isClubMember)
        {
            return new Unauthorized();
        }

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
