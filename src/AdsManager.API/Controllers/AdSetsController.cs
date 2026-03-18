using AdsManager.API.Authorization;
using AdsManager.Application.Common;
using AdsManager.Application.DTOs.AdSets;
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
[Route("api/v{version:apiVersion}/adsets")]
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
    [ProducesResponseType(typeof(Result<PagedResponse<AdSetDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<PagedResponse<AdSetDto>>>> GetAdSets([FromQuery] AdSetListRequest request, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _adSetService.GetAdSetsAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.AdSetsRead)]
    [ProducesResponseType(typeof(Result<AdSetDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<AdSetDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<AdSetDto>>> GetAdSetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _adSetService.GetAdSetByIdAsync(id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpGet("{id:guid}/insights")]
    [Authorize(Policy = AuthorizationPolicies.ReportsRead)]
    [ProducesResponseType(typeof(Result<IReadOnlyCollection<InsightDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<IReadOnlyCollection<InsightDto>>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<IReadOnlyCollection<InsightDto>>>> GetInsightsByAdSet([FromRoute] Guid id, [FromQuery] DateOnly? dateFrom, [FromQuery] DateOnly? dateTo, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _reportService.GetAdSetInsightsAsync(id, dateFrom, dateTo, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.AdSetsWrite)]
    [ProducesResponseType(typeof(Result<AdSetDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<AdSetDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<AdSetDto>>> Create([FromBody] CreateAdSetRequest request, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _adSetService.CreateAdSetAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.AdSetsWrite)]
    [ProducesResponseType(typeof(Result<AdSetDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<AdSetDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<AdSetDto>>> UpdateAdSet([FromRoute] Guid id, [FromBody] UpdateAdSetRequest request, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _adSetService.UpdateAdSetAsync(id, request, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPut("{id:guid}/pause")]
    [Authorize(Policy = AuthorizationPolicies.AdSetsWrite)]
    [ProducesResponseType(typeof(Result<AdSetDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<AdSetDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<AdSetDto>>> PauseAdSet([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _adSetService.PauseAdSetAsync(id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPut("{id:guid}/activate")]
    [Authorize(Policy = AuthorizationPolicies.AdSetsWrite)]
    [ProducesResponseType(typeof(Result<AdSetDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<AdSetDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<AdSetDto>>> ActivateAdSet([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _adSetService.ActivateAdSetAsync(id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }
}
