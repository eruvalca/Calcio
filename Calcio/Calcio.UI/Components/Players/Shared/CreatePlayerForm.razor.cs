using System.ComponentModel.DataAnnotations;

using Calcio.Shared.DTOs.Players;
using Calcio.Shared.Enums;
using Calcio.Shared.Results;
using Calcio.Shared.Services.Players;
using Calcio.Shared.Validation;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Calcio.UI.Components.Players.Shared;

/// <summary>
/// Provides player creation with optional client-side image cropping and photo upload.
/// </summary>
/// <param name="playersService">The service used to create players and upload player photos.</param>
/// <param name="navigationManager">The navigation manager used after submission.</param>
public partial class CreatePlayerForm(
    IPlayersService playersService,
    NavigationManager navigationManager)
{
    /// <summary>
    /// Defines the maximum accepted image file size in bytes.
    /// </summary>
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

    /// <summary>
    /// Gets or sets the club identifier where the player will be created.
    /// </summary>
    [Parameter]
    public required long ClubId { get; set; }

    /// <summary>
    /// Gets or sets the URL to navigate to when creation completes or is canceled.
    /// </summary>
    [Parameter]
    public string CancelUrl { get; set; } = "/account/manage/clubs";

    /// <summary>
    /// Gets or sets the create-player input model.
    /// </summary>
    private CreatePlayerInputModel Input { get; set; } = new();

    /// <summary>
    /// Gets or sets the selected source photo file before cropping.
    /// </summary>
    private IBrowserFile? SelectedPhoto { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether player submission is in progress.
    /// </summary>
    private bool IsSubmitting { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether photo upload is in progress.
    /// </summary>
    private bool IsUploadingPhoto { get; set; }

    /// <summary>
    /// Gets or sets upload progress percentage displayed in the UI.
    /// </summary>
    private double UploadProgressPercent { get; set; }

    /// <summary>
    /// Gets or sets the data URL for the original selected image.
    /// </summary>
    private string? OriginalPhotoDataUrl { get; set; }

    /// <summary>
    /// Gets or sets the data URL for the cropped image.
    /// </summary>
    private string? CroppedPhotoDataUrl { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the cropper modal is visible.
    /// </summary>
    private bool ShowCropperModal { get; set; }

    /// <summary>
    /// Gets or sets the current error message shown to the user.
    /// </summary>
    private string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the current success message shown to the user.
    /// </summary>
    private string? SuccessMessage { get; set; }

    /// <summary>
    /// Gets the current calendar year.
    /// </summary>
    private static int CurrentYear => DateTime.Today.Year;

    /// <summary>
    /// Gets the maximum graduation year accepted by the form.
    /// </summary>
    private static int MaxYear => DateTime.Today.Year + 25;

    /// <summary>
    /// Validates and reads a selected image file for cropping.
    /// </summary>
    /// <param name="e">File selection event data.</param>
    /// <returns>A task that completes when file selection is processed.</returns>
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

    /// <summary>
    /// Stores the cropped image data and closes the cropper modal.
    /// </summary>
    /// <param name="croppedDataUrl">The cropped image data URL.</param>
    private void OnCropApplied(string croppedDataUrl)
    {
        CroppedPhotoDataUrl = croppedDataUrl;
        ShowCropperModal = false;
    }

    /// <summary>
    /// Clears selected image state when cropping is canceled.
    /// </summary>
    private void OnCropCancelled()
    {
        // User cancelled cropping, clear the selection
        SelectedPhoto = null;
        OriginalPhotoDataUrl = null;
        CroppedPhotoDataUrl = null;
        ShowCropperModal = false;
    }

    /// <summary>
    /// Clears the selected photo and crop data.
    /// </summary>
    private void ClearPhoto()
    {
        SelectedPhoto = null;
        OriginalPhotoDataUrl = null;
        CroppedPhotoDataUrl = null;
    }

    /// <summary>
    /// Creates a player and uploads a cropped photo when one is available.
    /// </summary>
    /// <returns>A task that completes when submission handling is finished.</returns>
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

    /// <summary>
    /// Uploads the cropped image to the created player's photo endpoint.
    /// </summary>
    /// <param name="playerId">The player identifier receiving the uploaded photo.</param>
    /// <returns>A task that completes when upload processing finishes.</returns>
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

    /// <summary>
    /// Formats a byte count using human-readable file size units.
    /// </summary>
    /// <param name="bytes">The byte count to format.</param>
    /// <returns>A formatted file size string.</returns>
    private static string FormatFileSize(long bytes)
        => bytes switch
        {
            < 1024 => $"{bytes} B",
            < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
            _ => $"{bytes / 1024.0 / 1024.0:F1} MB"
        };

    /// <summary>
    /// Represents form input values used to create a player.
    /// </summary>
    private sealed class CreatePlayerInputModel
    {
        /// <summary>
        /// Gets or sets the player's first name.
        /// </summary>
        [Required(ErrorMessage = "First name is required.")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "First name must be between 1 and 100 characters.")]
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the player's last name.
        /// </summary>
        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Last name must be between 1 and 100 characters.")]
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the player's date of birth.
        /// </summary>
        [Required(ErrorMessage = "Date of birth is required.")]
        public DateOnly DateOfBirth { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddYears(-10));

        /// <summary>
        /// Gets or sets the player's graduation year.
        /// </summary>
        [Required(ErrorMessage = "Graduation year is required.")]
        [GraduationYear]
        public int GraduationYear { get; set; } = DateTime.Today.Year + 10;

        /// <summary>
        /// Gets or sets the player's gender.
        /// </summary>
        public Gender? Gender { get; set; }

        /// <summary>
        /// Gets or sets the player's jersey number.
        /// </summary>
        [Range(0, 999, ErrorMessage = "Jersey number must be between 0 and 999.")]
        public int? JerseyNumber { get; set; }

        /// <summary>
        /// Gets or sets the player's tryout number.
        /// </summary>
        [Range(0, 9999, ErrorMessage = "Tryout number must be between 0 and 9999.")]
        public int? TryoutNumber { get; set; }
    }
}
