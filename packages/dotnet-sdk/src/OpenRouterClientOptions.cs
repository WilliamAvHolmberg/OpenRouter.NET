using OpenRouter.NET.Observability;

namespace OpenRouter.NET;

public class OpenRouterClientOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string? SiteUrl { get; set; }
    public string? SiteName { get; set; }
    public HttpClient? HttpClient { get; set; }
    public Action<string>? OnLogMessage { get; set; }
    public string BaseUrl { get; set; } = "https://openrouter.ai/api/v1";

    /// <summary>
    /// Telemetry and observability options. Default: all disabled (opt-in).
    /// </summary>
    public OpenRouterTelemetryOptions Telemetry { get; set; } = OpenRouterTelemetryOptions.Default;
}

