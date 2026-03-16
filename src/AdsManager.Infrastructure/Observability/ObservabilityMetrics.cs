using System.Diagnostics.Metrics;
using AdsManager.Application.Interfaces;

namespace AdsManager.Infrastructure.Observability;

public sealed class ObservabilityMetrics : IObservabilityMetrics
{
    private static readonly Meter Meter = new("AdsManager.Observability", "1.0.0");
    private readonly Histogram<double> _httpRequestDurationMs;
    private readonly Counter<long> _httpRequestErrorCount;
    private readonly Histogram<double> _metaApiLatencyMs;
    private readonly Counter<long> _metaApiErrorCount;
    private readonly Counter<long> _campaignCreationCount;
    private readonly Histogram<double> _syncDurationMs;
    private readonly Counter<long> _cacheHitCount;
    private readonly Counter<long> _cacheMissCount;
    private readonly Counter<long> _ruleExecutionCount;

    public ObservabilityMetrics()
    {
        _httpRequestDurationMs = Meter.CreateHistogram<double>("http_request_duration_ms", unit: "ms", description: "Duration of HTTP requests.");
        _httpRequestErrorCount = Meter.CreateCounter<long>("http_request_errors_total", unit: "errors", description: "Count of HTTP requests that completed with errors.");
        _metaApiLatencyMs = Meter.CreateHistogram<double>("meta_api_latency_ms", unit: "ms", description: "Latency of Meta API calls.");
        _metaApiErrorCount = Meter.CreateCounter<long>("meta_api_errors_total", unit: "errors", description: "Meta API errors count.");
        _campaignCreationCount = Meter.CreateCounter<long>("campaign_creation_total", unit: "campaigns", description: "Campaigns created.");
        _syncDurationMs = Meter.CreateHistogram<double>("sync_duration_ms", unit: "ms", description: "Duration of sync jobs.");
        _cacheHitCount = Meter.CreateCounter<long>("cache_hits_total", unit: "hits", description: "Cache hits count.");
        _cacheMissCount = Meter.CreateCounter<long>("cache_misses_total", unit: "misses", description: "Cache misses count.");
        _ruleExecutionCount = Meter.CreateCounter<long>("rule_executions_total", unit: "executions", description: "Rule execution count.");
    }

    public void RecordHttpRequestDuration(double durationMs, string method, string route, int statusCode)
        => _httpRequestDurationMs.Record(durationMs,
            new KeyValuePair<string, object?>("method", method),
            new KeyValuePair<string, object?>("route", route),
            new KeyValuePair<string, object?>("status_code", statusCode));

    public void RecordHttpRequestError(string method, string route, int statusCode)
        => _httpRequestErrorCount.Add(1,
            new KeyValuePair<string, object?>("method", method),
            new KeyValuePair<string, object?>("route", route),
            new KeyValuePair<string, object?>("status_code", statusCode));

    public void RecordMetaApiLatency(double durationMs, string endpoint, string method, string status)
        => _metaApiLatencyMs.Record(durationMs,
            new KeyValuePair<string, object?>("endpoint", endpoint),
            new KeyValuePair<string, object?>("method", method),
            new KeyValuePair<string, object?>("status", status));

    public void RecordMetaApiError(string endpoint, string method, string status)
        => _metaApiErrorCount.Add(1,
            new KeyValuePair<string, object?>("endpoint", endpoint),
            new KeyValuePair<string, object?>("method", method),
            new KeyValuePair<string, object?>("status", status));

    public void IncrementCampaignCreation()
        => _campaignCreationCount.Add(1);

    public void RecordSyncDuration(string jobName, double durationMs, string status)
        => _syncDurationMs.Record(durationMs,
            new KeyValuePair<string, object?>("job", jobName),
            new KeyValuePair<string, object?>("status", status));

    public void RecordCacheHit(string provider, string cacheName)
        => _cacheHitCount.Add(1,
            new KeyValuePair<string, object?>("provider", provider),
            new KeyValuePair<string, object?>("cache", cacheName));

    public void RecordCacheMiss(string provider, string cacheName)
        => _cacheMissCount.Add(1,
            new KeyValuePair<string, object?>("provider", provider),
            new KeyValuePair<string, object?>("cache", cacheName));

    public void RecordRuleExecution(string ruleAction, string status)
        => _ruleExecutionCount.Add(1,
            new KeyValuePair<string, object?>("action", ruleAction),
            new KeyValuePair<string, object?>("status", status));
}
