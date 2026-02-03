using Calcio.Shared.Services.Clubs;

using Microsoft.AspNetCore.Components;

namespace Calcio.Components.Pages;

public partial class Home(
    IClubsService clubsService,
    NavigationManager navigationManager)
{
    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        if (HttpContext.User.Identity?.IsAuthenticated is not true)
        {
            return;
        }

        var userClubsResult = await clubsService.GetUserClubsAsync(CancellationToken);

        userClubsResult.Switch(
            clubs =>
            {
                if (clubs.Count == 0)
                {
                    navigationManager.NavigateTo("Account/Manage/Clubs");
                }
            },
            _ => { }); // Fail open on service errors
    }
}
