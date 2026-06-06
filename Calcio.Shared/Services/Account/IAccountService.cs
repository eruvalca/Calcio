using Calcio.Shared.Results;

using OneOf.Types;

namespace Calcio.Shared.Services.Account;

public interface IAccountService
{
    /// <summary>
    /// Refreshes the current user's sign-in to update claims and roles.
    /// </summary>
    Task<ServiceResult<Success>> RefreshSignInAsync(CancellationToken cancellationToken);
}
