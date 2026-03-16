using AdsManager.Application.DTOs.Insights;
using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace AdsManager.API.Controllers;

internal static class DashboardEndpointHandler
{
    public static async Task<IActionResult> HandleGetAsync(
        ControllerBase controller,
        IDashboardService dashboardService,
        ITenantProvider tenantProvider,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        Guid? campaignId,
        Guid? adAccountId,
        CancellationToken cancellationToken,
        bool markAsDeprecated = false)
    {
        if (!tenantProvider.GetTenantId().HasValue)
            return controller.Unauthorized();

        if (markAsDeprecated)
        {
            controller.Response.Headers["Deprecation"] = "true";
            controller.Response.Headers["Link"] = "</api/dashboard>; rel=\"successor-version\"";
        }

        var filter = new DashboardFilter(dateFrom, dateTo, campaignId, adAccountId);
        var result = await dashboardService.GetDashboardAsync(filter, cancellationToken);
        return controller.Ok(result);
    }
}
