using AdsManager.API.Authorization;
using AdsManager.Application.Common;
using AdsManager.Application.DTOs.AdAccounts;
using AdsManager.Application.DTOs.Common;
using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    [ProducesResponseType(typeof(Result<PagedResponse<AdAccountDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<PagedResponse<AdAccountDto>>>> GetAll([FromQuery] AdAccountListRequest request, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _adAccountService.GetAllAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("import-from-meta")]
    [Authorize(Policy = AuthorizationPolicies.AdAccountsManage)]
    [ProducesResponseType(typeof(Result<IReadOnlyCollection<AdAccountDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<IReadOnlyCollection<AdAccountDto>>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<IReadOnlyCollection<AdAccountDto>>>> ImportFromMeta(CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _adAccountService.ImportFromMetaAsync(cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{id:guid}/sync")]
    [Authorize(Policy = AuthorizationPolicies.AdAccountsManage)]
    [ProducesResponseType(typeof(Result<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<string>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<string>>> Sync([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _adAccountService.SyncAsync(id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }
}
