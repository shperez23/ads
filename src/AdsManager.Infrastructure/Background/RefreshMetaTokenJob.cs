using System.Diagnostics;
using System.Text.Json;
using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Meta;
using AdsManager.Application.Interfaces.Repositories;
using AdsManager.Application.Interfaces.Services;
using AdsManager.Domain.Entities;
using AdsManager.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace AdsManager.Infrastructure.Background;

public sealed class RefreshMetaTokenJob
{
    private readonly IMetaConnectionRepository _metaConnectionRepository;
    private readonly IMetaConnectionApiClient _metaConnectionApiClient;
    private readonly ISecretEncryptionService _secretEncryptionService;
    private readonly IApplicationDbContext _dbContext;
    private readonly IAuditService _auditService;
    private readonly ILogger<RefreshMetaTokenJob> _logger;

    public RefreshMetaTokenJob(
        IMetaConnectionRepository metaConnectionRepository,
        IMetaConnectionApiClient metaConnectionApiClient,
        ISecretEncryptionService secretEncryptionService,
        IApplicationDbContext dbContext,
        IAuditService auditService,
        ILogger<RefreshMetaTokenJob> logger)
    {
        _metaConnectionRepository = metaConnectionRepository;
        _metaConnectionApiClient = metaConnectionApiClient;
        _secretEncryptionService = secretEncryptionService;
        _dbContext = dbContext;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task ExecuteAsync(int expirationThresholdDays = 7, CancellationToken cancellationToken = default)
    {
        var expiresBeforeUtc = DateTime.UtcNow.AddDays(expirationThresholdDays);
        var expiringConnections = await _metaConnectionRepository.GetConnectionsExpiringBeforeAsync(expiresBeforeUtc, cancellationToken);

        foreach (var connection in expiringConnections)
        {
            try
            {
                await RefreshConnectionAsync(connection, expirationThresholdDays, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RefreshMetaTokenJob failed for connection {ConnectionId}", connection.Id);
                connection.LastHealthCheckAt = DateTime.UtcNow;
                connection.LastHealthCheckStatus = "RefreshError";
                connection.LastHealthCheckDetails = ex.Message;
                await _metaConnectionRepository.UpdateAsync(connection, cancellationToken);
            }
        }
    }

    private async Task RefreshConnectionAsync(MetaConnection connection, int expirationThresholdDays, CancellationToken cancellationToken)
    {
        if (!connection.IsTokenExpiringSoon(TimeSpan.FromDays(expirationThresholdDays)))
            return;

        if (string.IsNullOrWhiteSpace(connection.RefreshToken))
        {
            connection.LastHealthCheckAt = DateTime.UtcNow;
            connection.LastHealthCheckStatus = "ReauthenticationRequired";
            connection.LastHealthCheckDetails = "No existe refresh token en la conexión.";
            await _metaConnectionRepository.UpdateAsync(connection, cancellationToken);
            await _auditService.LogAsync(Guid.Empty, connection.TenantId, "refresh token job", nameof(MetaConnection), connection.Id.ToString(), JsonSerializer.Serialize(new { status = "ReauthenticationRequired" }), cancellationToken);
            return;
        }

        var appSecret = _secretEncryptionService.Decrypt(connection.AppSecret);
        var accessToken = _secretEncryptionService.Decrypt(connection.AccessToken);

        var stopwatch = Stopwatch.StartNew();
        var apiResult = await _metaConnectionApiClient.TryRefreshTokenAsync(connection.AppId, appSecret, accessToken, cancellationToken);
        stopwatch.Stop();

        await LogApiAsync(connection.Id, apiResult, stopwatch.ElapsedMilliseconds, cancellationToken);

        if (!apiResult.IsSupported)
        {
            connection.LastHealthCheckAt = DateTime.UtcNow;
            connection.LastHealthCheckStatus = "ReauthenticationRequired";
            connection.LastHealthCheckDetails = "Flujo actual sin refresh directo.";
            await _metaConnectionRepository.UpdateAsync(connection, cancellationToken);
            await _auditService.LogAsync(Guid.Empty, connection.TenantId, "refresh token job", nameof(MetaConnection), connection.Id.ToString(), JsonSerializer.Serialize(new { status = "ReauthenticationRequired" }), cancellationToken);
            return;
        }

        if (!apiResult.Success || string.IsNullOrWhiteSpace(apiResult.AccessToken))
        {
            connection.Status = ConnectionStatus.Invalid;
            connection.LastHealthCheckAt = DateTime.UtcNow;
            connection.LastHealthCheckStatus = "RefreshFailed";
            connection.LastHealthCheckDetails = apiResult.Message;
            await _metaConnectionRepository.UpdateAsync(connection, cancellationToken);
            await _auditService.LogAsync(Guid.Empty, connection.TenantId, "refresh token job", nameof(MetaConnection), connection.Id.ToString(), JsonSerializer.Serialize(new { status = "RefreshFailed", apiResult.Message }), cancellationToken);
            return;
        }

        connection.AccessToken = _secretEncryptionService.Encrypt(apiResult.AccessToken);
        connection.TokenExpiration = apiResult.ExpiresAtUtc ?? DateTime.UtcNow.AddDays(60);
        connection.Status = ConnectionStatus.Connected;
        connection.LastHealthCheckAt = DateTime.UtcNow;
        connection.LastHealthCheckStatus = "Healthy";
        connection.LastHealthCheckDetails = "Token refrescado por job.";

        await _metaConnectionRepository.UpdateAsync(connection, cancellationToken);
        await _auditService.LogAsync(Guid.Empty, connection.TenantId, "refresh token job", nameof(MetaConnection), connection.Id.ToString(), JsonSerializer.Serialize(new { status = "Refreshed", connection.TokenExpiration }), cancellationToken);
    }

    private async Task LogApiAsync(Guid connectionId, MetaTokenRefreshApiResult apiResult, long durationMs, CancellationToken cancellationToken)
    {
        _dbContext.ApiLogs.Add(new ApiLog
        {
            Provider = "Meta",
            Endpoint = "oauth/access_token",
            Method = "GET",
            RequestJson = JsonSerializer.Serialize(new { connectionId }),
            ResponseJson = string.IsNullOrWhiteSpace(apiResult.ResponsePayload) ? "{}" : apiResult.ResponsePayload,
            Status = apiResult.Success ? "Success" : "Failed",
            StatusCode = apiResult.StatusCode,
            DurationMs = durationMs,
            TraceId = "hangfire-refresh-meta-token"
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
