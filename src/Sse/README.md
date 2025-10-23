# SSE Streaming Helper

The SSE (Server-Sent Events) helper makes it trivial to create streaming endpoints in ASP.NET Core that work seamlessly with browsers and other SSE clients.

## Quick Start

### Basic Endpoint

```csharp
using OpenRouter.NET;
using OpenRouter.NET.Sse;
using OpenRouter.NET.Models;
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapPost("/api/chat/stream", async (HttpContext context, ChatRequest body) =>
{
    var client = new OpenRouterClient(Environment.GetEnvironmentVariable("OPENROUTER_API_KEY")!);
    
    var request = new ChatCompletionRequest
    {
        Model = "anthropic/claude-3.5-sonnet",
        Messages = new List<Message> { Message.FromUser(body.Message) }
    };

    await client.StreamAsSseAsync(request, context.Response);
});

app.Run();

record ChatRequest(string Message);
```

That's it! One line: `await client.StreamAsSseAsync(request, context.Response);`

## What Gets Streamed

The helper automatically converts all `StreamChunk` events into standardized SSE events:

### Event Types

| Event Type | Description | Example Use Case |
|------------|-------------|------------------|
| `text` | Text content delta | Display streaming text in UI |
| `tool_executing` | Tool starting execution | Show loading spinner |
| `tool_completed` | Tool finished successfully | Display tool result |
| `tool_error` | Tool execution failed | Show error message |
| `tool_client` | Client needs to execute tool | Handle in frontend |
| `artifact_started` | Artifact beginning | Create artifact container |
| `artifact_content` | Artifact content chunk | Stream artifact content |
| `artifact_completed` | Artifact finished | Finalize artifact display |
| `completion` | Stream finished | Clean up, hide spinner |
| `error` | Error occurred | Show error notification |

### Event Structure

All events share a base structure:

```json
{
  "type": "text",
  "chunkIndex": 5,
  "elapsedMs": 1234.56,
  ...
}
```

#### Text Event
```json
{
  "type": "text",
  "chunkIndex": 1,
  "elapsedMs": 150.3,
  "textDelta": "Hello"
}
```

#### Tool Events
```json
{
  "type": "tool_executing",
  "chunkIndex": 2,
  "elapsedMs": 200.5,
  "toolName": "search",
  "toolId": "call_abc123",
  "arguments": "{\"query\":\"weather\"}"
}
```

```json
{
  "type": "tool_completed",
  "chunkIndex": 3,
  "elapsedMs": 450.2,
  "toolName": "search",
  "toolId": "call_abc123",
  "arguments": "{\"query\":\"weather\"}",
  "result": "{\"temperature\":72}",
  "executionMs": 250.0
}
```

#### Artifact Events
```json
{
  "type": "artifact_started",
  "chunkIndex": 4,
  "elapsedMs": 500.0,
  "artifactId": "artifact_1",
  "title": "Hello World",
  "artifactType": "code",
  "language": "python"
}
```

```json
{
  "type": "artifact_content",
  "chunkIndex": 5,
  "elapsedMs": 510.0,
  "artifactId": "artifact_1",
  "contentDelta": "def hello():\n"
}
```

```json
{
  "type": "artifact_completed",
  "chunkIndex": 10,
  "elapsedMs": 750.0,
  "artifactId": "artifact_1",
  "title": "Hello World",
  "artifactType": "code",
  "language": "python",
  "content": "def hello():\n    print('Hello, World!')"
}
```

#### Completion Event
```json
{
  "type": "completion",
  "chunkIndex": 15,
  "elapsedMs": 2000.0,
  "finishReason": "stop",
  "model": "anthropic/claude-3.5-sonnet",
  "id": "gen-abc123"
}
```

## Client-Side Consumption

### JavaScript (Browser)

```javascript
const eventSource = new EventSource('/api/chat/stream', {
    method: 'POST',
    body: JSON.stringify({ message: 'Tell me a story' })
});

eventSource.onmessage = (event) => {
    const data = JSON.parse(event.data);
    
    switch (data.type) {
        case 'text':
            appendText(data.textDelta);
            break;
        case 'tool_executing':
            showToolSpinner(data.toolName);
            break;
        case 'tool_completed':
            displayToolResult(data.toolName, data.result);
            break;
        case 'artifact_completed':
            displayArtifact(data.title, data.content);
            break;
        case 'completion':
            hideSpinner();
            break;
    }
};
```

### TypeScript

```typescript
interface SseEvent {
    type: string;
    chunkIndex: number;
    elapsedMs: number;
}

interface TextEvent extends SseEvent {
    type: 'text';
    textDelta: string;
}

interface ToolCompletedEvent extends SseEvent {
    type: 'tool_completed';
    toolName: string;
    toolId: string;
    result: string;
    executionMs: number;
}

// ... other event types

const eventSource = new EventSource('/api/chat/stream');

eventSource.onmessage = (event) => {
    const data = JSON.parse(event.data) as SseEvent;
    
    if (data.type === 'text') {
        const textEvent = data as TextEvent;
        appendText(textEvent.textDelta);
    }
};
```

## Advanced Usage

### Custom JSON Options

```csharp
var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
};

await client.StreamAsSseAsync(request, context.Response, jsonOptions);
```

### Lower-Level Access

If you need more control, use the `StreamAsSseEventsAsync` method:

```csharp
await foreach (var sseEvent in client.StreamAsSseEventsAsync(request))
{
    // Process or filter events
    if (sseEvent is TextEvent textEvent)
    {
        Console.WriteLine($"Text: {textEvent.TextDelta}");
    }
    
    // Write manually if needed
    var json = JsonSerializer.Serialize(sseEvent);
    await Response.WriteAsync($"data: {json}\n\n");
    await Response.Body.FlushAsync();
}
```

## Best Practices

1. **Always set proper content type** - The helper does this automatically via `SseWriter.SetupSseHeaders()`
2. **Flush after each event** - The helper handles this automatically
3. **Handle disconnections** - Use `CancellationToken` from the request context
4. **Error handling** - The helper automatically sends error events
5. **Keep-alive** - For long-running streams, consider periodic heartbeat events (future feature)

## Comparison

### Before (Manual)
```csharp
// 40+ lines of repetitive code
Response.ContentType = "text/event-stream";
Response.Headers.Append("Cache-Control", "no-cache");
Response.Headers.Append("Connection", "keep-alive");

await foreach (var chunk in client.StreamAsync(request))
{
    if (chunk.TextDelta != null)
    {
        var json = JsonSerializer.Serialize(new { type = "text", textDelta = chunk.TextDelta });
        await Response.WriteAsync($"data: {json}\n\n");
        await Response.Body.FlushAsync();
    }
    
    if (chunk.ServerTool != null)
    {
        // ... more repetitive code
    }
    
    // ... repeat for all chunk types
}
```

### After (With Helper)
```csharp
// 1 line
await client.StreamAsSseAsync(request, context.Response);
```

## Troubleshooting

### Events not appearing in browser
- Ensure you're using `text/event-stream` content type (helper does this automatically)
- Check that responses are being flushed (helper does this automatically)
- Verify CORS settings if calling from different domain

### Connection drops
- Use cancellation tokens properly
- Consider implementing reconnection logic on client side
- Check server timeout settings

### Invalid JSON
- Ensure your tool results are JSON-serializable
- Use custom `JsonSerializerOptions` if needed

## Future Enhancements

Planned features:
- Event filtering options
- Custom event types
- Heartbeat/keep-alive configuration
- WebSocket support
- JavaScript/TypeScript client library
