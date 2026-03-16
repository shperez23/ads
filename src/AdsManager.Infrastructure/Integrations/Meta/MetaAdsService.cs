using System.Net.Http.Json;
using System.Text.Json;
using AdsManager.Application.DTOs.Meta;
using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Meta;
using AdsManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AdsManager.Infrastructure.Integrations.Meta;

public sealed class MetaAdsService : IMetaAdsService
{
    private const string BaseUrl = "https://graph.facebook.com/v19.0/";
    private readonly HttpClient _httpClient;
    private readonly IApplicationDbContext _dbContext;

    public MetaAdsService(HttpClient httpClient, IApplicationDbContext dbContext)
    {
        _httpClient = httpClient;
        _dbContext = dbContext;
        _httpClient.BaseAddress = new Uri(BaseUrl);
    }

    public async Task<IReadOnlyCollection<MetaAdAccountDto>> GetAdAccountsAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        var endpoint = $"me/adaccounts?fields=id,name,account_status,currency,timezone_name&access_token={Uri.EscapeDataString(accessToken)}";
        var result = await GetDataAsync(endpoint, e => new MetaAdAccountDto(
            e.GetProperty("id").GetString() ?? string.Empty,
            e.GetProperty("name").GetString() ?? string.Empty,
            e.GetProperty("account_status").ToString(),
            e.TryGetProperty("currency", out var currency) ? currency.GetString() ?? string.Empty : string.Empty,
            e.TryGetProperty("timezone_name", out var tz) ? tz.GetString() ?? string.Empty : string.Empty), cancellationToken);

        return result;
    }

    public async Task<IReadOnlyCollection<MetaCampaignDto>> GetCampaignsAsync(string adAccountId, string accessToken, CancellationToken cancellationToken = default)
    {
        var endpoint = $"act_{adAccountId}/campaigns?fields=id,name,status,objective&access_token={Uri.EscapeDataString(accessToken)}";
        return await GetDataAsync(endpoint, e => new MetaCampaignDto(
            e.GetProperty("id").GetString() ?? string.Empty,
            e.GetProperty("name").GetString() ?? string.Empty,
            e.GetProperty("status").GetString() ?? string.Empty,
            e.TryGetProperty("objective", out var objective) ? objective.GetString() ?? string.Empty : string.Empty), cancellationToken);
    }

    public async Task<string> CreateCampaignAsync(string adAccountId, MetaCampaignCreateRequest request, string accessToken, CancellationToken cancellationToken = default)
    {
        var body = new Dictionary<string, string>
        {
            ["name"] = request.Name,
            ["objective"] = request.Objective,
            ["status"] = request.Status,
            ["special_ad_categories"] = "[]",
            ["access_token"] = accessToken
        };

        if (request.DailyBudget.HasValue) body["daily_budget"] = request.DailyBudget.Value.ToString();
        if (request.LifetimeBudget.HasValue) body["lifetime_budget"] = request.LifetimeBudget.Value.ToString();

        var campaignId = await PostForIdAsync($"act_{adAccountId}/campaigns", body, cancellationToken);
        await PersistCampaignAsync(adAccountId, campaignId, request, cancellationToken);
        return campaignId;
    }

    public async Task UpdateCampaignStatusAsync(MetaCampaignStatusUpdateRequest request, string accessToken, CancellationToken cancellationToken = default)
    {
        var body = new Dictionary<string, string> { ["status"] = request.Status, ["access_token"] = accessToken };
        await PostAsync(request.CampaignId, body, cancellationToken);
    }

    public async Task<string> CreateAdSetAsync(string adAccountId, MetaAdSetCreateRequest request, string accessToken, CancellationToken cancellationToken = default)
    {
        var body = new Dictionary<string, string>
        {
            ["name"] = request.Name,
            ["campaign_id"] = request.CampaignId,
            ["status"] = request.Status,
            ["daily_budget"] = request.DailyBudget.ToString(),
            ["billing_event"] = request.BillingEvent,
            ["optimization_goal"] = request.OptimizationGoal,
            ["targeting"] = request.TargetingJson,
            ["access_token"] = accessToken
        };

        var adSetId = await PostForIdAsync($"act_{adAccountId}/adsets", body, cancellationToken);
        await PersistAdSetAsync(request.CampaignId, adSetId, request, cancellationToken);
        return adSetId;
    }

    public async Task<string> CreateAdAsync(MetaAdCreateRequest request, string accessToken, CancellationToken cancellationToken = default)
    {
        var body = new Dictionary<string, string>
        {
            ["name"] = request.Name,
            ["adset_id"] = request.AdSetId,
            ["status"] = request.Status,
            ["creative"] = request.CreativeJson,
            ["access_token"] = accessToken
        };

        return await PostForIdAsync("ads", body, cancellationToken);
    }

    public async Task<IReadOnlyCollection<MetaInsightDto>> GetInsightsAsync(string adAccountId, DateOnly since, DateOnly until, string accessToken, CancellationToken cancellationToken = default)
    {
        var endpoint = $"act_{adAccountId}/insights?fields=date_start,date_stop,campaign_id,campaign_name,spend,impressions,clicks,ctr&level=campaign&time_range={{\"since\":\"{since:yyyy-MM-dd}\",\"until\":\"{until:yyyy-MM-dd}\"}}&access_token={Uri.EscapeDataString(accessToken)}";
        return await GetDataAsync(endpoint, e => new MetaInsightDto(
            e.GetProperty("date_start").GetString() ?? string.Empty,
            e.GetProperty("date_stop").GetString() ?? string.Empty,
            e.TryGetProperty("campaign_id", out var campaignId) ? campaignId.GetString() ?? string.Empty : string.Empty,
            e.TryGetProperty("campaign_name", out var campaignName) ? campaignName.GetString() ?? string.Empty : string.Empty,
            e.TryGetProperty("spend", out var spend) ? spend.GetString() ?? "0" : "0",
            e.TryGetProperty("impressions", out var impressions) ? impressions.GetString() ?? "0" : "0",
            e.TryGetProperty("clicks", out var clicks) ? clicks.GetString() ?? "0" : "0",
            e.TryGetProperty("ctr", out var ctr) ? ctr.GetString() ?? "0" : "0"), cancellationToken);
    }

    private async Task<IReadOnlyCollection<T>> GetDataAsync<T>(string endpoint, Func<JsonElement, T> mapper, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(endpoint, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        await LogApiAsync(endpoint, HttpMethod.Get.Method, string.Empty, json, (int)response.StatusCode, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(json);
        var rootData = doc.RootElement.GetProperty("data");
        return rootData.EnumerateArray().Select(mapper).ToArray();
    }

    private async Task<string> PostForIdAsync(string endpoint, Dictionary<string, string> body, CancellationToken cancellationToken)
    {
        var json = await PostAsync(endpoint, body, cancellationToken);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("id").GetString() ?? string.Empty;
    }

    private async Task<string> PostAsync(string endpoint, Dictionary<string, string> body, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync(endpoint, body, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        await LogApiAsync(endpoint, HttpMethod.Post.Method, JsonSerializer.Serialize(body), json, (int)response.StatusCode, cancellationToken);
        response.EnsureSuccessStatusCode();

        return json;
    }


    private async Task PersistCampaignAsync(string adAccountId, string campaignId, MetaCampaignCreateRequest request, CancellationToken cancellationToken)
    {
        var adAccountMatches = await _dbContext.AdAccounts
            .Where(x => x.MetaAccountId == adAccountId)
            .ToListAsync(cancellationToken);

        if (adAccountMatches.Count != 1)
            return;

        var adAccount = adAccountMatches[0];

        var exists = await _dbContext.Campaigns.AnyAsync(x => x.TenantId == adAccount.TenantId && x.MetaCampaignId == campaignId, cancellationToken);
        if (exists)
            return;

        _dbContext.Campaigns.Add(new Campaign
        {
            TenantId = adAccount.TenantId,
            AdAccountId = adAccount.Id,
            MetaCampaignId = campaignId,
            Name = request.Name,
            Objective = request.Objective,
            Status = request.Status,
            DailyBudget = request.DailyBudget,
            LifetimeBudget = request.LifetimeBudget
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task PersistAdSetAsync(string metaCampaignId, string adSetId, MetaAdSetCreateRequest request, CancellationToken cancellationToken)
    {
        var campaignMatches = await _dbContext.Campaigns
            .Where(x => x.MetaCampaignId == metaCampaignId)
            .ToListAsync(cancellationToken);

        if (campaignMatches.Count != 1)
            return;

        var campaign = campaignMatches[0];

        var exists = await _dbContext.AdSets.AnyAsync(x => x.TenantId == campaign.TenantId && x.MetaAdSetId == adSetId, cancellationToken);
        if (exists)
            return;

        _dbContext.AdSets.Add(new AdSet
        {
            TenantId = campaign.TenantId,
            CampaignId = campaign.Id,
            MetaAdSetId = adSetId,
            Name = request.Name,
            Status = request.Status,
            Budget = request.DailyBudget,
            BillingEvent = request.BillingEvent,
            OptimizationGoal = request.OptimizationGoal,
            BidStrategy = string.Empty,
            TargetingJson = request.TargetingJson
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task LogApiAsync(string endpoint, string method, string requestJson, string responseJson, int statusCode, CancellationToken cancellationToken)
    {
        _dbContext.ApiLogs.Add(new ApiLog
        {
            Provider = "Meta",
            Endpoint = endpoint,
            Method = method,
            RequestJson = requestJson,
            ResponseJson = responseJson,
            StatusCode = statusCode
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
