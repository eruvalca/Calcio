using Calcio.Shared.DTOs.CalcioUsers;
using Calcio.Shared.Extensions.Shared;
using Calcio.Shared.Results;
using Calcio.Shared.Services.CalcioUsers;

using Microsoft.AspNetCore.Components;

namespace Calcio.UI.Components.CalcioUsers.Shared;

/// <summary>
/// Displays club members, supports filtering, and allows member removal when editing is enabled.
/// </summary>
/// <param name="calcioUsersService">The service used to remove club members.</param>
public partial class ClubMembersGrid(ICalcioUsersService calcioUsersService)
{
    /// <summary>
    /// Gets or sets the club identifier for membership operations.
    /// </summary>
    [Parameter]
    public required long ClubId { get; set; }

    /// <summary>
    /// Gets or sets the members displayed in the grid.
    /// </summary>
    [Parameter]
    public List<ClubMemberDto> Members { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether member-management actions are disabled.
    /// </summary>
    [Parameter]
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// Gets or sets the current free-text filter value.
    /// </summary>
    private string SearchTerm { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the member awaiting removal confirmation.
    /// </summary>
    private ClubMemberDto? ConfirmingRemoveMember { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a remove operation is in progress.
    /// </summary>
    private bool IsProcessing { get; set; }

    /// <summary>
    /// Gets or sets an error message shown after failed operations.
    /// </summary>
    private string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets members matching the current search term.
    /// </summary>
    private IEnumerable<ClubMemberDto> FilteredMembers
        => string.IsNullOrWhiteSpace(SearchTerm)
            ? Members
            : Members.Where(member => member.FullName.ContainsIgnoreCase(SearchTerm)
                || member.Email.ContainsIgnoreCase(SearchTerm));

    /// <summary>
    /// Opens the remove confirmation dialog for a member.
    /// </summary>
    /// <param name="member">The member targeted for removal.</param>
    private void ShowRemoveConfirmation(ClubMemberDto member)
    {
        ErrorMessage = null;
        ConfirmingRemoveMember = member;
    }

    /// <summary>
    /// Closes the remove confirmation dialog.
    /// </summary>
    private void CancelRemoveConfirmation()
    {
        ConfirmingRemoveMember = null;
        ErrorMessage = null;
    }

    /// <summary>
    /// Removes the selected member from the club.
    /// </summary>
    /// <returns>A task that completes when removal processing finishes.</returns>
    private async Task ConfirmRemove()
    {
        if (ConfirmingRemoveMember is not null && !IsProcessing)
        {
            IsProcessing = true;
            ErrorMessage = null;

            var result = await calcioUsersService.RemoveClubMemberAsync(
                ClubId,
                ConfirmingRemoveMember.UserId,
                CancellationToken);

            result.Switch(
                success =>
                {
                    Members.Remove(ConfirmingRemoveMember);
                    ConfirmingRemoveMember = null;
                    IsProcessing = false;
                },
                problem =>
                {
                    ErrorMessage = problem.Kind switch
                    {
                        ServiceProblemKind.NotFound => "The member could not be found.",
                        ServiceProblemKind.Forbidden => "You are not authorized to remove this member.",
                        _ => problem.Detail ?? "An unexpected error occurred. Please try again."
                    };
                    IsProcessing = false;
                });
        }
    }
}
