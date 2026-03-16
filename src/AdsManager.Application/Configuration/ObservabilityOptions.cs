namespace AdsManager.Application.Configuration;

public sealed class ObservabilityOptions
{
    public const string SectionName = "Observability";

    public bool EnablePrometheus { get; set; } = true;
    public string MetricsEndpoint { get; set; } = "/metrics";
}
