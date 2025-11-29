using Calcio.Shared.DTOs.Seasons;
using Calcio.Shared.Results;
using Calcio.Shared.Services.Seasons;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace Calcio.UI.Components.Clubs.Shared;

[Authorize(Roles = "ClubAdmin")]
public partial class SeasonsGrid(ISeasonsService seasonService)
{
    [Parameter]
    public long ClubId { get; set; }

    private List<SeasonDto> Seasons { get; set; } = [];

    private bool IsLoading { get; set; } = true;

    private string? ErrorMessage { get; set; }

    protected override async Task OnInitializedAsync()
        => await LoadSeasonsAsync();

    private async Task LoadSeasonsAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        var result = await seasonService.GetSeasonsAsync(ClubId, CancellationToken);

        result.Switch(
            seasons =>
            {
                Seasons = seasons;
                IsLoading = false;
            },
            problem =>
            {
                ErrorMessage = problem.Kind switch
                {
                    ServiceProblemKind.Forbidden => "You are not authorized to view seasons.",
                    _ => problem.Detail ?? "An unexpected error occurred while loading seasons."
                };
                IsLoading = false;
            });
    }
}
