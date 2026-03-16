using AdsManager.Application.Common;
using AdsManager.Application.DTOs.Auth;
using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Services;
using AdsManager.Domain.Entities;
using AdsManager.Domain.Enums;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace AdsManager.Application.Services;

public sealed class AuthService : IAuthService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IMapper _mapper;
    private readonly IAuthProtectionService _authProtectionService;
    private readonly ITenantProvider _tenantProvider;

    public AuthService(
        IApplicationDbContext dbContext,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenService refreshTokenService,
        IMapper mapper,
        IAuthProtectionService authProtectionService,
        ITenantProvider tenantProvider)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenService = refreshTokenService;
        _mapper = mapper;
        _authProtectionService = authProtectionService;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var ipAddress = _tenantProvider.GetClientIp();

        var protection = await _authProtectionService.CheckRegisterAttemptAsync(normalizedEmail, ipAddress, cancellationToken);
        if (protection.IsBlocked)
            return Result<AuthResponse>.Fail(protection.Message, BuildRetryAfterDetails(protection.RetryAfterUtc));

        if (await _dbContext.Users.AnyAsync(u => u.Email == normalizedEmail, cancellationToken))
        {
            await _authProtectionService.RecordAttemptAsync("register", null, normalizedEmail, ipAddress, false, "email_already_registered", cancellationToken);
            return Result<AuthResponse>.Fail("El correo ya está registrado");
        }

        if (await _dbContext.Tenants.AnyAsync(t => t.Slug == request.TenantSlug, cancellationToken))
        {
            await _authProtectionService.RecordAttemptAsync("register", null, normalizedEmail, ipAddress, false, "tenant_slug_exists", cancellationToken);
            return Result<AuthResponse>.Fail("El slug de tenant ya existe");
        }

        var adminRole = await _dbContext.Roles.FirstOrDefaultAsync(x => x.Name == "Admin", cancellationToken);
        if (adminRole is null)
        {
            await _authProtectionService.RecordAttemptAsync("register", null, normalizedEmail, ipAddress, false, "admin_role_not_found", cancellationToken);
            return Result<AuthResponse>.Fail("No se encontró el rol Admin. Ejecuta migraciones y seed.");
        }

        var tenant = new Tenant { Name = request.TenantName, Slug = request.TenantSlug, Status = TenantStatus.Active };
        var user = new User
        {
            Tenant = tenant,
            RoleId = adminRole.Id,
            Name = request.Name,
            Email = normalizedEmail,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Status = UserStatus.Active
        };

        _dbContext.Tenants.Add(tenant);
        _dbContext.Users.Add(user);

        var refreshToken = _refreshTokenService.GenerateToken();
        _dbContext.RefreshTokens.Add(CreateRefreshTokenEntity(user.Id, refreshToken));

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _authProtectionService.RecordAttemptAsync("register", user.Id, normalizedEmail, ipAddress, true, null, cancellationToken);

        user.RoleNavigation = adminRole;
        return Result<AuthResponse>.Ok(BuildAuthResponse(user, refreshToken), "Usuario registrado correctamente");
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var ipAddress = _tenantProvider.GetClientIp();

        var protection = await _authProtectionService.CheckLoginAttemptAsync(normalizedEmail, ipAddress, cancellationToken);
        if (protection.IsBlocked)
            return Result<AuthResponse>.Fail(protection.Message, BuildRetryAfterDetails(protection.RetryAfterUtc));

        var user = await _dbContext.Users
            .Include(x => x.RoleNavigation)
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            await _authProtectionService.RecordLoginAttemptAsync(user?.Id, normalizedEmail, ipAddress, false, "invalid_credentials", cancellationToken);
            return Result<AuthResponse>.Fail("Credenciales inválidas");
        }

        var refreshToken = _refreshTokenService.GenerateToken();
        _dbContext.RefreshTokens.Add(CreateRefreshTokenEntity(user.Id, refreshToken));

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _authProtectionService.RecordLoginAttemptAsync(user.Id, normalizedEmail, ipAddress, true, null, cancellationToken);

        return Result<AuthResponse>.Ok(BuildAuthResponse(user, refreshToken));
    }

    public async Task<Result<AuthResponse>> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var ipAddress = _tenantProvider.GetClientIp();
        var protection = await _authProtectionService.CheckRefreshAttemptAsync(ipAddress, cancellationToken);
        if (protection.IsBlocked)
            return Result<AuthResponse>.Fail(protection.Message, BuildRetryAfterDetails(protection.RetryAfterUtc));

        var tokenHash = _refreshTokenService.HashToken(request.RefreshToken);

        var existingRefreshToken = await _dbContext.RefreshTokens
            .Include(x => x.User)
            .ThenInclude(x => x.RoleNavigation)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash && !rt.IsRevoked, cancellationToken);

        var refreshEmail = existingRefreshToken?.User.Email ?? string.Empty;
        if (existingRefreshToken is null
            || existingRefreshToken.ExpiresAt <= DateTime.UtcNow
            || !_refreshTokenService.VerifyToken(request.RefreshToken, existingRefreshToken.TokenHash)
            || existingRefreshToken.User.Status != UserStatus.Active)
        {
            await _authProtectionService.RecordAttemptAsync("refresh", existingRefreshToken?.UserId, refreshEmail, ipAddress, false, "invalid_or_expired_refresh_token", cancellationToken);
            return Result<AuthResponse>.Fail("Refresh token inválido o expirado");
        }

        existingRefreshToken.IsRevoked = true;

        var newRefreshToken = _refreshTokenService.GenerateToken();
        _dbContext.RefreshTokens.Add(CreateRefreshTokenEntity(existingRefreshToken.UserId, newRefreshToken));

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _authProtectionService.RecordAttemptAsync("refresh", existingRefreshToken.UserId, existingRefreshToken.User.Email, ipAddress, true, null, cancellationToken);

        return Result<AuthResponse>.Ok(BuildAuthResponse(existingRefreshToken.User, newRefreshToken));
    }

    public async Task<Result<UserProfileDto>> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.AsNoTracking().Include(x => x.RoleNavigation).FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null) return Result<UserProfileDto>.Fail("Usuario no encontrado");

        return Result<UserProfileDto>.Ok(_mapper.Map<UserProfileDto>(user));
    }

    private RefreshToken CreateRefreshTokenEntity(Guid userId, string refreshToken)
        => new()
        {
            UserId = userId,
            TokenHash = _refreshTokenService.HashToken(refreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };

    private AuthResponse BuildAuthResponse(User user, string refreshToken) =>
        new(
            _jwtTokenGenerator.GenerateAccessToken(user),
            refreshToken,
            _jwtTokenGenerator.GetAccessTokenExpirationUtc(),
            _mapper.Map<UserProfileDto>(user));

    private static string BuildRetryAfterDetails(DateTime? retryAfterUtc)
        => retryAfterUtc.HasValue ? $"retryAfterUtc={retryAfterUtc.Value:O}" : string.Empty;
}
