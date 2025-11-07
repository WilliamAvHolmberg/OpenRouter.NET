using System.Diagnostics;
using OpenTelemetry.Trace;

namespace OpenRouter.NET.Observability;

/// <summary>
/// Extension methods for integrating OpenRouter.NET with OpenTelemetry
/// </summary>
public static class OpenTelemetryExtensions
{
    /// <summary>
    /// Adds OpenRouter.NET instrumentation to the OpenTelemetry TracerProviderBuilder.
    /// This enables automatic tracing of OpenRouter API calls.
    /// </summary>
    /// <param name="builder">The TracerProviderBuilder to add the source to</param>
    /// <returns>The TracerProviderBuilder for method chaining</returns>
    /// <example>
    /// <code>
    /// services.AddOpenTelemetry()
    ///     .WithTracing(tracerProvider =>
    ///     {
    ///         tracerProvider
    ///             .AddOpenRouterInstrumentation()
    ///             .AddOtlpExporter(options =>
    ///             {
    ///                 options.Endpoint = new Uri("http://localhost:4317");
    ///             });
    ///     });
    /// </code>
    /// </example>
    public static TracerProviderBuilder AddOpenRouterInstrumentation(this TracerProviderBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder.AddSource(OpenRouterActivitySource.SourceName);
    }

    /// <summary>
    /// Creates an ActivityListener that listens to OpenRouter.NET telemetry.
    /// Use this for manual subscription without OpenTelemetry SDK.
    /// </summary>
    /// <param name="sample">Sampling function. Return ActivitySamplingResult.AllDataAndRecorded to capture.</param>
    /// <param name="activityStarted">Called when an activity starts</param>
    /// <param name="activityStopped">Called when an activity stops</param>
    /// <returns>An ActivityListener configured for OpenRouter.NET</returns>
    /// <example>
    /// <code>
    /// var listener = OpenTelemetryExtensions.CreateOpenRouterActivityListener(
    ///     sample: (ref ActivityCreationOptions&lt;ActivityContext&gt; options) => ActivitySamplingResult.AllDataAndRecorded,
    ///     activityStarted: activity => Console.WriteLine($"Started: {activity.DisplayName}"),
    ///     activityStopped: activity => Console.WriteLine($"Stopped: {activity.DisplayName} ({activity.Duration})")
    /// );
    /// ActivitySource.AddActivityListener(listener);
    /// </code>
    /// </example>
    public static ActivityListener CreateOpenRouterActivityListener(
        Func<ActivityCreationOptions<ActivityContext>, ActivitySamplingResult>? sample = null,
        Action<Activity>? activityStarted = null,
        Action<Activity>? activityStopped = null)
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == OpenRouterActivitySource.SourceName,
            Sample = sample ?? ((ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded),
            ActivityStarted = activityStarted,
            ActivityStopped = activityStopped
        };

        return listener;
    }
}
