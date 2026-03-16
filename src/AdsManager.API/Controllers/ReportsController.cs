using AdsManager.Application.DTOs.Insights;
using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AdsManager.API.Authorization;

namespace AdsManager.API.Controllers;

[ApiController]
[Authorize]
[Route("api/reports")]
public sealed class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly IDashboardService _dashboardService;
    private readonly ITenantProvider _tenantProvider;

    public ReportsController(IReportService reportService, IDashboardService dashboardService, ITenantProvider tenantProvider)
    {
        _reportService = reportService;
        _dashboardService = dashboardService;
        _tenantProvider = tenantProvider;
    }

    [HttpGet("insights")]
    [Authorize(Policy = AuthorizationPolicies.ReportsRead)]
    public async Task<IActionResult> GetInsights([FromQuery] DateOnly? dateFrom, [FromQuery] DateOnly? dateTo, [FromQuery] Guid? campaignId, [FromQuery] Guid? adAccountId, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _reportService.GetInsightsAsync(new DashboardFilter(dateFrom, dateTo, campaignId, adAccountId), cancellationToken);
        return Ok(result);
    }

    [HttpGet("dashboard")]
    [Authorize(Policy = AuthorizationPolicies.ReportsRead)]
    [Obsolete("Use GET /api/dashboard. This endpoint will be removed in a future release.")]
    public Task<IActionResult> GetDashboard([FromQuery] DateOnly? dateFrom, [FromQuery] DateOnly? dateTo, [FromQuery] Guid? campaignId, [FromQuery] Guid? adAccountId, CancellationToken cancellationToken)
        => DashboardEndpointHandler.HandleGetAsync(this, _dashboardService, _tenantProvider, dateFrom, dateTo, campaignId, adAccountId, cancellationToken, markAsDeprecated: true);
}
