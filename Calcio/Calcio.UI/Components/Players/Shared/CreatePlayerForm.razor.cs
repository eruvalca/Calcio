using System.ComponentModel.DataAnnotations;

using Calcio.Shared.DTOs.Players;
using Calcio.Shared.Enums;
using Calcio.Shared.Results;
using Calcio.Shared.Services.Players;
using Calcio.Shared.Validation;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Calcio.UI.Components.Players.Shared;

public partial class CreatePlayerForm(
    IPlayersService playersService,
    NavigationManager navigationManager)
{
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

    [Parameter]
    public required long ClubId { get; set; }

    [Parameter]
    public string CancelUrl { get; set; } = "/account/manage/clubs";

    private CreatePlayerInputModel Input { get; set; } = new();

    private IBrowserFile? SelectedPhoto { get; set; }

    private bool IsSubmitting { get; set; }

    private bool IsUploadingPhoto { get; set; }

    private double UploadProgressPercent { get; set; }

    private string? ErrorMessage { get; set; }

    private string? SuccessMessage { get; set; }

    private static int CurrentYear => DateTime.Today.Year;

    private static int MaxYear => DateTime.Today.Year + 25;

    private void OnPhotoSelected(InputFileChangeEventArgs e)
    {
        ErrorMessage = null;

        var file = e.File;

        if (file.Size > MaxFileSize)
        {
            ErrorMessage = $"File size ({FormatFileSize(file.Size)}) exceeds maximum of 10 MB.";
            SelectedPhoto = null;
            return;
        }

        SelectedPhoto = file;
    }

    private void ClearPhoto()
    {
        SelectedPhoto = null;
    }

    private async Task HandleSubmit()
    {
        if (IsSubmitting)
        {
            return;
        }

        IsSubmitting = true;
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            // Step 1: Create the player
            var createDto = new CreatePlayerDto(
                Input.FirstName,
                Input.LastName,
                Input.DateOfBirth,
                Input.GraduationYear,
                Input.Gender,
                Input.JerseyNumber,
                Input.TryoutNumber);

            var createResult = await playersService.CreatePlayerAsync(ClubId, createDto, CancellationToken);

            if (createResult.IsProblem)
            {
                ErrorMessage = createResult.Problem.Kind switch
                {
                    ServiceProblemKind.Forbidden => "You are not authorized to create players.",
                    ServiceProblemKind.BadRequest => createResult.Problem.Detail ?? "Invalid player data.",
                    ServiceProblemKind.Conflict => "A player with this information already exists.",
                    _ => createResult.Problem.Detail ?? "An unexpected error occurred while creating the player."
                };
                return;
            }

            var createdPlayer = createResult.Value;

            // Step 2: Upload photo if selected (auto-upload in background)
            if (SelectedPhoto is not null)
            {
                IsUploadingPhoto = true;
                UploadProgressPercent = 0;
                StateHasChanged();

                try
                {
                    await UploadPhotoWithProgressAsync(createdPlayer.PlayerId);
                }
                catch (Exception ex)
                {
                    // Player was created, but photo upload failed
                    // Show warning but don't fail the whole operation
                    SuccessMessage = $"Player '{createdPlayer.FullName}' was created, but photo upload failed: {ex.Message}. You can add a photo later.";
                    await Task.Delay(2000, CancellationToken); // Brief pause to show message
                    navigationManager.NavigateTo(CancelUrl);
                    return;
                }
                finally
                {
                    IsUploadingPhoto = false;
                }
            }

            // Success - navigate back
            navigationManager.NavigateTo(CancelUrl);
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    private async Task UploadPhotoWithProgressAsync(long playerId)
    {
        if (SelectedPhoto is null)
        {
            return;
        }

        // Read file into memory with progress reporting
        var totalBytes = SelectedPhoto.Size;
        var bytesRead = 0L;
        var buffer = new byte[81920]; // 80 KB buffer
        int read;

        using var memoryStream = new MemoryStream();
        await using var fileStream = SelectedPhoto.OpenReadStream(MaxFileSize, CancellationToken);

        while ((read = await fileStream.ReadAsync(buffer.AsMemory(), CancellationToken)) > 0)
        {
            await memoryStream.WriteAsync(buffer.AsMemory(0, read), CancellationToken);
            bytesRead += read;

            // Update progress (reading phase = 0-50%)
            UploadProgressPercent = (double)bytesRead / totalBytes * 50;
            StateHasChanged();
        }

        memoryStream.Position = 0;

        // Upload phase (50-100%)
        UploadProgressPercent = 50;
        StateHasChanged();

        var uploadResult = await playersService.UploadPlayerPhotoAsync(
            ClubId,
            playerId,
            memoryStream,
            SelectedPhoto.ContentType,
            CancellationToken);

        if (uploadResult.IsProblem)
        {
            throw new InvalidOperationException(uploadResult.Problem.Detail ?? "Photo upload failed.");
        }

        UploadProgressPercent = 100;
        StateHasChanged();
    }

    private static string FormatFileSize(long bytes)
    {
        return bytes switch
        {
            < 1024 => $"{bytes} B",
            < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
            _ => $"{bytes / 1024.0 / 1024.0:F1} MB"
        };
    }

    private sealed class CreatePlayerInputModel
    {
        [Required(ErrorMessage = "First name is required.")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "First name must be between 1 and 100 characters.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Last name must be between 1 and 100 characters.")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Date of birth is required.")]
        public DateOnly DateOfBirth { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddYears(-10));

        [Required(ErrorMessage = "Graduation year is required.")]
        [GraduationYear]
        public int GraduationYear { get; set; } = DateTime.Today.Year + 10;

        public Gender? Gender { get; set; }

        [Range(0, 999, ErrorMessage = "Jersey number must be between 0 and 999.")]
        public int? JerseyNumber { get; set; }

        [Range(0, 9999, ErrorMessage = "Tryout number must be between 0 and 9999.")]
        public int? TryoutNumber { get; set; }
    }
}
