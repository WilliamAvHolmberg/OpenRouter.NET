# OpenRouter.NET Observability Guide

## Overview

OpenRouter.NET includes built-in observability support using **OpenTelemetry**, enabling comprehensive tracking of LLM API calls, token usage, performance metrics, and more. This guide shows you how to integrate with observability platforms like **Arize Phoenix**, **Jaeger**, **Zipkin**, or any OpenTelemetry-compatible backend.

## Features

✅ **Request & Response Tracing** - Track all API calls with detailed attributes
✅ **Token Usage Tracking** - Monitor input/output tokens and costs
✅ **Streaming Metrics** - Time-to-first-token, chunks/sec, duration
✅ **Tool Execution Spans** - Child spans for tool calls with arguments and results
✅ **Error Tracking** - Automatic exception recording with stack traces
✅ **Privacy Controls** - Opt-in capture with sanitization callbacks
✅ **Zero Overhead** - No performance impact when disabled

## Quick Start

### 1. Install OpenTelemetry Packages

```bash
dotnet add package OpenTelemetry
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol
dotnet add package OpenTelemetry.Extensions.Hosting  # For ASP.NET Core
```

### 2. Configure Telemetry

**Option A: Full Telemetry (Development/Debug)**

```csharp
using OpenRouter.NET;
using OpenRouter.NET.Observability;
using OpenTelemetry.Trace;

// Configure OpenTelemetry
var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddOpenRouterInstrumentation()  // Add OpenRouter source
    .AddConsoleExporter()  // Export to console for testing
    .Build();

// Create client with telemetry enabled
var client = new OpenRouterClient(new OpenRouterClientOptions
{
    ApiKey = "your-api-key",
    Telemetry = OpenRouterTelemetryOptions.Debug  // Captures everything
});

// Make API calls - telemetry is automatic!
var response = await client.CreateChatCompletionAsync(new ChatCompletionRequest
{
    Model = "anthropic/claude-3.5-sonnet",
    Messages = new List<Message>
    {
        Message.FromUser("Explain quantum computing in simple terms")
    }
});
```

**Option B: Production (Metrics Only)**

```csharp
var client = new OpenRouterClient(new OpenRouterClientOptions
{
    ApiKey = "your-api-key",
    Telemetry = OpenRouterTelemetryOptions.Production  // No sensitive content
});
```

**Option C: Custom Configuration**

```csharp
var client = new OpenRouterClient(new OpenRouterClientOptions
{
    ApiKey = "your-api-key",
    Telemetry = new OpenRouterTelemetryOptions
    {
        EnableTelemetry = true,
        CapturePrompts = true,
        CaptureCompletions = true,
        CaptureToolDetails = true,
        MaxEventBodySize = 64_000,  // 64KB limit

        // Sanitize sensitive data
        SanitizePrompt = prompt => prompt.Replace("SECRET", "[REDACTED]"),
        SanitizeCompletion = completion => completion  // Pass through
    }
});
```

## Integration with Arize Phoenix

### 1. Start Phoenix

**Using Docker:**
```bash
docker run -p 6006:6006 -p 4317:4317 arizephoenix/phoenix:latest
```

**Using Python:**
```bash
pip install arize-phoenix
phoenix serve
```

### 2. Configure OpenRouter Client

```csharp
using OpenTelemetry.Exporter;
using OpenTelemetry.Trace;

var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddOpenRouterInstrumentation()
    .AddOtlpExporter(options =>
    {
        options.Endpoint = new Uri("http://localhost:4317");  // Phoenix OTLP endpoint
        options.Protocol = OtlpExportProtocol.Grpc;
    })
    .Build();

var client = new OpenRouterClient(new OpenRouterClientOptions
{
    ApiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY"),
    Telemetry = new OpenRouterTelemetryOptions
    {
        EnableTelemetry = true,
        CapturePrompts = true,
        CaptureCompletions = true,
        CaptureToolDetails = true
    }
});
```

### 3. View Traces

Open http://localhost:6006 in your browser to see:
- **Request traces** with full prompt/completion history
- **Token usage** and cost tracking
- **Latency metrics** (time-to-first-token, total duration)
- **Tool execution** details
- **Error rates** and exception traces

## ASP.NET Core Integration

```csharp
// Program.cs
using OpenRouter.NET;
using OpenRouter.NET.Observability;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Add OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("my-api"))
    .WithTracing(tracerProvider =>
    {
        tracerProvider
            .AddAspNetCoreInstrumentation()  // HTTP requests
            .AddHttpClientInstrumentation()  // Outgoing HTTP
            .AddOpenRouterInstrumentation()  // OpenRouter LLM calls
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri("http://localhost:4317");
            });
    });

// Register OpenRouter client
builder.Services.AddSingleton(sp => new OpenRouterClient(new OpenRouterClientOptions
{
    ApiKey = builder.Configuration["OpenRouter:ApiKey"],
    Telemetry = new OpenRouterTelemetryOptions
    {
        EnableTelemetry = true,
        CapturePrompts = true,
        CaptureCompletions = true
    }
}));

var app = builder.Build();
app.MapControllers();
app.Run();
```

## Telemetry Configuration Options

| Option | Default | Description |
|--------|---------|-------------|
| `EnableTelemetry` | `false` | Master switch for telemetry (opt-in) |
| `CaptureRawRequests` | `false` | Log raw HTTP request JSON |
| `CaptureRawResponses` | `false` | Log raw HTTP response JSON |
| `CapturePrompts` | `false` | Log structured prompt messages |
| `CaptureCompletions` | `false` | Log structured completion messages |
| `CaptureStreamChunks` | `false` | Log individual streaming chunks (high volume!) |
| `CaptureToolDetails` | `false` | Log tool arguments and results |
| `MaxEventBodySize` | `32000` | Max bytes before truncation (32KB) |
| `SanitizePrompt` | `null` | Callback to redact sensitive prompt data |
| `SanitizeCompletion` | `null` | Callback to redact sensitive completion data |
| `SanitizeToolArguments` | `null` | Callback to redact sensitive tool arguments |

## Captured Attributes

### Span Attributes (Always Captured)
- `gen_ai.system` = "openrouter"
- `gen_ai.operation.name` = "chat" | "stream" | "generate_object"
- `gen_ai.request.model` = e.g., "anthropic/claude-3.5-sonnet"
- `gen_ai.response.model` = actual model used
- `gen_ai.response.finish_reasons` = ["stop", "tool_calls", etc.]
- `gen_ai.usage.input_tokens` = prompt token count
- `gen_ai.usage.output_tokens` = completion token count
- `http.response.status_code` = 200, 429, 500, etc.

### Optional Event Data (Opt-in)
- `gen_ai.prompt` - Full prompt messages with roles
- `gen_ai.completion` - Full completion text
- `gen_ai.request.raw_json` - Raw HTTP request
- `gen_ai.response.raw_json` - Raw HTTP response

### Tool Execution (Opt-in)
- `gen_ai.tool.name` - Tool function name
- `gen_ai.tool.execution_mode` - "auto_execute" | "client_side"
- `gen_ai.tool.arguments` - JSON arguments
- `gen_ai.tool.result` - Execution result
- `gen_ai.tool.error` - Error message if failed

### Streaming Metrics
- `gen_ai.stream.time_to_first_token_ms` - TTFT metric
- `gen_ai.stream.total_chunks` - Chunk count
- `gen_ai.stream.duration_ms` - Total time
- `gen_ai.stream.tokens_per_second` - Throughput

## Example Trace Hierarchy

```
gen_ai.client.chat (2.3s)
├─ gen_ai.request.model: "anthropic/claude-3.5-sonnet"
├─ gen_ai.usage.input_tokens: 250
├─ gen_ai.usage.output_tokens: 1000
├─ Event: gen_ai.client.input (prompt messages)
├─ Event: gen_ai.client.output (completion)
└─ Child: gen_ai.tool.call "search_web" (0.8s)
   ├─ gen_ai.tool.arguments: {"query": "quantum computing"}
   └─ gen_ai.tool.result: {"results": [...]}
```

## Privacy & Security Best Practices

### 1. Sanitize Sensitive Data

```csharp
Telemetry = new OpenRouterTelemetryOptions
{
    EnableTelemetry = true,
    CapturePrompts = true,
    SanitizePrompt = prompt =>
    {
        // Redact email addresses
        return Regex.Replace(prompt, @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", "[EMAIL]");
    },
    SanitizeToolArguments = args =>
    {
        // Parse JSON and remove sensitive fields
        var obj = JsonSerializer.Deserialize<JsonElement>(args);
        // ... custom sanitization logic
        return JsonSerializer.Serialize(obj);
    }
}
```

### 2. Use Environment-Specific Configuration

```csharp
var telemetryOptions = builder.Environment.IsDevelopment()
    ? OpenRouterTelemetryOptions.Debug  // Full capture in dev
    : OpenRouterTelemetryOptions.Production;  // Metrics only in prod
```

### 3. Set Size Limits

```csharp
Telemetry = new OpenRouterTelemetryOptions
{
    MaxEventBodySize = 16_000,  // Limit to 16KB to avoid huge traces
}
```

### 4. Sampling Strategy

```csharp
// Sample 10% of requests in production
.AddOtlpExporter(options =>
{
    options.Endpoint = new Uri("http://localhost:4317");
})
.SetSampler(new TraceIdRatioBasedSampler(0.1))  // 10% sampling
```

## Performance Impact

- **Disabled**: Zero overhead (Activity creation returns null)
- **Enabled (Production)**: ~1-2ms per request for span creation
- **With Prompts/Completions**: ~5-10ms for JSON serialization
- **With Raw JSON**: Depends on payload size

**Recommendation**: Use `Production` preset in production for minimal overhead.

## Troubleshooting

### No Traces Appearing

1. Verify telemetry is enabled:
   ```csharp
   Telemetry = new OpenRouterTelemetryOptions { EnableTelemetry = true }
   ```

2. Check ActivityListener is attached:
   ```csharp
   var listener = OpenTelemetryExtensions.CreateOpenRouterActivityListener(
       activityStarted: a => Console.WriteLine($"Started: {a.DisplayName}")
   );
   ActivitySource.AddActivityListener(listener);
   ```

3. Verify OTLP endpoint is reachable:
   ```bash
   curl http://localhost:4317
   ```

### High Memory Usage

- Reduce `MaxEventBodySize`
- Disable `CaptureRawRequests` and `CaptureRawResponses`
- Disable `CaptureStreamChunks`
- Increase sampling ratio

### PII in Traces

- Enable sanitization callbacks
- Review captured data in Phoenix UI
- Disable `CapturePrompts` and `CaptureCompletions` entirely

## Advanced: Manual Activity Subscription

If you don't want to use the full OpenTelemetry SDK, you can manually subscribe to activities:

```csharp
using System.Diagnostics;
using OpenRouter.NET.Observability;

var listener = OpenTelemetryExtensions.CreateOpenRouterActivityListener(
    activityStarted: activity =>
    {
        Console.WriteLine($"[START] {activity.DisplayName}");
        foreach (var tag in activity.Tags)
        {
            Console.WriteLine($"  {tag.Key}: {tag.Value}");
        }
    },
    activityStopped: activity =>
    {
        Console.WriteLine($"[STOP] {activity.DisplayName} - {activity.Duration}");

        // Access token usage
        if (activity.GetTagItem("gen_ai.usage.input_tokens") is int inputTokens)
        {
            Console.WriteLine($"  Input Tokens: {inputTokens}");
        }
    }
);

ActivitySource.AddActivityListener(listener);

var client = new OpenRouterClient(new OpenRouterClientOptions
{
    ApiKey = "your-key",
    Telemetry = new OpenRouterTelemetryOptions { EnableTelemetry = true }
});
```

## Resources

- [OpenTelemetry .NET Docs](https://opentelemetry.io/docs/languages/dotnet/)
- [Arize Phoenix Docs](https://docs.arize.com/phoenix)
- [GenAI Semantic Conventions](https://opentelemetry.io/docs/specs/semconv/gen-ai/)
- [OpenRouter.NET GitHub](https://github.com/williamholmberg/OpenRouter.NET)

## Support

For questions or issues with observability:
- Open an issue on [GitHub](https://github.com/williamholmberg/OpenRouter.NET/issues)
- Check [examples](../samples/) for working code
