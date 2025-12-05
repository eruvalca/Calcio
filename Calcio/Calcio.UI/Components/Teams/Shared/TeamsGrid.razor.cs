using System.ComponentModel.DataAnnotations;

using Calcio.Shared.DTOs.Teams;
using Calcio.Shared.Results;
using Calcio.Shared.Services.Teams;
using Calcio.Shared.Validation;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace Calcio.UI.Components.Teams.Shared;

[Authorize(Roles = "ClubAdmin")]
public partial class TeamsGrid(
    ITeamsService teamService,
    NavigationManager navigationManager)
{
    [Parameter]
    public long ClubId { get; set; }

    [Parameter]
    public List<TeamDto> Teams { get; set; } = [];

    private bool ShowCreateForm { get; set; }

    private bool IsCreating { get; set; }

    private string? CreateErrorMessage { get; set; }

    private CreateTeamInputModel CreateInput { get; set; } = new();

    private static int CurrentYear => DateTime.Today.Year;

    private static int MaxYear => DateTime.Today.Year + 25;

    private void ToggleCreateForm()
    {
        ShowCreateForm = !ShowCreateForm;
        CreateErrorMessage = null;
        CreateInput = new CreateTeamInputModel();
    }

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

    private sealed class CreateTeamInputModel
    {
        [Required(ErrorMessage = "Team name is required.")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Team name must be between 1 and 100 characters.")]
        public string? Name { get; set; }

        [GraduationYear]
        public int GraduationYear { get; set; } = DateTime.Today.Year;
    }
}