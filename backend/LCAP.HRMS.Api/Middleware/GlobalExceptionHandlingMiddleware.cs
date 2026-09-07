using LCAP.HRMS.Api.Contracts;
using System.ComponentModel.DataAnnotations;
using LCAP.HRMS.Application.Common.Exceptions;

namespace LCAP.HRMS.Api.Middleware;

public sealed class GlobalExceptionHandlingMiddleware(RequestDelegate next,
    ILogger<GlobalExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            context.Abort();
        }
        catch (Exception exception) when (exception is NotFoundException or ConflictException or ValidationException or ForbiddenException)
        {
            if (context.Response.HasStarted) throw;
            context.Response.Clear();
            context.Response.StatusCode = exception switch
            {
                ForbiddenException => StatusCodes.Status403Forbidden,
                NotFoundException => StatusCodes.Status404NotFound,
                ConflictException => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status400BadRequest
            };
            await context.Response.WriteAsJsonAsync(ApiResponse<object>.Failure(
                exception.Message, context.TraceIdentifier), context.RequestAborted);
        }
        catch (Exception exception)
        {
            logger.LogError("Unhandled exception for {Method} {Path}. TraceId: {TraceId}. Type: {ExceptionType}",
                context.Request.Method, context.Request.Path, context.TraceIdentifier, exception.GetType().Name);
            if (context.Response.HasStarted)
                throw;

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(ApiResponse<object>.Failure(
                "An unexpected error occurred.", context.TraceIdentifier), context.RequestAborted);
        }
    }
}
