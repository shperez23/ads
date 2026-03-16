namespace AdsManager.Application.Interfaces.Services;

public interface IAuthProtectionService
{
    Task<AuthProtectionDecision> CheckLoginAttemptAsync(string email, string ipAddress, CancellationToken cancellationToken = default);
    Task<AuthProtectionDecision> CheckRefreshAttemptAsync(string ipAddress, CancellationToken cancellationToken = default);
    Task<AuthProtectionDecision> CheckRegisterAttemptAsync(string email, string ipAddress, CancellationToken cancellationToken = default);
    Task RecordLoginAttemptAsync(Guid? userId, string email, string ipAddress, bool success, string? failureReason, CancellationToken cancellationToken = default);
    Task RecordAttemptAsync(string attemptType, Guid? userId, string email, string ipAddress, bool success, string? failureReason, CancellationToken cancellationToken = default);
}

public sealed record AuthProtectionDecision(bool IsBlocked, string Message, DateTime? RetryAfterUtc = null);
