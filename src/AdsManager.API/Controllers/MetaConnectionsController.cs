using AdsManager.API.Authorization;
using AdsManager.Application.Common;
using AdsManager.Application.DTOs.Meta;
using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdsManager.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/meta/connections")]
public sealed class MetaConnectionsController : ControllerBase
{
    private readonly IMetaConnectionService _metaConnectionService;
    private readonly ITenantProvider _tenantProvider;

    public MetaConnectionsController(IMetaConnectionService metaConnectionService, ITenantProvider tenantProvider)
    {
        _metaConnectionService = metaConnectionService;
        _tenantProvider = tenantProvider;
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.MetaConnectionsManage)]
    [ProducesResponseType(typeof(Result<IReadOnlyCollection<MetaConnectionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<IReadOnlyCollection<MetaConnectionDto>>>> GetConnections(CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _metaConnectionService.GetConnectionsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.MetaConnectionsManage)]
    [ProducesResponseType(typeof(Result<MetaConnectionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<MetaConnectionDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<MetaConnectionDto>>> CreateConnection([FromBody] CreateMetaConnectionRequest request, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _metaConnectionService.CreateConnectionAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.MetaConnectionsManage)]
    [ProducesResponseType(typeof(Result<MetaConnectionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<MetaConnectionDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<MetaConnectionDto>>> UpdateConnection([FromRoute] Guid id, [FromBody] UpdateMetaConnectionRequest request, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _metaConnectionService.UpdateConnectionAsync(id, request, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.MetaConnectionsManage)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<bool>>> DeleteConnection([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _metaConnectionService.DeleteConnectionAsync(id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost("{id:guid}/refresh-token")]
    [Authorize(Policy = AuthorizationPolicies.MetaConnectionsManage)]
    [ProducesResponseType(typeof(Result<MetaConnectionTokenRefreshResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<MetaConnectionTokenRefreshResultDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<MetaConnectionTokenRefreshResultDto>>> RefreshToken([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _metaConnectionService.RefreshTokenAsync(id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost("{id:guid}/validate")]
    [Authorize(Policy = AuthorizationPolicies.MetaConnectionsManage)]
    [ProducesResponseType(typeof(Result<MetaConnectionValidationResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<MetaConnectionValidationResultDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<MetaConnectionValidationResultDto>>> ValidateConnection([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _metaConnectionService.ValidateConnectionAsync(id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }
}
