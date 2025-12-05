using System.ComponentModel.DataAnnotations;

using Calcio.Shared.DTOs.Teams;
using Calcio.Shared.Results;
using Calcio.Shared.Services.Teams;
using Calcio.Shared.Validation;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace Calcio.UI.Components.Teams.Shared;

[Authorize(Roles = "ClubAdmin")]
public partial class TeamsGrid(ITeamsService teamService)
{
    [Parameter]
    public long ClubId { get; set; }

    private List<TeamDto> Teams { get; set; } = [];

    private bool IsLoading { get; set; } = true;

    private string? ErrorMessage { get; set; }

    private bool ShowCreateForm { get; set; }

    private bool IsCreating { get; set; }

    private string? CreateErrorMessage { get; set; }

    private CreateTeamInputModel CreateInput { get; set; } = new();

    private static int CurrentYear => DateTime.Today.Year;

    private static int MaxYear => DateTime.Today.Year + 25;

    protected override async Task OnInitializedAsync()
        => await LoadTeamsAsync();

    private async Task LoadTeamsAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        var result = await teamService.GetTeamsAsync(ClubId, CancellationToken);

        result.Switch(
            teams =>
            {
                Teams = teams;
                IsLoading = false;
            },
            problem =>
            {
                ErrorMessage = problem.Kind switch
                {
                    ServiceProblemKind.Forbidden => "You are not authorized to view the teams requested.",
                    _ => problem.Detail ?? "An unexpected error occurred while loading teams."
                };
                IsLoading = false;
            });
    }

    private void ToggleCreateForm()
    {
        ShowCreateForm = !ShowCreateForm;
        CreateErrorMessage = null;
        CreateInput = new CreateTeamInputModel();
    }

    private async Task HandleCreateTeam()
    {
        IsCreating = true;
        CreateErrorMessage = null;

        var dto = new CreateTeamDto(CreateInput.Name!, CreateInput.GraduationYear);
        var result = await teamService.CreateTeamAsync(ClubId, dto, CancellationToken);

        result.Switch(
            _ =>
            {
                ShowCreateForm = false;
                CreateInput = new CreateTeamInputModel();
            },
            problem =>
            {
                CreateErrorMessage = problem.Kind switch
                {
                    ServiceProblemKind.Forbidden => "You are not authorized to create teams.",
                    ServiceProblemKind.Conflict => "A team with this name already exists.",
                    _ => problem.Detail ?? "An unexpected error occurred while creating the team."
                };
            });

        IsCreating = false;

        if (result.IsSuccess)
        {
            await LoadTeamsAsync();
        }
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