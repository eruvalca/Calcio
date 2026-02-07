using Calcio.Shared.Entities;
using Calcio.Shared.Results;
using Calcio.Shared.Services.Account;

using Microsoft.AspNetCore.Identity;

using OneOf.Types;

namespace Calcio.Services.Account;

public sealed class AccountService(
    UserManager<CalcioUserEntity> userManager,
    SignInManager<CalcioUserEntity> signInManager,
    IHttpContextAccessor httpContextAccessor) : IAccountService
{
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
