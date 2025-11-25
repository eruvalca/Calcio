using System.Diagnostics;

using Microsoft.AspNetCore.Mvc;

namespace Calcio.Endpoints.Filters;

public sealed partial class UnhandledExceptionFilter(ILogger<UnhandledExceptionFilter> logger) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            return await next(context);
        }
        catch (Exception ex)
        {
            var traceId = Activity.Current?.TraceId.ToString();

            LogUnhandledException(logger, ex, context.HttpContext.Request.Method, context.HttpContext.Request.Path, traceId);

            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
                Detail = "An internal server error has occurred. Please try again later.",
                Instance = context.HttpContext.Request.Path
            };

            if (!string.IsNullOrEmpty(traceId))
            {
                problemDetails.Extensions["traceId"] = traceId;
            }

            return TypedResults.Problem(problemDetails);
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception in {Method} {Path}. TraceId: {TraceId}")]
    private static partial void LogUnhandledException(ILogger logger, Exception ex, string method, PathString path, string? traceId);
}