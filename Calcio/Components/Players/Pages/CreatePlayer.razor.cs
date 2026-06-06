using Microsoft.AspNetCore.Components;

namespace Calcio.Components.Players.Pages;

/// <summary>
/// Hosts the player creation page for a specific club.
/// </summary>
public partial class CreatePlayer
{
    /// <summary>
    /// Gets or sets the target club identifier for the player creation workflow.
    /// </summary>
    [Parameter]
    public long ClubId { get; set; }
}
