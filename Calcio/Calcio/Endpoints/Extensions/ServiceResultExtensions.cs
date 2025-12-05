using Calcio.Shared.Results;

using Microsoft.AspNetCore.Http.HttpResults;

namespace Calcio.Endpoints.Extensions;

/// <summary>
/// Extension methods for converting <see cref="ServiceResult{T}"/> to HTTP results in endpoints.
/// </summary>
public static class ServiceResultExtensions
{
    extension<TSuccess>(ServiceResult<TSuccess> result)
    {
        /// <summary>
        /// Converts this <see cref="ServiceResult{T}"/> to an HTTP result using the provided success mapper.
        /// </summary>
        /// <typeparam name="THttpSuccess">The HTTP result type for success (e.g., Ok{T}, Created{T}).</typeparam>
        /// <param name="onSuccess">Function to create the success HTTP result.</param>
        /// <returns>The HTTP result.</returns>
        public Results<THttpSuccess, ProblemHttpResult> ToHttpResult<THttpSuccess>(Func<TSuccess, THttpSuccess> onSuccess)
            where THttpSuccess : IResult
            => result.Match<Results<THttpSuccess, ProblemHttpResult>>(
                success => onSuccess(success),
                problem => TypedResults.Problem(statusCode: problem.StatusCode, detail: problem.Detail));

        /// <summary>
        /// Converts this <see cref="ServiceResult{T}"/> with no meaningful success value to an HTTP result.
        /// </summary>
        /// <typeparam name="THttpSuccess">The HTTP result type for success (e.g., NoContent, Ok).</typeparam>
        /// <param name="successResult">The HTTP result to return on success.</param>
        /// <returns>The HTTP result.</returns>
        public Results<THttpSuccess, ProblemHttpResult> ToHttpResult<THttpSuccess>(THttpSuccess successResult)
            where THttpSuccess : IResult
            => result.Match<Results<THttpSuccess, ProblemHttpResult>>(
                success => successResult,
                problem => TypedResults.Problem(statusCode: problem.StatusCode, detail: problem.Detail));
    }
}
