using Calcio.Shared.DTOs.CalcioUsers;
using Calcio.Shared.DTOs.ClubJoinRequests;
using Calcio.Shared.DTOs.Clubs;
using Calcio.Shared.DTOs.Players;
using Calcio.Shared.DTOs.Seasons;
using Calcio.Shared.DTOs.Teams;
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
public partial class ClubDetail(
    IClubsService clubsService,
    IClubJoinRequestsService clubJoinRequestsService,
    ICalcioUsersService calcioUsersService,
    IPlayersService playersService,
    ISeasonsService seasonsService,
    ITeamsService teamsService)
{
    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

    [Parameter]
    public long ClubId { get; set; }

    private BaseClubDto? Club { get; set; }
    private List<ClubJoinRequestWithUserDto> ClubJoinRequests { get; set; } = [];
    private List<ClubMemberDto> ClubMembers { get; set; } = [];
    private List<ClubPlayerDto> ClubPlayers { get; set; } = [];
    private List<SeasonDto> ClubSeasons { get; set; } = [];
    private List<TeamDto> ClubTeams { get; set; } = [];
    private bool IsClubAdmin { get; set; }

    protected override async Task OnInitializedAsync()
    {
        IsClubAdmin = HttpContext.User.IsInRole("ClubAdmin");

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
}
