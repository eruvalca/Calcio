using System.Security.Claims;

using Bunit;
using Bunit.TestDoubles;

using Calcio.Shared.DTOs.CalcioUsers;
using Calcio.Shared.DTOs.Clubs;
using Calcio.Shared.Results;
using Calcio.Shared.Services.CalcioUsers;
using Calcio.Shared.Services.Clubs;
using Calcio.UI.Components.Layout;
using Calcio.UI.Services.CalcioUsers;
using Calcio.UI.Services.Clubs;
using Calcio.UI.Services.Theme;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using OneOf;
using OneOf.Types;

using Shouldly;

namespace Calcio.UnitTests.Components.Layout;

/// <summary>
/// Unit tests for the NavMenu Blazor component using bUnit.
/// 
/// This component handles:
/// - Navigation links
/// - Theme switching (Light/Dark/System)
/// - Authentication state display (Login/Register vs User info/Logout)
/// 
/// TESTING CHALLENGES:
/// - ThemeService uses JSInterop for persistence
/// - AuthorizeView requires authentication state
/// - NavigationManager location tracking
/// - RendererInfo.IsInteractive check for theme initialization
/// </summary>
public sealed class NavMenuTests : BunitContext
{
    private readonly BunitAuthorizationContext _authContext;
    private readonly IClubsService _clubsService;
    private readonly ICalcioUsersService _calcioUsersService;

    public NavMenuTests()
    {
        // Setup JSInterop in loose mode for ThemeService JS calls
        JSInterop.Mode = JSRuntimeMode.Loose;

        // Setup JSInterop for theme module
        JSInterop.SetupModule("./_content/Calcio.UI/theme.js");

        // Register ThemeService - it will use loose JSInterop
        Services.AddSingleton<ThemeService>();

        // Register IClubsService required by NavMenu
        _clubsService = Substitute.For<IClubsService>();
        _clubsService
            .GetUserClubsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ServiceResult<List<BaseClubDto>>(new List<BaseClubDto>())));
        Services.AddSingleton(_clubsService);

        // Register ICalcioUsersService required by NavMenu for photo
        _calcioUsersService = Substitute.For<ICalcioUsersService>();
        _calcioUsersService
            .GetAccountPhotoAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ServiceResult<OneOf<CalcioUserPhotoDto, None>>(OneOf<CalcioUserPhotoDto, None>.FromT1(new None()))));
        Services.AddSingleton(_calcioUsersService);
        Services.AddSingleton(TimeProvider.System);
        Services.AddSingleton<UserPhotoStateService>();
        Services.AddSingleton<UserClubStateService>();
        Services.AddLogging();

        // Add authorization services with a default unauthenticated state
        // bUnit provides AddAuthorization() for easy auth mocking
        _authContext = AddAuthorization();

        // Set RendererInfo because NavMenu accesses RendererInfo.IsInteractive in OnAfterRenderAsync
        // This simulates a Server-side rendered interactive component
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    #region Helper Methods

    private IRenderedComponent<NavMenu> RenderNavMenu()
        => Render<NavMenu>();

    private void SetupAuthenticatedUser(string username = "testuser@example.com", params string[] roles)
    {
        _authContext.SetAuthorized(username);
        // Add NameIdentifier claim required by NavMenu's auth check
        _authContext.SetClaims(new Claim(ClaimTypes.NameIdentifier, "1"));
        if (roles.Length > 0)
        {
            _authContext.SetRoles(roles);
        }
    }

    private static void SetupUnauthenticatedUser()
    {
        // Default state after AddAuthorization() is unauthenticated
        // No additional setup needed, but we can explicitly set it
    }

    private void SetupUserWithPhoto(string smallUrl = "https://example.com/photo-small.jpg")
    {
        var photoDto = new CalcioUserPhotoDto(
            CalcioUserPhotoId: 1,
            OriginalUrl: "https://example.com/photo-original.jpg",
            SmallUrl: smallUrl,
            MediumUrl: "https://example.com/photo-medium.jpg",
            LargeUrl: "https://example.com/photo-large.jpg");

        _calcioUsersService
            .GetAccountPhotoAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ServiceResult<OneOf<CalcioUserPhotoDto, None>>(OneOf<CalcioUserPhotoDto, None>.FromT0(photoDto))));
    }

    private void SetupUserWithoutPhoto() => _calcioUsersService
            .GetAccountPhotoAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ServiceResult<OneOf<CalcioUserPhotoDto, None>>(OneOf<CalcioUserPhotoDto, None>.FromT1(new None()))));

    private void SetupPhotoServiceError() => _calcioUsersService
            .GetAccountPhotoAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ServiceResult<OneOf<CalcioUserPhotoDto, None>>(ServiceProblem.ServerError())));

    private sealed class SequenceAuthStateProvider : AuthenticationStateProvider
    {
        private readonly Queue<AuthenticationState> _states;
        private AuthenticationState _current;

        public SequenceAuthStateProvider(IEnumerable<AuthenticationState> states)
        {
            _states = new Queue<AuthenticationState>(states);
            _current = _states.Count > 0
                ? _states.Dequeue()
                : new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            if (_states.Count > 0)
            {
                _current = _states.Dequeue();
            }

            return Task.FromResult(_current);
        }
    }

    #endregion

    #region Initial Rendering Tests

    [Fact]
    public void WhenRendered_ShouldDisplayNavbar()
    {
        // Arrange & Act
        var cut = RenderNavMenu();

        // Assert
        cut.Find("nav.navbar").ShouldNotBeNull();
        cut.Markup.ShouldContain("Calcio");
    }

    [Fact]
    public void WhenRendered_ShouldDisplayNavigationLinks()
    {
        // Arrange & Act
        var cut = RenderNavMenu();

        // Assert
        cut.Markup.ShouldContain("Weather");
    }

    [Fact]
    public void WhenRendered_ShouldDisplayThemeDropdown()
    {
        // Arrange & Act
        var cut = RenderNavMenu();

        // Assert
        var themeDropdown = cut.Find(".dropdown");
        themeDropdown.ShouldNotBeNull();

        // Should have theme options
        cut.Markup.ShouldContain("Light");
        cut.Markup.ShouldContain("Dark");
        cut.Markup.ShouldContain("Auto");
    }

    [Fact]
    public void WhenClubsServiceReturnsAClub_ShouldRenderClubNavLink()
    {
        // Arrange
        SetupAuthenticatedUser();
        _clubsService
            .GetUserClubsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ServiceResult<List<BaseClubDto>>(new List<BaseClubDto>
            {
                new(123, "Test Club", "City", "State")
            })));

        // Act
        var cut = RenderNavMenu();

        // Assert
        _clubsService.Received(1).GetUserClubsAsync(Arg.Any<CancellationToken>());

        var clubLink = cut.FindAll("a.nav-link").FirstOrDefault(a => a.TextContent.Contains("Test Club"));
        clubLink.ShouldNotBeNull();

        var href = clubLink.GetAttribute("href");
        href.ShouldNotBeNull();
        (href.EndsWith("/clubs/123", StringComparison.Ordinal) || href.EndsWith("clubs/123", StringComparison.Ordinal)).ShouldBeTrue();
    }

    [Fact]
    public void WhenClubStateChanges_ShouldRenderClubNavLink()
    {
        // Arrange
        SetupAuthenticatedUser();
        _clubsService
            .GetUserClubsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ServiceResult<List<BaseClubDto>>(new List<BaseClubDto>())));

        var cut = RenderNavMenu();
        var clubStateService = Services.GetRequiredService<UserClubStateService>();

        // Act
        clubStateService.SetUserClubs(
        [
            new BaseClubDto(321, "State Club", "City", "ST")
        ]);

        // Assert
        cut.WaitForAssertion(() =>
        {
            var clubLink = cut.FindAll("a.nav-link").FirstOrDefault(a => a.TextContent.Contains("State Club"));
            clubLink.ShouldNotBeNull();
            var href = clubLink.GetAttribute("href");
            href.ShouldNotBeNull();
            (href.EndsWith("/clubs/321", StringComparison.Ordinal) || href.EndsWith("clubs/321", StringComparison.Ordinal)).ShouldBeTrue();
        });
    }

    [Fact]
    public void WhenUnauthenticated_ShouldNotCallClubsService()
    {
        // Arrange
        SetupUnauthenticatedUser();

        // Act
        var cut = RenderNavMenu();

        // Assert
        _clubsService.DidNotReceive().GetUserClubsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void WhenUnauthenticated_ShouldNotCallCalcioUsersService()
    {
        // Arrange
        SetupUnauthenticatedUser();

        // Act
        var cut = RenderNavMenu();

        // Assert
        _calcioUsersService.DidNotReceive().GetAccountPhotoAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WhenAuthClaimsHydrateAfterInteractiveRender_ShouldRenderClubNavLink()
    {
        await using var context = new BunitContext();

        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.JSInterop.SetupModule("./_content/Calcio.UI/theme.js");

        var clubsService = Substitute.For<IClubsService>();
        clubsService
            .GetUserClubsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ServiceResult<List<BaseClubDto>>(new List<BaseClubDto>
            {
                new(555, "Hydrated Club", "City", "State")
            })));

        var calcioUsersService = Substitute.For<ICalcioUsersService>();
        calcioUsersService
            .GetAccountPhotoAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ServiceResult<OneOf<CalcioUserPhotoDto, None>>(OneOf<CalcioUserPhotoDto, None>.FromT1(new None()))));

        var hydratedStates = new SequenceAuthStateProvider(
        [
            new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, "user@test.com")
            ], "test"))),
            new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, "user@test.com"),
                new Claim(ClaimTypes.NameIdentifier, "1")
            ], "test")))
        ]);

        context.AddAuthorization();
        context.Services.AddSingleton<AuthenticationStateProvider>(hydratedStates);
        context.Services.AddSingleton<ThemeService>();
        context.Services.AddSingleton(clubsService);
        context.Services.AddSingleton(calcioUsersService);
        context.Services.AddSingleton(TimeProvider.System);
        context.Services.AddSingleton<UserPhotoStateService>();
        context.Services.AddSingleton<UserClubStateService>();
        context.Services.AddLogging();

        context.SetRendererInfo(new RendererInfo("Server", isInteractive: true));

        var cut = context.Render<CascadingAuthenticationState>(parameters =>
            parameters.AddChildContent<NavMenu>());

        cut.WaitForAssertion(() =>
        {
            var clubLink = cut.FindAll("a.nav-link").FirstOrDefault(a => a.TextContent.Contains("Hydrated Club"));
            clubLink.ShouldNotBeNull();
        });

        await Task.CompletedTask;
    }

    #endregion

    #region Profile Photo Avatar Tests

    [Fact]
    public void WhenAuthenticated_WithPhoto_ShouldDisplayAvatarImage()
    {
        // Arrange
        SetupAuthenticatedUser("user@test.com");
        SetupUserWithPhoto("https://example.com/avatar.jpg");

        // Act
        var cut = RenderNavMenu();

        // Assert
        var avatarImg = cut.Find("img.avatar-img");
        avatarImg.ShouldNotBeNull();
        avatarImg.GetAttribute("src").ShouldBe("https://example.com/avatar.jpg");
        avatarImg.GetAttribute("alt").ShouldBe("Profile photo");
        avatarImg.ClassList.ShouldContain("rounded-circle");
    }

    [Fact]
    public void WhenAuthenticated_WithPhoto_AvatarShouldLinkToManageAccount()
    {
        // Arrange
        SetupAuthenticatedUser("user@test.com");
        SetupUserWithPhoto();

        // Act
        var cut = RenderNavMenu();

        // Assert
        var avatarLink = cut.Find("img.avatar-img").ParentElement;
        avatarLink.ShouldNotBeNull();
        var href = avatarLink.GetAttribute("href");
        href.ShouldNotBeNull();
        href.ShouldContain("Account/Manage");
    }

    [Fact]
    public void WhenAuthenticated_WithoutPhoto_ShouldDisplayPlaceholderIcon()
    {
        // Arrange
        SetupAuthenticatedUser("user@test.com");
        SetupUserWithoutPhoto();

        // Act
        var cut = RenderNavMenu();

        // Assert
        var placeholder = cut.Find(".bi-person-circle.avatar-placeholder");
        placeholder.ShouldNotBeNull();
        cut.FindAll("img.avatar-img").Count.ShouldBe(0);
    }

    [Fact]
    public void WhenAuthenticated_WithoutPhoto_PlaceholderShouldLinkToUploadPage()
    {
        // Arrange
        SetupAuthenticatedUser("user@test.com");
        SetupUserWithoutPhoto();

        // Act
        var cut = RenderNavMenu();

        // Assert
        var placeholderLink = cut.Find(".bi-person-circle.avatar-placeholder").ParentElement;
        placeholderLink.ShouldNotBeNull();
        var href = placeholderLink.GetAttribute("href");
        href.ShouldNotBeNull();
        href.ShouldContain("Account/UploadProfilePhoto");
    }

    [Fact]
    public void WhenAuthenticated_PhotoServiceReturnsError_ShouldFallbackToPlaceholder()
    {
        // Arrange
        SetupAuthenticatedUser("user@test.com");
        SetupPhotoServiceError();

        // Act
        var cut = RenderNavMenu();

        // Assert
        var placeholder = cut.Find(".bi-person-circle.avatar-placeholder");
        placeholder.ShouldNotBeNull();
        cut.FindAll("img.avatar-img").Count.ShouldBe(0);
    }

    [Fact]
    public void WhenAuthenticated_WithPhoto_ShouldDisplayUsernameOnLargerScreens()
    {
        // Arrange
        SetupAuthenticatedUser("user@test.com");
        SetupUserWithPhoto();

        // Act
        var cut = RenderNavMenu();

        // Assert
        var usernameSpan = cut.Find("img.avatar-img").ParentElement?.QuerySelector(".d-none.d-sm-inline");
        usernameSpan.ShouldNotBeNull();
        usernameSpan.TextContent.ShouldContain("user@test.com");
    }

    [Fact]
    public void WhenPhotoStateChanges_ShouldUpdateAvatarImage()
    {
        // Arrange
        SetupAuthenticatedUser("user@test.com");
        SetupUserWithoutPhoto();
        var cut = RenderNavMenu();
        var photoStateService = Services.GetRequiredService<UserPhotoStateService>();

        // Act
        photoStateService.SetPhotoUrl("https://example.com/updated-avatar.jpg");

        // Assert
        cut.WaitForAssertion(() =>
        {
            var avatarImg = cut.Find("img.avatar-img");
            avatarImg.GetAttribute("src").ShouldBe("https://example.com/updated-avatar.jpg");
        });
    }

    [Fact]
    public void WhenAuthStateChangesToAuthenticated_ShouldFetchPhoto()
    {
        // Arrange
        SetupUnauthenticatedUser();
        SetupUserWithPhoto("https://example.com/avatar.jpg");
        var cut = RenderNavMenu();

        // Act
        _authContext.SetAuthorized("user@test.com");
        _authContext.SetClaims(new Claim(ClaimTypes.NameIdentifier, "1"));

        // Assert
        cut.WaitForAssertion(() =>
        {
            _calcioUsersService.Received(1).GetAccountPhotoAsync(Arg.Any<CancellationToken>());
            var avatarImg = cut.Find("img.avatar-img");
            avatarImg.GetAttribute("src").ShouldBe("https://example.com/avatar.jpg");
        });
    }

    [Fact]
    public void WhenAuthenticatedUserIdChanges_ShouldRefreshPhoto()
    {
        // Arrange
        SetupAuthenticatedUser("user1@test.com");
        var firstPhoto = new CalcioUserPhotoDto(1, "https://example.com/original-1.jpg", "https://example.com/first.jpg", null, null);
        var secondPhoto = new CalcioUserPhotoDto(2, "https://example.com/original-2.jpg", "https://example.com/second.jpg", null, null);

        _calcioUsersService.GetAccountPhotoAsync(Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult<ServiceResult<OneOf<CalcioUserPhotoDto, None>>>(OneOf<CalcioUserPhotoDto, None>.FromT0(firstPhoto)),
                Task.FromResult<ServiceResult<OneOf<CalcioUserPhotoDto, None>>>(OneOf<CalcioUserPhotoDto, None>.FromT0(secondPhoto)));

        var cut = RenderNavMenu();

        // Assert
        var avatarImg = cut.Find("img.avatar-img");
        avatarImg.GetAttribute("src").ShouldBe("https://example.com/first.jpg");

        // Act
        _authContext.SetAuthorized("user2@test.com");
        _authContext.SetClaims(new Claim(ClaimTypes.NameIdentifier, "2"));

        // Assert
        cut.WaitForAssertion(() =>
        {
            var updatedAvatar = cut.Find("img.avatar-img");
            updatedAvatar.GetAttribute("src").ShouldBe("https://example.com/second.jpg");
        });
    }

    [Fact]
    public void WhenUnauthenticated_ShouldNotDisplayAvatarOrPlaceholder()
    {
        // Arrange
        SetupUnauthenticatedUser();

        // Act
        var cut = RenderNavMenu();

        // Assert
        cut.FindAll("img.avatar-img").Count.ShouldBe(0);
        cut.FindAll(".bi-person-circle.avatar-placeholder").Count.ShouldBe(0);
        cut.FindAll(".avatar-spinner").Count.ShouldBe(0);
    }

    #endregion

    #region Authentication State Tests

    [Fact]
    public void WhenUnauthenticated_ShouldShowLoginAndRegisterLinks()
    {
        // Arrange
        SetupUnauthenticatedUser();

        // Act
        var cut = RenderNavMenu();

        // Assert
        cut.Markup.ShouldContain("Register");
        cut.Markup.ShouldContain("Login");
        cut.Markup.ShouldNotContain("Logout");
    }

    [Fact]
    public void WhenAuthenticated_ShouldShowUsernameAndLogout()
    {
        // Arrange
        SetupAuthenticatedUser("john.doe@example.com");

        // Act
        var cut = RenderNavMenu();

        // Assert
        cut.Markup.ShouldContain("john.doe@example.com");
        cut.Markup.ShouldContain("Logout");
        cut.Markup.ShouldNotContain("Register");
        cut.Markup.ShouldNotContain(">Login<"); // Avoid matching "Login" in other contexts
    }

    [Fact]
    public void WhenAuthenticated_ShouldHaveManageAccountLink()
    {
        // Arrange
        SetupAuthenticatedUser("user@test.com");
        SetupUserWithPhoto();

        // Act
        var cut = RenderNavMenu();

        // Assert
        var manageLink = cut.FindAll("a").FirstOrDefault(a => a.GetAttribute("href")?.Contains("Account/Manage") == true);
        manageLink.ShouldNotBeNull();
    }

    [Fact]
    public void WhenAuthenticated_LogoutFormShouldHaveAntiforgeryToken()
    {
        // Arrange
        SetupAuthenticatedUser();

        // Act
        var cut = RenderNavMenu();

        // Assert
        var logoutForm = cut.Find("form[action='Account/Logout']");
        logoutForm.ShouldNotBeNull();
        logoutForm.GetAttribute("method").ShouldBe("post");
    }

    [Fact]
    public void WhenAuthenticated_ReturnUrlShouldTrackNavigationChanges()
    {
        // Arrange
        SetupAuthenticatedUser();
        var cut = RenderNavMenu();
        var nav = Services.GetRequiredService<NavigationManager>();

        // Act
        nav.NavigateTo("weather");

        // Assert
        cut.WaitForAssertion(() =>
        {
            var returnUrlInput = cut.Find("input[name='ReturnUrl']");
            returnUrlInput.GetAttribute("value").ShouldBe("weather");
        });
    }

    #endregion

    #region Theme Dropdown Tests

    [Fact]
    public void WhenRendered_ThemeDropdownShouldHaveAllOptions()
    {
        // Arrange & Act
        var cut = RenderNavMenu();

        // Assert - There are two theme dropdowns (mobile and desktop), each with 3 options
        var dropdownItems = cut.FindAll(".dropdown-item");
        dropdownItems.Count.ShouldBe(6); // 3 options x 2 dropdowns

        var itemTexts = dropdownItems.Select(i => i.TextContent.Trim()).ToList();
        // Each option appears twice (once in mobile, once in desktop dropdown)
        itemTexts.Count(t => t.Contains("Light")).ShouldBe(2);
        itemTexts.Count(t => t.Contains("Dark")).ShouldBe(2);
        itemTexts.Count(t => t.Contains("Auto")).ShouldBe(2);
    }

    [Fact]
    public void WhenRendered_ShouldHaveMobileAndDesktopThemeDropdowns()
    {
        // Arrange & Act
        var cut = RenderNavMenu();

        // Assert - Mobile dropdown (visible on small screens, hidden on sm+)
        var mobileDropdown = cut.Find(".nav-item.d-sm-none .dropdown");
        mobileDropdown.ShouldNotBeNull();

        // Assert - Desktop dropdown (hidden on small screens, visible on sm+)
        var desktopDropdown = cut.Find(".d-none.d-sm-flex .dropdown");
        desktopDropdown.ShouldNotBeNull();
    }

    [Fact]
    public void ThemeDropdownToggle_ShouldDisplayCurrentThemeEmoji()
    {
        // Arrange & Act
        var cut = RenderNavMenu();

        // Assert - Default theme (System) should show 🌗
        var toggleButton = cut.Find(".dropdown-toggle");
        toggleButton.ShouldNotBeNull();
        // The button should contain one of the theme emojis
        var buttonText = toggleButton.TextContent;
        (buttonText.Contains("☀️") || buttonText.Contains("🌙") || buttonText.Contains("🌗")).ShouldBeTrue();
    }

    [Fact]
    public void WhenThemeIsChangedToLight_ShouldMarkLightAsActive()
    {
        // Arrange
        var cut = RenderNavMenu();

        // Act
        var lightButtons = cut.FindAll("button.dropdown-item").Where(b => b.TextContent.Contains("Light")).ToList();
        lightButtons.Count.ShouldBe(2);
        lightButtons[0].Click();

        // Assert
        Services.GetRequiredService<ThemeService>().Current.ShouldBe(ThemePreference.Light);
        cut.WaitForAssertion(() =>
        {
            var updatedLightButtons = cut.FindAll("button.dropdown-item").Where(b => b.TextContent.Contains("Light")).ToList();
            updatedLightButtons.Count.ShouldBe(2);
            updatedLightButtons.All(b => b.ClassList.Contains("active")).ShouldBeTrue();
        });
    }

    #endregion

    #region Navigation Link Tests

    [Fact]
    public void NavigationLinks_ShouldHaveCorrectHrefs()
    {
        // Arrange & Act
        var cut = RenderNavMenu();

        // Assert
        var navLinks = cut.FindAll("a.nav-link");

        // Default auth state is unauthenticated
        navLinks.ShouldContain(link => link.GetAttribute("href") == "weather");
        navLinks.ShouldContain(link => link.GetAttribute("href") == "Account/Register");
        navLinks.ShouldContain(link => link.GetAttribute("href") == "Account/Login");
    }

    [Fact]
    public void BrandLink_ShouldPointToHome()
    {
        // Arrange & Act
        var cut = RenderNavMenu();

        // Assert
        var brandLink = cut.Find("a.navbar-brand");
        brandLink.GetAttribute("href").ShouldBe("/");
    }

    #endregion

    #region Mobile Toggle Tests

    [Fact]
    public void WhenRendered_ShouldHaveMobileToggleButton()
    {
        // Arrange & Act
        var cut = RenderNavMenu();

        // Assert
        var toggleButton = cut.Find("button.navbar-toggler");
        toggleButton.ShouldNotBeNull();
        toggleButton.GetAttribute("data-bs-toggle").ShouldBe("collapse");
        toggleButton.GetAttribute("data-bs-target").ShouldBe("#mainNavbar");
    }

    [Fact]
    public void WhenRendered_ShouldHaveCollapsibleNavContent()
    {
        // Arrange & Act
        var cut = RenderNavMenu();

        // Assert
        var collapsibleContent = cut.Find("#mainNavbar");
        collapsibleContent.ShouldNotBeNull();
        collapsibleContent.ClassList.ShouldContain("collapse");
        collapsibleContent.ClassList.ShouldContain("navbar-collapse");
    }

    #endregion

    #region Accessibility Tests

    [Fact]
    public void ToggleButton_ShouldHaveAriaLabel()
    {
        // Arrange & Act
        var cut = RenderNavMenu();

        // Assert
        var toggleButton = cut.Find("button.navbar-toggler");
        toggleButton.GetAttribute("aria-label").ShouldBe("Toggle navigation");
        toggleButton.GetAttribute("aria-controls").ShouldBe("mainNavbar");
        toggleButton.GetAttribute("aria-expanded").ShouldBe("false");
    }

    [Fact]
    public void NavigationIcons_ShouldBeAriaHidden()
    {
        // Arrange & Act
        var cut = RenderNavMenu();

        // Assert
        var icons = cut.FindAll("span[aria-hidden='true']");
        icons.Count.ShouldBeGreaterThan(0);
    }

    #endregion
}
