using AdsManager.Application.Common;
using AdsManager.Application.DTOs.Auth;
using AdsManager.Application.Interfaces;
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
    private readonly IMapper _mapper;

    public AuthService(IApplicationDbContext dbContext, IPasswordHasher passwordHasher, IJwtTokenGenerator jwtTokenGenerator, IMapper mapper)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _mapper = mapper;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        if (await _dbContext.Users.AnyAsync(u => u.Email == request.Email, cancellationToken))
            return Result<AuthResponse>.Fail("El correo ya está registrado");

        if (await _dbContext.Tenants.AnyAsync(t => t.Slug == request.TenantSlug, cancellationToken))
            return Result<AuthResponse>.Fail("El slug de tenant ya existe");

        var tenant = new Tenant { Name = request.TenantName, Slug = request.TenantSlug, Status = TenantStatus.Active };
        var user = new User
        {
            Tenant = tenant,
            Name = request.Name,
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = UserRole.Admin,
            Status = UserStatus.Active
        };

        _dbContext.Tenants.Add(tenant);
        _dbContext.Users.Add(user);

        var refreshToken = new RefreshToken
        {
            User = user,
            Token = _jwtTokenGenerator.GenerateRefreshToken(),
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<AuthResponse>.Ok(BuildAuthResponse(user, refreshToken.Token), "Usuario registrado correctamente");
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email.Trim().ToLowerInvariant(), cancellationToken);
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            return Result<AuthResponse>.Fail("Credenciales inválidas");

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = _jwtTokenGenerator.GenerateRefreshToken(),
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<AuthResponse>.Ok(BuildAuthResponse(user, refreshToken.Token));
    }

    public async Task<Result<AuthResponse>> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var existingRefreshToken = await _dbContext.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken && !rt.IsRevoked, cancellationToken);

        if (existingRefreshToken is null || existingRefreshToken.ExpiresAt <= DateTime.UtcNow)
            return Result<AuthResponse>.Fail("Refresh token inválido o expirado");

        existingRefreshToken.IsRevoked = true;

        var newRefreshToken = new RefreshToken
        {
            UserId = existingRefreshToken.UserId,
            Token = _jwtTokenGenerator.GenerateRefreshToken(),
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        _dbContext.RefreshTokens.Add(newRefreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<AuthResponse>.Ok(BuildAuthResponse(existingRefreshToken.User, newRefreshToken.Token));
    }

    public async Task<Result<UserProfileDto>> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null) return Result<UserProfileDto>.Fail("Usuario no encontrado");

        return Result<UserProfileDto>.Ok(_mapper.Map<UserProfileDto>(user));
    }

    private AuthResponse BuildAuthResponse(User user, string refreshToken) =>
        new(
            _jwtTokenGenerator.GenerateAccessToken(user),
            refreshToken,
            _jwtTokenGenerator.GetAccessTokenExpirationUtc(),
            _mapper.Map<UserProfileDto>(user));
}
