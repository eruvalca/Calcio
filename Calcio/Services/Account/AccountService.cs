using Calcio.Entities;
using Calcio.Shared.Results;
using Calcio.Shared.Services.Account;

using Microsoft.AspNetCore.Identity;

using OneOf.Types;

namespace Calcio.Services.Account;

/// <summary>
/// Provides Account Service operations.
/// </summary>
/// <param name="userManager">The user Manager.</param>
/// <param name="signInManager">The sign In Manager.</param>
/// <param name="httpContextAccessor">The http Context Accessor.</param>
public sealed class AccountService(
    UserManager<CalcioUserEntity> userManager,
    SignInManager<CalcioUserEntity> signInManager,
    IHttpContextAccessor httpContextAccessor) : IAccountService
{
    /// <summary>
    /// Executes the Refresh Sign In Async operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>The operation result.</returns>
    public async Task<ServiceResult<Success>> RefreshSignInAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var principal = httpContextAccessor.HttpContext?.User;
        if (principal is null)
        {
            return ServiceProblem.NotFound();
        }

        var userId = userManager.GetUserId(principal);
        if (string.IsNullOrEmpty(userId))
        {
            return ServiceProblem.NotFound();
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return ServiceProblem.NotFound();
        }

        await signInManager.RefreshSignInAsync(user);
        return new Success();
    }
}
