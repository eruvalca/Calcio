using Calcio.Shared.DTOs.Players;
using Calcio.Shared.Results;
using Calcio.Shared.Services.Players;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace Calcio.UI.Components.Players;

[Authorize(Roles = "ClubAdmin")]
public partial class ClubPlayersGrid(IPlayersService playersService)
{
    [Parameter]
    public required long ClubId { get; set; }

    private List<ClubPlayerDto> Players { get; set; } = [];

    private string SearchTerm { get; set; } = string.Empty;

    private bool IsLoading { get; set; } = true;

    private string? LoadErrorMessage { get; set; }

    private IEnumerable<ClubPlayerDto> FilteredPlayers
        => string.IsNullOrWhiteSpace(SearchTerm)
            ? Players
            : Players.Where(player => player.FullName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)
                || player.FirstName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)
                || player.LastName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));

    protected override async Task OnInitializedAsync()
        => await LoadPlayersAsync();

    private async Task LoadPlayersAsync()
    {
        IsLoading = true;
        LoadErrorMessage = null;

        var result = await playersService.GetClubPlayersAsync(ClubId, CancellationToken);

        result.Switch(
            players =>
            {
                Players = players;
                IsLoading = false;
            },
            problem =>
            {
                LoadErrorMessage = problem.Kind switch
                {
                    ServiceProblemKind.Forbidden => "You are not authorized to view the club players requested.",
                    _ => problem.Detail ?? "An unexpected error occurred while loading players."
                };
                IsLoading = false;
            });
    }
}
