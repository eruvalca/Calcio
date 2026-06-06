using Cropper.Blazor.Components;
using Cropper.Blazor.Extensions;
using Cropper.Blazor.Models;

using Microsoft.AspNetCore.Components;

namespace Calcio.UI.Components.Shared;

/// <summary>
/// Provides an image cropping modal that emits a square PNG data URL when cropping is applied.
/// </summary>
public partial class ImageCropperModal
{
    /// <summary>
    /// Holds a reference to the cropper component instance.
    /// </summary>
    private CropperComponent? CropperComponentRef { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the modal is visible.
    /// </summary>
    [Parameter]
    public bool IsVisible { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when the modal visibility changes.
    /// </summary>
    [Parameter]
    public EventCallback<bool> IsVisibleChanged { get; set; }

    /// <summary>
    /// Gets or sets the source image URL to crop.
    /// </summary>
    [Parameter]
    public string? ImageSrc { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when a cropped image is produced.
    /// </summary>
    [Parameter]
    public EventCallback<string> OnCropApplied { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when cropping is canceled.
    /// </summary>
    [Parameter]
    public EventCallback OnCancelled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether crop processing is in progress.
    /// </summary>
    private bool IsProcessing { get; set; }

    /// <summary>
    /// Gets cropper configuration options for square avatar-style crops.
    /// </summary>
    private Options CropperOptions { get; } = new()
    {
        AspectRatio = 1m, // 1:1 for circular crop
        ViewMode = ViewMode.Vm1, // Restrict crop box to canvas
        DragMode = "move",
        AutoCropArea = 0.8m,
        Restore = false,
        Guides = false,
        Center = true,
        Highlight = false,
        CropBoxMovable = true,
        CropBoxResizable = true,
        ToggleDragModeOnDblclick = false
    };

    /// <summary>
    /// Increases the cropper zoom level.
    /// </summary>
    private void ZoomIn() => CropperComponentRef?.Zoom(0.1m);

    /// <summary>
    /// Decreases the cropper zoom level.
    /// </summary>
    private void ZoomOut() => CropperComponentRef?.Zoom(-0.1m);

    /// <summary>
    /// Rotates the image counterclockwise.
    /// </summary>
    private void RotateLeft() => CropperComponentRef?.Rotate(-90m);

    /// <summary>
    /// Rotates the image clockwise.
    /// </summary>
    private void RotateRight() => CropperComponentRef?.Rotate(90m);

    /// <summary>
    /// Resets the cropper transform state.
    /// </summary>
    private void Reset() => CropperComponentRef?.Reset();

    /// <summary>
    /// Produces a cropped PNG image and returns it through <see cref="OnCropApplied"/>.
    /// </summary>
    /// <returns>A task that completes when crop output is emitted.</returns>
    private async Task ApplyCrop()
    {
        if (CropperComponentRef is null)
        {
            return;
        }

        IsProcessing = true;

        try
        {
            var getCroppedCanvasOptions = new GetCroppedCanvasOptions
            {
                MaxWidth = 512,
                MaxHeight = 512,
                ImageSmoothingQuality = ImageSmoothingQuality.High.ToEnumString()
            };

            var imageReceiver = await CropperComponentRef.GetCroppedCanvasDataInBackgroundAsync(
                getCroppedCanvasOptions,
                "image/png",
                1,
                null,
                CancellationToken);

            using var croppedCanvasDataStream = await imageReceiver.GetImageChunkStreamAsync(CancellationToken);
            var croppedCanvasData = croppedCanvasDataStream.ToArray();
            var croppedImageDataUrl = "data:image/png;base64," + Convert.ToBase64String(croppedCanvasData);

            await OnCropApplied.InvokeAsync(croppedImageDataUrl);
            await CloseModal();
        }
        finally
        {
            IsProcessing = false;
        }
    }

    /// <summary>
    /// Cancels cropping and closes the modal.
    /// </summary>
    /// <returns>A task that completes when cancellation is handled.</returns>
    private async Task Cancel()
    {
        await OnCancelled.InvokeAsync();
        await CloseModal();
    }

    /// <summary>
    /// Closes the modal and notifies two-way binding consumers.
    /// </summary>
    /// <returns>A task that completes when visibility change is emitted.</returns>
    private async Task CloseModal()
    {
        IsVisible = false;
        await IsVisibleChanged.InvokeAsync(false);
    }
}
