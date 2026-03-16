using AdsManager.Application.DTOs.Meta;

namespace AdsManager.Application.Interfaces.Meta;

public interface IMetaAdsService
{
    Task<IReadOnlyCollection<MetaAdAccountDto>> GetAdAccountsAsync(string accessToken, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<MetaCampaignDto>> GetCampaignsAsync(string adAccountId, string accessToken, CancellationToken cancellationToken = default);
    Task<string> CreateCampaignAsync(string adAccountId, MetaCampaignCreateRequest request, string accessToken, CancellationToken cancellationToken = default);
    Task UpdateCampaignStatusAsync(MetaCampaignStatusUpdateRequest request, string accessToken, CancellationToken cancellationToken = default);
    Task<string> CreateAdSetAsync(string adAccountId, MetaAdSetCreateRequest request, string accessToken, CancellationToken cancellationToken = default);
    Task<string> CreateAdAsync(MetaAdCreateRequest request, string accessToken, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<MetaInsightDto>> GetInsightsAsync(string adAccountId, DateOnly since, DateOnly until, string accessToken, CancellationToken cancellationToken = default);
}
