namespace AdsManager.Application.Configuration;

public sealed class CorsOptions
{
    public const string SectionName = "Cors";

    public string[] AllowedOrigins { get; init; } = [];
    public string[] AllowedMethods { get; init; } = [];
    public string[] AllowedHeaders { get; init; } = [];
    public bool AllowCredentials { get; init; }
}
