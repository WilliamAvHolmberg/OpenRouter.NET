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
Console.WriteLine(response.Choices[0].Message.Content);
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
var assistantMessage = response.Choices[0].Message.Content;

// Add assistant response to history
conversationHistory.AddAssistantMessage(assistantMessage);

// Continue conversation
conversationHistory.AddUserMessage("What's the population?");
request.Messages = conversationHistory;

response = await client.CreateChatCompletionAsync(request);
Console.WriteLine(response.Choices[0].Message.Content);
```

## Samples

Check out the [samples/](samples/) directory for complete working examples:

- **[BasicCliSample](samples/BasicCliSample/)** - Interactive CLI to list models and chat with streaming

To run a sample:
```bash
export OPENROUTER_API_KEY="your-key-here"
cd samples/BasicCliSample
dotnet run
```

## Requirements

- .NET 9.0 or later

## Documentation

Full documentation coming soon.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

- 🐛 [Report a bug](https://github.com/williamholmberg/OpenRouter.NET/issues)
- 💡 [Request a feature](https://github.com/williamholmberg/OpenRouter.NET/issues)

## Acknowledgments

Built for the OpenRouter API platform.

