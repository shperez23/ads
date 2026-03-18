using AdsManager.Application.Common;
using AdsManager.Application.DTOs.Auth;
using AdsManager.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AdsManager.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ITenantProvider _tenantProvider;

    public AuthController(IAuthService authService, ITenantProvider tenantProvider)
    {
        _authService = authService;
        _tenantProvider = tenantProvider;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthRegister")]
    [ProducesResponseType(typeof(Result<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<AuthResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Result<AuthResponse>>> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthLogin")]
    [ProducesResponseType(typeof(Result<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<AuthResponse>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<AuthResponse>>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request, cancellationToken);
        return result.Success ? Ok(result) : Unauthorized(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthRefresh")]
    [ProducesResponseType(typeof(Result<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<AuthResponse>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<AuthResponse>>> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.RefreshAsync(request, cancellationToken);
        return result.Success ? Ok(result) : Unauthorized(result);
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(Result<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<UserProfileDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<UserProfileDto>>> Me(CancellationToken cancellationToken)
    {
        var userId = _tenantProvider.GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var result = await _authService.GetCurrentUserAsync(userId.Value, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }
}
