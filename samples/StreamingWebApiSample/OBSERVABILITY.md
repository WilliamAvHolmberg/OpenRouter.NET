# Observability in StreamingWebApiSample

This sample demonstrates OpenRouter.NET's built-in observability features using **OpenTelemetry** with a **Console Exporter**.

## What's Enabled

The sample API has observability configured for all LLM endpoints:

### ✅ Configured Endpoints

1. **`/api/stream`** - Main streaming chat endpoint
   - ✅ Prompts captured
   - ✅ Completions captured
   - ✅ Tool execution tracked
   - ✅ **Stream chunks logged** (high volume!)

2. **`/api/dashboard/stream`** - Dashboard widget builder
   - ✅ Prompts captured
   - ✅ Completions captured
   - ✅ Tool execution tracked

3. **`/api/generate-object`** - Structured output generation
   - ✅ Prompts captured
   - ✅ Completions captured
   - ✅ Schema validation tracking

4. **`/api/triage-bug`** - Bug analysis
   - ✅ Prompts captured
   - ✅ Completions captured

5. **`/api/demo-typed-tools`** - Typed tools demo
   - ✅ Prompts captured
   - ✅ Completions captured
   - ✅ Tool execution tracked

## Running the Sample

### 1. Set API Key

```bash
export OPENROUTER_API_KEY="your-api-key-here"
```

### 2. Run the API

```bash
cd samples/StreamingWebApiSample
dotnet run
```

### 3. Make a Request

```bash
# Using the samples.http file
POST http://localhost:5000/api/stream
Content-Type: application/json

{
  "message": "Calculate 15 * 23 and tell me the result",
  "model": "google/gemini-2.5-flash"
}
```

Or using curl:

```bash
curl -X POST http://localhost:5000/api/stream \
  -H "Content-Type: application/json" \
  -d '{"message":"Calculate 15 * 23 and tell me the result","model":"google/gemini-2.5-flash"}'
```

### 4. View Telemetry in Console

You'll see OpenTelemetry traces printed to the console like:

```
Activity.TraceId:            8a1f3c2e9b7d4a5f6e8c9b0a1d2e3f4g
Activity.SpanId:             7b6c5d4e3f2a
Activity.TraceFlags:         Recorded
Activity.ParentSpanId:       1a2b3c4d5e6f
Activity.ActivitySourceName: OpenRouter.NET
Activity.DisplayName:        gen_ai.client.stream
Activity.Kind:               Client
Activity.StartTime:          2025-01-08T10:30:45.1234567Z
Activity.Duration:           00:00:02.3456789
Activity.Tags:
    gen_ai.system: openrouter
    gen_ai.operation.name: stream
    gen_ai.request.model: google/gemini-2.5-flash
    gen_ai.response.model: google/gemini-2.5-flash
    gen_ai.usage.input_tokens: 45
    gen_ai.usage.output_tokens: 123
    gen_ai.response.finish_reasons: ["stop"]
    http.response.status_code: 200
Activity.Events:
    gen_ai.client.input [10:30:45.234]
        gen_ai.prompt: [{"role":"user","content":"Calculate 15 * 23"}]
    gen_ai.client.output [10:30:47.456]
        gen_ai.completion: [{"index":0,"content":"The result is 345","finish_reason":"stop"}]
Resource associated with Activity:
    service.name: openrouter-streaming-api
    deployment.environment: Development
```

## What Gets Logged

### Attributes (Always Captured)
- **Request model**: `gen_ai.request.model`
- **Response model**: `gen_ai.response.model` (actual model used)
- **Token usage**: `gen_ai.usage.input_tokens`, `gen_ai.usage.output_tokens`
- **Finish reasons**: `gen_ai.response.finish_reasons`
- **HTTP status**: `http.response.status_code`
- **Request/response sizes**: `gen_ai.request.size_bytes`, `gen_ai.response.size_bytes`

### Events (Opt-in Captured)
- **Input prompts**: Full conversation history with roles
- **Output completions**: Model responses with finish reasons
- **Tool execution**: Arguments, results, timing
- **Stream chunks**: Individual SSE chunks (high volume!)

### Tool Execution Spans
Each tool call creates a child span with:
- Tool name
- Execution mode (auto_execute/client_side)
- Arguments (if `CaptureToolDetails` enabled)
- Results (if `CaptureToolDetails` enabled)
- Execution duration
- Error details (if failed)

## Switching to Phoenix/Jaeger

To send traces to **Arize Phoenix** or **Jaeger** instead of console:

### Option 1: Arize Phoenix

1. **Start Phoenix**:
   ```bash
   docker run -p 6006:6006 -p 4317:4317 arizephoenix/phoenix:latest
   ```

2. **Update Program.cs**:
   ```csharp
   // Replace .AddConsoleExporter() with:
   .AddOtlpExporter(options =>
   {
       options.Endpoint = new Uri("http://localhost:4317");
       options.Protocol = OtlpExportProtocol.Grpc;
   });
   ```

3. **Add package**:
   ```bash
   dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol
   ```

4. **View traces**: Open http://localhost:6006

### Option 2: Jaeger

1. **Start Jaeger**:
   ```bash
   docker run -d -p 16686:16686 -p 4317:4317 jaegertracing/all-in-one:latest
   ```

2. **Update Program.cs** (same as Phoenix)

3. **View traces**: Open http://localhost:16686

## Telemetry Configuration Options

You can customize telemetry per endpoint:

```csharp
var client = new OpenRouterClient(new OpenRouterClientOptions
{
    ApiKey = apiKey,
    Telemetry = new OpenRouterTelemetryOptions
    {
        // Master switch
        EnableTelemetry = true,

        // Content capture (opt-in)
        CapturePrompts = true,          // Log input messages
        CaptureCompletions = true,      // Log output messages
        CaptureRawRequests = false,     // Log raw HTTP request JSON
        CaptureRawResponses = false,    // Log raw HTTP response JSON
        CaptureStreamChunks = false,    // ⚠️ High volume!
        CaptureToolDetails = true,      // Log tool args/results

        // Size limits
        MaxEventBodySize = 32_000,      // 32KB truncation limit

        // PII sanitization
        SanitizePrompt = prompt => prompt.Replace("SECRET", "[REDACTED]"),
        SanitizeCompletion = null,
        SanitizeToolArguments = null
    }
});
```

### Presets

Use built-in presets for common scenarios:

```csharp
// Production: Metrics only, no content
Telemetry = OpenRouterTelemetryOptions.Production

// Debug: Full capture
Telemetry = OpenRouterTelemetryOptions.Debug

// Default: All disabled (opt-in)
Telemetry = OpenRouterTelemetryOptions.Default
```

## Performance Impact

- **Console Exporter**: ~5-10ms overhead per request
- **OTLP Exporter**: ~2-5ms overhead per request
- **Disabled**: Zero overhead (no spans created)

The `/api/stream` endpoint has `CaptureStreamChunks = true` which generates **high volume** of events. Consider disabling in production or using sampling.

## Privacy Considerations

⚠️ **Warning**: This sample captures full prompts and completions!

For production:
1. Use `OpenRouterTelemetryOptions.Production` (metrics only)
2. Enable sanitization callbacks for PII redaction
3. Disable raw request/response capture
4. Review captured data in Phoenix/Jaeger UI

## Resources

- **Full Guide**: [../../docs/OBSERVABILITY.md](../../docs/OBSERVABILITY.md)
- **Arize Phoenix**: https://docs.arize.com/phoenix
- **OpenTelemetry .NET**: https://opentelemetry.io/docs/languages/dotnet/
