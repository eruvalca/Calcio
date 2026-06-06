using Calcio.Shared.DTOs.CalcioUsers;
using Calcio.Shared.DTOs.ClubJoinRequests;
using Calcio.Shared.DTOs.Clubs;
using Calcio.Shared.DTOs.Players;
using Calcio.Shared.DTOs.Seasons;
using Calcio.Shared.DTOs.Teams;
using Calcio.Shared.Security;
using Calcio.Shared.Services.CalcioUsers;
using Calcio.Shared.Services.ClubJoinRequests;
using Calcio.Shared.Services.Clubs;
using Calcio.Shared.Services.Players;
using Calcio.Shared.Services.Seasons;
using Calcio.Shared.Services.Teams;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace Calcio.Components.Clubs.Pages;

[Authorize]
/// <summary>
/// Loads and displays club details, membership data, and related club resources.
/// </summary>
/// <param name="clubsService">Provides club retrieval operations.</param>
/// <param name="clubJoinRequestsService">Provides pending club join request operations.</param>
/// <param name="calcioUsersService">Provides club membership retrieval operations.</param>
/// <param name="playersService">Provides club player retrieval operations.</param>
/// <param name="seasonsService">Provides season retrieval operations for the club.</param>
/// <param name="teamsService">Provides team retrieval operations for the club.</param>
public partial class ClubDetail(
    IClubsService clubsService,
    IClubJoinRequestsService clubJoinRequestsService,
    ICalcioUsersService calcioUsersService,
    IPlayersService playersService,
    ISeasonsService seasonsService,
    ITeamsService teamsService)
{
    /// <summary>
    /// Gets the cascading HTTP context used for authorization checks and status code responses.
    /// </summary>
    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

    /// <summary>
    /// Gets or sets the target club identifier from the route.
    /// </summary>
    [Parameter]
    public long ClubId { get; set; }

    /// <summary>
    /// Gets the loaded club summary data.
    /// </summary>
    private BaseClubDto? Club { get; set; }

    /// <summary>
    /// Gets the pending join requests when the current user is a club administrator.
    /// </summary>
    private List<ClubJoinRequestWithUserDto> ClubJoinRequests { get; set; } = [];

    /// <summary>
    /// Gets the club members list.
    /// </summary>
    private List<ClubMemberDto> ClubMembers { get; set; } = [];

    /// <summary>
    /// Gets the club players list.
    /// </summary>
    private List<ClubPlayerDto> ClubPlayers { get; set; } = [];

    /// <summary>
    /// Gets the seasons associated with the club.
    /// </summary>
    private List<SeasonDto> ClubSeasons { get; set; } = [];

    /// <summary>
    /// Gets the teams associated with the club.
    /// </summary>
    private List<TeamDto> ClubTeams { get; set; } = [];

    /// <summary>
    /// Gets a value indicating whether the current user has the club administrator role.
    /// </summary>
    private bool IsClubAdmin { get; set; }

    /// <summary>
    /// Loads club details and related datasets needed by the page.
    /// </summary>
    /// <returns>A task that completes when all required data has been loaded.</returns>
    protected override async Task OnInitializedAsync()
    {
        IsClubAdmin = HttpContext.User.IsInRole(Roles.ClubAdmin);

        var clubResult = await clubsService.GetClubByIdAsync(ClubId, CancellationToken);
        clubResult.Switch(
            club => Club = club,
            problem => Club = null);

        if (Club is null)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        if (IsClubAdmin)
        {
            var pendingRequestsResult = await clubJoinRequestsService.GetPendingRequestsForClubAsync(ClubId, CancellationToken);
            pendingRequestsResult.Switch(
                requests => ClubJoinRequests = requests,
                problem => ClubJoinRequests = []);
        }

        var membersResult = await calcioUsersService.GetClubMembersAsync(ClubId, CancellationToken);
        membersResult.Switch(
            members => ClubMembers = members,
            problem => ClubMembers = []);

        var playersResult = await playersService.GetClubPlayersAsync(ClubId, CancellationToken);
        playersResult.Switch(
            players => ClubPlayers = players,
            problem => ClubPlayers = []);

        var seasonsResult = await seasonsService.GetSeasonsAsync(ClubId, CancellationToken);
        seasonsResult.Switch(
            seasons => ClubSeasons = seasons,
            problem => ClubSeasons = []);

        var teamsResult = await teamsService.GetTeamsAsync(ClubId, CancellationToken);
        teamsResult.Switch(
            teams => ClubTeams = teams,
            problem => ClubTeams = []);
    }
}
