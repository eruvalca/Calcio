using System.Diagnostics;

namespace Calcio.Endpoints.Filters;

public sealed partial class UnhandledExceptionFilter(
    ILogger<UnhandledExceptionFilter> logger,
    IProblemDetailsService problemDetailsService) : IEndpointFilter
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
            var httpContext = context.HttpContext;

            LogUnhandledException(logger, ex, httpContext.Request.Method, httpContext.Request.Path, traceId);

            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

            await problemDetailsService.WriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails =
                {
                    Title = "An unexpected error occurred.",
                    Detail = "An internal server error has occurred. Please try again later.",
                    Status = StatusCodes.Status500InternalServerError,
                    Instance = httpContext.Request.Path
                }
            });

            return Results.Empty;
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception in {Method} {Path}. TraceId: {TraceId}")]
    private static partial void LogUnhandledException(ILogger logger, Exception ex, string method, PathString path, string? traceId);
}