using Cropper.Blazor.Components;
using Cropper.Blazor.Extensions;
using Cropper.Blazor.Models;

using Microsoft.AspNetCore.Components;

namespace Calcio.UI.Components.Shared;

public partial class ImageCropperModal
{
    private CropperComponent? CropperComponentRef { get; set; }

    [Parameter]
    public bool IsVisible { get; set; }

    [Parameter]
    public EventCallback<bool> IsVisibleChanged { get; set; }

    [Parameter]
    public string? ImageSrc { get; set; }

    [Parameter]
    public EventCallback<string> OnCropApplied { get; set; }

    [Parameter]
    public EventCallback OnCancelled { get; set; }

    private bool IsProcessing { get; set; }

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

    private void ZoomIn() => CropperComponentRef?.Zoom(0.1m);

    private void ZoomOut() => CropperComponentRef?.Zoom(-0.1m);

    private void RotateLeft() => CropperComponentRef?.Rotate(-90m);

    private void RotateRight() => CropperComponentRef?.Rotate(90m);

    private void Reset() => CropperComponentRef?.Reset();

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

    private async Task Cancel()
    {
        await OnCancelled.InvokeAsync();
        await CloseModal();
    }

    private async Task CloseModal()
    {
        IsVisible = false;
        await IsVisibleChanged.InvokeAsync(false);
    }
}
