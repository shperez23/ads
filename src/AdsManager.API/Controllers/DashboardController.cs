using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AdsManager.API.Authorization;

namespace AdsManager.API.Controllers;

[ApiController]
[Authorize]
[Route("api/dashboard")]
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
    public Task<IActionResult> Get([FromQuery] DateOnly? dateFrom, [FromQuery] DateOnly? dateTo, [FromQuery] Guid? campaignId, [FromQuery] Guid? adAccountId, CancellationToken cancellationToken)
        => DashboardEndpointHandler.HandleGetAsync(this, _dashboardService, _tenantProvider, dateFrom, dateTo, campaignId, adAccountId, cancellationToken);
}
