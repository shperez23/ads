using AdsManager.Application.DTOs.Ads;
using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AdsManager.API.Authorization;

namespace AdsManager.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ads")]
public sealed class AdsController : ControllerBase
{
    private readonly IAdsService _adsService;
    private readonly ITenantProvider _tenantProvider;
    private readonly IReportService _reportService;

    public AdsController(IAdsService adsService, IReportService reportService, ITenantProvider tenantProvider)
    {
        _adsService = adsService;
        _reportService = reportService;
        _tenantProvider = tenantProvider;
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.AdsRead)]
    public async Task<IActionResult> GetAds([FromQuery] AdListRequest request, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _adsService.GetAdsAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.AdsRead)]
    public async Task<IActionResult> GetAdById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _adsService.GetAdByIdAsync(id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }


    [HttpGet("{id:guid}/insights")]
    [Authorize(Policy = AuthorizationPolicies.ReportsRead)]
    public async Task<IActionResult> GetInsightsByAd([FromRoute] Guid id, [FromQuery] DateOnly? dateFrom, [FromQuery] DateOnly? dateTo, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _reportService.GetAdInsightsAsync(id, dateFrom, dateTo, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.AdsWrite)]
    public async Task<IActionResult> Create([FromBody] CreateAdRequest request, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _adsService.CreateAdAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.AdsWrite)]
    public async Task<IActionResult> UpdateAd([FromRoute] Guid id, [FromBody] UpdateAdRequest request, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _adsService.UpdateAdAsync(id, request, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPut("{id:guid}/pause")]
    [Authorize(Policy = AuthorizationPolicies.AdsWrite)]
    public async Task<IActionResult> PauseAd([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _adsService.PauseAdAsync(id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPut("{id:guid}/activate")]
    [Authorize(Policy = AuthorizationPolicies.AdsWrite)]
    public async Task<IActionResult> ActivateAd([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _adsService.ActivateAdAsync(id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }
}
