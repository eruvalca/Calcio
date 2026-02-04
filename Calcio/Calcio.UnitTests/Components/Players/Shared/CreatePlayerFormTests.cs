using Bunit;
using Bunit.TestDoubles;

using Calcio.Shared.DTOs.Players;
using Calcio.Shared.Results;
using Calcio.Shared.Security;
using Calcio.Shared.Services.Players;
using Calcio.UI.Components.Players.Shared;

using Cropper.Blazor.Extensions;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using Shouldly;

namespace Calcio.UnitTests.Components.Players.Shared;

/// <summary>
/// Unit tests for the CreatePlayerForm Blazor component using bUnit.
/// 
/// This component handles:
/// - Form submission for creating new players
/// - Optional photo upload with cropping
/// - Validation of player data
/// - Error and success message display
/// </summary>
public sealed class CreatePlayerFormTests : BunitContext
{
    private readonly IPlayersService _mockPlayersService;
    private readonly BunitAuthorizationContext _authContext;

    public CreatePlayerFormTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        // Register mock service BEFORE authorization is set up (which triggers service resolution)
        _mockPlayersService = Substitute.For<IPlayersService>();
        Services.AddSingleton(_mockPlayersService);

        // Register Cropper.Blazor services for ImageCropperModal
        Services.AddCropper();

        // Set up authorization after services are registered
        _authContext = AddAuthorization();
        _authContext.SetAuthorized("TestUser");
        _authContext.SetRoles(Roles.ClubAdmin);

        // Set RendererInfo for interactive components
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    #region Helper Methods

    private IRenderedComponent<CreatePlayerForm> RenderForm(long clubId = 1, string cancelUrl = "/account/manage/clubs")
        => Render<CreatePlayerForm>(parameters => parameters
            .Add(p => p.ClubId, clubId)
            .Add(p => p.CancelUrl, cancelUrl));

    private static void FillValidPlayerData(IRenderedComponent<CreatePlayerForm> cut)
    {
        cut.Find("#firstName").Change("John");
        cut.Find("#lastName").Change("Doe");
        cut.Find("#dateOfBirth").Change(DateOnly.FromDateTime(DateTime.Today.AddYears(-15)).ToString("yyyy-MM-dd"));
        cut.Find("#graduationYear").Change((DateTime.Today.Year + 3).ToString());
    }

    #endregion

    #region Initial Rendering Tests

    [Fact]
    public void WhenRendered_ShouldDisplayFormFields()
    {
        // Act
        var cut = RenderForm();

        // Assert
        cut.Find("#firstName").ShouldNotBeNull();
        cut.Find("#lastName").ShouldNotBeNull();
        cut.Find("#dateOfBirth").ShouldNotBeNull();
        cut.Find("#graduationYear").ShouldNotBeNull();
        cut.Find("#gender").ShouldNotBeNull();
        cut.Find("#jerseyNumber").ShouldNotBeNull();
        cut.Find("#tryoutNumber").ShouldNotBeNull();
    }

    [Fact]
    public void WhenRendered_ShouldDisplayRequiredFieldIndicators()
    {
        // Act
        var cut = RenderForm();

        // Assert
        var requiredMarkers = cut.FindAll("span.text-danger");
        requiredMarkers.Count.ShouldBeGreaterThanOrEqualTo(4); // First name, last name, DOB, grad year
    }

    [Fact]
    public void WhenRendered_ShouldDisplayPhotoUploadField()
    {
        // Act
        var cut = RenderForm();

        // Assert
        var photoInput = cut.Find("#photo");
        photoInput.ShouldNotBeNull();
        photoInput.GetAttribute("accept").ShouldBe("image/jpeg,image/png,image/gif,image/webp");
    }

    [Fact]
    public void WhenRendered_ShouldDisplaySubmitButton()
    {
        // Act
        var cut = RenderForm();

        // Assert
        var submitButton = cut.Find("button[type='submit']");
        submitButton.ShouldNotBeNull();
        submitButton.TextContent.ShouldContain("Create Player");
    }

    [Fact]
    public void WhenRendered_ShouldDisplayCancelLink()
    {
        // Arrange
        var cancelUrl = "/clubs/1/players";

        // Act
        var cut = RenderForm(cancelUrl: cancelUrl);

        // Assert
        var cancelLink = cut.Find("a.btn-outline-secondary");
        cancelLink.ShouldNotBeNull();
        cancelLink.GetAttribute("href").ShouldBe(cancelUrl);
    }

    #endregion

    #region Form Submission Tests

    [Fact]
    public async Task WhenValidDataSubmitted_ShouldCreatePlayer()
    {
        // Arrange
        var clubId = 1L;
        var expectedResponse = new PlayerCreatedDto(100, "John", "Doe", "John Doe");

        _mockPlayersService.CreatePlayerAsync(clubId, Arg.Any<CreatePlayerDto>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ServiceResult<PlayerCreatedDto>>(expectedResponse));

        var cut = RenderForm(clubId);
        FillValidPlayerData(cut);

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        await _mockPlayersService.Received(1).CreatePlayerAsync(
            clubId,
            Arg.Is<CreatePlayerDto>(dto =>
                dto.FirstName == "John" &&
                dto.LastName == "Doe"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WhenCreatedSuccessfully_ShouldNavigateToClubsPage()
    {
        // Arrange
        var clubId = 1L;
        var cancelUrl = "/account/manage/clubs";
        var expectedResponse = new PlayerCreatedDto(100, "John", "Doe", "John Doe");

        _mockPlayersService.CreatePlayerAsync(Arg.Any<long>(), Arg.Any<CreatePlayerDto>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ServiceResult<PlayerCreatedDto>>(expectedResponse));

        var cut = RenderForm(clubId, cancelUrl);
        FillValidPlayerData(cut);

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        var navManager = Services.GetRequiredService<NavigationManager>();
        navManager.Uri.ShouldContain(cancelUrl);
    }

    [Fact]
    public async Task WhenCreateFails_ShouldDisplayErrorMessage()
    {
        // Arrange
        var clubId = 1L;

        _mockPlayersService.CreatePlayerAsync(Arg.Any<long>(), Arg.Any<CreatePlayerDto>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ServiceResult<PlayerCreatedDto>>(ServiceProblem.ServerError("Creation failed")));

        var cut = RenderForm(clubId);
        FillValidPlayerData(cut);

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        cut.WaitForAssertion(() =>
        {
            var errorAlert = cut.Find(".alert-danger");
            errorAlert.ShouldNotBeNull();
        });
    }

    [Fact]
    public async Task WhenForbidden_ShouldDisplayForbiddenMessage()
    {
        // Arrange
        var clubId = 1L;

        _mockPlayersService.CreatePlayerAsync(Arg.Any<long>(), Arg.Any<CreatePlayerDto>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ServiceResult<PlayerCreatedDto>>(ServiceProblem.Forbidden()));

        var cut = RenderForm(clubId);
        FillValidPlayerData(cut);

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        cut.WaitForAssertion(() =>
        {
            var errorAlert = cut.Find(".alert-danger");
            errorAlert.TextContent.ShouldContain("not authorized");
        });
    }

    [Fact]
    public async Task WhenBadRequest_ShouldDisplayBadRequestMessage()
    {
        // Arrange
        var clubId = 1L;

        _mockPlayersService.CreatePlayerAsync(Arg.Any<long>(), Arg.Any<CreatePlayerDto>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ServiceResult<PlayerCreatedDto>>(ServiceProblem.BadRequest("Invalid data")));

        var cut = RenderForm(clubId);
        FillValidPlayerData(cut);

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        cut.WaitForAssertion(() =>
        {
            var errorAlert = cut.Find(".alert-danger");
            errorAlert.TextContent.ShouldContain("Invalid data");
        });
    }

    [Fact]
    public async Task WhenConflict_ShouldDisplayConflictMessage()
    {
        // Arrange
        var clubId = 1L;

        _mockPlayersService.CreatePlayerAsync(Arg.Any<long>(), Arg.Any<CreatePlayerDto>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ServiceResult<PlayerCreatedDto>>(ServiceProblem.Conflict()));

        var cut = RenderForm(clubId);
        FillValidPlayerData(cut);

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        cut.WaitForAssertion(() =>
        {
            var errorAlert = cut.Find(".alert-danger");
            errorAlert.TextContent.ShouldContain("already exists");
        });
    }

    #endregion

    #region Button State Tests

    [Fact]
    public async Task WhenSubmitting_ShouldDisableButton()
    {
        // Arrange
        var clubId = 1L;
        var tcs = new TaskCompletionSource<ServiceResult<PlayerCreatedDto>>();

        _mockPlayersService.CreatePlayerAsync(Arg.Any<long>(), Arg.Any<CreatePlayerDto>(), Arg.Any<CancellationToken>())
            .Returns(tcs.Task);

        var cut = RenderForm(clubId);
        FillValidPlayerData(cut);

        // Act
        var submitTask = cut.Find("form").SubmitAsync();

        // Assert
        cut.WaitForAssertion(() =>
        {
            var submitButton = cut.Find("button[type='submit']");
            submitButton.GetAttribute("disabled").ShouldNotBeNull();
        });

        // Cleanup
        tcs.SetResult(new PlayerCreatedDto(100, "John", "Doe", "John Doe"));
        await submitTask;
    }

    [Fact]
    public async Task WhenSubmitting_ShouldShowSpinner()
    {
        // Arrange
        var clubId = 1L;
        var tcs = new TaskCompletionSource<ServiceResult<PlayerCreatedDto>>();

        _mockPlayersService.CreatePlayerAsync(Arg.Any<long>(), Arg.Any<CreatePlayerDto>(), Arg.Any<CancellationToken>())
            .Returns(tcs.Task);

        var cut = RenderForm(clubId);
        FillValidPlayerData(cut);

        // Act
        var submitTask = cut.Find("form").SubmitAsync();

        // Assert
        cut.WaitForAssertion(() =>
        {
            var spinner = cut.Find(".spinner-border");
            spinner.ShouldNotBeNull();
        });

        // Cleanup
        tcs.SetResult(new PlayerCreatedDto(100, "John", "Doe", "John Doe"));
        await submitTask;
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task WhenFirstNameEmpty_ShouldShowValidationError()
    {
        // Arrange
        var cut = RenderForm();
        cut.Find("#lastName").Change("Doe");
        cut.Find("#dateOfBirth").Change(DateOnly.FromDateTime(DateTime.Today.AddYears(-15)).ToString("yyyy-MM-dd"));
        cut.Find("#graduationYear").Change((DateTime.Today.Year + 3).ToString());

        // Act - submit without first name
        await cut.Find("form").SubmitAsync();

        // Assert
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("First name is required"));
    }

    [Fact]
    public async Task WhenLastNameEmpty_ShouldShowValidationError()
    {
        // Arrange
        var cut = RenderForm();
        cut.Find("#firstName").Change("John");
        cut.Find("#dateOfBirth").Change(DateOnly.FromDateTime(DateTime.Today.AddYears(-15)).ToString("yyyy-MM-dd"));
        cut.Find("#graduationYear").Change((DateTime.Today.Year + 3).ToString());

        // Act - submit without last name
        await cut.Find("form").SubmitAsync();

        // Assert
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Last name is required"));
    }

    #endregion

    #region Gender Select Tests

    [Fact]
    public void WhenRendered_GenderSelectShouldHaveAllOptions()
    {
        // Act
        var cut = RenderForm();

        // Assert
        var genderSelect = cut.Find("#gender");
        var options = genderSelect.QuerySelectorAll("option");

        options.Length.ShouldBeGreaterThanOrEqualTo(3); // Default + Male + Female (at minimum)
    }

    #endregion

    #region Image Cropper Modal Tests

    [Fact]
    public void WhenRendered_ShouldIncludeImageCropperModal()
    {
        // Act
        var cut = RenderForm();

        // Assert - The ImageCropperModal should be present in the component
        // It starts hidden (IsVisible = false)
        var modal = cut.Find(".modal");
        modal.ShouldNotBeNull();
        modal.ClassList.ShouldNotContain("show"); // Modal should start hidden
    }

    [Fact]
    public void WhenRendered_PhotoInputShouldAcceptImageTypes()
    {
        // Act
        var cut = RenderForm();

        // Assert
        var photoInput = cut.Find("#photo");
        var acceptAttr = photoInput.GetAttribute("accept");
        acceptAttr.ShouldNotBeNull();
        acceptAttr.ShouldContain("image/jpeg");
        acceptAttr.ShouldContain("image/png");
        acceptAttr.ShouldContain("image/gif");
        acceptAttr.ShouldContain("image/webp");
    }

    [Fact]
    public void WhenNoCroppedPhoto_ShouldNotShowPhotoPreview()
    {
        // Act
        var cut = RenderForm();

        // Assert
        var photoPreviewImages = cut.FindAll("img.photo-preview");
        photoPreviewImages.ShouldBeEmpty();
    }

    [Fact]
    public void WhenNoCroppedPhoto_ShouldNotShowCroppedBadge()
    {
        // Act
        var cut = RenderForm();

        // Assert
        var croppedBadges = cut.FindAll(".badge.bg-success");
        croppedBadges.ShouldBeEmpty();
    }

    #endregion
}
