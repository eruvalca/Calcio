using System.ComponentModel.DataAnnotations;

using Calcio.Shared.DTOs.Teams;
using Calcio.Shared.Extensions.Shared;
using Calcio.Shared.Results;
using Calcio.Shared.Security;
using Calcio.Shared.Services.Teams;
using Calcio.Shared.Validation;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace Calcio.UI.Components.Teams.Shared;

[Authorize(Roles = Roles.ClubAdmin)]
/// <summary>
/// Displays teams for a club, supports filtering, and allows new team creation.
/// </summary>
/// <param name="teamService">The service used to create teams.</param>
/// <param name="navigationManager">The navigation manager used to refresh after creation.</param>
public partial class TeamsGrid(
    ITeamsService teamService,
    NavigationManager navigationManager)
{
    /// <summary>
    /// Gets or sets the club identifier associated with the teams.
    /// </summary>
    [Parameter]
    public long ClubId { get; set; }

    /// <summary>
    /// Gets or sets the teams displayed in the grid.
    /// </summary>
    [Parameter]
    public List<TeamDto> Teams { get; set; } = [];

    /// <summary>
    /// Gets or sets the current search term used to filter teams.
    /// </summary>
    private string SearchTerm { get; set; } = string.Empty;

    /// <summary>
    /// Gets teams matching the current search term.
    /// </summary>
    private IEnumerable<TeamDto> FilteredTeams
        => string.IsNullOrWhiteSpace(SearchTerm)
            ? Teams
            : Teams.Where(team => team.Name.ContainsIgnoreCase(SearchTerm)
                || team.GraduationYear.ToString().Contains(SearchTerm));

    /// <summary>
    /// Gets or sets a value indicating whether the create-team form is visible.
    /// </summary>
    private bool ShowCreateForm { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a team is currently being created.
    /// </summary>
    private bool IsCreating { get; set; }

    /// <summary>
    /// Gets or sets the create-team error message.
    /// </summary>
    private string? CreateErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the create-team input model.
    /// </summary>
    private CreateTeamInputModel CreateInput { get; set; } = new();

    /// <summary>
    /// Gets the current calendar year.
    /// </summary>
    private static int CurrentYear => DateTime.Today.Year;

    /// <summary>
    /// Gets the maximum graduation year accepted by the form.
    /// </summary>
    private static int MaxYear => DateTime.Today.Year + 25;

    /// <summary>
    /// Toggles visibility of the create-team form.
    /// </summary>
    private void ToggleCreateForm()
    {
        ShowCreateForm = !ShowCreateForm;
        CreateErrorMessage = null;
        CreateInput = new CreateTeamInputModel();
    }

    /// <summary>
    /// Creates a team using the current form input.
    /// </summary>
    /// <returns>A task that completes when create handling finishes.</returns>
    private async Task HandleCreateTeam()
    {
        if (IsCreating)
        {
            return;
        }

        IsCreating = true;
        CreateErrorMessage = null;

        var dto = new CreateTeamDto(CreateInput.Name!, CreateInput.GraduationYear);
        var result = await teamService.CreateTeamAsync(ClubId, dto, CancellationToken);

        result.Switch(
            success =>
            {
                ShowCreateForm = false;
                CreateInput = new CreateTeamInputModel();
                IsCreating = false;
                navigationManager.Refresh();
            },
            problem =>
            {
                CreateErrorMessage = problem.Kind switch
                {
                    ServiceProblemKind.Forbidden => "You are not authorized to create teams.",
                    ServiceProblemKind.Conflict => "A team with this name already exists.",
                    _ => problem.Detail ?? "An unexpected error occurred while creating the team."
                };
                IsCreating = false;
            });
    }

    /// <summary>
    /// Represents form input used to create a team.
    /// </summary>
    private sealed class CreateTeamInputModel
    {
        /// <summary>
        /// Gets or sets the team name.
        /// </summary>
        [Required(ErrorMessage = "Team name is required.")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Team name must be between 1 and 100 characters.")]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the team's graduation year.
        /// </summary>
        [GraduationYear]
        public int GraduationYear { get; set; } = DateTime.Today.Year;
    }
}