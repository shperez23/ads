using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdsManager.API.Controllers;

[ApiController]
[Authorize]
[Route("api/adaccounts")]
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
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _adAccountService.GetAllAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost("import-from-meta")]
    public async Task<IActionResult> ImportFromMeta(CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _adAccountService.ImportFromMetaAsync(cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{id:guid}/sync")]
    public async Task<IActionResult> Sync([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _adAccountService.SyncAsync(id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }
}
