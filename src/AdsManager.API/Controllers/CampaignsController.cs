using System.Security.Claims;
using AdsManager.Application.DTOs.Campaigns;
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

    public CampaignsController(ICampaignService campaignService)
    {
        _campaignService = campaignService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        if (!TryGetTenantId(out var tenantId))
            return Unauthorized();

        var result = await _campaignService.GetAllAsync(tenantId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetTenantId(out var tenantId))
            return Unauthorized();

        var result = await _campaignService.GetByIdAsync(tenantId, id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCampaignRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetTenantId(out var tenantId))
            return Unauthorized();

        var result = await _campaignService.CreateAsync(tenantId, request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateCampaignRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetTenantId(out var tenantId))
            return Unauthorized();

        var result = await _campaignService.UpdateAsync(tenantId, id, request, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPut("{id:guid}/pause")]
    public async Task<IActionResult> Pause([FromRoute] Guid id, [FromQuery] string accessToken, CancellationToken cancellationToken)
    {
        if (!TryGetTenantId(out var tenantId))
            return Unauthorized();

        var result = await _campaignService.PauseAsync(tenantId, id, accessToken, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPut("{id:guid}/activate")]
    public async Task<IActionResult> Activate([FromRoute] Guid id, [FromQuery] string accessToken, CancellationToken cancellationToken)
    {
        if (!TryGetTenantId(out var tenantId))
            return Unauthorized();

        var result = await _campaignService.ActivateAsync(tenantId, id, accessToken, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    private bool TryGetTenantId(out Guid tenantId)
    {
        var tenantClaim = User.FindFirstValue("tenantId");
        return Guid.TryParse(tenantClaim, out tenantId);
    }
}
