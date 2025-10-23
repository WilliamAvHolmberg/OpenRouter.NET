using OpenRouter.NET;
using OpenRouter.NET.Sse;
using OpenRouter.NET.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Configure CORS if needed
app.UseCors(policy => policy
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());

// SSE streaming endpoint - ONE LINE!
app.MapPost("/api/chat/stream", async (HttpContext context, ChatRequest body) =>
{
    var apiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
    if (string.IsNullOrEmpty(apiKey))
    {
        context.Response.StatusCode = 500;
        await context.Response.WriteAsJsonAsync(new { error = "API key not configured" });
        return;
    }

    var client = new OpenRouterClient(apiKey);
    
    var request = new ChatCompletionRequest
    {
        Model = body.Model ?? "anthropic/claude-3.5-sonnet",
        Messages = body.Messages
    };

    // This ONE line handles everything:
    // - Sets SSE headers
    // - Streams text deltas
    // - Streams tool calls (executing, completed, error)
    // - Streams artifacts (started, content, completed)
    // - Streams completion events
    // - Handles errors
    await client.StreamAsSseAsync(request, context.Response, context.RequestAborted);
});

// Non-streaming endpoint for comparison
app.MapPost("/api/chat", async (HttpContext context, ChatRequest body) =>
{
    var apiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
    if (string.IsNullOrEmpty(apiKey))
    {
        context.Response.StatusCode = 500;
        await context.Response.WriteAsJsonAsync(new { error = "API key not configured" });
        return;
    }

    var client = new OpenRouterClient(apiKey);
    
    var request = new ChatCompletionRequest
    {
        Model = body.Model ?? "anthropic/claude-3.5-sonnet",
        Messages = body.Messages
    };

    var response = await client.CreateChatCompletionAsync(request, context.RequestAborted);
    await context.Response.WriteAsJsonAsync(response);
});

app.Run();

record ChatRequest(List<Message> Messages, string? Model = null);
