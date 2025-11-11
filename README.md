# OpenRouter.NET

A modern .NET SDK for the OpenRouter API - providing a unified interface to access multiple LLM providers with streaming support, **fully-typed tools**, **typed structured outputs**, artifacts, and a **React SDK**.

## ✨ Strongly-Typed Everything

### 1. Typed Structured Outputs (.NET)

**No more JSON parsing hell.** Get fully-typed objects from LLMs:

```csharp
public class BugAnalysis
{
    public string Title { get; set; }
    public Severity Severity { get; set; }  // Enum!
    public List<string> AffectedComponents { get; set; }
}

var analysis = await client.GenerateObjectAsync<BugAnalysis>(
    prompt: "Analyze this bug: Database connection timeout",
    model: "anthropic/claude-sonnet-4.5"
);

// Clean, typed access - no JsonElement.TryGetProperty() mess!
Console.WriteLine(analysis.Object.Title);
Console.WriteLine(analysis.Object.Severity); // Actual enum value
```

### 2. Typed Tools (.NET)

**One-line registration.** Full type safety on inputs AND outputs:

```csharp
public class SearchParams
{
    public string Query { get; set; }
    public int MaxResults { get; set; } = 10;
}

public class SearchTool : Tool<SearchParams, SearchResult>
{
    public override string Name => "search";

    protected override SearchResult Handle(SearchParams p)
    {
        // p.Query, p.MaxResults - fully typed!
        return new SearchResult { Items = DoSearch(p.Query) };
    }
}

// One line to register!
client.RegisterTool<SearchTool>();
```

**No other .NET LLM SDK has this.** Full type safety + automatic schema generation + circular reference protection.

### 3. React SDK with Zod Schemas

Typed structured outputs for React/Next.js:

```typescript
import { z } from 'zod';
import { useGenerateObject } from '@openrouter-dotnet/react';

const schema = z.object({
  name: z.string(),
  age: z.number(),
  hobbies: z.array(z.string())
});

const { object } = useGenerateObject({
  schema,
  prompt: "Generate a profile for a developer named Sarah",
  endpoint: '/api/generate-object'
});

// object is fully typed with TypeScript IntelliSense!
```

**[📘 Quick Start Guide](./GENERATE_OBJECT_QUICKSTART.md)** | **[📚 Full Documentation](./packages/dotnet-sdk/src/GenerateObject.md)**

---

## Features

🚀 Simple and intuitive API
🔒 Type-safe with full async/await support
📦 Lightweight with minimal dependencies
🎯 Built for .NET 9.0+
🔄 Full streaming support
✨ **Fully-typed tool system** (inputs + outputs + auto schema)
✨ **Strongly-typed structured outputs** (no JSON parsing)
⚛️ **React SDK** with Zod validation
📄 Artifact support with incremental parsing
⚡ Async enumerable streaming (IAsyncEnumerable)
🛡️ Circular reference protection in schema generation

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

#### Server-Side History Pattern (Simple)

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

#### Client-Side History Pattern (Production-Ready, Stateless)

For production applications, you can implement **zero-server-memory architecture** by managing conversation history on the client:

**Backend (ASP.NET Core):**
```csharp
app.MapPost("/api/stream-stateless", async (ChatRequest chatRequest, HttpContext context) =>
{
    var client = new OpenRouterClient(apiKey);
    
    List<Message> messagesToSend;
    
    if (chatRequest.Messages != null && chatRequest.Messages.Count > 0)
    {
        // Client provides full history - server is stateless!
        messagesToSend = new List<Message>(chatRequest.Messages);
        messagesToSend.Add(Message.FromUser(chatRequest.Message));
    }
    else
    {
        // First message in conversation
        messagesToSend = new List<Message>
        {
            Message.FromSystem("You are a helpful assistant."),
            Message.FromUser(chatRequest.Message)
        };
    }
    
    var request = new ChatCompletionRequest
    {
        Model = chatRequest.Model ?? "anthropic/claude-3.5-sonnet",
        Messages = messagesToSend
    };
    
    // Stream response - NO server-side storage!
    await client.StreamAsSseAsync(request, context.Response);
});

record ChatRequest(string Message, string? Model = null)
{
    public List<Message>? Messages { get; init; }
}
```

**Frontend (React/Next.js with TypeScript):**
```typescript
import { 
  useOpenRouterChat, 
  saveHistory, 
  loadHistory 
} from '@openrouter-dotnet/react';

function StatelessChat() {
  const [conversationId] = useState(`conv_${Date.now()}`);
  
  const { state, actions } = useOpenRouterChat({
    endpoints: { stream: '/api/stream-stateless' },
    defaultModel: 'anthropic/claude-3.5-sonnet'
  });
  
  // Load history when conversation changes
  useEffect(() => {
    const savedHistory = loadHistory(conversationId);
    actions.setMessages(savedHistory); // Populate hook state
  }, [conversationId]);
  
  // Auto-save after each message
  useEffect(() => {
    if (state.messages.length > 0) {
      saveHistory(conversationId, state.messages);
    }
  }, [state.messages, conversationId]);
  
  const handleSend = async (input: string) => {
    const currentHistory = loadHistory(conversationId);
    
    // Send with full history from localStorage
    await actions.sendMessage(input, {
      history: currentHistory
    });
  };
  
  return <MessageList messages={state.messages} />;
}
```

**Benefits of Client-Side History:**
- ✅ **Zero server memory** - scales infinitely
- ✅ **Survives server restarts** - no data loss
- ✅ **No session affinity** - any server can handle any request
- ✅ **Natural limits** - browser storage quota prevents unbounded growth
- ✅ **User privacy** - conversations stay on device

**How it works:**
1. Client loads conversation history from localStorage
2. Client sends full history + new message to backend
3. Backend processes request (stateless - no storage)
4. Client receives response and saves to localStorage
5. Tool calls are automatically included in history

See [STATELESS_CHAT_README.md](samples/NextJsClientSample/STATELESS_CHAT_README.md) for complete implementation guide.

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

