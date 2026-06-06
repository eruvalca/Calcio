using Calcio.Shared.DTOs.Players.BulkImport;
using Calcio.Shared.Results;
using Calcio.Shared.Services.Players;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Calcio.UI.Components.Players.Shared;

/// <summary>
/// Provides a multi-step workflow to validate and bulk import players from a CSV file.
/// </summary>
/// <param name="playersService">The service used to validate and import player data.</param>
public partial class BulkImportPlayersForm(IPlayersService playersService)
{
    /// <summary>
    /// Defines the maximum accepted import file size in bytes.
    /// </summary>
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

    /// <summary>
    /// Gets or sets the club identifier for import operations.
    /// </summary>
    [Parameter]
    public required long ClubId { get; set; }

    /// <summary>
    /// Gets or sets the URL to navigate to when the user cancels.
    /// </summary>
    [Parameter]
    public string CancelUrl { get; set; } = "/";

    /// <summary>
    /// Gets or sets the current workflow step.
    /// </summary>
    private int CurrentStep { get; set; } = 1;

    /// <summary>
    /// Gets or sets a value indicating whether validation or import processing is active.
    /// </summary>
    private bool IsProcessing { get; set; }

    /// <summary>
    /// Gets or sets the current error message shown in the UI.
    /// </summary>
    private string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether column mapping guidance should be shown.
    /// </summary>
    private bool ShowColumnMappings { get; set; }

    /// <summary>
    /// Gets or sets the currently selected CSV file.
    /// </summary>
    private IBrowserFile? SelectedFile { get; set; }

    /// <summary>
    /// Gets or sets the display name of the selected CSV file.
    /// </summary>
    private string? SelectedFileName { get; set; }

    /// <summary>
    /// Gets or sets the latest validation result.
    /// </summary>
    private BulkValidateResultDto? ValidationResult { get; set; }

    /// <summary>
    /// Gets or sets the latest import result.
    /// </summary>
    private BulkImportResultDto? ImportResult { get; set; }

    /// <summary>
    /// Gets a value indicating whether import can proceed with the current validation result.
    /// </summary>
    private bool CanImport => ValidationResult is not null
        && ValidationResult.ErrorCount == 0
        && ValidationResult.ValidCount > 0;

    /// <summary>
    /// Validates the selected file's type and size and stores it for processing.
    /// </summary>
    /// <param name="e">File selection event data.</param>
    private void OnFileSelected(InputFileChangeEventArgs e)
    {
        ErrorMessage = null;
        var file = e.File;

        var extension = Path.GetExtension(file.Name);
        if (!extension.Equals(".csv", StringComparison.OrdinalIgnoreCase))
        {
            ErrorMessage = "Only CSV files are supported. Save your Excel or Google Sheets file as CSV and try again.";
            SelectedFile = null;
            SelectedFileName = null;
            return;
        }

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

    /// <summary>
    /// Clears the selected file and related validation errors.
    /// </summary>
    private void ClearFile()
    {
        SelectedFile = null;
        SelectedFileName = null;
        ErrorMessage = null;
    }

    /// <summary>
    /// Validates the selected CSV file against bulk-import rules.
    /// </summary>
    /// <returns>A task that completes when validation finishes.</returns>
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
                        ServiceProblemKind.BadRequest => "Only CSV files are supported. Save your Excel or Google Sheets file as CSV and try again.",
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

    /// <summary>
    /// Executes the player import using previously validated rows.
    /// </summary>
    /// <returns>A task that completes when import processing finishes.</returns>
    private async Task ExecuteImport()
    {
        if (ValidationResult?.Rows is null || !CanImport)
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

    /// <summary>
    /// Returns the workflow to step 1 while preserving the selected file.
    /// </summary>
    private void GoToStep1()
    {
        CurrentStep = 1;
        ValidationResult = null;
        ErrorMessage = null;
    }

    /// <summary>
    /// Resets the workflow to its initial state.
    /// </summary>
    private void StartOver()
    {
        CurrentStep = 1;
        SelectedFile = null;
        SelectedFileName = null;
        ValidationResult = null;
        ImportResult = null;
        ErrorMessage = null;
    }

    /// <summary>
    /// Formats a byte count using human-readable file size units.
    /// </summary>
    /// <param name="bytes">The byte count to format.</param>
    /// <returns>A formatted file size string.</returns>
    private static string FormatFileSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes / 1024.0 / 1024.0:F1} MB"
    };
}
