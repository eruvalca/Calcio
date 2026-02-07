using Calcio.Endpoints.Extensions;
using Calcio.Shared.Endpoints;
using Calcio.Shared.Services.Account;

using Microsoft.AspNetCore.Http.HttpResults;

namespace Calcio.Endpoints.Account;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(Routes.Account.RefreshSignIn, RefreshSignIn)
            .RequireAuthorization()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return endpoints;
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> RefreshSignIn(
        IAccountService accountService,
        CancellationToken cancellationToken)
    {
        var result = await accountService.RefreshSignInAsync(cancellationToken);

        return result.ToHttpResult(TypedResults.NoContent());
    }
}
