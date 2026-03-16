using Serilog.Context;

namespace AdsManager.API.Middleware;

public sealed class TraceContextMiddleware
{
    private const string TraceHeaderName = "X-Trace-Id";
    private readonly RequestDelegate _next;

    public TraceContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var traceId = context.TraceIdentifier;
        context.Response.Headers[TraceHeaderName] = traceId;

        using (LogContext.PushProperty("TraceId", traceId))
        {
            await _next(context);
        }
    }
}
