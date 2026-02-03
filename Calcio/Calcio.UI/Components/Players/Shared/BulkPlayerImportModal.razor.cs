using Calcio.Shared.DTOs.Players;
using Calcio.Shared.Endpoints;
using Calcio.Shared.Results;
using Calcio.Shared.Services.Players;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Calcio.UI.Components.Players.Shared;

public partial class BulkPlayerImportModal(IPlayersService playersService)
{
    [Parameter]
    public required long ClubId { get; set; }

    [Parameter]
    public bool IsVisible { get; set; }

    [Parameter]
    public EventCallback OnClose { get; set; }

    [Parameter]
    public EventCallback OnImportComplete { get; set; }

    private IBrowserFile? SelectedFile { get; set; }
    private bool IsProcessing { get; set; }
    private string? ErrorMessage { get; set; }
    private string? SuccessMessage { get; set; }
    private PlayerImportResultDto? ImportResult { get; set; }
    private PlayerImportStatusDto? ImportStatus { get; set; }

    private string TemplateUrl => Routes.Players.ForImportTemplate(ClubId);

    private void OnFileSelected(InputFileChangeEventArgs e)
    {
        SelectedFile = e.File;
        ErrorMessage = null;
        SuccessMessage = null;
    }

    private async Task HandleImport()
    {
        if (SelectedFile is null)
        {
            ErrorMessage = "Please select a file to upload.";
            return;
        }

        ErrorMessage = null;
        SuccessMessage = null;
        IsProcessing = true;
        ImportResult = null;
        ImportStatus = null;

        try
        {
            const long maxFileSize = 10 * 1024 * 1024; // 10MB
            await using var stream = SelectedFile.OpenReadStream(maxFileSize, CancellationToken);

            var result = await playersService.BulkImportPlayersAsync(
                ClubId,
                stream,
                SelectedFile.Name,
                SelectedFile.ContentType,
                CancellationToken);

            if (result.IsProblem)
            {
                ErrorMessage = result.Problem.Kind switch
                {
                    ServiceProblemKind.BadRequest => result.Problem.Detail ?? "Invalid file format or data.",
                    ServiceProblemKind.Forbidden => "You do not have permission to import players to this club.",
                    _ => "An error occurred during import. Please try again."
                };
            }
            else
            {
                ImportResult = result.Value;

                // Get detailed status
                var statusResult = await playersService.GetImportStatusAsync(ClubId, ImportResult.ImportId, CancellationToken);
                if (statusResult.IsSuccess)
                {
                    ImportStatus = statusResult.Value;
                }

                if (ImportResult.FailedRows == 0)
                {
                    SuccessMessage = $"Successfully imported {ImportResult.SuccessfulRows} player(s).";
                    
                    // Notify parent to refresh
                    await OnImportComplete.InvokeAsync();
                }
                else
                {
                    ErrorMessage = $"{ImportResult.FailedRows} row(s) failed validation. No players were imported. Please review the errors below and correct your file.";
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"An error occurred: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private async Task Close()
    {
        IsVisible = false;
        SelectedFile = null;
        ErrorMessage = null;
        SuccessMessage = null;
        ImportResult = null;
        ImportStatus = null;
        await OnClose.InvokeAsync();
    }

    private void OnBackdropClick()
    {
        // Clicking on backdrop closes the modal
        _ = Close();
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB"];
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
