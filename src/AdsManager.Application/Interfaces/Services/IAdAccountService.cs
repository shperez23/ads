using AdsManager.Application.Common;
using AdsManager.Application.DTOs.AdAccounts;

namespace AdsManager.Application.Interfaces.Services;

public interface IAdAccountService
{
    Task<Result<IReadOnlyCollection<AdAccountDto>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyCollection<AdAccountDto>>> ImportFromMetaAsync(CancellationToken cancellationToken = default);
    Task<Result<string>> SyncAsync(Guid adAccountId, CancellationToken cancellationToken = default);
}
