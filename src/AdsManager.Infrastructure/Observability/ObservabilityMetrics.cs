using System.Diagnostics.Metrics;
using AdsManager.Application.Interfaces;

namespace AdsManager.Infrastructure.Observability;

public sealed class ObservabilityMetrics : IObservabilityMetrics
{
    private static readonly Meter Meter = new("AdsManager.Observability", "1.0.0");
    private readonly Histogram<double> _metaApiLatencyMs;
    private readonly Counter<long> _metaApiErrorCount;
    private readonly Counter<long> _campaignCreationCount;
    private readonly Histogram<double> _syncDurationMs;

    public ObservabilityMetrics()
    {
        _metaApiLatencyMs = Meter.CreateHistogram<double>("meta_api_latency_ms", unit: "ms", description: "Latency of Meta API calls.");
        _metaApiErrorCount = Meter.CreateCounter<long>("meta_api_error_rate", unit: "errors", description: "Meta API errors count.");
        _campaignCreationCount = Meter.CreateCounter<long>("campaign_creation_rate", unit: "campaigns", description: "Campaigns created.");
        _syncDurationMs = Meter.CreateHistogram<double>("sync_duration_ms", unit: "ms", description: "Duration of sync jobs.");
    }

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
}
