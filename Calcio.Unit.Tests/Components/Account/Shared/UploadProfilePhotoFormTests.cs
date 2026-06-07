using Bunit;

using Calcio.Shared.DTOs.CalcioUsers;
using Calcio.Shared.Results;
using Calcio.Shared.Services.CalcioUsers;
using Calcio.UI.Components.Account.Shared;

using Cropper.Blazor.Extensions;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using Shouldly;

namespace Calcio.UnitTests.Components.Account.Shared;

/// <summary>
/// Unit tests for the UploadProfilePhotoForm Blazor component using bUnit.
/// 
/// This component handles:
/// - Photo selection with file size validation
/// - Image cropping via ImageCropperModal
/// - Photo upload to server
/// - Navigation after successful upload
/// </summary>
public sealed class UploadProfilePhotoFormTests : BunitContext
{
    private readonly ICalcioUsersService _mockCalcioUsersService;

    public UploadProfilePhotoFormTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        // Register mock service
        _mockCalcioUsersService = Substitute.For<ICalcioUsersService>();
        Services.AddSingleton(_mockCalcioUsersService);
        Services.AddSingleton(TimeProvider.System);
        Services.AddLogging();

        // Register Cropper.Blazor services for ImageCropperModal
        Services.AddCropper();

        // Set up authorization
        var authContext = AddAuthorization();
        authContext.SetAuthorized("TestUser");

        // Set RendererInfo for interactive components
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    #region Helper Methods

    private IRenderedComponent<UploadProfilePhotoForm> RenderForm(string? returnUrl = null)
        => Render<UploadProfilePhotoForm>(parameters => parameters
            .Add(p => p.ReturnUrl, returnUrl));

    #endregion

    #region Initial Rendering Tests

    [Fact]
    public void WhenRendered_ShouldDisplayPhotoPlaceholder()
    {
        // Act
        var cut = RenderForm();

        // Assert
        var placeholder = cut.Find(".photo-placeholder");
        placeholder.ShouldNotBeNull();
        placeholder.ClassList.ShouldContain("rounded-circle");
    }

    [Fact]
    public void WhenRendered_ShouldDisplayPhotoInputField()
    {
        // Act
        var cut = RenderForm();

        // Assert
        var photoInput = cut.Find("#photo");
        photoInput.ShouldNotBeNull();
        photoInput.GetAttribute("accept").ShouldBe("image/jpeg,image/png,image/gif,image/webp");
    }

    [Fact]
    public void WhenRendered_ShouldDisplayFileSizeHint()
    {
        // Act
        var cut = RenderForm();

        // Assert
        var formText = cut.Find(".form-text");
        formText.ShouldNotBeNull();
        formText.TextContent.ShouldContain("10 MB");
    }

    [Fact]
    public void WhenRendered_ShouldDisplayRequiredIndicator()
    {
        // Act
        var cut = RenderForm();

        // Assert
        var requiredMarker = cut.Find("span.text-danger");
        requiredMarker.ShouldNotBeNull();
        requiredMarker.TextContent.ShouldBe("*");
    }

    [Fact]
    public void WhenRendered_ShouldDisplayDisabledUploadButton()
    {
        // Act
        var cut = RenderForm();

        // Assert
        var uploadButton = cut.Find("button.btn-primary");
        uploadButton.ShouldNotBeNull();
        uploadButton.GetAttribute("disabled").ShouldNotBeNull();
        uploadButton.TextContent.ShouldContain("Upload Photo");
    }

    [Fact]
    public void WhenRendered_ShouldNotDisplayErrorMessage()
    {
        // Act
        var cut = RenderForm();

        // Assert
        var errorAlerts = cut.FindAll(".alert-danger");
        errorAlerts.Count.ShouldBe(0);
    }

    [Fact]
    public void WhenRendered_ShouldNotDisplayPhotoPreview()
    {
        // Act
        var cut = RenderForm();

        // Assert
        var previews = cut.FindAll(".photo-preview");
        previews.Count.ShouldBe(0);
    }

    #endregion

    #region Upload Button State Tests

    [Fact]
    public async Task WhenRendered_UploadServiceShouldNotBeCalledOnInitialization()
    {
        // Arrange
        var returnUrl = "/dashboard";
        _mockCalcioUsersService.UploadAccountPhotoAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ServiceResult<CalcioUserPhotoDto>>(new CalcioUserPhotoDto(1, "url", null, null, null)));

        var cut = RenderForm(returnUrl);
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert - upload service must not be called before the user selects and crops a photo.
        // End-to-end navigation after successful upload is covered by AccountPhotoUploadIntegrationTests.
        await _mockCalcioUsersService.DidNotReceive().UploadAccountPhotoAsync(
            Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void WhenUploadFails_ErrorMessageShouldNotBeShownBeforeUploadAttempt()
    {
        // Arrange
        _mockCalcioUsersService.UploadAccountPhotoAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ServiceResult<CalcioUserPhotoDto>>(ServiceProblem.ServerError("Upload failed")));

        var cut = RenderForm();

        // Assert - no error shown before the user has attempted an upload.
        // The upload flow (InputFile → crop → submit) cannot be driven in bUnit because
        // InputFile requires a real browser file picker. Upload error handling at the
        // service level is covered by AccountPhotoUploadIntegrationTests.
        cut.FindAll(".alert-danger").Count.ShouldBe(0);
        cut.Find("button.btn-primary").GetAttribute("disabled").ShouldNotBeNull();
    }

    #endregion

    #region Error State Tests

    [Fact]
    public void WhenRendered_ShouldNotShowProgressBar()
    {
        // Act
        var cut = RenderForm();

        // Assert
        var progressBars = cut.FindAll(".progress");
        progressBars.Count.ShouldBe(0);
    }

    [Fact]
    public void WhenRendered_ShouldNotShowRemoveButton()
    {
        // Act
        var cut = RenderForm();

        // Assert
        var removeButtons = cut.FindAll("button.btn-outline-danger");
        removeButtons.Count.ShouldBe(0);
    }

    [Fact]
    public void WhenRendered_ShouldNotShowCroppedBadge()
    {
        // Act
        var cut = RenderForm();

        // Assert
        var badges = cut.FindAll(".badge.bg-success");
        badges.Count.ShouldBe(0);
    }

    #endregion

    #region ImageCropperModal Tests

    [Fact]
    public void WhenRendered_ShouldIncludeImageCropperModal()
    {
        // Act
        var cut = RenderForm();

        // Assert - The modal is rendered in the DOM but not shown until a photo is selected
        var modal = cut.Find(".modal");
        modal.ShouldNotBeNull();
        modal.ClassList.ShouldNotContain("show");
    }

    #endregion

    #region Parameter Tests

    [Fact]
    public void WhenReturnUrlProvided_ShouldAcceptParameter()
    {
        // Arrange
        var returnUrl = "/custom/return/path";

        // Act
        var cut = RenderForm(returnUrl);

        // Assert - Component renders with the parameter; file input and disabled upload button present
        cut.Find("#photo").ShouldNotBeNull();
        cut.Find("button.btn-primary").GetAttribute("disabled").ShouldNotBeNull();
    }

    [Fact]
    public void WhenNoReturnUrl_ShouldRenderSuccessfully()
    {
        // Act
        var cut = RenderForm(returnUrl: null);

        // Assert
        cut.Find("#photo").ShouldNotBeNull();
        cut.Find("button.btn-primary").GetAttribute("disabled").ShouldNotBeNull();
    }

    #endregion
}
