using Calcio.Shared.Endpoints;
using Calcio.Shared.Results;
using Calcio.Shared.Services.Account;

using OneOf.Types;

namespace Calcio.Client.Services.Account;

/// <summary>
/// Calls account endpoints required by the client to keep the authenticated user session in sync.
/// </summary>
/// <param name="httpClient">HTTP client configured for authenticated API calls.</param>
public sealed class AccountService(HttpClient httpClient) : IAccountService
{
    /// <summary>
    /// Requests the server to refresh the current authentication state and issue an updated sign-in context.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the HTTP request.</param>
    /// <returns>
    /// A successful result when the sign-in state was refreshed; otherwise a mapped service problem.
    /// </returns>
    public async Task<ServiceResult<Success>> RefreshSignInAsync(CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsync(Routes.Account.ForRefreshSignIn(), content: null, cancellationToken);

        return response.IsSuccessStatusCode
            ? new Success()
            : await response.ToServiceProblemAsync(cancellationToken);
    }
}
