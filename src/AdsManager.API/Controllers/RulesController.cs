using AdsManager.API.Authorization;
using AdsManager.Application.DTOs.Rules;
using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdsManager.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/rules")]
public sealed class RulesController : ControllerBase
{
    private readonly IRuleService _ruleService;
    private readonly ITenantProvider _tenantProvider;

    public RulesController(IRuleService ruleService, ITenantProvider tenantProvider)
    {
        _ruleService = ruleService;
        _tenantProvider = tenantProvider;
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.RulesRead)]
    public async Task<IActionResult> GetAll([FromQuery] RuleListRequest request, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _ruleService.GetAllAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.RulesWrite)]
    public async Task<IActionResult> Create([FromBody] CreateRuleRequest request, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _ruleService.CreateAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.RulesWrite)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateRuleRequest request, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _ruleService.UpdateAsync(id, request, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPut("{id:guid}/activate")]
    [Authorize(Policy = AuthorizationPolicies.RulesWrite)]
    public async Task<IActionResult> Activate([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _ruleService.ActivateAsync(id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPut("{id:guid}/deactivate")]
    [Authorize(Policy = AuthorizationPolicies.RulesWrite)]
    public async Task<IActionResult> Deactivate([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _ruleService.DeactivateAsync(id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }
}
