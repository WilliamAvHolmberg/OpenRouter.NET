using System.Diagnostics;

namespace OpenRouter.NET.Observability;

/// <summary>
/// ActivitySource for OpenRouter.NET telemetry.
/// Provides a centralized source for creating spans/activities for observability.
/// </summary>
internal static class OpenRouterActivitySource
{
    /// <summary>
    /// The name of the ActivitySource used for OpenRouter.NET telemetry.
    /// </summary>
    public const string SourceName = "OpenRouter.NET";

    /// <summary>
    /// The version of the ActivitySource, matching the package version.
    /// </summary>
    public const string SourceVersion = "0.5.0";

    /// <summary>
    /// The singleton ActivitySource instance.
    /// When no ActivityListener is registered, StartActivity returns null with zero overhead.
    /// </summary>
    public static readonly ActivitySource Instance = new(SourceName, SourceVersion);
}
