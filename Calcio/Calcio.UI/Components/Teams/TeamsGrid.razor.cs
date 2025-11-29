using Calcio.Shared.DTOs.Teams;
using Calcio.Shared.Services.Teams;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace Calcio.UI.Components.Teams;

[Authorize(Roles = "ClubAdmin")]
public partial class TeamsGrid(ITeamService teamService)
{
    [Parameter]
    public long ClubId { get; set; }

    private List<TeamDto> Teams { get; set; } = [];

    private bool IsLoading { get; set; } = true;

    private string? ErrorMessage { get; set; }

    protected override async Task OnInitializedAsync()
        => await LoadTeamsAsync();

    private async Task LoadTeamsAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        var result = await teamService.GetTeamsAsync(ClubId, CancellationToken);

        result.Switch(
            teams =>
            {
                Teams = teams;
                IsLoading = false;
            },
            unauthorized =>
            {
                ErrorMessage = "You are not authorized to view teams.";
                IsLoading = false;
            },
            error =>
            {
                ErrorMessage = "An unexpected error occurred while loading teams.";
                IsLoading = false;
            });
    }
}
