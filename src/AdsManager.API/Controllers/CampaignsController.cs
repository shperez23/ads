using AdsManager.Application.DTOs.Campaigns;
using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdsManager.API.Controllers;

[ApiController]
[Authorize]
[Route("api/campaigns")]
public sealed class CampaignsController : ControllerBase
{
    private readonly ICampaignService _campaignService;
    private readonly ITenantProvider _tenantProvider;

    public CampaignsController(ICampaignService campaignService, ITenantProvider tenantProvider)
    {
        _campaignService = campaignService;
        _tenantProvider = tenantProvider;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _campaignService.GetAllAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _campaignService.GetByIdAsync(id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCampaignRequest request, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _campaignService.CreateAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateCampaignRequest request, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _campaignService.UpdateAsync(id, request, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPut("{id:guid}/pause")]
    public async Task<IActionResult> Pause([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _campaignService.PauseAsync(id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPut("{id:guid}/activate")]
    public async Task<IActionResult> Activate([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _campaignService.ActivateAsync(id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }
}
