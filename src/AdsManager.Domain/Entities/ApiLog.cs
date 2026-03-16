using AdsManager.Domain.Common;

namespace AdsManager.Domain.Entities;

public sealed class ApiLog : BaseEntity
{
    public string Provider { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string RequestJson { get; set; } = "{}";
    public string ResponseJson { get; set; } = "{}";
    public string Status { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public long DurationMs { get; set; }
}
