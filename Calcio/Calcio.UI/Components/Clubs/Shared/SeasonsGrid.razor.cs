using System.ComponentModel.DataAnnotations;

using Calcio.Shared.DTOs.Seasons;
using Calcio.Shared.Results;
using Calcio.Shared.Services.Seasons;
using Calcio.Shared.Validation;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace Calcio.UI.Components.Clubs.Shared;

[Authorize(Roles = "ClubAdmin")]
public partial class SeasonsGrid(ISeasonsService seasonService)
{
    [Parameter]
    public long ClubId { get; set; }

    private List<SeasonDto> Seasons { get; set; } = [];

    private bool IsLoading { get; set; } = true;

    private string? ErrorMessage { get; set; }

    private bool ShowCreateForm { get; set; }

    private bool IsCreating { get; set; }

    private string? CreateErrorMessage { get; set; }

    private CreateSeasonInputModel CreateInput { get; set; } = new();

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

    private static DateOnly Tomorrow => Today.AddDays(1);

    protected override async Task OnInitializedAsync()
        => await LoadSeasonsAsync();

    private async Task LoadSeasonsAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        var result = await seasonService.GetSeasonsAsync(ClubId, CancellationToken);

        result.Switch(
            seasons =>
            {
                Seasons = seasons;
                IsLoading = false;
            },
            problem =>
            {
                ErrorMessage = problem.Kind switch
                {
                    ServiceProblemKind.Forbidden => "You are not authorized to view seasons.",
                    _ => problem.Detail ?? "An unexpected error occurred while loading seasons."
                };
                IsLoading = false;
            });
    }

    private void ToggleCreateForm()
    {
        ShowCreateForm = !ShowCreateForm;
        CreateErrorMessage = null;

        if (ShowCreateForm)
        {
            CreateInput = new CreateSeasonInputModel();
        }
    }

    private async Task HandleCreateSeason()
    {
        if (IsCreating)
        {
            return;
        }

        IsCreating = true;
        CreateErrorMessage = null;

        var dto = new CreateSeasonDto(CreateInput.Name, CreateInput.StartDate, CreateInput.EndDate);
        var result = await seasonService.CreateSeasonAsync(ClubId, dto, CancellationToken);

        result.Switch(
            success =>
            {
                ShowCreateForm = false;
                CreateInput = new CreateSeasonInputModel();
                IsCreating = false;
                InvokeAsync(LoadSeasonsAsync);
            },
            problem =>
            {
                CreateErrorMessage = problem.Kind switch
                {
                    ServiceProblemKind.Forbidden => "You are not authorized to create seasons.",
                    ServiceProblemKind.Conflict => "A season with this name already exists.",
                    _ => problem.Detail ?? "An unexpected error occurred while creating the season."
                };
                IsCreating = false;
            });
    }

    private sealed class CreateSeasonInputModel
    {
        private static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

        [Required]
        [StringLength(100, MinimumLength = 1)]
        [Display(Name = "Season Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [DateNotBeforeToday]
        [Display(Name = "Start Date")]
        public DateOnly StartDate
        {
            get;
            set => field = value < Today ? Today : value;
        } = Today;

        [DateAfterToday]
        [DateAfter(nameof(StartDate))]
        [Display(Name = "End Date")]
        public DateOnly? EndDate
        {
            get;
            set
            {
                if (value is null)
                {
                    field = null;
                }
                else
                {
                    var minEndDate = StartDate.AddDays(1);
                    field = value < minEndDate ? minEndDate : value;
                }
            }
        }
    }
}
