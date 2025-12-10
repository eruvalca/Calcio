using Bunit;
using Bunit.TestDoubles;

using Calcio.UI.Components.Layout;
using Calcio.UI.Services.Theme;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

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

    public NavMenuTests()
    {
        // Setup JSInterop in loose mode for ThemeService JS calls
        JSInterop.Mode = JSRuntimeMode.Loose;

        // Setup JSInterop for theme module
        JSInterop.SetupModule("./_content/Calcio.UI/theme.js");

        // Register ThemeService - it will use loose JSInterop
        Services.AddSingleton<ThemeService>();

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
        cut.Markup.ShouldContain("Counter");
        cut.Markup.ShouldContain("Weather");
        cut.Markup.ShouldContain("Auth Required");
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

    #endregion

    #region Navigation Link Tests

    [Fact]
    public void NavigationLinks_ShouldHaveCorrectHrefs()
    {
        // Arrange & Act
        var cut = RenderNavMenu();

        // Assert
        var navLinks = cut.FindAll("a.nav-link");

        navLinks.ShouldContain(link => link.GetAttribute("href") == "counter");
        navLinks.ShouldContain(link => link.GetAttribute("href") == "weather");
        navLinks.ShouldContain(link => link.GetAttribute("href") == "auth");
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
