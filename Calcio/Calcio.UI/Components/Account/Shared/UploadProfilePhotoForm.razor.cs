using Calcio.Shared.DTOs.CalcioUsers;
using Calcio.Shared.Services.CalcioUsers;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Calcio.UI.Components.Account.Shared;

/// <summary>
/// Provides a one-time profile photo upload workflow, including crop-and-upload behavior.
/// </summary>
/// <param name="calcioUsersService">The service used to upload the account photo.</param>
/// <param name="navigationManager">The navigation manager used after successful upload.</param>
public partial class UploadProfilePhotoForm(
    ICalcioUsersService calcioUsersService,
    NavigationManager navigationManager)
{
    /// <summary>
    /// Defines the maximum accepted image file size in bytes.
    /// </summary>
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

    /// <summary>
    /// URL to navigate to after successful photo upload.
    /// </summary>
    [Parameter]
    public string? ReturnUrl { get; set; }

    /// <summary>
    /// Gets or sets the selected source image file before cropping.
    /// </summary>
    private IBrowserFile? SelectedPhoto { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether form submission is in progress.
    /// </summary>
    private bool IsSubmitting { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the photo upload operation is in progress.
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
    /// Gets or sets a value indicating whether the cropper modal is displayed.
    /// </summary>
    private bool ShowCropperModal { get; set; }

    /// <summary>
    /// Gets or sets the current error message shown to the user.
    /// </summary>
    private string? ErrorMessage { get; set; }

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
    /// Clears the currently selected photo and crop data.
    /// </summary>
    private void ClearPhoto()
    {
        SelectedPhoto = null;
        OriginalPhotoDataUrl = null;
        CroppedPhotoDataUrl = null;
    }

    /// <summary>
    /// Submits the cropped profile photo.
    /// </summary>
    /// <returns>A task that completes when submission handling finishes.</returns>
    private async Task HandleSubmit()
    {
        if (IsSubmitting || string.IsNullOrEmpty(CroppedPhotoDataUrl))
        {
            return;
        }

        IsSubmitting = true;
        IsUploadingPhoto = true;
        UploadProgressPercent = 0;
        ErrorMessage = null;
        StateHasChanged();

        try
        {
            await UploadCroppedPhotoAsync();

            // Success - navigate to return URL or home
            var targetUrl = !string.IsNullOrEmpty(ReturnUrl) ? ReturnUrl : "/";
            navigationManager.NavigateTo(targetUrl);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsSubmitting = false;
            IsUploadingPhoto = false;
        }
    }

    /// <summary>
    /// Uploads the cropped image data to the account photo endpoint.
    /// </summary>
    /// <returns>A task that resolves to the uploaded account photo details.</returns>
    private async Task<CalcioUserPhotoDto> UploadCroppedPhotoAsync()
    {
        if (string.IsNullOrEmpty(CroppedPhotoDataUrl))
        {
            throw new InvalidOperationException("No cropped photo is available.");
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

        var uploadResult = await calcioUsersService.UploadAccountPhotoAsync(
            memoryStream,
            "image/png", // Cropped images are always PNG
            CancellationToken);

        if (uploadResult.IsProblem)
        {
            throw new InvalidOperationException(uploadResult.Problem.Detail ?? "Photo upload failed.");
        }

        UploadProgressPercent = 100;
        StateHasChanged();
        return uploadResult.Value;
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
}
