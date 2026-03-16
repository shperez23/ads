using AdsManager.Application.Common;
using AdsManager.Application.DTOs.Meta;
using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Meta;
using AdsManager.Application.Interfaces.Repositories;
using AdsManager.Application.Interfaces.Services;
using AdsManager.Domain.Entities;
using AdsManager.Domain.Enums;
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

    public MetaConnectionService(
        IMetaConnectionRepository metaConnectionRepository,
        ITenantProvider tenantProvider,
        ISecretEncryptionService secretEncryptionService,
        IMetaConnectionApiClient metaConnectionApiClient,
        IAuditService auditService)
    {
        _metaConnectionRepository = metaConnectionRepository;
        _tenantProvider = tenantProvider;
        _secretEncryptionService = secretEncryptionService;
        _metaConnectionApiClient = metaConnectionApiClient;
        _auditService = auditService;
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

    private bool TryGetTenantId(out Guid tenantId)
    {
        tenantId = _tenantProvider.GetTenantId() ?? Guid.Empty;
        return tenantId != Guid.Empty;
    }

    private static MetaConnectionDto Map(MetaConnection connection)
        => new(connection.Id, connection.AppId, connection.BusinessId, connection.TokenExpiration, connection.Status);
}
