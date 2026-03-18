using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AdsManager.Application.DTOs.Meta;
using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Meta;
using AdsManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace AdsManager.Infrastructure.Integrations.Meta;

public sealed class MetaAdsService : IMetaAdsService
{
    private const string BaseUrl = "https://graph.facebook.com/v25.0/";
    private const string CampaignEntityType = "Campaign";
    private const string AdSetEntityType = "AdSet";
    private const string AdEntityType = "Ad";
    private const string InsightEntityType = "Insight";
    private readonly HttpClient _httpClient;
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<MetaAdsService> _logger;
    private readonly ISecretEncryptionService _secretEncryptionService;
    private readonly ITenantProvider _tenantProvider;
    private readonly IObservabilityMetrics _observabilityMetrics;
    private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;
    private readonly AsyncCircuitBreakerPolicy _circuitBreakerPolicy;
    private readonly AsyncTimeoutPolicy<HttpResponseMessage> _timeoutPolicy;

    public MetaAdsService(HttpClient httpClient, IApplicationDbContext dbContext, ILogger<MetaAdsService> logger, ISecretEncryptionService secretEncryptionService, ITenantProvider tenantProvider, IObservabilityMetrics observabilityMetrics)
    {
        _httpClient = httpClient;
        _dbContext = dbContext;
        _logger = logger;
        _secretEncryptionService = secretEncryptionService;
        _tenantProvider = tenantProvider;
        _observabilityMetrics = observabilityMetrics;
        _httpClient.BaseAddress = new Uri(BaseUrl);

        _retryPolicy = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .OrResult(IsTransientResponse)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (outcome, delay, attempt, _) =>
                {
                    var statusCode = outcome.Result?.StatusCode;
                    _logger.LogWarning(
                        "Retry Meta API attempt {Attempt} in {Delay}s due to status {StatusCode} or transient network failure",
                        attempt,
                        delay.TotalSeconds,
                        statusCode);
                });

        _circuitBreakerPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .Or<TimeoutRejectedException>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (exception, breakDelay) =>
                {
                    _logger.LogError(exception, "Meta API circuit opened for {BreakDelay}s", breakDelay.TotalSeconds);
                },
                onReset: () => _logger.LogInformation("Meta API circuit reset"),
                onHalfOpen: () => _logger.LogInformation("Meta API circuit half-open, testing next request"));

        _timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(
            TimeSpan.FromSeconds(15),
            TimeoutStrategy.Optimistic,
            onTimeoutAsync: (_, timeout, _, _) =>
            {
                _logger.LogError("Meta API timeout after {TimeoutSeconds}s", timeout.TotalSeconds);
                return Task.CompletedTask;
            });
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


    public Task UpdateAdSetAsync(Guid tenantId, MetaAdSetUpdateRequest request, CancellationToken cancellationToken = default)
        => PostAsync(
            tenantId,
            request.AdSetId,
            new Dictionary<string, string>
            {
                ["name"] = request.Name,
                ["status"] = request.Status,
                ["daily_budget"] = Convert.ToInt64(request.DailyBudget).ToString(),
                ["billing_event"] = request.BillingEvent,
                ["optimization_goal"] = request.OptimizationGoal,
                ["targeting"] = request.TargetingJson
            },
            cancellationToken);

    public Task UpdateAdSetStatusAsync(Guid tenantId, MetaAdSetStatusUpdateRequest request, CancellationToken cancellationToken = default)
        => PostAsync(tenantId, request.AdSetId, new Dictionary<string, string> { ["status"] = request.Status }, cancellationToken);

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

    public Task UpdateAdAsync(Guid tenantId, MetaAdUpdateRequest request, CancellationToken cancellationToken = default)
        => PostAsync(tenantId,
            request.AdId,
            new Dictionary<string, string>
            {
                ["name"] = request.Name,
                ["status"] = request.Status,
                ["creative"] = request.CreativeJson
            },
            cancellationToken);

    public Task UpdateAdStatusAsync(Guid tenantId, MetaAdStatusUpdateRequest request, CancellationToken cancellationToken = default)
        => PostAsync(tenantId, request.AdId, new Dictionary<string, string> { ["status"] = request.Status }, cancellationToken);

    public Task<IReadOnlyCollection<MetaInsightDto>> GetInsightsAsync(Guid tenantId, string adAccountId, DateOnly since, DateOnly until, string level = "campaign", CancellationToken cancellationToken = default)
    {
        var effectiveLevel = level is "campaign" or "adset" or "ad" ? level : "campaign";

        return GetDataAsync(tenantId,
            $"act_{adAccountId}/insights?fields=date_start,date_stop,campaign_id,campaign_name,adset_id,adset_name,ad_id,ad_name,spend,impressions,reach,clicks,ctr,cpc,cpm,actions&time_range={{\"since\":\"{since:yyyy-MM-dd}\",\"until\":\"{until:yyyy-MM-dd}\"}}&level={effectiveLevel}",
            e => new MetaInsightDto(
                e.TryGetProperty("date_start", out var dateStart) ? dateStart.GetString() ?? string.Empty : string.Empty,
                e.TryGetProperty("date_stop", out var dateStop) ? dateStop.GetString() ?? string.Empty : string.Empty,
                e.TryGetProperty("campaign_id", out var campaignId) ? campaignId.GetString() ?? string.Empty : string.Empty,
                e.TryGetProperty("campaign_name", out var campaignName) ? campaignName.GetString() ?? string.Empty : string.Empty,
                e.TryGetProperty("adset_id", out var adSetId) ? adSetId.GetString() ?? string.Empty : string.Empty,
                e.TryGetProperty("adset_name", out var adSetName) ? adSetName.GetString() ?? string.Empty : string.Empty,
                e.TryGetProperty("ad_id", out var adId) ? adId.GetString() ?? string.Empty : string.Empty,
                e.TryGetProperty("ad_name", out var adName) ? adName.GetString() ?? string.Empty : string.Empty,
                e.TryGetProperty("spend", out var spend) ? spend.GetString() ?? "0" : "0",
                e.TryGetProperty("impressions", out var impressions) ? impressions.GetString() ?? "0" : "0",
                e.TryGetProperty("reach", out var reach) ? reach.GetString() ?? "0" : "0",
                e.TryGetProperty("clicks", out var clicks) ? clicks.GetString() ?? "0" : "0",
                ParseLinkClicks(e).ToString(CultureInfo.InvariantCulture),
                e.TryGetProperty("ctr", out var ctr) ? ctr.GetString() ?? "0" : "0",
                e.TryGetProperty("cpc", out var cpc) ? cpc.GetString() ?? "0" : "0",
                e.TryGetProperty("cpm", out var cpm) ? cpm.GetString() ?? "0" : "0"),
            cancellationToken);
    }

    public async Task SyncCampaignsAsync(Guid tenantId, string adAccountId, CancellationToken cancellationToken = default)
    {
        var cursor = await GetOrCreateSyncCursorAsync(tenantId, adAccountId, CampaignEntityType, cancellationToken);
        var adAccount = await GetAdAccountAsync(tenantId, adAccountId, cancellationToken);
        var campaigns = await GetDataAsync(tenantId,
            $"act_{adAccountId}/campaigns?fields=id,name,status,objective&updated_since={new DateTimeOffset(cursor.LastSyncedAt).ToUnixTimeSeconds()}",
            e => new MetaCampaignDto(
                e.GetProperty("id").GetString() ?? string.Empty,
                e.GetProperty("name").GetString() ?? string.Empty,
                e.GetProperty("status").GetString() ?? string.Empty,
                e.TryGetProperty("objective", out var objective) ? objective.GetString() ?? string.Empty : string.Empty),
            cancellationToken);

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
        await UpdateSyncCursorAsync(cursor, cancellationToken);
    }

    public async Task SyncAdSetsAsync(Guid tenantId, string adAccountId, CancellationToken cancellationToken = default)
    {
        var cursor = await GetOrCreateSyncCursorAsync(tenantId, adAccountId, AdSetEntityType, cancellationToken);
        var campaigns = await _dbContext.Campaigns.Where(x => x.TenantId == tenantId && x.AdAccount.MetaAccountId == adAccountId)
            .Select(x => new { x.Id, x.MetaCampaignId })
            .ToListAsync(cancellationToken);

        foreach (var campaign in campaigns)
        {
            var adSets = await GetDataAsync(tenantId,
                $"{campaign.MetaCampaignId}/adsets?fields=id,campaign_id,name,status,daily_budget,billing_event,optimization_goal,targeting&updated_since={new DateTimeOffset(cursor.LastSyncedAt).ToUnixTimeSeconds()}",
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
        await UpdateSyncCursorAsync(cursor, cancellationToken);
    }

    public async Task SyncAdsAsync(Guid tenantId, string adAccountId, CancellationToken cancellationToken = default)
    {
        var cursor = await GetOrCreateSyncCursorAsync(tenantId, adAccountId, AdEntityType, cancellationToken);
        var adSets = await _dbContext.AdSets.Where(x => x.TenantId == tenantId && x.Campaign.AdAccount.MetaAccountId == adAccountId)
            .Select(x => new { x.Id, x.MetaAdSetId })
            .ToListAsync(cancellationToken);

        foreach (var adSet in adSets)
        {
            var ads = await GetDataAsync(tenantId,
                $"{adSet.MetaAdSetId}/ads?fields=id,adset_id,name,status,creative&updated_since={new DateTimeOffset(cursor.LastSyncedAt).ToUnixTimeSeconds()}",
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
        await UpdateSyncCursorAsync(cursor, cancellationToken);
    }

    public async Task SyncInsightsAsync(Guid tenantId, string adAccountId, DateOnly since, DateOnly until, CancellationToken cancellationToken = default)
    {
        var cursor = await GetOrCreateSyncCursorAsync(tenantId, adAccountId, InsightEntityType, cancellationToken);
        var cursorDate = DateOnly.FromDateTime(cursor.LastSyncedAt);
        var effectiveSince = cursorDate > since ? cursorDate : since;

        var adAccount = await GetAdAccountAsync(tenantId, adAccountId, cancellationToken);
        var campaignInsights = await GetInsightsAsync(tenantId, adAccountId, effectiveSince, until, "campaign", cancellationToken);
        var adSetInsights = await GetInsightsAsync(tenantId, adAccountId, effectiveSince, until, "adset", cancellationToken);
        var adInsights = await GetInsightsAsync(tenantId, adAccountId, effectiveSince, until, "ad", cancellationToken);

        var campaignsByMetaId = await _dbContext.Campaigns
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.AdAccountId == adAccount.Id)
            .ToDictionaryAsync(x => x.MetaCampaignId, cancellationToken);

        var adSetsByMetaId = await _dbContext.AdSets
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .ToDictionaryAsync(x => x.MetaAdSetId, cancellationToken);

        var adsByMetaId = await _dbContext.Ads
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .ToDictionaryAsync(x => x.MetaAdId, cancellationToken);

        await UpsertInsightsAsync(campaignInsights, adAccount.Id, tenantId, campaignsByMetaId, adSetsByMetaId, adsByMetaId, cancellationToken);
        await UpsertInsightsAsync(adSetInsights, adAccount.Id, tenantId, campaignsByMetaId, adSetsByMetaId, adsByMetaId, cancellationToken);
        await UpsertInsightsAsync(adInsights, adAccount.Id, tenantId, campaignsByMetaId, adSetsByMetaId, adsByMetaId, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await UpdateSyncCursorAsync(cursor, cancellationToken);
    }


    private async Task UpsertInsightsAsync(
        IReadOnlyCollection<MetaInsightDto> insights,
        Guid adAccountId,
        Guid tenantId,
        IReadOnlyDictionary<string, Campaign> campaignsByMetaId,
        IReadOnlyDictionary<string, AdSet> adSetsByMetaId,
        IReadOnlyDictionary<string, Ad> adsByMetaId,
        CancellationToken cancellationToken)
    {
        foreach (var insight in insights)
        {
            if (!DateOnly.TryParse(insight.DateStart, out var date))
                continue;

            campaignsByMetaId.TryGetValue(insight.CampaignId, out var campaign);
            adSetsByMetaId.TryGetValue(insight.AdSetId, out var adSet);
            adsByMetaId.TryGetValue(insight.AdId, out var ad);

            var campaignId = campaign?.Id;
            var adSetId = adSet?.Id;
            var adId = ad?.Id;

            var existing = await _dbContext.InsightsDaily.FirstOrDefaultAsync(x =>
                x.TenantId == tenantId &&
                x.AdAccountId == adAccountId &&
                x.CampaignId == campaignId &&
                x.AdSetId == adSetId &&
                x.AdId == adId &&
                x.Date == date,
                cancellationToken);

            if (existing is null)
            {
                _dbContext.InsightsDaily.Add(new InsightDaily
                {
                    TenantId = tenantId,
                    AdAccountId = adAccountId,
                    CampaignId = campaignId,
                    AdSetId = adSetId,
                    AdId = adId,
                    Date = date,
                    Impressions = TryParseLong(insight.Impressions),
                    Clicks = TryParseLong(insight.Clicks),
                    Reach = TryParseLong(insight.Reach),
                    LinkClicks = TryParseLong(insight.LinkClicks),
                    Spend = TryParseDecimal(insight.Spend),
                    Ctr = TryParseDecimal(insight.Ctr),
                    Cpc = TryParseDecimal(insight.Cpc),
                    Cpm = TryParseDecimal(insight.Cpm)
                });
            }
            else
            {
                existing.Impressions = TryParseLong(insight.Impressions);
                existing.Clicks = TryParseLong(insight.Clicks);
                existing.Reach = TryParseLong(insight.Reach);
                existing.LinkClicks = TryParseLong(insight.LinkClicks);
                existing.Spend = TryParseDecimal(insight.Spend);
                existing.Ctr = TryParseDecimal(insight.Ctr);
                existing.Cpc = TryParseDecimal(insight.Cpc);
                existing.Cpm = TryParseDecimal(insight.Cpm);
            }
        }
    }

    private static long ParseLinkClicks(JsonElement element)
    {
        if (!element.TryGetProperty("actions", out var actions) || actions.ValueKind != JsonValueKind.Array)
            return 0;

        foreach (var action in actions.EnumerateArray())
        {
            if (!action.TryGetProperty("action_type", out var type) || type.GetString() != "link_click")
                continue;

            if (!action.TryGetProperty("value", out var value))
                continue;

            if (long.TryParse(value.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
                return parsed;
        }

        return 0;
    }

    private async Task<IReadOnlyCollection<T>> GetDataAsync<T>(Guid tenantId, string endpoint, Func<JsonElement, T> mapper, CancellationToken cancellationToken)
    {
        var accessToken = await GetAccessTokenAsync(tenantId, cancellationToken);
        var url = endpoint.Contains('?') ? $"{endpoint}&access_token={Uri.EscapeDataString(accessToken)}" : $"{endpoint}?access_token={Uri.EscapeDataString(accessToken)}";

        using var response = await ExecuteWithResilienceAsync(
            endpoint,
            HttpMethod.Get.Method,
            string.Empty,
            ct => _httpClient.GetAsync(url, ct),
            cancellationToken);

        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        ThrowIfUnsuccessful(response, json);

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
        var requestJson = JsonSerializer.Serialize(body);

        using var response = await ExecuteWithResilienceAsync(
            endpoint,
            HttpMethod.Post.Method,
            requestJson,
            ct => _httpClient.PostAsJsonAsync(endpoint, body, ct),
            cancellationToken);

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        ThrowIfUnsuccessful(response, json);

        return json;
    }

    private async Task<HttpResponseMessage> ExecuteWithResilienceAsync(
        string endpoint,
        string method,
        string request,
        Func<CancellationToken, Task<HttpResponseMessage>> operation,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        HttpResponseMessage? response = null;
        string responseBody = "{}";
        string status = "Unknown";
        int statusCode = 0;

        try
        {
            response = await _retryPolicy.ExecuteAsync(async ct =>
                await _circuitBreakerPolicy.ExecuteAsync(async innerCt =>
                {
                    var result = await _timeoutPolicy.ExecuteAsync(timeoutCt => operation(timeoutCt), innerCt);
                    if (IsTransientResponse(result))
                    {
                        throw new HttpRequestException($"Meta API transient status code: {(int)result.StatusCode}");
                    }

                    return result;
                }, ct), cancellationToken);

            statusCode = (int)response.StatusCode;
            status = response.IsSuccessStatusCode ? "Success" : "HttpError";
            responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.StatusCode == (HttpStatusCode)429)
            {
                _logger.LogWarning("Meta API rate limit hit on {Endpoint}. StatusCode={StatusCode}", endpoint, statusCode);
            }
            else if ((int)response.StatusCode >= 500)
            {
                _logger.LogError("Meta API server error on {Endpoint}. StatusCode={StatusCode}", endpoint, statusCode);
            }

            _logger.LogInformation("Meta API {Method} {Endpoint} responded with {StatusCode}", method, endpoint, response.StatusCode);
            return response;
        }
        catch (BrokenCircuitException ex)
        {
            status = "CircuitOpen";
            responseBody = ex.Message;
            _logger.LogError(ex, "Meta API circuit breaker open for {Endpoint}", endpoint);
            throw;
        }
        catch (TimeoutRejectedException ex)
        {
            status = "Timeout";
            responseBody = ex.Message;
            _logger.LogError(ex, "Meta API timeout for {Endpoint}", endpoint);
            throw;
        }
        catch (HttpRequestException ex)
        {
            status = "NetworkFailure";
            responseBody = ex.Message;
            _logger.LogError(ex, "Meta API network failure for {Endpoint}", endpoint);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _observabilityMetrics.RecordMetaApiLatency(stopwatch.Elapsed.TotalMilliseconds, endpoint, method, status);
            if (status != "Success")
            {
                _observabilityMetrics.RecordMetaApiError(endpoint, method, status);
            }

            await LogApiAsync(endpoint, method, request, responseBody, statusCode, status, stopwatch.ElapsedMilliseconds, cancellationToken);
        }
    }

    private static bool IsTransientResponse(HttpResponseMessage response)
        => response.StatusCode == HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500;

    private static void ThrowIfUnsuccessful(HttpResponseMessage response, string responseBody)
    {
        if (response.IsSuccessStatusCode)
            return;

        throw new HttpRequestException(BuildMetaErrorMessage(response, responseBody), null, response.StatusCode);
    }

    private static string BuildMetaErrorMessage(HttpResponseMessage response, string responseBody)
    {
        if (!string.IsNullOrWhiteSpace(responseBody))
        {
            try
            {
                using var document = JsonDocument.Parse(responseBody);
                if (document.RootElement.TryGetProperty("error", out var error))
                {
                    var userMessage = TryGetString(error, "error_user_msg");
                    var message = TryGetString(error, "message");
                    var errorType = TryGetString(error, "type");
                    var errorCode = TryGetString(error, "code");

                    if (!string.IsNullOrWhiteSpace(userMessage))
                        return userMessage;

                    if (!string.IsNullOrWhiteSpace(message) && !string.IsNullOrWhiteSpace(errorType) && !string.IsNullOrWhiteSpace(errorCode))
                        return $"{message} ({errorType} - {errorCode})";

                    if (!string.IsNullOrWhiteSpace(message))
                        return message;
                }
            }
            catch (JsonException)
            {
            }
        }

        return $"Meta API request failed with status code {(int)response.StatusCode}.";
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


    private async Task<SyncCursor> GetOrCreateSyncCursorAsync(Guid tenantId, string adAccountId, string entityType, CancellationToken cancellationToken)
    {
        var cursor = await _dbContext.SyncCursors
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.AdAccountId == adAccountId && x.EntityType == entityType, cancellationToken);

        if (cursor is not null)
            return cursor;

        cursor = new SyncCursor
        {
            TenantId = tenantId,
            AdAccountId = adAccountId,
            EntityType = entityType,
            LastSyncedAt = DateTime.UnixEpoch
        };

        _dbContext.SyncCursors.Add(cursor);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return cursor;
    }

    private async Task UpdateSyncCursorAsync(SyncCursor cursor, CancellationToken cancellationToken)
    {
        cursor.LastSyncedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task LogApiAsync(string endpoint, string method, string requestJson, string responseJson, int statusCode, string status, long durationMs, CancellationToken cancellationToken)
    {
        _dbContext.ApiLogs.Add(new ApiLog
        {
            Provider = "Meta",
            Endpoint = endpoint,
            Method = method,
            RequestJson = requestJson,
            ResponseJson = responseJson,
            StatusCode = statusCode,
            Status = status,
            DurationMs = durationMs,
            TraceId = _tenantProvider.GetTraceId()
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
