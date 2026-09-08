using backend.Api.Contracts;
using backend.Api.Http;
using backend.Messaging;

namespace backend.Api.Http;

public sealed class ApiExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionHandlingMiddleware> _logger;

    public ApiExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ApiExceptionHandlingMiddleware> logger
    )
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Unhandled request exception. Method: {Method}, Path: {Path}",
                context.Request.Method,
                context.Request.Path
            );

            if (context.Response.HasStarted)
            {
                throw;
            }

            ApiErrorMetrics.Record(
                StatusCodes.Status500InternalServerError,
                AppMessages.ErrorCodes.UnexpectedServerError,
                "middleware"
            );
            await ErrorResponseFactory.WriteAsync(
                context.Response,
                StatusCodes.Status500InternalServerError,
                AppMessages.Client.UnexpectedServerError,
                AppMessages.ErrorCodes.UnexpectedServerError
            );
        }
    }
}
