namespace OpenRouter.NET.Observability;

/// <summary>
/// Configuration options for OpenRouter.NET telemetry and observability.
/// All options are opt-in by default for privacy and performance.
/// </summary>
public class OpenRouterTelemetryOptions
{
    /// <summary>
    /// Enables or disables telemetry collection. Default: false (opt-in).
    /// When disabled, no spans or events are created (zero overhead).
    /// </summary>
    public bool EnableTelemetry { get; set; } = false;

    /// <summary>
    /// Captures raw HTTP request JSON payloads in events. Default: false (opt-in).
    /// Enable this for debugging but be aware of PII and payload size.
    /// </summary>
    public bool CaptureRawRequests { get; set; } = false;

    /// <summary>
    /// Captures raw HTTP response JSON payloads in events. Default: false (opt-in).
    /// Enable this for debugging but be aware of PII and payload size.
    /// </summary>
    public bool CaptureRawResponses { get; set; } = false;

    /// <summary>
    /// Captures prompt messages (user/system/assistant inputs) in structured events.
    /// Default: false (opt-in).
    /// </summary>
    public bool CapturePrompts { get; set; } = false;

    /// <summary>
    /// Captures completion messages (model outputs) in structured events.
    /// Default: false (opt-in).
    /// </summary>
    public bool CaptureCompletions { get; set; } = false;

    /// <summary>
    /// Captures individual streaming chunks as events. Default: false (opt-in).
    /// Warning: This can generate high event volume. Consider sampling.
    /// </summary>
    public bool CaptureStreamChunks { get; set; } = false;

    /// <summary>
    /// Captures tool arguments and results in spans. Default: false (opt-in).
    /// </summary>
    public bool CaptureToolDetails { get; set; } = false;

    /// <summary>
    /// Maximum size in bytes for event body content before truncation.
    /// Default: 32KB. Set to -1 for unlimited (not recommended).
    /// </summary>
    public int MaxEventBodySize { get; set; } = 32_000;

    /// <summary>
    /// Optional callback to sanitize sensitive data from prompts before logging.
    /// Return the sanitized prompt text.
    /// </summary>
    public Func<string, string>? SanitizePrompt { get; set; }

    /// <summary>
    /// Optional callback to sanitize sensitive data from completions before logging.
    /// Return the sanitized completion text.
    /// </summary>
    public Func<string, string>? SanitizeCompletion { get; set; }

    /// <summary>
    /// Optional callback to sanitize tool arguments before logging.
    /// Return the sanitized arguments JSON string.
    /// </summary>
    public Func<string, string>? SanitizeToolArguments { get; set; }

    /// <summary>
    /// Creates a default configuration with all telemetry disabled (opt-in).
    /// </summary>
    public static OpenRouterTelemetryOptions Default => new();

    /// <summary>
    /// Creates a configuration with all telemetry features enabled.
    /// Use with caution - may log sensitive data and generate high volume.
    /// </summary>
    public static OpenRouterTelemetryOptions FullTelemetry => new()
    {
        EnableTelemetry = true,
        CaptureRawRequests = true,
        CaptureRawResponses = true,
        CapturePrompts = true,
        CaptureCompletions = true,
        CaptureStreamChunks = true,
        CaptureToolDetails = true
    };

    /// <summary>
    /// Creates a recommended production configuration with core metrics enabled
    /// but sensitive content capture disabled.
    /// </summary>
    public static OpenRouterTelemetryOptions Production => new()
    {
        EnableTelemetry = true,
        CaptureRawRequests = false,
        CaptureRawResponses = false,
        CapturePrompts = false,
        CaptureCompletions = false,
        CaptureStreamChunks = false,
        CaptureToolDetails = false
    };

    /// <summary>
    /// Creates a debug configuration with detailed logging enabled.
    /// </summary>
    public static OpenRouterTelemetryOptions Debug => new()
    {
        EnableTelemetry = true,
        CaptureRawRequests = true,
        CaptureRawResponses = true,
        CapturePrompts = true,
        CaptureCompletions = true,
        CaptureStreamChunks = false,  // Still too verbose even for debug
        CaptureToolDetails = true
    };
}
