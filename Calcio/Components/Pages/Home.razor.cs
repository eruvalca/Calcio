using Calcio.Shared.Services.Clubs;

using Microsoft.AspNetCore.Components;

namespace Calcio.Components.Pages;

/// <summary>
/// Loads home page navigation behavior based on the authenticated user's club membership.
/// </summary>
/// <param name="clubsService">Provides club retrieval operations for the signed-in user.</param>
/// <param name="navigationManager">Navigates to the clubs management page when no clubs are available.</param>
public partial class Home(
    IClubsService clubsService,
    NavigationManager navigationManager)
{
    /// <summary>
    /// Gets the cascading HTTP context used to determine authentication status.
    /// </summary>
    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

    /// <summary>
    /// Redirects authenticated users without clubs to the club management screen.
    /// </summary>
    /// <returns>A task that completes when the initialization workflow finishes.</returns>
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
