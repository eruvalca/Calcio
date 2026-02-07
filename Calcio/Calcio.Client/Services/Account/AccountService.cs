using Calcio.Shared.Endpoints;
using Calcio.Shared.Results;
using Calcio.Shared.Services.Account;

using OneOf.Types;

namespace Calcio.Client.Services.Account;

public sealed class AccountService(HttpClient httpClient) : IAccountService
{
    public async Task<ServiceResult<Success>> RefreshSignInAsync(CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsync(Routes.Account.ForRefreshSignIn(), content: null, cancellationToken);

        return response.IsSuccessStatusCode
            ? new Success()
            : await response.ToServiceProblemAsync(cancellationToken);
    }
}
