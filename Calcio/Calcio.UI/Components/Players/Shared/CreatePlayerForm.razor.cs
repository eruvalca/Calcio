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

    private string? OriginalPhotoDataUrl { get; set; }

    private string? CroppedPhotoDataUrl { get; set; }

    private bool ShowCropperModal { get; set; }

    private string? ErrorMessage { get; set; }

    private string? SuccessMessage { get; set; }

    private static int CurrentYear => DateTime.Today.Year;

    private static int MaxYear => DateTime.Today.Year + 25;

    private async Task OnPhotoSelected(InputFileChangeEventArgs e)
    {
        ErrorMessage = null;
        OriginalPhotoDataUrl = null;
        CroppedPhotoDataUrl = null;

        var file = e.File;

        if (file.Size > MaxFileSize)
        {
            ErrorMessage = $"File size ({FormatFileSize(file.Size)}) exceeds maximum of 10 MB.";
            SelectedPhoto = null;
            return;
        }

        SelectedPhoto = file;

        // Generate data URL for cropper
        try
        {
            await using var stream = file.OpenReadStream(MaxFileSize, CancellationToken);
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream, CancellationToken);
            var base64 = Convert.ToBase64String(memoryStream.ToArray());
            OriginalPhotoDataUrl = $"data:{file.ContentType};base64,{base64}";

            // Show cropper modal
            ShowCropperModal = true;
        }
        catch (Exception)
        {
            ErrorMessage = "Failed to read the selected image. Please try again.";
            SelectedPhoto = null;
        }
    }

    private void OnCropApplied(string croppedDataUrl)
    {
        CroppedPhotoDataUrl = croppedDataUrl;
        ShowCropperModal = false;
    }

    private void OnCropCancelled()
    {
        // User cancelled cropping, clear the selection
        SelectedPhoto = null;
        OriginalPhotoDataUrl = null;
        CroppedPhotoDataUrl = null;
        ShowCropperModal = false;
    }

    private void ClearPhoto()
    {
        SelectedPhoto = null;
        OriginalPhotoDataUrl = null;
        CroppedPhotoDataUrl = null;
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

            // Step 2: Upload photo if cropped image is available
            if (!string.IsNullOrEmpty(CroppedPhotoDataUrl))
            {
                IsUploadingPhoto = true;
                UploadProgressPercent = 0;
                StateHasChanged();

                try
                {
                    await UploadCroppedPhotoAsync(createdPlayer.PlayerId);
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

    private async Task UploadCroppedPhotoAsync(long playerId)
    {
        if (string.IsNullOrEmpty(CroppedPhotoDataUrl))
        {
            return;
        }

        // Parse the data URL to extract base64 data
        // Format: data:image/png;base64,<base64data>
        var commaIndex = CroppedPhotoDataUrl.IndexOf(',');
        if (commaIndex < 0)
        {
            throw new InvalidOperationException("Invalid cropped image data.");
        }

        var base64Data = CroppedPhotoDataUrl[(commaIndex + 1)..];
        var imageBytes = Convert.FromBase64String(base64Data);

        UploadProgressPercent = 50;
        StateHasChanged();

        using var memoryStream = new MemoryStream(imageBytes);

        var uploadResult = await playersService.UploadPlayerPhotoAsync(
            ClubId,
            playerId,
            memoryStream,
            "image/png", // Cropped images are always PNG
            CancellationToken);

        if (uploadResult.IsProblem)
        {
            throw new InvalidOperationException(uploadResult.Problem.Detail ?? "Photo upload failed.");
        }

        UploadProgressPercent = 100;
        StateHasChanged();
    }

    private static string FormatFileSize(long bytes)
        => bytes switch
        {
            < 1024 => $"{bytes} B",
            < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
            _ => $"{bytes / 1024.0 / 1024.0:F1} MB"
        };

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
