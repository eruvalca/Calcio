using Calcio.Shared.DTOs.Clubs;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace Calcio.UI.Components.Clubs.Shared;

[Authorize]
public partial class FilterableClubsGrid
{
    [Parameter]
    public List<BaseClubDto> Clubs { get; set; } = [];

    private string SearchTerm { get; set; } = string.Empty;

    private IEnumerable<BaseClubDto> FilteredClubs
        => string.IsNullOrWhiteSpace(SearchTerm)
            ? Clubs
            : Clubs.Where(club => club.Name.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)
                || club.City.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)
                || club.State.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));
}
