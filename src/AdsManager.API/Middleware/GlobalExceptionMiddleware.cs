using System.Diagnostics;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace AdsManager.API.Middleware;

public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var problemDetails = BuildProblemDetails(context, ex);
            var traceId = context.TraceIdentifier;

            _logger.LogError(ex, "Unhandled exception mapped to status code {StatusCode}. TraceId {TraceId}", problemDetails.Status, traceId);

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

            await context.Response.WriteAsJsonAsync(problemDetails);
        }
    }

    private ProblemDetails BuildProblemDetails(HttpContext context, Exception exception)
    {
        var (status, title, type, detail) = exception switch
        {
            ValidationException => (
                StatusCodes.Status400BadRequest,
                "Validation Error",
                "https://httpstatuses.com/400",
                "One or more validation errors occurred."),
            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                "https://httpstatuses.com/401",
                "Authentication is required to access this resource."),
            System.Security.SecurityException => (
                StatusCodes.Status403Forbidden,
                "Forbidden",
                "https://httpstatuses.com/403",
                "You do not have permission to access this resource."),
            KeyNotFoundException => (
                StatusCodes.Status404NotFound,
                "Not Found",
                "https://httpstatuses.com/404",
                exception.Message),
            InvalidOperationException => (
                StatusCodes.Status409Conflict,
                "Conflict",
                "https://httpstatuses.com/409",
                exception.Message),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Unexpected Error",
                "https://httpstatuses.com/500",
                _environment.IsDevelopment() ? exception.Message : "An unexpected error occurred. Contact support with the traceId.")
        };

        var problemDetails = new ProblemDetails
        {
            Type = type,
            Title = title,
            Status = status,
            Detail = detail,
            Instance = context.Request.Path
        };

        problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;

        if (exception is ValidationException validationException)
        {
            problemDetails.Extensions["errors"] = validationException.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(error => error.ErrorMessage).ToArray());
        }

        return problemDetails;
    }
}
