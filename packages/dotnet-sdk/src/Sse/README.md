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

## Telemetry & Observability

`StreamAsSseAsync()` returns a `StreamingResult` that provides comprehensive telemetry data **after** streaming completes:

```csharp
var result = await client.StreamAsSseAsync(request, context.Response);

// Access telemetry data
Console.WriteLine($"Tokens used: {result.Usage?.TotalTokens ?? 0}");
Console.WriteLine($"Time to first token: {result.TimeToFirstToken?.TotalMilliseconds ?? 0}ms");
Console.WriteLine($"Total time: {result.TotalElapsed.TotalMilliseconds}ms");
Console.WriteLine($"Chunks received: {result.ChunkCount}");
Console.WriteLine($"Finish reason: {result.FinishReason}");

// Track tool performance
foreach (var tool in result.ToolExecutions)
{
    Console.WriteLine($"Tool {tool.ToolName} executed in {tool.ExecutionTime?.TotalMilliseconds ?? 0}ms");
}

// Access the messages
foreach (var message in result.Messages)
{
    Console.WriteLine($"{message.Role}: {message.Content}");
}
```

### StreamingResult Properties

| Property | Type | Description |
|----------|------|-------------|
| `Messages` | `List<Message>` | Complete conversation history (assistant + tool messages) |
| `Usage` | `ResponseUsage?` | Token counts (prompt, completion, total) |
| `FinishReason` | `string?` | Why streaming stopped ("stop", "length", "tool_calls") |
| `TimeToFirstToken` | `TimeSpan?` | Time from request start to first token |
| `TotalElapsed` | `TimeSpan` | Total streaming duration |
| `ChunkCount` | `int` | Number of chunks received |
| `ToolExecutions` | `List<ToolExecutionInfo>` | Server-side tool execution details |
| `RequestId` | `string?` | API request ID for debugging |
| `Model` | `string?` | Actual model used by the API |

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

> **Important:** The standard browser `EventSource` API only supports GET requests without request bodies. Since our endpoints accept POST requests with chat history and configuration, we use the fetch API with streaming instead.

### JavaScript (Browser) - Complete Example

```javascript
async function streamChat(messages, options = {}) {
    const response = await fetch('/api/chat/stream', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ 
            messages,
            model: options.model || 'anthropic/claude-3.5-sonnet'
        })
    });

    if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
    }

    const reader = response.body.getReader();
    const decoder = new TextDecoder();
    let buffer = '';

    while (true) {
        const { done, value } = await reader.read();
        if (done) break;
        
        buffer += decoder.decode(value, { stream: true });
        
        // Process complete SSE messages (separated by \n\n)
        const parts = buffer.split('\n\n');
        buffer = parts.pop() || ''; // Keep incomplete message in buffer
        
        for (const part of parts) {
            const lines = part.split('\n');
            for (const line of lines) {
                if (line.startsWith('data: ')) {
                    const data = JSON.parse(line.slice(6));
                    handleEvent(data);
                }
            }
        }
    }
}

function handleEvent(event) {
    switch (event.type) {
        case 'text':
            appendText(event.textDelta);
            break;
        case 'tool_executing':
            showToolSpinner(event.toolName);
            break;
        case 'tool_completed':
            displayToolResult(event.toolName, event.result);
            break;
        case 'artifact_completed':
            displayArtifact(event.title, event.content);
            break;
        case 'completion':
            hideSpinner();
            console.log('Stream finished:', event.finishReason);
            break;
        case 'error':
            showError(event.message);
            break;
    }
}

// Usage
const messages = [
    { role: 'user', content: 'Tell me a story' }
];

streamChat(messages).catch(err => {
    console.error('Streaming failed:', err);
});
```

### TypeScript - Type-Safe Implementation

```typescript
// Event type definitions
interface SseEvent {
    type: string;
    chunkIndex: number;
    elapsedMs: number;
}

interface TextEvent extends SseEvent {
    type: 'text';
    textDelta: string;
}

interface ToolExecutingEvent extends SseEvent {
    type: 'tool_executing';
    toolName: string;
    toolId: string;
    arguments: string;
}

interface ToolCompletedEvent extends SseEvent {
    type: 'tool_completed';
    toolName: string;
    toolId: string;
    arguments: string;
    result: string;
    executionMs: number;
}

interface ToolErrorEvent extends SseEvent {
    type: 'tool_error';
    toolName: string;
    toolId: string;
    error: string;
}

interface ArtifactCompletedEvent extends SseEvent {
    type: 'artifact_completed';
    artifactId: string;
    title: string;
    artifactType: string;
    language?: string;
    content: string;
}

interface CompletionEvent extends SseEvent {
    type: 'completion';
    finishReason?: string;
    model?: string;
    id?: string;
}

interface ErrorEvent extends SseEvent {
    type: 'error';
    message: string;
    details?: string;
}

// Message types
interface Message {
    role: 'user' | 'assistant' | 'system';
    content: string;
}

interface ChatRequest {
    messages: Message[];
    model?: string;
}

// Event handler type
type EventHandler = (event: SseEvent) => void;

// Streaming client class
class OpenRouterStreamClient {
    private handlers: Map<string, EventHandler[]> = new Map();

    on(eventType: string, handler: EventHandler): void {
        if (!this.handlers.has(eventType)) {
            this.handlers.set(eventType, []);
        }
        this.handlers.get(eventType)!.push(handler);
    }

    private emit(event: SseEvent): void {
        const handlers = this.handlers.get(event.type);
        if (handlers) {
            handlers.forEach(handler => handler(event));
        }
        
        // Also emit to wildcard handlers
        const wildcardHandlers = this.handlers.get('*');
        if (wildcardHandlers) {
            wildcardHandlers.forEach(handler => handler(event));
        }
    }

    async stream(endpoint: string, request: ChatRequest): Promise<void> {
        const response = await fetch(endpoint, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(request)
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const reader = response.body!.getReader();
        const decoder = new TextDecoder();
        let buffer = '';

        while (true) {
            const { done, value } = await reader.read();
            if (done) break;
            
            buffer += decoder.decode(value, { stream: true });
            
            const parts = buffer.split('\n\n');
            buffer = parts.pop() || '';
            
            for (const part of parts) {
                const lines = part.split('\n');
                for (const line of lines) {
                    if (line.startsWith('data: ')) {
                        try {
                            const event = JSON.parse(line.slice(6)) as SseEvent;
                            this.emit(event);
                        } catch (err) {
                            console.error('Failed to parse SSE event:', err);
                        }
                    }
                }
            }
        }
    }
}

// Usage example
const client = new OpenRouterStreamClient();

client.on('text', (event) => {
    const textEvent = event as TextEvent;
    appendText(textEvent.textDelta);
});

client.on('tool_completed', (event) => {
    const toolEvent = event as ToolCompletedEvent;
    console.log(`Tool ${toolEvent.toolName} completed in ${toolEvent.executionMs}ms`);
});

client.on('completion', (event) => {
    const completion = event as CompletionEvent;
    console.log('Stream finished:', completion.finishReason);
});

// Start streaming
await client.stream('/api/chat/stream', {
    messages: [{ role: 'user', content: 'Hello!' }],
    model: 'anthropic/claude-3.5-sonnet'
});
```

### React Hook Example

```typescript
import { useState, useCallback } from 'react';

interface UseStreamChatOptions {
    onText?: (text: string) => void;
    onToolCall?: (toolName: string, result: string) => void;
    onComplete?: () => void;
    onError?: (error: string) => void;
}

export function useStreamChat(options: UseStreamChatOptions = {}) {
    const [isStreaming, setIsStreaming] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const streamChat = useCallback(async (messages: Message[]) => {
        setIsStreaming(true);
        setError(null);

        try {
            const response = await fetch('/api/chat/stream', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ messages })
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const reader = response.body!.getReader();
            const decoder = new TextDecoder();
            let buffer = '';

            while (true) {
                const { done, value } = await reader.read();
                if (done) break;
                
                buffer += decoder.decode(value, { stream: true });
                
                const parts = buffer.split('\n\n');
                buffer = parts.pop() || '';
                
                for (const part of parts) {
                    const lines = part.split('\n');
                    for (const line of lines) {
                        if (line.startsWith('data: ')) {
                            const event = JSON.parse(line.slice(6)) as SseEvent;
                            
                            switch (event.type) {
                                case 'text':
                                    options.onText?.((event as TextEvent).textDelta);
                                    break;
                                case 'tool_completed':
                                    const tool = event as ToolCompletedEvent;
                                    options.onToolCall?.(tool.toolName, tool.result);
                                    break;
                                case 'completion':
                                    options.onComplete?.();
                                    break;
                                case 'error':
                                    options.onError?.((event as ErrorEvent).message);
                                    break;
                            }
                        }
                    }
                }
            }
        } catch (err) {
            const errorMsg = err instanceof Error ? err.message : 'Unknown error';
            setError(errorMsg);
            options.onError?.(errorMsg);
        } finally {
            setIsStreaming(false);
        }
    }, [options]);

    return { streamChat, isStreaming, error };
}

// Usage in component
function ChatComponent() {
    const [text, setText] = useState('');
    
    const { streamChat, isStreaming } = useStreamChat({
        onText: (delta) => setText(prev => prev + delta),
        onComplete: () => console.log('Done!'),
    });

    return (
        <div>
            <button 
                onClick={() => streamChat([{ role: 'user', content: 'Hello!' }])}
                disabled={isStreaming}
            >
                Send
            </button>
            <div>{text}</div>
        </div>
    );
}
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
- Check browser console for fetch errors or CORS issues
- Verify the endpoint returns 200 OK before streaming starts

### Connection drops
- Use cancellation tokens properly
- Implement reconnection logic on client side (fetch doesn't auto-reconnect like EventSource)
- Check server timeout settings
- Consider adding heartbeat/keep-alive mechanism for long-running streams

### Invalid JSON
- Ensure your tool results are JSON-serializable
- Use custom `JsonSerializerOptions` if needed
- Check that SSE message parsing correctly splits on `\n\n`

### Why not use EventSource?
The browser's native `EventSource` API only supports:
- ✅ GET requests
- ❌ No request body support
- ❌ No custom headers (except credentials)

For chat applications where you need to send:
- Conversation history
- Model configuration  
- System prompts
- Tool definitions

...you must use POST with a request body. The fetch API with streaming provides the same functionality with more flexibility.

## Future Enhancements

Planned features:
- Event filtering options
- Custom event types
- Heartbeat/keep-alive configuration
- WebSocket support
- JavaScript/TypeScript client library
