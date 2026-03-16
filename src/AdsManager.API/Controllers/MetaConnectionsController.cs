using AdsManager.Application.DTOs.Meta;
using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AdsManager.API.Authorization;

namespace AdsManager.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/meta/connections")]
[Route("api/meta/connections")]
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
    public async Task<IActionResult> GetConnections(CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _metaConnectionService.GetConnectionsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.MetaConnectionsManage)]
    public async Task<IActionResult> CreateConnection([FromBody] CreateMetaConnectionRequest request, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _metaConnectionService.CreateConnectionAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.MetaConnectionsManage)]
    public async Task<IActionResult> UpdateConnection([FromRoute] Guid id, [FromBody] UpdateMetaConnectionRequest request, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _metaConnectionService.UpdateConnectionAsync(id, request, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.MetaConnectionsManage)]
    public async Task<IActionResult> DeleteConnection([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _metaConnectionService.DeleteConnectionAsync(id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }


    [HttpPost("{id:guid}/refresh-token")]
    [Authorize(Policy = AuthorizationPolicies.MetaConnectionsManage)]
    public async Task<IActionResult> RefreshToken([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _metaConnectionService.RefreshTokenAsync(id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost("{id:guid}/validate")]
    [Authorize(Policy = AuthorizationPolicies.MetaConnectionsManage)]
    public async Task<IActionResult> ValidateConnection([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _metaConnectionService.ValidateConnectionAsync(id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }
}
