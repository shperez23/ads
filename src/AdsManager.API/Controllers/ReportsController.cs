using System.Security.Claims;
using AdsManager.Application.DTOs.Insights;
using AdsManager.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdsManager.API.Controllers;

[ApiController]
[Authorize]
[Route("api/reports")]
public sealed class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly IDashboardService _dashboardService;

    public ReportsController(IReportService reportService, IDashboardService dashboardService)
    {
        _reportService = reportService;
        _dashboardService = dashboardService;
    }

    [HttpGet("insights")]
    public async Task<IActionResult> GetInsights([FromQuery] DateOnly? dateFrom, [FromQuery] DateOnly? dateTo, [FromQuery] Guid? campaignId, [FromQuery] Guid? adAccountId, CancellationToken cancellationToken)
    {
        if (!TryGetTenantId(out var tenantId))
            return Unauthorized();

        var result = await _reportService.GetInsightsAsync(tenantId, new DashboardFilter(dateFrom, dateTo, campaignId, adAccountId), cancellationToken);
        return Ok(result);
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard([FromQuery] DateOnly? dateFrom, [FromQuery] DateOnly? dateTo, [FromQuery] Guid? campaignId, [FromQuery] Guid? adAccountId, CancellationToken cancellationToken)
    {
        if (!TryGetTenantId(out var tenantId))
            return Unauthorized();

        var result = await _dashboardService.GetDashboardAsync(tenantId, new DashboardFilter(dateFrom, dateTo, campaignId, adAccountId), cancellationToken);
        return Ok(result);
    }

    private bool TryGetTenantId(out Guid tenantId)
    {
        var tenantClaim = User.FindFirstValue("tenantId");
        return Guid.TryParse(tenantClaim, out tenantId);
    }
}
