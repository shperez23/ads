using System.Security.Claims;
using AdsManager.Application.DTOs.Insights;
using AdsManager.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdsManager.API.Controllers;

[ApiController]
[Authorize]
[Route("api/dashboard")]
public sealed class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] DateOnly? dateFrom, [FromQuery] DateOnly? dateTo, [FromQuery] Guid? campaignId, [FromQuery] Guid? adAccountId, CancellationToken cancellationToken)
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
