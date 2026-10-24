using System.Text.Json;
using MaleMove.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace MaleMove.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    // Match the camelCase envelope that controllers return.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (AppException exception)
        {
            context.Response.StatusCode = exception.StatusCode;
            context.Response.ContentType = "application/json";
            var response = ApiResponse<object?>.Fail(exception.Message, [new ApiError(exception.Message, Code: exception.Code)]);
            await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions), context.RequestAborted);
        }
        catch (DbUpdateConcurrencyException)
        {
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            context.Response.ContentType = "application/json";
            var message = "This record changed while you were editing it. Refresh and review the latest version.";
            var response = ApiResponse<object?>.Fail(message, [new ApiError(message, Code: "revision_conflict")]);
            await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions), context.RequestAborted);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            logger.LogDebug("Request cancelled by client. TraceId={TraceId}", context.TraceIdentifier);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled request failure. TraceId={TraceId}", context.TraceIdentifier);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";
            var response = ApiResponse<object?>.Fail("An unexpected server error occurred.",
                [new ApiError("An unexpected server error occurred.", Code: "unexpected_error")]);
            await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions), context.RequestAborted);
        }
    }
}

