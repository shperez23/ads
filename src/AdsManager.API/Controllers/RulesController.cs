using AdsManager.API.Authorization;
using AdsManager.Application.Common;
using AdsManager.Application.DTOs.Common;
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
    [ProducesResponseType(typeof(Result<PagedResponse<RuleDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<PagedResponse<RuleDto>>>> GetAll([FromQuery] RuleListRequest request, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _ruleService.GetAllAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.RulesWrite)]
    [ProducesResponseType(typeof(Result<RuleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<RuleDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<RuleDto>>> Create([FromBody] CreateRuleRequest request, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _ruleService.CreateAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.RulesWrite)]
    [ProducesResponseType(typeof(Result<RuleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<RuleDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<RuleDto>>> Update([FromRoute] Guid id, [FromBody] UpdateRuleRequest request, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _ruleService.UpdateAsync(id, request, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPut("{id:guid}/activate")]
    [Authorize(Policy = AuthorizationPolicies.RulesWrite)]
    [ProducesResponseType(typeof(Result<RuleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<RuleDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<RuleDto>>> Activate([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _ruleService.ActivateAsync(id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPut("{id:guid}/deactivate")]
    [Authorize(Policy = AuthorizationPolicies.RulesWrite)]
    [ProducesResponseType(typeof(Result<RuleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<RuleDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<RuleDto>>> Deactivate([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _ruleService.DeactivateAsync(id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }
}
