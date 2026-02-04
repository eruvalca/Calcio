using Calcio.Shared.DTOs.Players.BulkImport;
using Calcio.Shared.Results;
using Calcio.Shared.Services.Players;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Calcio.UI.Components.Players.Shared;

public enum GridFilter
{
    All,
    Errors,
    Warnings,
    Valid
}

public partial class BulkImportPlayersForm(IPlayersService playersService)
{
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

    [Parameter]
    public required long ClubId { get; set; }

    [Parameter]
    public string CancelUrl { get; set; } = "/";

    private int CurrentStep { get; set; } = 1;
    private bool IsProcessing { get; set; }
    private string? ErrorMessage { get; set; }
    private bool IsDragOver { get; set; }
    private bool ShowColumnMappings { get; set; }
    private bool ShowWarningDetails { get; set; }
    private GridFilter CurrentFilter { get; set; } = GridFilter.All;

    private IBrowserFile? SelectedFile { get; set; }
    private string? SelectedFileName { get; set; }

    private BulkValidateResultDto? ValidationResult { get; set; }
    private BulkImportResultDto? ImportResult { get; set; }

    private int MarkedForImportCount => ValidationResult?.Rows.Count(r => r.IsMarkedForImport && r.IsValid) ?? 0;

    private List<PlayerImportRowDto> FilteredRows => ValidationResult?.Rows is null
        ? []
        : CurrentFilter switch
        {
            GridFilter.Errors => [.. ValidationResult.Rows.Where(r => r.Errors.Count > 0)],
            GridFilter.Warnings => [.. ValidationResult.Rows.Where(r => r.Warnings.Count > 0 && r.Errors.Count == 0)],
            GridFilter.Valid => [.. ValidationResult.Rows.Where(r => r.IsValid && r.Warnings.Count == 0)],
            _ => ValidationResult.Rows
        };

    private IEnumerable<(string Message, int Count)> GroupedWarnings => ValidationResult?.Rows is null
        ? []
        : ValidationResult.Rows
            .SelectMany(r => r.Warnings)
            .GroupBy(w => w)
            .Select(g => (Message: g.Key, Count: g.Count()))
            .OrderByDescending(g => g.Count);

    private void OnFileSelected(InputFileChangeEventArgs e)
    {
        ErrorMessage = null;
        var file = e.File;

        if (file.Size > MaxFileSize)
        {
            ErrorMessage = $"File size ({FormatFileSize(file.Size)}) exceeds maximum of 10 MB.";
            SelectedFile = null;
            SelectedFileName = null;
            return;
        }

        SelectedFile = file;
        SelectedFileName = file.Name;
    }

    private void HandleDragOver() => IsDragOver = true;

    private void HandleDragLeave() => IsDragOver = false;

    private void HandleDrop() => IsDragOver = false;// Note: Blazor doesn't directly support getting files from drag events// The InputFile component handles this via the browser

    private void ClearFile()
    {
        SelectedFile = null;
        SelectedFileName = null;
        ErrorMessage = null;
    }

    private async Task ValidateFile()
    {
        if (SelectedFile is null)
        {
            return;
        }

        IsProcessing = true;
        ErrorMessage = null;

        try
        {
            await using var stream = SelectedFile.OpenReadStream(MaxFileSize, CancellationToken);
            var result = await playersService.ValidateBulkImportAsync(ClubId, stream, SelectedFile.Name, CancellationToken);

            result.Switch(
                success =>
                {
                    ValidationResult = success;

                    if (success.MissingRequiredColumns.Count > 0)
                    {
                        ShowColumnMappings = true;
                        ErrorMessage = $"Required columns are missing: {string.Join(", ", success.MissingRequiredColumns)}. Please update your file or download the template.";
                    }
                    else
                    {
                        CurrentStep = 2;
                    }
                },
                problem =>
                {
                    ErrorMessage = problem.Detail ?? problem.Kind switch
                    {
                        ServiceProblemKind.BadRequest => "The file could not be processed. Please check the format and try again.",
                        ServiceProblemKind.Forbidden => "You do not have permission to import players to this club.",
                        _ => "An unexpected error occurred. Please try again."
                    };
                });
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error reading file: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private async Task RevalidateRows()
    {
        if (ValidationResult?.Rows is null || ValidationResult.Rows.Count == 0)
        {
            return;
        }

        IsProcessing = true;
        ErrorMessage = null;

        try
        {
            var result = await playersService.RevalidateBulkImportAsync(ClubId, ValidationResult.Rows, CancellationToken);

            result.Switch(
                success =>
                {
                    // Preserve column mappings from original validation
                    ValidationResult = success with
                    {
                        ColumnMappings = ValidationResult.ColumnMappings,
                        MissingRequiredColumns = ValidationResult.MissingRequiredColumns
                    };
                },
                problem => ErrorMessage = problem.Detail ?? "Failed to re-validate rows. Please try again.");
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private async Task ExecuteImport()
    {
        if (ValidationResult?.Rows is null || MarkedForImportCount == 0)
        {
            return;
        }

        IsProcessing = true;
        ErrorMessage = null;

        try
        {
            var result = await playersService.BulkCreatePlayersAsync(ClubId, ValidationResult.Rows, CancellationToken);

            result.Switch(
                success =>
                {
                    ImportResult = success;
                    CurrentStep = 3;
                },
                problem =>
                {
                    ErrorMessage = problem.Detail ?? problem.Kind switch
                    {
                        ServiceProblemKind.Forbidden => "You do not have permission to import players to this club.",
                        ServiceProblemKind.BadRequest => "Some player data is invalid. Please review and try again.",
                        _ => "An unexpected error occurred during import. Please try again."
                    };
                });
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private void GoToStep1()
    {
        CurrentStep = 1;
        ValidationResult = null;
        ErrorMessage = null;
        CurrentFilter = GridFilter.All;
    }

    private void StartOver()
    {
        CurrentStep = 1;
        SelectedFile = null;
        SelectedFileName = null;
        ValidationResult = null;
        ImportResult = null;
        ErrorMessage = null;
        CurrentFilter = GridFilter.All;
    }

    private void HandleRowsModified()
        // Trigger re-render to update counts
        => StateHasChanged();

    private static string FormatFileSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes / 1024.0 / 1024.0:F1} MB"
    };
}
