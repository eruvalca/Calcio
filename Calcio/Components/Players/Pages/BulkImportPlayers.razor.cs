using Microsoft.AspNetCore.Components;

namespace Calcio.Components.Players.Pages;

/// <summary>
/// Hosts the bulk player import page for a specific club.
/// </summary>
public partial class BulkImportPlayers
{
    /// <summary>
    /// Gets or sets the target club identifier for the import workflow.
    /// </summary>
    [Parameter]
    public long ClubId { get; set; }
}
