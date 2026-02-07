using Calcio.Shared.DTOs.CalcioUsers;
using Calcio.Shared.Services.CalcioUsers;
using Calcio.UI.Services.CalcioUsers;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Calcio.UI.Components.Account.Shared;

public partial class ProfilePhotoManager(
    ICalcioUsersService calcioUsersService,
    UserPhotoStateService userPhotoStateService,
    NavigationManager navigationManager)
{
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

    private bool IsLoading { get; set; } = true;

    private CalcioUserPhotoDto? CurrentPhoto { get; set; }

    private IBrowserFile? SelectedPhoto { get; set; }

    private bool IsSubmitting { get; set; }

    private bool IsUploadingPhoto { get; set; }

    private double UploadProgressPercent { get; set; }

    private string? OriginalPhotoDataUrl { get; set; }

    private string? CroppedPhotoDataUrl { get; set; }

    private bool ShowCropperModal { get; set; }

    private string? ErrorMessage { get; set; }

    protected override async Task OnInitializedAsync() => await LoadCurrentPhotoAsync();

    private async Task LoadCurrentPhotoAsync()
    {
        IsLoading = true;

        try
        {
            var result = await calcioUsersService.GetAccountPhotoAsync(CancellationToken);

            if (result.IsSuccess)
            {
                result.Value.Switch(
                    photo => CurrentPhoto = photo,
                    _ => CurrentPhoto = null);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

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
            var uploadedPhoto = await UploadCroppedPhotoAsync();
            userPhotoStateService.UpdateFromPhoto(uploadedPhoto);
            navigationManager.NavigateTo(navigationManager.Uri, forceLoad: false);
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
        CurrentPhoto = uploadResult.Value;
        return uploadResult.Value;
    }

    private static string FormatFileSize(long bytes)
        => bytes switch
        {
            < 1024 => $"{bytes} B",
            < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
            _ => $"{bytes / 1024.0 / 1024.0:F1} MB"
        };
}
