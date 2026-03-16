using AdsManager.Application.DTOs.Meta;

namespace AdsManager.Application.Interfaces.Meta;

public interface IMetaAdsService
{
    Task<IReadOnlyCollection<MetaAdAccountDto>> GetAdAccountsAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<MetaCampaignDto>> GetCampaignsAsync(Guid tenantId, string adAccountId, CancellationToken cancellationToken = default);
    Task<string> CreateCampaignAsync(Guid tenantId, string adAccountId, MetaCampaignCreateRequest request, CancellationToken cancellationToken = default);
    Task UpdateCampaignStatusAsync(Guid tenantId, MetaCampaignStatusUpdateRequest request, CancellationToken cancellationToken = default);
    Task<string> CreateAdSetAsync(Guid tenantId, string adAccountId, MetaAdSetCreateRequest request, CancellationToken cancellationToken = default);
    Task UpdateAdSetAsync(Guid tenantId, MetaAdSetUpdateRequest request, CancellationToken cancellationToken = default);
    Task UpdateAdSetStatusAsync(Guid tenantId, MetaAdSetStatusUpdateRequest request, CancellationToken cancellationToken = default);
    Task<string> CreateAdAsync(Guid tenantId, MetaAdCreateRequest request, CancellationToken cancellationToken = default);
    Task UpdateAdAsync(Guid tenantId, MetaAdUpdateRequest request, CancellationToken cancellationToken = default);
    Task UpdateAdStatusAsync(Guid tenantId, MetaAdStatusUpdateRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<MetaInsightDto>> GetInsightsAsync(Guid tenantId, string adAccountId, DateOnly since, DateOnly until, CancellationToken cancellationToken = default);
    Task SyncCampaignsAsync(Guid tenantId, string adAccountId, CancellationToken cancellationToken = default);
    Task SyncAdSetsAsync(Guid tenantId, string adAccountId, CancellationToken cancellationToken = default);
    Task SyncAdsAsync(Guid tenantId, string adAccountId, CancellationToken cancellationToken = default);
    Task SyncInsightsAsync(Guid tenantId, string adAccountId, DateOnly since, DateOnly until, CancellationToken cancellationToken = default);
}
