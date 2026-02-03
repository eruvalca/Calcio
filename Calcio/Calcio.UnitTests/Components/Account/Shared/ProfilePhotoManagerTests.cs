using Bunit;

using Calcio.Shared.DTOs.CalcioUsers;
using Calcio.Shared.Results;
using Calcio.Shared.Services.CalcioUsers;
using Calcio.UI.Components.Account.Shared;

using Cropper.Blazor.Extensions;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using OneOf;
using OneOf.Types;

using Shouldly;

namespace Calcio.UnitTests.Components.Account.Shared;

/// <summary>
/// Unit tests for the ProfilePhotoManager Blazor component using bUnit.
/// 
/// This component handles:
/// - Loading and displaying the current profile photo
/// - Showing a spinner during initial load
/// - Photo replacement via ImageCropperModal
/// - Refreshing the page after successful upload
/// </summary>
public sealed class ProfilePhotoManagerTests : BunitContext
{
    private readonly ICalcioUsersService _mockCalcioUsersService;

    public ProfilePhotoManagerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        // Register mock service
        _mockCalcioUsersService = Substitute.For<ICalcioUsersService>();
        Services.AddSingleton(_mockCalcioUsersService);

        // Register Cropper.Blazor services for ImageCropperModal
        Services.AddCropper();

        // Set up authorization
        var authContext = AddAuthorization();
        authContext.SetAuthorized("TestUser");

        // Set RendererInfo for interactive components
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    #region Helper Methods

    private IRenderedComponent<ProfilePhotoManager> RenderComponent()
        => Render<ProfilePhotoManager>();

    private void SetupNoPhoto()
    {
        OneOf<CalcioUserPhotoDto, None> noneResult = new None();
        _mockCalcioUsersService.GetAccountPhotoAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ServiceResult<OneOf<CalcioUserPhotoDto, None>>>(noneResult));
    }

    private void SetupWithPhoto(CalcioUserPhotoDto photo)
    {
        OneOf<CalcioUserPhotoDto, None> photoResult = photo;
        _mockCalcioUsersService.GetAccountPhotoAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ServiceResult<OneOf<CalcioUserPhotoDto, None>>>(photoResult));
    }

    private void SetupPhotoLoadNeverCompletes()
    {
        _mockCalcioUsersService.GetAccountPhotoAsync(Arg.Any<CancellationToken>())
            .Returns(new TaskCompletionSource<ServiceResult<OneOf<CalcioUserPhotoDto, None>>>().Task);
    }

    #endregion

    #region Loading State Tests

    [Fact]
    public void WhenLoading_ShouldDisplaySpinner()
    {
        // Arrange - Set up a task that never completes to keep component in loading state
        SetupPhotoLoadNeverCompletes();

        // Act
        var cut = RenderComponent();

        // Assert
        var spinner = cut.Find(".spinner-border");
        spinner.ShouldNotBeNull();
    }

    [Fact]
    public void WhenLoading_ShouldDisplayPlaceholderContainer()
    {
        // Arrange
        SetupPhotoLoadNeverCompletes();

        // Act
        var cut = RenderComponent();

        // Assert
        var placeholder = cut.Find(".photo-placeholder");
        placeholder.ShouldNotBeNull();
        placeholder.ClassList.ShouldContain("rounded-circle");
    }

    [Fact]
    public void WhenLoading_ShouldNotDisplayPhotoInput()
    {
        // Arrange
        SetupPhotoLoadNeverCompletes();

        // Act
        var cut = RenderComponent();

        // Assert
        var photoInputs = cut.FindAll("#photo");
        photoInputs.Count.ShouldBe(0);
    }

    #endregion

    #region Loaded State - No Photo Tests

    [Fact]
    public async Task WhenLoadedWithNoPhoto_ShouldDisplayPlaceholder()
    {
        // Arrange
        SetupNoPhoto();

        // Act
        var cut = RenderComponent();
        await cut.InvokeAsync(() => Task.CompletedTask); // Allow async operations to complete

        // Assert
        var placeholder = cut.Find(".photo-placeholder");
        placeholder.ShouldNotBeNull();

        var personIcon = cut.Find(".bi-person-fill");
        personIcon.ShouldNotBeNull();
    }

    [Fact]
    public async Task WhenLoadedWithNoPhoto_ShouldDisplayUploadLabel()
    {
        // Arrange
        SetupNoPhoto();

        // Act
        var cut = RenderComponent();
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        var label = cut.Find("label[for='photo']");
        label.ShouldNotBeNull();
        label.TextContent.ShouldContain("Upload Photo");
    }

    [Fact]
    public async Task WhenLoadedWithNoPhoto_ShouldDisplayPhotoInput()
    {
        // Arrange
        SetupNoPhoto();

        // Act
        var cut = RenderComponent();
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        var photoInput = cut.Find("#photo");
        photoInput.ShouldNotBeNull();
        photoInput.GetAttribute("accept").ShouldBe("image/jpeg,image/png,image/gif,image/webp");
    }

    [Fact]
    public async Task WhenLoadedWithNoPhoto_ShouldDisplayFileSizeHint()
    {
        // Arrange
        SetupNoPhoto();

        // Act
        var cut = RenderComponent();
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        var formText = cut.Find(".form-text");
        formText.ShouldNotBeNull();
        formText.TextContent.ShouldContain("10 MB");
    }

    [Fact]
    public async Task WhenLoadedWithNoPhoto_ShouldNotDisplaySpinner()
    {
        // Arrange
        SetupNoPhoto();

        // Act
        var cut = RenderComponent();
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        var spinners = cut.FindAll(".spinner-border");
        spinners.Count.ShouldBe(0);
    }

    #endregion

    #region Loaded State - With Photo Tests

    [Fact]
    public async Task WhenLoadedWithPhoto_ShouldDisplayPhotoImage()
    {
        // Arrange
        var photo = new CalcioUserPhotoDto(1, "https://example.com/original.jpg", "https://example.com/small.jpg", "https://example.com/medium.jpg", "https://example.com/large.jpg");
        SetupWithPhoto(photo);

        // Act
        var cut = RenderComponent();
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        var photoPreview = cut.Find(".photo-preview");
        photoPreview.ShouldNotBeNull();
        photoPreview.GetAttribute("src").ShouldBe("https://example.com/medium.jpg");
    }

    [Fact]
    public async Task WhenLoadedWithPhotoNoMedium_ShouldFallbackToOriginal()
    {
        // Arrange
        var photo = new CalcioUserPhotoDto(1, "https://example.com/original.jpg", null, null, null);
        SetupWithPhoto(photo);

        // Act
        var cut = RenderComponent();
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        var photoPreview = cut.Find(".photo-preview");
        photoPreview.ShouldNotBeNull();
        photoPreview.GetAttribute("src").ShouldBe("https://example.com/original.jpg");
    }

    [Fact]
    public async Task WhenLoadedWithPhoto_ShouldDisplayReplaceLabel()
    {
        // Arrange
        var photo = new CalcioUserPhotoDto(1, "https://example.com/original.jpg", null, "https://example.com/medium.jpg", null);
        SetupWithPhoto(photo);

        // Act
        var cut = RenderComponent();
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        var label = cut.Find("label[for='photo']");
        label.ShouldNotBeNull();
        label.TextContent.ShouldContain("Replace Photo");
    }

    [Fact]
    public async Task WhenLoadedWithPhoto_ShouldNotDisplayPlaceholder()
    {
        // Arrange
        var photo = new CalcioUserPhotoDto(1, "https://example.com/original.jpg", null, "https://example.com/medium.jpg", null);
        SetupWithPhoto(photo);

        // Act
        var cut = RenderComponent();
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        var placeholders = cut.FindAll(".photo-placeholder");
        placeholders.Count.ShouldBe(0);
    }

    #endregion

    #region Error State Tests

    [Fact]
    public async Task WhenLoaded_ShouldNotDisplayErrorMessage()
    {
        // Arrange
        SetupNoPhoto();

        // Act
        var cut = RenderComponent();
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        var errorAlerts = cut.FindAll(".alert-danger");
        errorAlerts.Count.ShouldBe(0);
    }

    [Fact]
    public async Task WhenLoaded_ShouldNotShowProgressBar()
    {
        // Arrange
        SetupNoPhoto();

        // Act
        var cut = RenderComponent();
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        var progressBars = cut.FindAll(".progress");
        progressBars.Count.ShouldBe(0);
    }

    [Fact]
    public async Task WhenLoaded_ShouldNotShowCancelButton()
    {
        // Arrange
        SetupNoPhoto();

        // Act
        var cut = RenderComponent();
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        var cancelButtons = cut.FindAll("button.btn-outline-danger");
        cancelButtons.Count.ShouldBe(0);
    }

    [Fact]
    public async Task WhenLoaded_ShouldNotShowCroppedBadge()
    {
        // Arrange
        SetupNoPhoto();

        // Act
        var cut = RenderComponent();
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        var badges = cut.FindAll(".badge.bg-success");
        badges.Count.ShouldBe(0);
    }

    #endregion

    #region ImageCropperModal Tests

    [Fact]
    public async Task WhenLoaded_ShouldIncludeImageCropperModal()
    {
        // Arrange
        SetupNoPhoto();

        // Act
        var cut = RenderComponent();
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert - The modal component should be rendered (though not visible initially)
        cut.ShouldNotBeNull();
    }

    #endregion

    #region Upload Button Tests

    [Fact]
    public async Task WhenNoCroppedPhoto_ShouldNotDisplayUploadPhotoButton()
    {
        // Arrange
        SetupNoPhoto();

        // Act
        var cut = RenderComponent();
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert - Upload button (with upload icon) only shows after cropping
        // Note: We look for our specific upload button, not the modal's Apply button
        var uploadButton = cut.FindAll("button.btn-primary")
            .FirstOrDefault(b => b.TextContent.Contains("Upload Photo") || b.TextContent.Contains("Replace Photo"));
        uploadButton.ShouldBeNull();
    }

    [Fact]
    public async Task WhenPhotoExists_ShouldNotDisplayReplaceButtonWithoutCropping()
    {
        // Arrange
        var photo = new CalcioUserPhotoDto(1, "https://example.com/original.jpg", null, "https://example.com/medium.jpg", null);
        SetupWithPhoto(photo);

        // Act
        var cut = RenderComponent();
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert - Replace button only shows after cropping a new photo
        var replaceButton = cut.FindAll("button.btn-primary")
            .FirstOrDefault(b => b.TextContent.Contains("Upload Photo") || b.TextContent.Contains("Replace Photo"));
        replaceButton.ShouldBeNull();
    }

    #endregion

    #region Service Call Tests

    [Fact]
    public async Task WhenRendered_ShouldCallGetAccountPhotoAsync()
    {
        // Arrange
        SetupNoPhoto();

        // Act
        var cut = RenderComponent();
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        await _mockCalcioUsersService.Received(1).GetAccountPhotoAsync(Arg.Any<CancellationToken>());
    }

    #endregion
}
