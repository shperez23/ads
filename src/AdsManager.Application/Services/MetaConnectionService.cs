using AdsManager.Application.Common;
using AdsManager.Application.DTOs.Meta;
using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Meta;
using AdsManager.Application.Interfaces.Repositories;
using AdsManager.Application.Interfaces.Services;
using AdsManager.Domain.Entities;
using AdsManager.Domain.Enums;
using System.Diagnostics;
using System.Text.Json;

namespace AdsManager.Application.Services;

public sealed class MetaConnectionService : IMetaConnectionService
{
    private static readonly string[] RequiredPermissions = ["ads_management", "ads_read", "business_management"];

    private readonly IMetaConnectionRepository _metaConnectionRepository;
    private readonly ITenantProvider _tenantProvider;
    private readonly ISecretEncryptionService _secretEncryptionService;
    private readonly IMetaConnectionApiClient _metaConnectionApiClient;
    private readonly IAuditService _auditService;
    private readonly IApplicationDbContext _dbContext;

    public MetaConnectionService(
        IMetaConnectionRepository metaConnectionRepository,
        ITenantProvider tenantProvider,
        ISecretEncryptionService secretEncryptionService,
        IMetaConnectionApiClient metaConnectionApiClient,
        IAuditService auditService,
        IApplicationDbContext dbContext)
    {
        _metaConnectionRepository = metaConnectionRepository;
        _tenantProvider = tenantProvider;
        _secretEncryptionService = secretEncryptionService;
        _metaConnectionApiClient = metaConnectionApiClient;
        _auditService = auditService;
        _dbContext = dbContext;
    }

    public async Task<Result<IReadOnlyCollection<MetaConnectionDto>>> GetConnectionsAsync(CancellationToken cancellationToken = default)
    {
        if (!TryGetTenantId(out var tenantId))
            return Result<IReadOnlyCollection<MetaConnectionDto>>.Fail("Tenant no resuelto");

        var connections = await _metaConnectionRepository.GetByTenantAsync(tenantId, cancellationToken);
        return Result<IReadOnlyCollection<MetaConnectionDto>>.Ok(connections.Select(Map).ToArray());
    }

    public async Task<Result<MetaConnectionDto>> CreateConnectionAsync(CreateMetaConnectionRequest request, CancellationToken cancellationToken = default)
    {
        if (!TryGetTenantId(out var tenantId))
            return Result<MetaConnectionDto>.Fail("Tenant no resuelto");

        var connection = new MetaConnection
        {
            TenantId = tenantId,
            AppId = request.AppId,
            AppSecret = _secretEncryptionService.Encrypt(request.AppSecret),
            AccessToken = _secretEncryptionService.Encrypt(request.AccessToken),
            RefreshToken = string.IsNullOrWhiteSpace(request.RefreshToken) ? null : _secretEncryptionService.Encrypt(request.RefreshToken),
            TokenExpiration = request.TokenExpiration,
            BusinessId = request.BusinessId,
            Status = ConnectionStatus.Invalid
        };

        await _metaConnectionRepository.AddAsync(connection, cancellationToken);
        await _auditService.LogAsync(_tenantProvider.GetUserId(), tenantId, "create connection", nameof(MetaConnection), connection.Id.ToString(), JsonSerializer.Serialize(request), cancellationToken);
        return Result<MetaConnectionDto>.Ok(Map(connection), "Conexión creada correctamente");
    }

    public async Task<Result<MetaConnectionDto>> UpdateConnectionAsync(Guid connectionId, UpdateMetaConnectionRequest request, CancellationToken cancellationToken = default)
    {
        if (!TryGetTenantId(out var tenantId))
            return Result<MetaConnectionDto>.Fail("Tenant no resuelto");

        var connection = await _metaConnectionRepository.GetByIdAsync(tenantId, connectionId, cancellationToken);
        if (connection is null)
            return Result<MetaConnectionDto>.Fail("Conexión no encontrada");

        connection.AppId = request.AppId;
        connection.AppSecret = _secretEncryptionService.Encrypt(request.AppSecret);
        connection.AccessToken = _secretEncryptionService.Encrypt(request.AccessToken);
        connection.RefreshToken = string.IsNullOrWhiteSpace(request.RefreshToken) ? null : _secretEncryptionService.Encrypt(request.RefreshToken);
        connection.TokenExpiration = request.TokenExpiration;
        connection.BusinessId = request.BusinessId;

        await _metaConnectionRepository.UpdateAsync(connection, cancellationToken);
        await _auditService.LogAsync(_tenantProvider.GetUserId(), tenantId, "update connection", nameof(MetaConnection), connection.Id.ToString(), JsonSerializer.Serialize(request), cancellationToken);
        return Result<MetaConnectionDto>.Ok(Map(connection), "Conexión actualizada correctamente");
    }

    public async Task<Result<bool>> DeleteConnectionAsync(Guid connectionId, CancellationToken cancellationToken = default)
    {
        if (!TryGetTenantId(out var tenantId))
            return Result<bool>.Fail("Tenant no resuelto");

        var connection = await _metaConnectionRepository.GetByIdAsync(tenantId, connectionId, cancellationToken);
        if (connection is null)
            return Result<bool>.Fail("Conexión no encontrada");

        await _metaConnectionRepository.DeleteAsync(connection, cancellationToken);
        await _auditService.LogAsync(_tenantProvider.GetUserId(), tenantId, "delete connection", nameof(MetaConnection), connection.Id.ToString(), JsonSerializer.Serialize(new { connection.AppId, connection.BusinessId }), cancellationToken);
        return Result<bool>.Ok(true, "Conexión eliminada correctamente");
    }

    public async Task<Result<MetaConnectionValidationResultDto>> ValidateConnectionAsync(Guid connectionId, CancellationToken cancellationToken = default)
    {
        if (!TryGetTenantId(out var tenantId))
            return Result<MetaConnectionValidationResultDto>.Fail("Tenant no resuelto");

        var connection = await _metaConnectionRepository.GetByIdAsync(tenantId, connectionId, cancellationToken);
        if (connection is null)
            return Result<MetaConnectionValidationResultDto>.Fail("Conexión no encontrada");

        var accessToken = _secretEncryptionService.Decrypt(connection.AccessToken);
        var appSecret = _secretEncryptionService.Decrypt(connection.AppSecret);

        var (isTokenValid, grantedPermissions) = await _metaConnectionApiClient
            .ValidateTokenAndPermissionsAsync(connection.AppId, appSecret, accessToken, cancellationToken);

        var missingPermissions = RequiredPermissions.Except(grantedPermissions, StringComparer.OrdinalIgnoreCase).ToArray();
        var hasRequiredPermissions = missingPermissions.Length == 0;

        connection.Status = isTokenValid && hasRequiredPermissions
            ? ConnectionStatus.Connected
            : ConnectionStatus.Invalid;

        await _metaConnectionRepository.UpdateAsync(connection, cancellationToken);

        await _auditService.LogAsync(
            _tenantProvider.GetUserId(),
            tenantId,
            "validate connection",
            nameof(MetaConnection),
            connection.Id.ToString(),
            JsonSerializer.Serialize(new { isTokenValid, hasRequiredPermissions, missingPermissions, connection.Status }),
            cancellationToken);

        var result = new MetaConnectionValidationResultDto(connection.Id, isTokenValid, hasRequiredPermissions, connection.Status, missingPermissions);
        return Result<MetaConnectionValidationResultDto>.Ok(result, "Conexión validada correctamente");
    }

    public async Task<Result<MetaConnectionTokenRefreshResultDto>> RefreshTokenAsync(Guid connectionId, CancellationToken cancellationToken = default)
    {
        if (!TryGetTenantId(out var tenantId))
            return Result<MetaConnectionTokenRefreshResultDto>.Fail("Tenant no resuelto");

        var connection = await _metaConnectionRepository.GetByIdAsync(tenantId, connectionId, cancellationToken);
        if (connection is null)
            return Result<MetaConnectionTokenRefreshResultDto>.Fail("Conexión no encontrada");

        if (!connection.IsTokenExpiringSoon(TimeSpan.FromDays(7)))
        {
            var notRequired = new MetaConnectionTokenRefreshResultDto(connection.Id, false, false, connection.TokenExpiration, connection.Status.ToString(), "El token aún no está próximo a expirar.");
            return Result<MetaConnectionTokenRefreshResultDto>.Ok(notRequired, "Refresh no requerido");
        }

        if (string.IsNullOrWhiteSpace(connection.RefreshToken))
        {
            connection.LastHealthCheckAt = DateTime.UtcNow;
            connection.LastHealthCheckStatus = "ReauthenticationRequired";
            connection.LastHealthCheckDetails = "No existe refresh token en la conexión actual.";
            await _metaConnectionRepository.UpdateAsync(connection, cancellationToken);

            await _auditService.LogAsync(_tenantProvider.GetUserId(), tenantId, "refresh token", nameof(MetaConnection), connection.Id.ToString(), JsonSerializer.Serialize(new { status = "ReauthenticationRequired" }), cancellationToken);

            var noRefreshTokenResult = new MetaConnectionTokenRefreshResultDto(connection.Id, false, true, connection.TokenExpiration, connection.Status.ToString(), "Reautenticación requerida: no hay refresh token configurado.");
            return Result<MetaConnectionTokenRefreshResultDto>.Ok(noRefreshTokenResult, "Reautenticación requerida");
        }

        var appSecret = _secretEncryptionService.Decrypt(connection.AppSecret);
        var accessToken = _secretEncryptionService.Decrypt(connection.AccessToken);

        var stopwatch = Stopwatch.StartNew();
        var apiResult = await _metaConnectionApiClient.TryRefreshTokenAsync(connection.AppId, appSecret, accessToken, cancellationToken);
        stopwatch.Stop();

        var payload = JsonSerializer.Serialize(new { connectionId, apiResult.IsSupported, apiResult.Success, apiResult.Message, apiResult.StatusCode });

        await LogApiAsync("oauth/access_token", "GET", payload, apiResult.ResponsePayload, apiResult.StatusCode, apiResult.Success ? "Success" : "Failed", stopwatch.ElapsedMilliseconds, cancellationToken);

        if (!apiResult.IsSupported)
        {
            connection.LastHealthCheckAt = DateTime.UtcNow;
            connection.LastHealthCheckStatus = "ReauthenticationRequired";
            connection.LastHealthCheckDetails = "La integración actual no soporta refresh directo.";
            await _metaConnectionRepository.UpdateAsync(connection, cancellationToken);

            await _auditService.LogAsync(_tenantProvider.GetUserId(), tenantId, "refresh token", nameof(MetaConnection), connection.Id.ToString(), JsonSerializer.Serialize(new { status = "ReauthenticationRequired" }), cancellationToken);

            var unsupportedResult = new MetaConnectionTokenRefreshResultDto(connection.Id, false, true, connection.TokenExpiration, connection.Status.ToString(), "Reautenticación requerida para renovar token.");
            return Result<MetaConnectionTokenRefreshResultDto>.Ok(unsupportedResult, "Refresh no soportado en flujo actual");
        }

        if (!apiResult.Success || string.IsNullOrWhiteSpace(apiResult.AccessToken))
        {
            connection.Status = ConnectionStatus.Invalid;
            connection.LastHealthCheckAt = DateTime.UtcNow;
            connection.LastHealthCheckStatus = "RefreshFailed";
            connection.LastHealthCheckDetails = apiResult.Message;
            await _metaConnectionRepository.UpdateAsync(connection, cancellationToken);

            await _auditService.LogAsync(_tenantProvider.GetUserId(), tenantId, "refresh token", nameof(MetaConnection), connection.Id.ToString(), JsonSerializer.Serialize(new { status = "RefreshFailed", apiResult.Message }), cancellationToken);

            var failedResult = new MetaConnectionTokenRefreshResultDto(connection.Id, false, true, connection.TokenExpiration, connection.Status.ToString(), "No se pudo refrescar el token. Reautenticación requerida.");
            return Result<MetaConnectionTokenRefreshResultDto>.Ok(failedResult, "Refresh fallido");
        }

        connection.AccessToken = _secretEncryptionService.Encrypt(apiResult.AccessToken);
        connection.TokenExpiration = apiResult.ExpiresAtUtc ?? DateTime.UtcNow.AddDays(60);
        connection.Status = ConnectionStatus.Connected;
        connection.LastHealthCheckAt = DateTime.UtcNow;
        connection.LastHealthCheckStatus = "Healthy";
        connection.LastHealthCheckDetails = "Token refrescado correctamente.";

        await _metaConnectionRepository.UpdateAsync(connection, cancellationToken);

        await _auditService.LogAsync(
            _tenantProvider.GetUserId(),
            tenantId,
            "refresh token",
            nameof(MetaConnection),
            connection.Id.ToString(),
            JsonSerializer.Serialize(new { status = "Refreshed", connection.TokenExpiration }),
            cancellationToken);

        var refreshedResult = new MetaConnectionTokenRefreshResultDto(connection.Id, true, false, connection.TokenExpiration, connection.Status.ToString(), "Token refrescado correctamente.");
        return Result<MetaConnectionTokenRefreshResultDto>.Ok(refreshedResult, "Token refrescado correctamente");
    }

    private async Task LogApiAsync(string endpoint, string method, string requestJson, string responseJson, int statusCode, string status, long durationMs, CancellationToken cancellationToken)
    {
        _dbContext.ApiLogs.Add(new ApiLog
        {
            Provider = "Meta",
            Endpoint = endpoint,
            Method = method,
            RequestJson = string.IsNullOrWhiteSpace(requestJson) ? "{}" : requestJson,
            ResponseJson = string.IsNullOrWhiteSpace(responseJson) ? "{}" : responseJson,
            Status = status,
            StatusCode = statusCode,
            DurationMs = durationMs,
            TraceId = _tenantProvider.GetTraceId()
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private bool TryGetTenantId(out Guid tenantId)
    {
        tenantId = _tenantProvider.GetTenantId() ?? Guid.Empty;
        return tenantId != Guid.Empty;
    }

    private static MetaConnectionDto Map(MetaConnection connection)
        => new(connection.Id, connection.AppId, connection.BusinessId, connection.TokenExpiration, connection.Status);
}
