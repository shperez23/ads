namespace AdsManager.Application.Interfaces;

public interface IObservabilityMetrics
{
    void RecordHttpRequestDuration(double durationMs, string method, string route, int statusCode);
    void RecordHttpRequestError(string method, string route, int statusCode);
    void RecordMetaApiLatency(double durationMs, string endpoint, string method, string status);
    void RecordMetaApiError(string endpoint, string method, string status);
    void IncrementCampaignCreation();
    void RecordSyncDuration(string jobName, double durationMs, string status);
    void RecordCacheHit(string provider, string cacheName);
    void RecordCacheMiss(string provider, string cacheName);
    void RecordRuleExecution(string ruleAction, string status);
}
