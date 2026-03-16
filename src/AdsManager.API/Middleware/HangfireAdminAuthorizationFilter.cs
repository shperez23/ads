using Hangfire.Annotations;
using Hangfire.Dashboard;
using System.Security.Claims;

namespace AdsManager.API.Middleware;

public sealed class HangfireAdminAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize([NotNull] DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var user = httpContext.User;

        return user.Identity?.IsAuthenticated == true
            && user.Claims.Any(claim =>
                claim.Type == ClaimTypes.Role
                && string.Equals(claim.Value, "Admin", StringComparison.OrdinalIgnoreCase));
    }
}
