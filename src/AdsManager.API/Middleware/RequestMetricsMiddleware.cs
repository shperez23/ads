using System.Diagnostics;
using AdsManager.Application.Interfaces;

namespace AdsManager.API.Middleware;

public sealed class RequestMetricsMiddleware
{
    private readonly RequestDelegate _next;

    public RequestMetricsMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IObservabilityMetrics observabilityMetrics)
    {
        var stopwatch = Stopwatch.StartNew();

        await _next(context);

        stopwatch.Stop();

        var route = context.GetEndpoint()?.DisplayName ?? context.Request.Path.Value ?? "unknown";
        var method = context.Request.Method;
        var statusCode = context.Response.StatusCode;

        observabilityMetrics.RecordHttpRequestDuration(stopwatch.Elapsed.TotalMilliseconds, method, route, statusCode);

        if (statusCode >= StatusCodes.Status400BadRequest)
        {
            observabilityMetrics.RecordHttpRequestError(method, route, statusCode);
        }
    }
}
