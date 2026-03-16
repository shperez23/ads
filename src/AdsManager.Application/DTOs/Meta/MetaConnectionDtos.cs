using AdsManager.Domain.Enums;

namespace AdsManager.Application.DTOs.Meta;

public sealed record MetaConnectionDto(Guid Id, string AppId, string BusinessId, DateTime TokenExpiration, ConnectionStatus Status);
public sealed record CreateMetaConnectionRequest(string AppId, string AppSecret, string AccessToken, string? RefreshToken, DateTime TokenExpiration, string BusinessId);
public sealed record UpdateMetaConnectionRequest(string AppId, string AppSecret, string AccessToken, string? RefreshToken, DateTime TokenExpiration, string BusinessId);
public sealed record MetaConnectionValidationResultDto(Guid ConnectionId, bool IsTokenValid, bool HasRequiredPermissions, ConnectionStatus Status, IReadOnlyCollection<string> MissingPermissions);
