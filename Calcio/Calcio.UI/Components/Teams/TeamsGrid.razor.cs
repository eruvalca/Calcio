using Calcio.Shared.DTOs.Teams;
using Calcio.Shared.Results;
using Calcio.Shared.Services.Teams;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace Calcio.UI.Components.Teams;

[Authorize(Roles = "ClubAdmin")]
public partial class TeamsGrid(ITeamsService teamService)
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
            problem =>
            {
                ErrorMessage = problem.Kind switch
                {
                    ServiceProblemKind.Forbidden => "You are not authorized to view the teams requested.",
                    _ => problem.Detail ?? "An unexpected error occurred while loading teams."
                };
                IsLoading = false;
            });
    }
}
