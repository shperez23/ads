using System.Text.Json;

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
            var traceId = context.TraceIdentifier;
            _logger.LogError(ex, "Unhandled exception. TraceId {TraceId}", traceId);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            object response;

            if (_environment.IsDevelopment())
            {
                response = new
                {
                    success = false,
                    message = "Ocurrió un error inesperado",
                    details = ex.Message,
                    stackTrace = ex.StackTrace,
                    traceId
                };
            }
            else
            {
                response = new
                {
                    success = false,
                    message = "Ocurrió un error inesperado",
                    details = "Contacte al administrador con el traceId",
                    traceId
                };
            }

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}
