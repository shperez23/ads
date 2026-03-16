using System.Security.Claims;

namespace AdsManager.API.Middleware;

public sealed class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var tenantClaim = context.User.FindFirst("tenantId")?.Value;
            if (Guid.TryParse(tenantClaim, out var tenantId))
                context.Items["TenantId"] = tenantId;

            var userClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? context.User.FindFirst("sub")?.Value;

            if (Guid.TryParse(userClaim, out var userId))
                context.Items["UserId"] = userId;
        }

        await _next(context);
    }
}
