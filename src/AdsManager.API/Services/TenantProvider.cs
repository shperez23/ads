using System.Security.Claims;
using AdsManager.Application.Interfaces;

namespace AdsManager.API.Services;

public sealed class TenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? GetTenantId()
    {
        var tenantClaim = _httpContextAccessor.HttpContext?.User.FindFirstValue("tenantId");
        return Guid.TryParse(tenantClaim, out var tenantId) ? tenantId : null;
    }

    public Guid? GetUserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var userClaim = user?.FindFirstValue(ClaimTypes.NameIdentifier) ?? user?.FindFirstValue("sub");
        return Guid.TryParse(userClaim, out var userId) ? userId : null;
    }

    public string GetTraceId()
        => _httpContextAccessor.HttpContext?.TraceIdentifier ?? string.Empty;
}
