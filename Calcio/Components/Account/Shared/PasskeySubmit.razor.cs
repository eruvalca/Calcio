using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components;

namespace Calcio.Components.Account.Shared;

/// <summary>
/// Represents the Passkey Submit.
/// </summary>
/// <param name="services">The services.</param>
public partial class PasskeySubmit(IServiceProvider services)
{
    /// <summary>
    /// Stores the tokens.
    /// </summary>
    private AntiforgeryTokenSet? tokens;

    /// <summary>
    /// Gets or sets the Http Context.
    /// </summary>
    [CascadingParameter]
    /// <summary>
    /// Gets or sets the http context.
    /// </summary>
    private HttpContext HttpContext { get; set; } = default!;

    /// <summary>
    /// Gets or sets the Operation.
    /// </summary>
    [Parameter]
    [EditorRequired]
    /// <summary>
    /// Gets or sets the operation.
    /// </summary>
    public PasskeyOperation Operation { get; set; }

    /// <summary>
    /// Gets or sets the Name.
    /// </summary>
    [Parameter]
    [EditorRequired]
    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string Name { get; set; } = default!;

    /// <summary>
    /// Gets or sets the Email Name.
    /// </summary>
    [Parameter]
    /// <summary>
    /// Gets or sets the email name.
    /// </summary>
    public string? EmailName { get; set; }

    /// <summary>
    /// Gets or sets the Child Content.
    /// </summary>
    [Parameter]
    /// <summary>
    /// Gets or sets the child content.
    /// </summary>
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Gets or sets the Additional Attributes.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>
    /// Executes the On Initialized operation.
    /// </summary>
    protected override void OnInitialized()
        => tokens = services.GetService<IAntiforgery>()?.GetTokens(HttpContext);
}
