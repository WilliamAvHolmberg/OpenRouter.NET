# OpenRouter.NET Web API Sample

A comprehensive ASP.NET Core Web API demonstrating various features of the OpenRouter.NET client library.

## Features Demonstrated

This sample showcases the following OpenRouter.NET capabilities:

- **Basic Chat**: Simple chat completions with temperature and token controls
- **Streaming**: Real-time streaming responses using Server-Sent Events (SSE)
- **Conversation History**: Multi-turn conversations with message history
- **Artifacts**: Generate code/content with artifact support
- **Multimodal**: Process images alongside text prompts
- **Function Calling**: Tool/function calling with calculator example
- **Model Management**: List available models and check account limits

## Prerequisites

- .NET 9.0 SDK or later
- OpenRouter API key ([Get one here](https://openrouter.ai/keys))

## Setup

1. Set your OpenRouter API key using one of these methods:

   **Option A: Environment Variable (Recommended)**
   ```bash
   export OPENROUTER_API_KEY="your-api-key-here"
   ```

   **Option B: Configuration File**

   Create or update `appsettings.json`:
   ```json
   {
     "OpenRouter": {
       "ApiKey": "your-api-key-here",
       "SiteUrl": "https://your-site.com",
       "SiteName": "Your Application Name"
     }
   }
   ```

2. Run the application:
   ```bash
   dotnet run
   ```

3. The API will be available at `https://localhost:5210` (or the port specified in launchSettings.json)

## API Endpoints

### Models

- `GET /api/models` - List all available models
- `GET /api/models/popular` - List popular models
- `GET /api/models/limits` - Get account usage limits

### Chat

- `POST /api/chat/basic` - Simple chat completion
- `POST /api/chat/stream` - Streaming chat completion
- `POST /api/chat/conversation` - Multi-turn conversation
- `POST /api/chat/artifacts` - Generate content with artifacts
- `POST /api/chat/multimodal` - Chat with image inputs
- `POST /api/chat/tools` - Chat with function calling (non-streaming)
- `POST /api/chat/tools/stream` - Chat with function calling (streaming)

## Testing

Use the included `OpenRouterWebApi.http` file with VS Code's REST Client extension or any HTTP client to test the endpoints.

Example basic chat request:
```http
POST https://localhost:5210/api/chat/basic
Content-Type: application/json

{
  "model": "anthropic/claude-3.5-sonnet",
  "message": "What is the capital of France?",
  "temperature": 0.7
}
```

## Key Implementation Details

### Dependency Injection

The OpenRouterClient is registered as a singleton in `Program.cs`:

```csharp
builder.Services.AddSingleton(sp => new OpenRouterClient(new OpenRouterClientOptions
{
    ApiKey = apiKey,
    SiteUrl = builder.Configuration["OpenRouter:SiteUrl"],
    SiteName = builder.Configuration["OpenRouter:SiteName"]
}));
```

### Error Handling

All endpoints include proper error handling with `OpenRouterException`:

```csharp
catch (OpenRouterException ex)
{
    _logger.LogError(ex, "OpenRouter API error");
    return StatusCode(ex.StatusCode ?? 500, new { error = ex.Message, code = ex.ErrorCode });
}
```

### Streaming Responses

Streaming endpoints use Server-Sent Events (SSE) format:

```csharp
Response.ContentType = "text/event-stream";
Response.Headers.Append("Cache-Control", "no-cache");
await foreach (var chunk in _client.StreamAsync(chatRequest))
{
    // Process chunks...
}
```

### Function Calling

The calculator tools demonstrate attribute-based function definition:

```csharp
[ToolMethod("Add two numbers")]
public int Add(
    [ToolParameter("First number")] int a,
    [ToolParameter("Second number")] int b)
{
    return a + b;
}
```

## Project Structure

```
OpenRouterWebApiSample/
├── Controllers/
│   ├── ChatController.cs      # Chat completion endpoints
│   └── ModelsController.cs    # Model information endpoints
├── Models/
│   └── Dtos.cs               # Request/response DTOs
├── Tools/
│   └── CalculatorTools.cs    # Function calling example
├── Program.cs                # Application setup
└── OpenRouterWebApi.http     # HTTP request examples
```

## Learn More

- [OpenRouter.NET GitHub Repository](https://github.com/WilliamAvHolmberg/OpenRouter.NET)
- [OpenRouter API Documentation](https://openrouter.ai/docs)
