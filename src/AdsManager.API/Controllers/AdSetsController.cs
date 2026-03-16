using AdsManager.Application.DTOs.AdSets;
using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AdsManager.API.Authorization;

namespace AdsManager.API.Controllers;

[ApiController]
[Authorize]
[Route("api/adsets")]
public sealed class AdSetsController : ControllerBase
{
    private readonly IAdSetService _adSetService;
    private readonly ITenantProvider _tenantProvider;
    private readonly IReportService _reportService;

    public AdSetsController(IAdSetService adSetService, IReportService reportService, ITenantProvider tenantProvider)
    {
        _adSetService = adSetService;
        _reportService = reportService;
        _tenantProvider = tenantProvider;
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.AdSetsRead)]
    public async Task<IActionResult> GetAdSets([FromQuery] AdSetListRequest request, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _adSetService.GetAdSetsAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.AdSetsRead)]
    public async Task<IActionResult> GetAdSetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _adSetService.GetAdSetByIdAsync(id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }


    [HttpGet("{id:guid}/insights")]
    [Authorize(Policy = AuthorizationPolicies.ReportsRead)]
    public async Task<IActionResult> GetInsightsByAdSet([FromRoute] Guid id, [FromQuery] DateOnly? dateFrom, [FromQuery] DateOnly? dateTo, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _reportService.GetAdSetInsightsAsync(id, dateFrom, dateTo, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.AdSetsWrite)]
    public async Task<IActionResult> Create([FromBody] CreateAdSetRequest request, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _adSetService.CreateAdSetAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.AdSetsWrite)]
    public async Task<IActionResult> UpdateAdSet([FromRoute] Guid id, [FromBody] UpdateAdSetRequest request, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _adSetService.UpdateAdSetAsync(id, request, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPut("{id:guid}/pause")]
    [Authorize(Policy = AuthorizationPolicies.AdSetsWrite)]
    public async Task<IActionResult> PauseAdSet([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _adSetService.PauseAdSetAsync(id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPut("{id:guid}/activate")]
    [Authorize(Policy = AuthorizationPolicies.AdSetsWrite)]
    public async Task<IActionResult> ActivateAdSet([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _adSetService.ActivateAdSetAsync(id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }
}
