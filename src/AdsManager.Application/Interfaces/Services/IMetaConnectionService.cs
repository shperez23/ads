using AdsManager.Application.Common;
using AdsManager.Application.DTOs.Meta;

namespace AdsManager.Application.Interfaces.Services;

public interface IMetaConnectionService
{
    Task<Result<IReadOnlyCollection<MetaConnectionDto>>> GetConnectionsAsync(CancellationToken cancellationToken = default);
    Task<Result<MetaConnectionDto>> CreateConnectionAsync(CreateMetaConnectionRequest request, CancellationToken cancellationToken = default);
    Task<Result<MetaConnectionDto>> UpdateConnectionAsync(Guid connectionId, UpdateMetaConnectionRequest request, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteConnectionAsync(Guid connectionId, CancellationToken cancellationToken = default);
    Task<Result<MetaConnectionValidationResultDto>> ValidateConnectionAsync(Guid connectionId, CancellationToken cancellationToken = default);
    Task<Result<MetaConnectionTokenRefreshResultDto>> RefreshTokenAsync(Guid connectionId, CancellationToken cancellationToken = default);
}
