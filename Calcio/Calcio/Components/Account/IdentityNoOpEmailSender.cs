using Calcio.Entities;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace Calcio.Components.Account;

// Remove the "else if (EmailSender is IdentityNoOpEmailSender)" block from RegisterConfirmation.razor after updating with a real implementation.
/// <summary>
/// Represents the Identity No Op Email Sender.
/// </summary>
internal sealed class IdentityNoOpEmailSender : IEmailSender<CalcioUserEntity>
{
    /// <summary>
    /// Stores the email Sender.
    /// </summary>
    private readonly IEmailSender emailSender = new NoOpEmailSender();

    /// <summary>
    /// Executes the Send Confirmation Link Async operation.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <param name="email">The email.</param>
    /// <param name="confirmationLink">The confirmation Link.</param>
    /// <returns>The operation result.</returns>
    public Task SendConfirmationLinkAsync(CalcioUserEntity user, string email, string confirmationLink)
        => emailSender.SendEmailAsync(email, "Confirm your email", $"Please confirm your account by <a href='{confirmationLink}'>clicking here</a>.");

    /// <summary>
    /// Executes the Send Password Reset Link Async operation.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <param name="email">The email.</param>
    /// <param name="resetLink">The reset Link.</param>
    /// <returns>The operation result.</returns>
    public Task SendPasswordResetLinkAsync(CalcioUserEntity user, string email, string resetLink)
        => emailSender.SendEmailAsync(email, "Reset your password", $"Please reset your password by <a href='{resetLink}'>clicking here</a>.");

    /// <summary>
    /// Executes the Send Password Reset Code Async operation.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <param name="email">The email.</param>
    /// <param name="resetCode">The reset Code.</param>
    /// <returns>The operation result.</returns>
    public Task SendPasswordResetCodeAsync(CalcioUserEntity user, string email, string resetCode)
        => emailSender.SendEmailAsync(email, "Reset your password", $"Please reset your password using the following code: {resetCode}");
}
