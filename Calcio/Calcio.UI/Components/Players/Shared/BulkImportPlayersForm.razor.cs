using Calcio.Shared.DTOs.Players.BulkImport;
using Calcio.Shared.Results;
using Calcio.Shared.Services.Players;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Calcio.UI.Components.Players.Shared;

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
    private bool ShowColumnMappings { get; set; }

    private IBrowserFile? SelectedFile { get; set; }
    private string? SelectedFileName { get; set; }

    private BulkValidateResultDto? ValidationResult { get; set; }
    private BulkImportResultDto? ImportResult { get; set; }

    private bool CanImport => ValidationResult is not null
        && ValidationResult.ErrorCount == 0
        && ValidationResult.ValidCount > 0;

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

    private void GoToStep1()
    {
        CurrentStep = 1;
        ValidationResult = null;
        ErrorMessage = null;
    }

    private void StartOver()
    {
        CurrentStep = 1;
        SelectedFile = null;
        SelectedFileName = null;
        ValidationResult = null;
        ImportResult = null;
        ErrorMessage = null;
    }

    private static string FormatFileSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes / 1024.0 / 1024.0:F1} MB"
    };
}
