using GPAHub.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace GPAHub.Web.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (Exception exception)
        {
            await HandleAsync(context, exception);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var (status, title, includeDetails) = exception switch
        {
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Authentication is required.", false),
            DomainException => (StatusCodes.Status409Conflict, "The request violates a business rule.", true),
            OperationCanceledException => (StatusCodes.Status499ClientClosedRequest, "Request cancelled.", false),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.", false)
        };

        if (status >= 500)
        {
            _logger.LogError(exception, "Unhandled exception while processing {Path}", context.Request.Path);
        }
        else
        {
            _logger.LogWarning("Request to {Path} failed: {Message}", context.Request.Path, exception.Message);
        }

        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = status;

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Instance = context.Request.Path
        };

        if (includeDetails && !string.IsNullOrWhiteSpace(exception.Message))
        {
            problem.Detail = exception.Message;
        }

        await context.Response.WriteAsJsonAsync(problem);
    }
}
