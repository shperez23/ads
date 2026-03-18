using AdsManager.API.Authorization;
using AdsManager.Application.Common;
using AdsManager.Application.DTOs.Common;
using AdsManager.Application.DTOs.Insights;
using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdsManager.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/reports")]
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
    [ProducesResponseType(typeof(Result<PagedResponse<InsightDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<PagedResponse<InsightDto>>>> GetInsights([FromQuery] InsightListRequest request, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _reportService.GetInsightsAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("dashboard")]
    [Authorize(Policy = AuthorizationPolicies.ReportsRead)]
    [Obsolete("Use GET /api/v1/dashboard. This endpoint will be removed in a future release.")]
    [ProducesResponseType(typeof(Result<DashboardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<ActionResult<Result<DashboardDto>>> GetDashboard([FromQuery] DateOnly? dateFrom, [FromQuery] DateOnly? dateTo, [FromQuery] Guid? campaignId, [FromQuery] Guid? adAccountId, CancellationToken cancellationToken)
        => DashboardEndpointHandler.HandleGetAsync(this, _dashboardService, _tenantProvider, dateFrom, dateTo, campaignId, adAccountId, cancellationToken, markAsDeprecated: true);
}
