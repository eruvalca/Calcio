using Calcio.Endpoints.Extensions;
using Calcio.Shared.Endpoints;
using Calcio.Shared.Services.Account;

using Microsoft.AspNetCore.Http.HttpResults;

namespace Calcio.Endpoints.Account;

/// <summary>
/// Registers API endpoints for Account Endpoints.
/// </summary>
public static class AccountEndpoints
{
    /// <summary>
    /// Executes the Map Account Endpoints operation.
    /// </summary>
    /// <param name="endpoints">The endpoints.</param>
    /// <returns>The operation result.</returns>
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(Routes.Account.RefreshSignIn, RefreshSignIn)
            .RequireAuthorization()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return endpoints;
    }

    /// <summary>
    /// Refreshes the authenticated user's sign-in cookie.
    /// </summary>
    /// <param name="accountService">The account service used to refresh the sign-in state.</param>
    /// <param name="cancellationToken">The cancellation token for the asynchronous operation.</param>
    /// <returns>A no-content response when the sign-in state is refreshed, or a problem response when the operation fails.</returns>
    private static async Task<Results<NoContent, ProblemHttpResult>> RefreshSignIn(
        IAccountService accountService,
        CancellationToken cancellationToken)
    {
        var result = await accountService.RefreshSignInAsync(cancellationToken);

        return result.ToHttpResult(TypedResults.NoContent());
    }
}
