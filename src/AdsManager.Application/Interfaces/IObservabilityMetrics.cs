namespace AdsManager.Application.Interfaces;

public interface IObservabilityMetrics
{
    void RecordMetaApiLatency(double durationMs, string endpoint, string method, string status);
    void RecordMetaApiError(string endpoint, string method, string status);
    void IncrementCampaignCreation();
    void RecordSyncDuration(string jobName, double durationMs, string status);
}
