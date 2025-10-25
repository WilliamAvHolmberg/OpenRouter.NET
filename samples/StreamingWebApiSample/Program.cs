using OpenRouter.NET;
using OpenRouter.NET.Sse;
using OpenRouter.NET.Models;
using OpenRouter.NET.Tools;
using System.Collections.Concurrent;
using StreamingWebApiSample;
using OpenRouter.NET.Artifacts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var apiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
if (string.IsNullOrEmpty(apiKey))
{
    Console.WriteLine("OPENROUTER_API_KEY environment variable is not set");
    return;
}

var conversationStore = new ConcurrentDictionary<string, List<Message>>();

app.MapGet("/api/models", async () =>
{
    var client = new OpenRouterClient(apiKey);
    var models = await client.GetModelsAsync();
    
    return Results.Ok(models.Select(m => new
    {
        id = m.Id,
        name = m.Name,
        contextLength = m.ContextLength,
        pricing = m.Pricing
    }).ToList());
})
.WithName("GetModels");

app.MapPost("/api/stream", async (ChatRequest chatRequest, HttpContext context) =>
{
    var client = new OpenRouterClient(apiKey);

    var calculator = new CalculatorTools();
    
    // Check for API keys and register tools accordingly
    var tavilyApiKey = Environment.GetEnvironmentVariable("TAVILY_API_KEY");
    var githubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
    
    // Always register calculator tools
    client
        .RegisterTool(calculator, nameof(calculator.Add))
        .RegisterTool(calculator, nameof(calculator.Subtract))
        .RegisterTool(calculator, nameof(calculator.Multiply))
        .RegisterTool(calculator, nameof(calculator.Divide));
    
    // Register GitHub search if token is available
    if (!string.IsNullOrEmpty(githubToken))
    {
        var githubSearch = new GitHubSearchTools(githubToken);
        client.RegisterTool(githubSearch, nameof(githubSearch.SearchRepository));
    }
    
    // Register web search (Tavily preferred, DuckDuckGo fallback)
    if (!string.IsNullOrEmpty(tavilyApiKey))
    {
        // Use Tavily for better search results
        var tavilySearch = new TavilySearchTools(tavilyApiKey);
        client.RegisterTool(tavilySearch, nameof(tavilySearch.SearchWeb));
    }
    else
    {
        // Fallback to DuckDuckGo Instant Answers
        var webSearch = new WebSearchTools();
        client.RegisterTool(webSearch, nameof(webSearch.SearchWeb));
    }

    var conversationId = chatRequest.ConversationId ?? Guid.NewGuid().ToString();
    var history = conversationStore.GetOrAdd(conversationId, _ => new List<Message>());

    if (history.Count == 0)
    {
        history.Add(Message.FromSystem("You are a helpful assistant that can perform calculations and create code artifacts."));
    }

    history.Add(Message.FromUser(chatRequest.Message));

    var request = new ChatCompletionRequest
    {
        Model = chatRequest.Model ?? "google/gemini-2.5-flash",
        Messages = history
    };

 

    if (chatRequest.EnabledArtifacts != null)
    {
        var enabled = chatRequest.EnabledArtifacts
            .Where(a => a != null && (a.Enabled == null || a.Enabled == true))
            .Select(a =>
            {
                var type = string.IsNullOrWhiteSpace(a!.Type) ? "code" : a.Type!;
                var def = Artifacts.Custom(type, a.PreferredTitle);
                if (!string.IsNullOrWhiteSpace(a.Language)) def.WithLanguage(a.Language!);
                if (!string.IsNullOrWhiteSpace(a.Instruction)) def.WithInstruction(a.Instruction!);
                if (!string.IsNullOrWhiteSpace(a.OutputFormat)) def.WithOutputFormat(a.OutputFormat!);
                if (a.Attributes != null)
                {
                    foreach (var kv in a.Attributes)
                    {
                        def.WithAttribute(kv.Key, kv.Value);
                    }
                }
                return (ArtifactDefinition)def;
            })
            .ToArray();

        if (enabled.Length > 0)
        {
            request.EnableArtifacts(enabled);
        }
        else
        {
            request.EnableArtifactSupport(
                customInstructions: "Create artifacts for code examples, scripts, or any deliverable content."
            );
        }
    }
    else
    {
        request.EnableArtifactSupport(
            customInstructions: "Create artifacts for code examples, scripts, or any deliverable content."
        );
    }

    var newMessages = await client.StreamAsSseAsync(request, context.Response);

    // Add all assistant and tool messages to conversation history
    history.AddRange(newMessages);
})
.WithName("Stream");

app.MapDelete("/api/conversation/{conversationId}", (string conversationId) =>
{
    conversationStore.TryRemove(conversationId, out _);
    return Results.Ok(new { message = "Conversation cleared" });
})
.WithName("ClearConversation");

app.Run();

public record ChatRequest(string Message, string? Model = null, string? ConversationId = null)
{
   public EnabledArtifact[]? EnabledArtifacts { get; init; }
}

public record EnabledArtifact
{
    public string? Id { get; init; }
    public bool? Enabled { get; init; }
    public string? Type { get; init; }
    public string? PreferredTitle { get; init; }
    public string? Language { get; init; }
    public string? Instruction { get; init; }
    public string? OutputFormat { get; init; }
    public Dictionary<string, string>? Attributes { get; init; }
}

public class CalculatorTools
{
    [ToolMethod("Add two numbers together")]
    public int Add(
        [ToolParameter("First number")] int a,
        [ToolParameter("Second number")] int b)
    {
        return a + b;
    }

    [ToolMethod("Subtract second number from first")]
    public int Subtract(
        [ToolParameter("First number")] int a,
        [ToolParameter("Second number")] int b)
    {
        return a - b;
    }

    [ToolMethod("Multiply two numbers")]
    public int Multiply(
        [ToolParameter("First number")] int a,
        [ToolParameter("Second number")] int b)
    {
        return a * b;
    }

    [ToolMethod("Divide first number by second")]
    public double Divide(
        [ToolParameter("Numerator")] double a,
        [ToolParameter("Denominator")] double b)
    {
        if (b == 0)
            throw new ArgumentException("Cannot divide by zero");
        return a / b;
    }
}
