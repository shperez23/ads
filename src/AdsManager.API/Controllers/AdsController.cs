using AdsManager.Application.DTOs.Ads;
using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdsManager.API.Controllers;

[ApiController]
[Authorize]
[Route("api/ads")]
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
    public async Task<IActionResult> GetAds(CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _adsService.GetAdsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetAdById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _adsService.GetAdByIdAsync(id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }


    [HttpGet("{id:guid}/insights")]
    public async Task<IActionResult> GetInsightsByAd([FromRoute] Guid id, [FromQuery] DateOnly? dateFrom, [FromQuery] DateOnly? dateTo, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _reportService.GetAdInsightsAsync(id, dateFrom, dateTo, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAdRequest request, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _adsService.CreateAdAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateAd([FromRoute] Guid id, [FromBody] UpdateAdRequest request, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _adsService.UpdateAdAsync(id, request, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPut("{id:guid}/pause")]
    public async Task<IActionResult> PauseAd([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _adsService.PauseAdAsync(id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPut("{id:guid}/activate")]
    public async Task<IActionResult> ActivateAd([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _adsService.ActivateAdAsync(id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }
}
