# OpenRouter.NET

A modern .NET SDK for the OpenRouter API - providing a unified interface to access multiple LLM providers.

## Features

🚀 Simple and intuitive API
🔒 Type-safe with full async/await support
📦 Lightweight with minimal dependencies
🎯 Built for .NET 9.0+
🔄 Full streaming support
🛠️ Tool calling (server-side auto-execute and client-side modes)
📄 Artifact support with incremental parsing
⚡ Async enumerable streaming (IAsyncEnumerable)

## Installation

```bash
dotnet add package OpenRouter.NET
```

Or via NuGet Package Manager:

```
Install-Package OpenRouter.NET
```

## For LLMs

Want to use this SDK with your LLM? Check out [`llms.txt`](llms.txt) - give this file to your LLM of choice and it will be ready to implement features with OpenRouter.NET for you!

## Quick Start

### Basic Usage

```csharp
using OpenRouter.NET;
using OpenRouter.NET.Models;

var client = new OpenRouterClient("your-api-key");

var request = new ChatCompletionRequest
{
    Model = "anthropic/claude-3.5-sonnet",
    Messages = new List<Message>
    {
        Message.FromUser("Hello! What's the weather like?")
    }
};

var response = await client.CreateChatCompletionAsync(request);
Console.WriteLine(response.Choices[0].Message.Content?.ToString() ?? "No response");
```

### Streaming

```csharp
using OpenRouter.NET;
using OpenRouter.NET.Models;

var client = new OpenRouterClient("your-api-key");

var request = new ChatCompletionRequest
{
    Model = "anthropic/claude-3.5-sonnet",
    Messages = new List<Message>
    {
        Message.FromUser("Tell me a story")
    }
};

await foreach (var chunk in client.StreamAsync(request))
{
    if (chunk.TextDelta != null)
    {
        Console.Write(chunk.TextDelta);
    }
}
```

### SSE Streaming (for Web APIs)

Perfect for building real-time streaming endpoints that work with browsers using Server-Sent Events:

```csharp
using OpenRouter.NET;
using OpenRouter.NET.Sse;
using OpenRouter.NET.Models;

// In your ASP.NET endpoint
app.MapPost("/api/chat/stream", async (HttpContext context, ChatRequest body) =>
{
    var client = new OpenRouterClient("your-api-key");
    
    var request = new ChatCompletionRequest
    {
        Model = "anthropic/claude-3.5-sonnet",
        Messages = body.Messages
    };

    // One line to stream everything as SSE!
    await client.StreamAsSseAsync(request, context.Response);
});

record ChatRequest(List<Message> Messages);
```

**Client-side (JavaScript/TypeScript):**

Since this is a POST endpoint with a request body, use the fetch API with streaming:

```javascript
async function streamChat(messages) {
    const response = await fetch('/api/chat/stream', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ messages })
    });

    const reader = response.body.getReader();
    const decoder = new TextDecoder();

    while (true) {
        const { done, value } = await reader.read();
        if (done) break;
        
        const chunk = decoder.decode(value);
        const lines = chunk.split('\n').filter(line => line.startsWith('data: '));
        
        for (const line of lines) {
            const event = JSON.parse(line.slice(6));
            if (event.type === 'text') {
                appendText(event.textDelta);
            }
        }
    }
}
```

> **Note:** The standard `EventSource` API only supports GET requests. For POST endpoints with request bodies, use fetch with streaming as shown above. See [SSE Helper Documentation](src/Sse/README.md) for complete client examples, event types, and TypeScript definitions.

**This automatically handles:**
- ✅ Text deltas
- ✅ Tool calls (server-side and client-side)
- ✅ Artifacts (started, content, completed)
- ✅ Completion events
- ✅ Proper SSE formatting
- ✅ Headers and connection management

**Event types sent:**
- `text` - Text content deltas
- `tool_executing` - Tool starting
- `tool_completed` - Tool finished with result
- `tool_error` - Tool failed
- `tool_client` - Client-side tool call requested
- `artifact_started` - Artifact beginning
- `artifact_content` - Artifact content chunk
- `artifact_completed` - Artifact finished
- `completion` - Stream finished
- `error` - Error occurred

### Artifacts

```csharp
using OpenRouter.NET;
using OpenRouter.NET.Models;

var client = new OpenRouterClient("your-api-key");

var request = new ChatCompletionRequest
{
    Model = "anthropic/claude-3.5-sonnet",
    Messages = new List<Message>
    {
        Message.FromUser("Create a hello world function")
    }
};

// Enable artifact support
request.EnableArtifactSupport();

await foreach (var chunk in client.StreamAsync(request))
{
    if (chunk.TextDelta != null)
    {
        Console.Write(chunk.TextDelta);
    }

    if (chunk.Artifact is ArtifactCompleted completed)
    {
        Console.WriteLine($"\n\nArtifact: {completed.Title}");
        Console.WriteLine(completed.Content);
    }
}
```

### Conversation (Multi-turn)

```csharp
using OpenRouter.NET;
using OpenRouter.NET.Models;

var client = new OpenRouterClient("your-api-key");
var conversationHistory = new List<Message>();

// Add user message
conversationHistory.AddUserMessage("What is the capital of France?");

var request = new ChatCompletionRequest
{
    Model = "anthropic/claude-3.5-sonnet",
    Messages = conversationHistory
};

var response = await client.CreateChatCompletionAsync(request);
var assistantMessage = response.Choices[0].Message.Content?.ToString() ?? "";

// Add assistant response to history
conversationHistory.AddAssistantMessage(assistantMessage);

// Continue conversation
conversationHistory.AddUserMessage("What's the population?");
request.Messages = conversationHistory;

response = await client.CreateChatCompletionAsync(request);
Console.WriteLine(response.Choices[0].Message.Content?.ToString() ?? "No response");
```

## Samples

Check out the [samples/](samples/) directory for complete working examples:

- **[BasicCliSample](samples/BasicCliSample/)** - Interactive CLI to list models and chat with streaming
- **[OpenRouterWebApiSample](samples/OpenRouterWebApiSample/)** - ASP.NET Core Web API showcasing all SDK features including streaming, tools, artifacts, and multimodal

To run a sample:
```bash
export OPENROUTER_API_KEY="your-key-here"
cd samples/BasicCliSample
dotnet run
```

Or for the Web API sample:
```bash
export OPENROUTER_API_KEY="your-key-here"
cd samples/OpenRouterWebApiSample
dotnet run
```

## Requirements

- .NET 9.0 or later

## Documentation

Full documentation coming soon.

## Known Issues

**Next.js SSE Streaming**: Next.js compression buffers responses before sending, causing SSE streams to arrive as one big chunk. Set `compress: false` in `next.config.js` to fix.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

- 🐛 [Report a bug](https://github.com/williamholmberg/OpenRouter.NET/issues)
- 💡 [Request a feature](https://github.com/williamholmberg/OpenRouter.NET/issues)

## Acknowledgments

Built for the OpenRouter API platform.

