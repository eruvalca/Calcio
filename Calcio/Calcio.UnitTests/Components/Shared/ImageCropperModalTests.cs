using Bunit;

using Calcio.UI.Components.Shared;

using Cropper.Blazor.Extensions;

using Microsoft.AspNetCore.Components;

using Shouldly;

namespace Calcio.UnitTests.Components.Shared;

/// <summary>
/// Unit tests for the ImageCropperModal Blazor component using bUnit.
/// 
/// This component handles:
/// - Displaying an image for cropping
/// - Zoom, rotate, and reset controls
/// - Apply and cancel actions
/// - Modal visibility state
/// </summary>
public sealed class ImageCropperModalTests : BunitContext
{
    private const string TestImageDataUrl = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";

    public ImageCropperModalTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        // Register Cropper.Blazor services
        Services.AddCropper();

        // Set RendererInfo for interactive components
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    #region Helper Methods

    private IRenderedComponent<ImageCropperModal> RenderModal(
        bool isVisible = true,
        string? imageSrc = TestImageDataUrl,
        Action<string>? onCropApplied = null,
        Action? onCancelled = null)
            => Render<ImageCropperModal>(parameters
                =>
                {
                    parameters.Add(p => p.IsVisible, isVisible);
                    parameters.Add(p => p.ImageSrc, imageSrc);
                    if (onCropApplied is not null)
                    {
                        parameters.Add(p => p.OnCropApplied, onCropApplied);
                    }

                    if (onCancelled is not null)
                    {
                        parameters.Add(p => p.OnCancelled, onCancelled);
                    }
                });

    #endregion

    #region Initial Rendering Tests

    [Fact]
    public void WhenVisibleWithImage_ShouldDisplayModal()
    {
        // Act
        var cut = RenderModal(isVisible: true);

        // Assert
        var modal = cut.Find(".modal");
        modal.ClassList.ShouldContain("show");
        modal.ClassList.ShouldContain("d-block");
    }

    [Fact]
    public void WhenNotVisible_ShouldHideModal()
    {
        // Act
        var cut = RenderModal(isVisible: false);

        // Assert
        var modal = cut.Find(".modal");
        modal.ClassList.ShouldNotContain("show");
        modal.ClassList.ShouldNotContain("d-block");
    }

    [Fact]
    public void WhenVisibleWithImage_ShouldDisplayCropperComponent()
    {
        // Act
        var cut = RenderModal(isVisible: true);

        // Assert - CropperComponent renders an img element with the source
        var img = cut.Find(".modal-body img");
        img.ShouldNotBeNull();
        img.GetAttribute("src").ShouldBe(TestImageDataUrl);
    }

    [Fact]
    public void WhenVisibleWithNoImage_ShouldNotDisplayCropperComponent()
    {
        // Act
        var cut = RenderModal(isVisible: true, imageSrc: null);

        // Assert - No img element should be rendered in the modal body
        cut.FindAll(".modal-body img").ShouldBeEmpty();
    }

    [Fact]
    public void WhenRendered_ShouldDisplayModalTitle()
    {
        // Act
        var cut = RenderModal();

        // Assert
        var title = cut.Find(".modal-title");
        title.TextContent.ShouldBe("Crop Image");
    }

    #endregion

    #region Control Button Tests

    [Fact]
    public void WhenVisibleWithImage_ShouldDisplayZoomButtons()
    {
        // Act
        var cut = RenderModal(isVisible: true);

        // Assert
        var zoomInButton = cut.Find("button[title='Zoom In']");
        var zoomOutButton = cut.Find("button[title='Zoom Out']");

        zoomInButton.ShouldNotBeNull();
        zoomOutButton.ShouldNotBeNull();
    }

    [Fact]
    public void WhenVisibleWithImage_ShouldDisplayRotateButtons()
    {
        // Act
        var cut = RenderModal(isVisible: true);

        // Assert
        var rotateLeftButton = cut.Find("button[title='Rotate Left']");
        var rotateRightButton = cut.Find("button[title='Rotate Right']");

        rotateLeftButton.ShouldNotBeNull();
        rotateRightButton.ShouldNotBeNull();
    }

    [Fact]
    public void WhenVisibleWithImage_ShouldDisplayResetButton()
    {
        // Act
        var cut = RenderModal(isVisible: true);

        // Assert
        var resetButton = cut.Find("button[title='Reset']");
        resetButton.ShouldNotBeNull();
    }

    [Fact]
    public void WhenVisibleWithImage_ShouldDisplayInstructionText()
    {
        // Act
        var cut = RenderModal(isVisible: true);

        // Assert
        var instructionText = cut.Find("p.text-muted");
        instructionText.TextContent.ShouldContain("Drag to move");
        instructionText.TextContent.ShouldContain("scroll to zoom");
    }

    #endregion

    #region Footer Button Tests

    [Fact]
    public void WhenRendered_ShouldDisplayCancelButton()
    {
        // Act
        var cut = RenderModal();

        // Assert
        var cancelButton = cut.Find(".modal-footer button.btn-outline-secondary");
        cancelButton.ShouldNotBeNull();
        cancelButton.TextContent.ShouldContain("Cancel");
    }

    [Fact]
    public void WhenRendered_ShouldDisplayApplyButton()
    {
        // Act
        var cut = RenderModal();

        // Assert
        var applyButton = cut.Find(".modal-footer button.btn-primary");
        applyButton.ShouldNotBeNull();
        applyButton.TextContent.ShouldContain("Apply");
    }

    [Fact]
    public void WhenRendered_ShouldDisplayCloseButton()
    {
        // Act
        var cut = RenderModal();

        // Assert
        var closeButton = cut.Find(".modal-header button.btn-close");
        closeButton.ShouldNotBeNull();
    }

    #endregion

    #region Cancel Action Tests

    [Fact]
    public async Task WhenCancelClicked_ShouldInvokeOnCancelled()
    {
        // Arrange
        var onCancelledCalled = false;

        var cut = RenderModal(onCancelled: () => onCancelledCalled = true);

        // Act
        await cut.Find(".modal-footer button.btn-outline-secondary").ClickAsync();

        // Assert
        onCancelledCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task WhenCloseButtonClicked_ShouldInvokeOnCancelled()
    {
        // Arrange
        var onCancelledCalled = false;

        var cut = RenderModal(onCancelled: () => onCancelledCalled = true);

        // Act
        await cut.Find(".modal-header button.btn-close").ClickAsync();

        // Assert
        onCancelledCalled.ShouldBeTrue();
    }

    #endregion

    #region Visibility State Tests

    [Fact]
    public void WhenIsVisibleTrue_ShouldHaveShowClass()
    {
        // Act
        var cut = RenderModal(isVisible: true);

        // Assert
        var modal = cut.Find(".modal");
        modal.ClassList.ShouldContain("show");
    }

    [Fact]
    public void WhenIsVisibleFalse_ShouldNotHaveShowClass()
    {
        // Act
        var cut = RenderModal(isVisible: false);

        // Assert
        var modal = cut.Find(".modal");
        modal.ClassList.ShouldNotContain("show");
    }

    #endregion

    #region Icon Tests

    [Fact]
    public void WhenRendered_ShouldDisplayCorrectIcons()
    {
        // Act
        var cut = RenderModal(isVisible: true);

        // Assert
        cut.Find("i.bi-zoom-in").ShouldNotBeNull();
        cut.Find("i.bi-zoom-out").ShouldNotBeNull();
        cut.Find("i.bi-arrow-counterclockwise").ShouldNotBeNull();
        cut.Find("i.bi-arrow-clockwise").ShouldNotBeNull();
        cut.Find("i.bi-arrow-repeat").ShouldNotBeNull();
        cut.Find("i.bi-info-circle").ShouldNotBeNull();
    }

    #endregion
}
