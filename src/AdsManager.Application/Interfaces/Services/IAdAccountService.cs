using AdsManager.Application.Common;
using AdsManager.Application.DTOs.AdAccounts;
using AdsManager.Application.DTOs.Common;

namespace AdsManager.Application.Interfaces.Services;

public interface IAdAccountService
{
    Task<Result<PagedResponse<AdAccountDto>>> GetAllAsync(AdAccountListRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyCollection<AdAccountDto>>> ImportFromMetaAsync(CancellationToken cancellationToken = default);
    Task<Result<string>> SyncAsync(Guid adAccountId, CancellationToken cancellationToken = default);
}
