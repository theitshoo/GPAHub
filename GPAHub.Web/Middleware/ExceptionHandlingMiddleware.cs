using GPAHub.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GPAHub.Web.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
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
            DbUpdateConcurrencyException => (
                StatusCodes.Status409Conflict,
                "This record was modified by someone else. Reload your changes and try again.",
                false),
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

        if (status == StatusCodes.Status409Conflict && exception is DbUpdateConcurrencyException)
        {
            problem.Extensions["code"] = "concurrency_conflict";
        }

        if (includeDetails && !string.IsNullOrWhiteSpace(exception.Message))
        {
            problem.Detail = exception.Message;
        }

        if (_environment.IsDevelopment() && status == StatusCodes.Status500InternalServerError)
        {
            problem.Extensions["exception"] = exception.ToString();
        }

        await context.Response.WriteAsJsonAsync(problem);
    }
}
