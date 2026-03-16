using System.Text.Json;
using AdsManager.Application.Configuration;
using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Services;
using AdsManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AdsManager.Application.Services;

public sealed class AuthProtectionService : IAuthProtectionService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAuditService _auditService;
    private readonly AuthProtectionOptions _options;

    public AuthProtectionService(IApplicationDbContext dbContext, IAuditService auditService, IOptions<AuthProtectionOptions> options)
    {
        _dbContext = dbContext;
        _auditService = auditService;
        _options = options.Value;
    }

    public async Task<AuthProtectionDecision> CheckLoginAttemptAsync(string email, string ipAddress, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var normalizedEmail = NormalizeEmail(email);

        var lockout = await _dbContext.AuthLockoutStates
            .FirstOrDefaultAsync(x => x.Email == normalizedEmail && x.IpAddress == ipAddress, cancellationToken);

        if (lockout?.LockoutUntil is not null && lockout.LockoutUntil > now)
            return new AuthProtectionDecision(true, "Autenticación temporalmente bloqueada por múltiples intentos fallidos.", lockout.LockoutUntil);

        var minuteStart = now.AddMinutes(-1);
        var failedByIp = await _dbContext.AuthAttemptLogs
            .CountAsync(x => x.AttemptType == "login" && x.AttemptedAt >= minuteStart && !x.Success && x.IpAddress == ipAddress, cancellationToken);
        var failedByEmail = await _dbContext.AuthAttemptLogs
            .CountAsync(x => x.AttemptType == "login" && x.AttemptedAt >= minuteStart && !x.Success && x.Email == normalizedEmail, cancellationToken);

        if (failedByIp >= _options.LoginPerMinute || failedByEmail >= _options.LoginPerMinute)
            return new AuthProtectionDecision(true, "Demasiados intentos de login. Intenta nuevamente en un minuto.", now.AddMinutes(1));

        return new AuthProtectionDecision(false, string.Empty);
    }

    public async Task<AuthProtectionDecision> CheckRefreshAttemptAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var minuteStart = now.AddMinutes(-1);

        var attempts = await _dbContext.AuthAttemptLogs
            .CountAsync(x => x.AttemptType == "refresh" && x.AttemptedAt >= minuteStart && x.IpAddress == ipAddress, cancellationToken);

        if (attempts >= _options.RefreshPerMinute)
            return new AuthProtectionDecision(true, "Demasiados intentos de refresh. Intenta nuevamente en un minuto.", now.AddMinutes(1));

        return new AuthProtectionDecision(false, string.Empty);
    }

    public async Task<AuthProtectionDecision> CheckRegisterAttemptAsync(string email, string ipAddress, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var normalizedEmail = NormalizeEmail(email);
        var hourStart = now.AddHours(-1);

        var attemptsByIp = await _dbContext.AuthAttemptLogs
            .CountAsync(x => x.AttemptType == "register" && x.AttemptedAt >= hourStart && x.IpAddress == ipAddress, cancellationToken);
        var attemptsByEmail = await _dbContext.AuthAttemptLogs
            .CountAsync(x => x.AttemptType == "register" && x.AttemptedAt >= hourStart && x.Email == normalizedEmail, cancellationToken);

        if (attemptsByIp >= _options.RegisterPerHour || attemptsByEmail >= _options.RegisterPerHour)
            return new AuthProtectionDecision(true, "Demasiados intentos de registro. Intenta nuevamente más tarde.", now.AddHours(1));

        return new AuthProtectionDecision(false, string.Empty);
    }

    public async Task RecordLoginAttemptAsync(Guid? userId, string email, string ipAddress, bool success, string? failureReason, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var normalizedEmail = NormalizeEmail(email);

        _dbContext.AuthAttemptLogs.Add(new AuthAttemptLog
        {
            UserId = userId,
            Email = normalizedEmail,
            IpAddress = ipAddress,
            AttemptedAt = now,
            Success = success,
            AttemptType = "login",
            FailureReason = failureReason
        });

        var lockout = await _dbContext.AuthLockoutStates
            .FirstOrDefaultAsync(x => x.Email == normalizedEmail && x.IpAddress == ipAddress, cancellationToken);

        if (success)
        {
            if (lockout is not null)
                _dbContext.AuthLockoutStates.Remove(lockout);

            await _dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        if (lockout is null)
        {
            lockout = new AuthLockoutState
            {
                UserId = userId,
                Email = normalizedEmail,
                IpAddress = ipAddress,
                FailedAttempts = 0
            };
            _dbContext.AuthLockoutStates.Add(lockout);
        }

        if (lockout.LastFailedAt is null || lockout.LastFailedAt < now.AddMinutes(-_options.FailedAttemptsWindowMinutes))
            lockout.FailedAttempts = 0;

        lockout.UserId = userId;
        lockout.FailedAttempts += 1;
        lockout.LastFailedAt = now;

        if (lockout.FailedAttempts >= _options.FailedAttemptsThreshold)
        {
            var level = lockout.FailedAttempts - _options.FailedAttemptsThreshold + 1;
            var lockoutMinutes = Math.Min(_options.LockoutBaseMinutes * (int)Math.Pow(2, Math.Max(0, level - 1)), _options.LockoutMaxMinutes);
            lockout.LockoutUntil = now.AddMinutes(lockoutMinutes);

            await _auditService.LogAsync(
                userId,
                Guid.Empty,
                "auth lockout",
                nameof(AuthLockoutState),
                lockout.Id.ToString(),
                JsonSerializer.Serialize(new
                {
                    lockout.Email,
                    lockout.IpAddress,
                    lockout.FailedAttempts,
                    lockout.LockoutUntil
                }),
                cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            userId,
            Guid.Empty,
            "auth failed login",
            nameof(AuthAttemptLog),
            string.Empty,
            JsonSerializer.Serialize(new
            {
                Email = normalizedEmail,
                IpAddress = ipAddress,
                AttemptedAt = now,
                Success = false,
                failureReason
            }),
            cancellationToken);
    }


    public async Task RecordAttemptAsync(string attemptType, Guid? userId, string email, string ipAddress, bool success, string? failureReason, CancellationToken cancellationToken = default)
    {
        _dbContext.AuthAttemptLogs.Add(new AuthAttemptLog
        {
            UserId = userId,
            Email = NormalizeEmail(email),
            IpAddress = ipAddress,
            AttemptedAt = DateTime.UtcNow,
            Success = success,
            AttemptType = attemptType,
            FailureReason = failureReason
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizeEmail(string email)
        => email.Trim().ToLowerInvariant();
}
