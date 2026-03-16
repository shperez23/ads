using Hangfire.Annotations;
using Hangfire.Dashboard;
using System.Net;
using System.Security.Claims;

namespace AdsManager.API.Middleware;

public sealed class HangfireAdminAuthorizationFilter : IDashboardAuthorizationFilter
{
    private readonly HashSet<IPAddress> _allowlistedIps;

    public HangfireAdminAuthorizationFilter(IEnumerable<IPAddress>? allowlistedIps = null)
    {
        _allowlistedIps = allowlistedIps is null
            ? []
            : [.. allowlistedIps];
    }

    public bool Authorize([NotNull] DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var user = httpContext.User;

        var isAdmin = user.Identity?.IsAuthenticated == true
            && user.Claims.Any(claim =>
                claim.Type == ClaimTypes.Role
                && string.Equals(claim.Value, "Admin", StringComparison.OrdinalIgnoreCase));

        if (!isAdmin)
        {
            return false;
        }

        if (_allowlistedIps.Count == 0)
        {
            return true;
        }

        var remoteIp = httpContext.Connection.RemoteIpAddress;
        if (remoteIp is null)
        {
            return false;
        }

        if (remoteIp.IsIPv4MappedToIPv6)
        {
            remoteIp = remoteIp.MapToIPv4();
        }

        return _allowlistedIps.Contains(remoteIp);
    }
}
