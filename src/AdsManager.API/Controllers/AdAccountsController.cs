using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Services;
using AdsManager.Application.DTOs.AdAccounts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AdsManager.API.Authorization;

namespace AdsManager.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/adaccounts")]
public sealed class AdAccountsController : ControllerBase
{
    private readonly IAdAccountService _adAccountService;
    private readonly ITenantProvider _tenantProvider;

    public AdAccountsController(IAdAccountService adAccountService, ITenantProvider tenantProvider)
    {
        _adAccountService = adAccountService;
        _tenantProvider = tenantProvider;
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.AdAccountsManage)]
    public async Task<IActionResult> GetAll([FromQuery] AdAccountListRequest request, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _adAccountService.GetAllAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("import-from-meta")]
    [Authorize(Policy = AuthorizationPolicies.AdAccountsManage)]
    public async Task<IActionResult> ImportFromMeta(CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _adAccountService.ImportFromMetaAsync(cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{id:guid}/sync")]
    [Authorize(Policy = AuthorizationPolicies.AdAccountsManage)]
    public async Task<IActionResult> Sync([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _adAccountService.SyncAsync(id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }
}
