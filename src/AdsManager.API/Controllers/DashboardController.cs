using AdsManager.API.Authorization;
using AdsManager.Application.Common;
using AdsManager.Application.DTOs.Insights;
using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdsManager.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/dashboard")]
public sealed class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ITenantProvider _tenantProvider;

    public DashboardController(IDashboardService dashboardService, ITenantProvider tenantProvider)
    {
        _dashboardService = dashboardService;
        _tenantProvider = tenantProvider;
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.ReportsRead)]
    [ProducesResponseType(typeof(Result<DashboardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<ActionResult<Result<DashboardDto>>> Get([FromQuery] DateOnly? dateFrom, [FromQuery] DateOnly? dateTo, [FromQuery] Guid? campaignId, [FromQuery] Guid? adAccountId, CancellationToken cancellationToken)
        => DashboardEndpointHandler.HandleGetAsync(this, _dashboardService, _tenantProvider, dateFrom, dateTo, campaignId, adAccountId, cancellationToken);
}
