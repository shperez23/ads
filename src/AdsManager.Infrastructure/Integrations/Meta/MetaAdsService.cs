using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using AdsManager.Application.DTOs.Meta;
using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Meta;
using AdsManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AdsManager.Infrastructure.Integrations.Meta;

public sealed class MetaAdsService : IMetaAdsService
{
    private const string BaseUrl = "https://graph.facebook.com/v19.0/";
    private readonly HttpClient _httpClient;
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<MetaAdsService> _logger;
    private readonly ISecretEncryptionService _secretEncryptionService;

    public MetaAdsService(HttpClient httpClient, IApplicationDbContext dbContext, ILogger<MetaAdsService> logger, ISecretEncryptionService secretEncryptionService)
    {
        _httpClient = httpClient;
        _dbContext = dbContext;
        _logger = logger;
        _secretEncryptionService = secretEncryptionService;
        _httpClient.BaseAddress = new Uri(BaseUrl);
    }

    public Task<IReadOnlyCollection<MetaAdAccountDto>> GetAdAccountsAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => GetDataAsync(tenantId,
            "me/adaccounts?fields=id,name,account_status,currency,timezone_name",
            e => new MetaAdAccountDto(
                e.GetProperty("id").GetString() ?? string.Empty,
                e.GetProperty("name").GetString() ?? string.Empty,
                e.GetProperty("account_status").ToString(),
                e.TryGetProperty("currency", out var currency) ? currency.GetString() ?? string.Empty : string.Empty,
                e.TryGetProperty("timezone_name", out var tz) ? tz.GetString() ?? string.Empty : string.Empty),
            cancellationToken);

    public Task<IReadOnlyCollection<MetaCampaignDto>> GetCampaignsAsync(Guid tenantId, string adAccountId, CancellationToken cancellationToken = default)
        => GetDataAsync(tenantId,
            $"act_{adAccountId}/campaigns?fields=id,name,status,objective",
            e => new MetaCampaignDto(
                e.GetProperty("id").GetString() ?? string.Empty,
                e.GetProperty("name").GetString() ?? string.Empty,
                e.GetProperty("status").GetString() ?? string.Empty,
                e.TryGetProperty("objective", out var objective) ? objective.GetString() ?? string.Empty : string.Empty),
            cancellationToken);

    public async Task<string> CreateCampaignAsync(Guid tenantId, string adAccountId, MetaCampaignCreateRequest request, CancellationToken cancellationToken = default)
    {
        var body = new Dictionary<string, string>
        {
            ["name"] = request.Name,
            ["objective"] = request.Objective,
            ["status"] = request.Status,
            ["special_ad_categories"] = "[]"
        };

        if (request.DailyBudget.HasValue) body["daily_budget"] = request.DailyBudget.Value.ToString()!;
        if (request.LifetimeBudget.HasValue) body["lifetime_budget"] = request.LifetimeBudget.Value.ToString()!;

        var campaignId = await PostForIdAsync(tenantId, $"act_{adAccountId}/campaigns", body, cancellationToken);
        await PersistCampaignAsync(tenantId, adAccountId, campaignId, request, cancellationToken);
        return campaignId;
    }

    public Task UpdateCampaignStatusAsync(Guid tenantId, MetaCampaignStatusUpdateRequest request, CancellationToken cancellationToken = default)
        => PostAsync(tenantId, request.CampaignId, new Dictionary<string, string> { ["status"] = request.Status }, cancellationToken);

    public async Task<string> CreateAdSetAsync(Guid tenantId, string adAccountId, MetaAdSetCreateRequest request, CancellationToken cancellationToken = default)
    {
        var body = new Dictionary<string, string>
        {
            ["name"] = request.Name,
            ["campaign_id"] = request.CampaignId,
            ["status"] = request.Status,
            ["daily_budget"] = request.DailyBudget.ToString(),
            ["billing_event"] = request.BillingEvent,
            ["optimization_goal"] = request.OptimizationGoal,
            ["targeting"] = request.TargetingJson
        };

        var adSetId = await PostForIdAsync(tenantId, $"act_{adAccountId}/adsets", body, cancellationToken);
        await PersistAdSetAsync(tenantId, request.CampaignId, adSetId, request, cancellationToken);
        return adSetId;
    }

    public Task<string> CreateAdAsync(Guid tenantId, MetaAdCreateRequest request, CancellationToken cancellationToken = default)
        => PostForIdAsync(tenantId,
            "ads",
            new Dictionary<string, string>
            {
                ["name"] = request.Name,
                ["adset_id"] = request.AdSetId,
                ["status"] = request.Status,
                ["creative"] = request.CreativeJson
            },
            cancellationToken);

    public Task<IReadOnlyCollection<MetaInsightDto>> GetInsightsAsync(Guid tenantId, string adAccountId, DateOnly since, DateOnly until, CancellationToken cancellationToken = default)
        => GetDataAsync(tenantId,
            $"act_{adAccountId}/insights?fields=date_start,date_stop,campaign_id,campaign_name,spend,impressions,clicks,ctr&level=campaign&time_range={{\"since\":\"{since:yyyy-MM-dd}\",\"until\":\"{until:yyyy-MM-dd}\"}}",
            e => new MetaInsightDto(
                e.GetProperty("date_start").GetString() ?? string.Empty,
                e.GetProperty("date_stop").GetString() ?? string.Empty,
                e.TryGetProperty("campaign_id", out var campaignId) ? campaignId.GetString() ?? string.Empty : string.Empty,
                e.TryGetProperty("campaign_name", out var campaignName) ? campaignName.GetString() ?? string.Empty : string.Empty,
                e.TryGetProperty("spend", out var spend) ? spend.GetString() ?? "0" : "0",
                e.TryGetProperty("impressions", out var impressions) ? impressions.GetString() ?? "0" : "0",
                e.TryGetProperty("clicks", out var clicks) ? clicks.GetString() ?? "0" : "0",
                e.TryGetProperty("ctr", out var ctr) ? ctr.GetString() ?? "0" : "0"),
            cancellationToken);

    public async Task SyncCampaignsAsync(Guid tenantId, string adAccountId, CancellationToken cancellationToken = default)
    {
        var adAccount = await GetAdAccountAsync(tenantId, adAccountId, cancellationToken);
        var campaigns = await GetCampaignsAsync(tenantId, adAccountId, cancellationToken);

        foreach (var campaign in campaigns)
        {
            var existing = await _dbContext.Campaigns.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.MetaCampaignId == campaign.Id, cancellationToken);
            if (existing is null)
            {
                _dbContext.Campaigns.Add(new Campaign
                {
                    TenantId = tenantId,
                    AdAccountId = adAccount.Id,
                    MetaCampaignId = campaign.Id,
                    Name = campaign.Name,
                    Status = campaign.Status,
                    Objective = campaign.Objective
                });
            }
            else
            {
                existing.Name = campaign.Name;
                existing.Status = campaign.Status;
                existing.Objective = campaign.Objective;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SyncAdSetsAsync(Guid tenantId, string adAccountId, CancellationToken cancellationToken = default)
    {
        var campaigns = await _dbContext.Campaigns.Where(x => x.TenantId == tenantId && x.AdAccount.MetaAccountId == adAccountId)
            .Select(x => new { x.Id, x.MetaCampaignId })
            .ToListAsync(cancellationToken);

        foreach (var campaign in campaigns)
        {
            var adSets = await GetDataAsync(tenantId,
                $"{campaign.MetaCampaignId}/adsets?fields=id,campaign_id,name,status,daily_budget,billing_event,optimization_goal,targeting",
                e => new MetaAdSetDto(
                    e.GetProperty("id").GetString() ?? string.Empty,
                    e.TryGetProperty("campaign_id", out var campaignId) ? campaignId.GetString() ?? string.Empty : string.Empty,
                    e.GetProperty("name").GetString() ?? string.Empty,
                    e.TryGetProperty("status", out var status) ? status.GetString() ?? string.Empty : string.Empty,
                    TryGetDecimal(e, "daily_budget"),
                    TryGetString(e, "billing_event"),
                    TryGetString(e, "optimization_goal"),
                    e.TryGetProperty("targeting", out var targeting) ? targeting.GetRawText() : "{}"),
                cancellationToken);

            foreach (var adSet in adSets)
            {
                var existing = await _dbContext.AdSets.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.MetaAdSetId == adSet.Id, cancellationToken);
                if (existing is null)
                {
                    _dbContext.AdSets.Add(new AdSet
                    {
                        TenantId = tenantId,
                        CampaignId = campaign.Id,
                        MetaAdSetId = adSet.Id,
                        Name = adSet.Name,
                        Status = adSet.Status,
                        Budget = adSet.DailyBudget,
                        BillingEvent = adSet.BillingEvent,
                        OptimizationGoal = adSet.OptimizationGoal,
                        BidStrategy = string.Empty,
                        TargetingJson = adSet.TargetingJson
                    });
                }
                else
                {
                    existing.Name = adSet.Name;
                    existing.Status = adSet.Status;
                    existing.Budget = adSet.DailyBudget;
                    existing.BillingEvent = adSet.BillingEvent;
                    existing.OptimizationGoal = adSet.OptimizationGoal;
                    existing.TargetingJson = adSet.TargetingJson;
                }
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SyncAdsAsync(Guid tenantId, string adAccountId, CancellationToken cancellationToken = default)
    {
        var adSets = await _dbContext.AdSets.Where(x => x.TenantId == tenantId && x.Campaign.AdAccount.MetaAccountId == adAccountId)
            .Select(x => new { x.Id, x.MetaAdSetId })
            .ToListAsync(cancellationToken);

        foreach (var adSet in adSets)
        {
            var ads = await GetDataAsync(tenantId,
                $"{adSet.MetaAdSetId}/ads?fields=id,adset_id,name,status,creative",
                e => new MetaAdDto(
                    e.GetProperty("id").GetString() ?? string.Empty,
                    TryGetString(e, "adset_id"),
                    TryGetString(e, "name"),
                    TryGetString(e, "status"),
                    e.TryGetProperty("creative", out var creative) ? creative.GetRawText() : "{}"),
                cancellationToken);

            foreach (var ad in ads)
            {
                var existing = await _dbContext.Ads.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.MetaAdId == ad.Id, cancellationToken);
                if (existing is null)
                {
                    _dbContext.Ads.Add(new Ad
                    {
                        TenantId = tenantId,
                        AdSetId = adSet.Id,
                        MetaAdId = ad.Id,
                        Name = ad.Name,
                        Status = ad.Status,
                        CreativeJson = ad.CreativeJson
                    });
                }
                else
                {
                    existing.Name = ad.Name;
                    existing.Status = ad.Status;
                    existing.CreativeJson = ad.CreativeJson;
                }
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SyncInsightsAsync(Guid tenantId, string adAccountId, DateOnly since, DateOnly until, CancellationToken cancellationToken = default)
    {
        var adAccount = await GetAdAccountAsync(tenantId, adAccountId, cancellationToken);
        var insights = await GetInsightsAsync(tenantId, adAccountId, since, until, cancellationToken);

        foreach (var insight in insights)
        {
            if (!DateOnly.TryParse(insight.DateStart, out var date))
                continue;

            var campaign = await _dbContext.Campaigns
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.MetaCampaignId == insight.CampaignId, cancellationToken);

            if (campaign is null)
                continue;

            var existing = await _dbContext.InsightsDaily.FirstOrDefaultAsync(x =>
                x.TenantId == tenantId &&
                x.AdAccountId == adAccount.Id &&
                x.CampaignId == campaign.Id &&
                x.Date == date,
                cancellationToken);

            if (existing is null)
            {
                _dbContext.InsightsDaily.Add(new InsightDaily
                {
                    TenantId = tenantId,
                    AdAccountId = adAccount.Id,
                    CampaignId = campaign?.Id,
                    Date = date,
                    Impressions = TryParseLong(insight.Impressions),
                    Clicks = TryParseLong(insight.Clicks),
                    Reach = 0,
                    LinkClicks = 0,
                    Spend = TryParseDecimal(insight.Spend),
                    Ctr = TryParseDecimal(insight.Ctr),
                    Cpc = 0,
                    Cpm = 0
                });
            }
            else
            {
                existing.Impressions = TryParseLong(insight.Impressions);
                existing.Clicks = TryParseLong(insight.Clicks);
                existing.Spend = TryParseDecimal(insight.Spend);
                existing.Ctr = TryParseDecimal(insight.Ctr);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<IReadOnlyCollection<T>> GetDataAsync<T>(Guid tenantId, string endpoint, Func<JsonElement, T> mapper, CancellationToken cancellationToken)
    {
        var accessToken = await GetAccessTokenAsync(tenantId, cancellationToken);
        var url = endpoint.Contains('?') ? $"{endpoint}&access_token={Uri.EscapeDataString(accessToken)}" : $"{endpoint}?access_token={Uri.EscapeDataString(accessToken)}";

        using var response = await _httpClient.GetAsync(url, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        await LogApiAsync(endpoint, HttpMethod.Get.Method, string.Empty, json, (int)response.StatusCode, cancellationToken);
        _logger.LogInformation("Meta API GET {Endpoint} responded with {StatusCode}", endpoint, response.StatusCode);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(json);
        var rootData = doc.RootElement.GetProperty("data");
        return rootData.EnumerateArray().Select(mapper).ToArray();
    }

    private async Task<string> PostForIdAsync(Guid tenantId, string endpoint, Dictionary<string, string> body, CancellationToken cancellationToken)
    {
        var json = await PostAsync(tenantId, endpoint, body, cancellationToken);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("id").GetString() ?? string.Empty;
    }

    private async Task<string> PostAsync(Guid tenantId, string endpoint, Dictionary<string, string> body, CancellationToken cancellationToken)
    {
        var accessToken = await GetAccessTokenAsync(tenantId, cancellationToken);
        body["access_token"] = accessToken;

        using var response = await _httpClient.PostAsJsonAsync(endpoint, body, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        await LogApiAsync(endpoint, HttpMethod.Post.Method, JsonSerializer.Serialize(body), json, (int)response.StatusCode, cancellationToken);
        _logger.LogInformation("Meta API POST {Endpoint} responded with {StatusCode}", endpoint, response.StatusCode);
        response.EnsureSuccessStatusCode();

        return json;
    }

    private async Task<string> GetAccessTokenAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var connection = await _dbContext.MetaConnections.AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken)
            ?? throw new InvalidOperationException("No hay conexión de Meta configurada para el tenant.");

        return _secretEncryptionService.Decrypt(connection.AccessToken);
    }

    private async Task<AdAccount> GetAdAccountAsync(Guid tenantId, string adAccountId, CancellationToken cancellationToken)
    {
        return await _dbContext.AdAccounts.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.MetaAccountId == adAccountId, cancellationToken)
            ?? throw new InvalidOperationException("Ad account no encontrada para el tenant.");
    }

    private async Task PersistCampaignAsync(Guid tenantId, string adAccountId, string campaignId, MetaCampaignCreateRequest request, CancellationToken cancellationToken)
    {
        var adAccount = await GetAdAccountAsync(tenantId, adAccountId, cancellationToken);

        var exists = await _dbContext.Campaigns.AnyAsync(x => x.TenantId == tenantId && x.MetaCampaignId == campaignId, cancellationToken);
        if (exists)
            return;

        _dbContext.Campaigns.Add(new Campaign
        {
            TenantId = tenantId,
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

    private async Task PersistAdSetAsync(Guid tenantId, string metaCampaignId, string adSetId, MetaAdSetCreateRequest request, CancellationToken cancellationToken)
    {
        var campaign = await _dbContext.Campaigns.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.MetaCampaignId == metaCampaignId, cancellationToken);
        if (campaign is null)
            return;

        var exists = await _dbContext.AdSets.AnyAsync(x => x.TenantId == tenantId && x.MetaAdSetId == adSetId, cancellationToken);
        if (exists)
            return;

        _dbContext.AdSets.Add(new AdSet
        {
            TenantId = tenantId,
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

    private static string TryGetString(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var value) ? value.GetString() ?? string.Empty : string.Empty;

    private static decimal TryGetDecimal(JsonElement element, string propertyName)
        => decimal.TryParse(TryGetString(element, propertyName), NumberStyles.Any, CultureInfo.InvariantCulture, out var value) ? value : 0;

    private static decimal TryParseDecimal(string value)
        => decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;

    private static long TryParseLong(string value)
        => long.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;
}
