using OpenRouter.NET;
using OpenRouter.NET.Observability;
using OpenRouter.NET.Sse;
using OpenRouter.NET.Models;
using OpenRouter.NET.Tools;
using System.Collections.Concurrent;
using StreamingWebApiSample;
using OpenRouter.NET.Artifacts;
using System.Text.Json;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// ✨ Configure OpenTelemetry with Console Exporter for observability
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService("openrouter-streaming-api")
        .AddAttributes(new Dictionary<string, object>
        {
            ["deployment.environment"] = builder.Environment.EnvironmentName
        }))
    .WithTracing(tracerProvider =>
    {
        tracerProvider
            .AddAspNetCoreInstrumentation()     // HTTP requests
            .AddHttpClientInstrumentation()     // Outgoing HTTP
            .AddOpenRouterInstrumentation()     // 🎯 OpenRouter LLM calls
            .AddConsoleExporter();              // Output traces to console
    });

builder.Services.AddOpenApi();
builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.MapControllers();

var apiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
if (string.IsNullOrEmpty(apiKey))
{
    Console.WriteLine("OPENROUTER_API_KEY environment variable is not set");
    return;
}

var conversationStore = new ConcurrentDictionary<string, List<Message>>();
var dashboardConversationStore = new ConcurrentDictionary<string, List<Message>>();

app.MapGet("/api/models", async () =>
{
    // Simple client without telemetry for models endpoint
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
    // ✨ Create client with observability enabled
    var client = new OpenRouterClient(new OpenRouterClientOptions
    {
        ApiKey = apiKey,
        Telemetry = new OpenRouterTelemetryOptions
        {
            EnableTelemetry = true,
            CapturePrompts = true,          // Log input prompts
            CaptureCompletions = true,      // Log outputs
            CaptureToolDetails = true,      // Log tool arguments/results
            CaptureStreamChunks = true,     // Log streaming chunks (high volume!)
            MaxEventBodySize = 32_000       // 32KB limit
        }
    });

    // Check for API keys and register tools accordingly
    var tavilyApiKey = Environment.GetEnvironmentVariable("TAVILY_API_KEY");
    var githubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN");

    // ✨ Typed tools - one-line registration!
    client
        .RegisterTool<AddTool>()
        .RegisterTool<SubtractTool>()
        .RegisterTool<MultiplyTool>()
        .RegisterTool<DivideTool>();

    // ✨ Typed client-side tool (emits tool_client SSE)
    client.RegisterTool<SetOrderFiltersTool>();

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

app.MapPost("/api/dashboard/stream", async (DashboardChatRequest chatRequest, HttpContext context) =>
{
    // ✨ Create client with observability enabled
    var client = new OpenRouterClient(new OpenRouterClientOptions
    {
        ApiKey = apiKey,
        Telemetry = new OpenRouterTelemetryOptions
        {
            EnableTelemetry = true,
            CapturePrompts = true,
            CaptureCompletions = true,
            CaptureToolDetails = true
        }
    });

    // ✨ Typed client-side tools for dashboard widgets
    client
        .RegisterTool<AddWidgetTool>()
        .RegisterTool<UpdateWidgetTool>()
        .RegisterTool<RemoveWidgetTool>();

    var conversationId = chatRequest.ConversationId ?? Guid.NewGuid().ToString();
    var history = dashboardConversationStore.GetOrAdd(conversationId, _ => new List<Message>());

    if (history.Count == 0)
    {
        var systemPrompt = @"You are a dashboard builder assistant. You help users create data visualizations from a SQLite database containing e-commerce data.

**Database Schema:**
- orders: id, customer_id, product_id, amount, quantity, status ('completed'|'pending'|'cancelled'), created_at, delivered_at
- customers: id, name, segment ('enterprise'|'smb'|'individual'), country, lifetime_value
- products: id, name, category, price, cost

**🚨 CRITICAL: MATCH USER'S REQUEST - DON'T BE OVERLY EAGER 🚨**

Widget Creation Guidelines:
1. If user asks for ONE thing (""show me sales over time"") → Create ONE widget
2. If user asks for a ""dashboard"" or lists multiple metrics → Create those widgets
3. NEVER add extra widgets beyond what the user explicitly requested
4. After creating, ask if they want to add more - don't assume

**Example - CORRECT (Single Request):**
User: ""Show me sales over time""
Assistant: Creates ONE line chart showing sales over time, then asks: ""Would you like me to add more widgets like revenue by category?""

**Example - CORRECT (Dashboard Request):**
User: ""Create a sales dashboard""
Assistant: Creates 3-4 relevant widgets (sales chart, revenue card, top products, etc.) because user asked for a ""dashboard""

**Example - WRONG (Too Eager):**
User: ""Show me sales over time""
Assistant: Creates 6 widgets (sales chart, revenue card, orders card, customers chart, etc.) ❌ User only asked for ONE thing!

**🚨 MANDATORY WORKFLOW - READ CAREFULLY 🚨**

When creating a widget, you MUST follow this sequence:

1. Generate a unique artifact ID (e.g., ""widget-revenue-x7k9m"")
2. Write COMPLETE artifact with the ID in the opening tag
3. Write some text like ""I've created the widget code above""
4. ONLY THEN call add_widget_to_dashboard tool with the SAME artifact ID
5. Wait for tool to complete
6. Repeat for next widget

**🚨 CRITICAL: AFTER TOOL COMPLETION - STOP AND WAIT 🚨**

When you receive a tool result (e.g., ""Widget added to dashboard""):
1. Write a brief acknowledgment (1-2 sentences): ""I've added the Sales Over Time widget to your dashboard.""
2. STOP immediately - do NOT recreate the artifact
3. STOP immediately - do NOT call the tool again
4. STOP immediately - do NOT generate new widgets unless the user asked for more
5. Your turn is OVER - wait for the user's next message

**Example - CORRECT:**
[Tool returns: success]
Assistant: ""I've added the Sales Over Time chart to your dashboard. What would you like to add next?""
[STOPS and waits for user]

**Example - WRONG - DO NOT DO THIS:**
[Tool returns: success]
Assistant: ""Great! Now let me create another widget..."" ❌ STOP! User didn't ask for more!
Assistant: Creates the same artifact again ❌ STOP! It's already created!
Assistant: Calls add_widget_to_dashboard again ❌ STOP! Tool already succeeded!

**ARTIFACT ID RULES:**
- ALWAYS include an 'id' attribute in the <artifact> tag
- Make IDs descriptive but unique (e.g., ""widget-revenue-a1b2c"", ""chart-orders-x9y8z"")
- Use lowercase, hyphens, and add 5 random characters at the end for uniqueness
- The SAME ID must be used in the add_widget_to_dashboard tool call

**CORRECT Example:**
<artifact id=""widget-revenue-k7m3p"" type=""code"" title=""Total Revenue"" language=""tsx.reactrunner"">
const SQL = `SELECT SUM(amount) as total FROM orders WHERE status = 'completed'`;
function Widget() {
  const db = useDatabase();
  const [value, setValue] = useState(0);
  useEffect(() => {
    if (!db) return;
    const result = db.exec(SQL);
    setValue(result[0]?.values[0]?.[0] || 0);
  }, [db]);
  return (
    <div className=""text-center p-8"">
      <div className=""text-4xl font-bold text-green-600"">${value.toLocaleString()}</div>
      <div className=""text-sm text-slate-500 mt-2"">Total Revenue</div>
    </div>
  );
}
</artifact>

I've created the Total Revenue widget. Let me add it to your dashboard now.

[TOOL CALL: add_widget_to_dashboard with artifactId=""widget-revenue-k7m3p"", widgetId=""total-revenue"", title=""Total Revenue"", size=""small""]

**WRONG Examples (DON'T DO THIS):**
❌ <artifact type=""code"" ...> (missing ID attribute!)
❌ Calling tool before artifact is complete
❌ Using different IDs in artifact tag and tool call
❌ Batch creating all artifacts then calling all tools

**RULES:**
- ALWAYS include 'id' attribute in artifact tags
- ARTIFACT FIRST, TOOL SECOND - NO EXCEPTIONS
- ONE widget at a time - create artifact, call tool, repeat
- artifactId in tool MUST match id in artifact tag

**Available in Widget Scope:**
- Database: `useDatabase()` hook returns db
- Execute queries: `db.exec(SQL)` returns array of results
- Parse results: `result[0]?.values` gives rows as arrays
- Recharts: BarChart, LineChart, PieChart, AreaChart, RadarChart, etc.
- ResponsiveContainer: ALWAYS wrap charts in this
- COLORS array: Pre-defined palette
- React hooks: useState, useEffect, useMemo
- Tailwind CSS for styling

**Widget Patterns:**

Metric Card (size: ""small""):
```
<artifact id=""widget-orders-q4w7e"" type=""code"" title=""Total Orders"" language=""tsx.reactrunner"">
const SQL = `SELECT COUNT(*) as total FROM orders WHERE status = 'completed'`;
function Widget() {
  const db = useDatabase();
  const [value, setValue] = useState(0);
  useEffect(() => {
    if (!db) return;
    const result = db.exec(SQL);
    setValue(result[0]?.values[0]?.[0] || 0);
  }, [db]);
  return (
    <div className=""text-center p-8"">
      <div className=""text-4xl font-bold text-blue-600"">{value.toLocaleString()}</div>
      <div className=""text-sm text-slate-500 mt-2"">Total Orders</div>
    </div>
  );
}
</artifact>

Then call: add_widget_to_dashboard(artifactId=""widget-orders-q4w7e"", ...)
```

Chart Widget (size: ""medium""):
```
<artifact id=""chart-products-r8t2y"" type=""code"" title=""Products by Category"" language=""tsx.reactrunner"">
const SQL = `SELECT category, COUNT(*) as count FROM products GROUP BY category`;
function Widget() {
  const db = useDatabase();
  const [data, setData] = useState([]);
  useEffect(() => {
    if (!db) return;
    const result = db.exec(SQL);
    const rows = result[0]?.values.map(([category, count]) => ({ category, count })) || [];
    setData(rows);
  }, [db]);
  return (
    <ResponsiveContainer width=""100%"" height={300}>
      <BarChart data={data}>
        <CartesianGrid strokeDasharray=""3 3"" />
        <XAxis dataKey=""category"" />
        <YAxis />
        <Tooltip />
        <Bar dataKey=""count"" fill={COLORS[0]} />
      </BarChart>
    </ResponsiveContainer>
  );
}
</artifact>

Then call: add_widget_to_dashboard(artifactId=""chart-products-r8t2y"", ...)
```

**Widget Sizes:**
- ""small"": Metric cards, KPIs
- ""medium"": Standard charts (default)
- ""large"": Tables, complex visualizations (spans 2 columns)

**Important Rules:**
- Match the user's request scope - don't add extra widgets they didn't ask for
- Single request (""show sales"") = ONE widget, Dashboard request = multiple widgets
- AFTER tool completion: Write brief acknowledgment, then STOP and WAIT for user
- NEVER recreate artifacts or recall tools after they succeed - your turn is OVER
- ALWAYS include 'id' attribute in <artifact> tags
- Use the same artifactId in both the artifact tag and the tool call
- Always handle null/undefined database
- Always handle empty query results
- Keep components simple and focused
- Use COLORS array for consistent styling
- The tool finds the artifact by ID - you MUST provide the correct artifactId";

        history.Add(Message.FromSystem(systemPrompt));
    }

    history.Add(Message.FromUser(chatRequest.Message));

    var request = new ChatCompletionRequest
    {
        Model = "anthropic/claude-haiku-4.5",
        Messages = history
    };

    request.EnableArtifacts(
        Artifacts.Custom("tsx.reactrunner", "Dashboard Widget")
            .WithLanguage("tsx")
            .WithInstruction("Create a React component with SQL query for data visualization")
    );

    var newMessages = await client.StreamAsSseAsync(request, context.Response);

    history.AddRange(newMessages);
})
.WithName("DashboardStream");

app.MapDelete("/api/conversation/{conversationId}", (string conversationId) =>
{
    conversationStore.TryRemove(conversationId, out _);
    return Results.Ok(new { message = "Conversation cleared" });
})
.WithName("ClearConversation");

app.MapPost("/api/generate-object", async (GenerateObjectApiRequest request) =>
{
    // ✨ Create client with observability for object generation
    var client = new OpenRouterClient(new OpenRouterClientOptions
    {
        ApiKey = apiKey,
        Telemetry = new OpenRouterTelemetryOptions
        {
            EnableTelemetry = true,
            CapturePrompts = true,
            CaptureCompletions = true
        }
    });
    
    try
    {
        var result = await client.GenerateObjectAsync(
            schema: request.Schema,
            prompt: request.Prompt,
            model: request.Model ?? "openai/gpt-4o-mini",
            options: new GenerateObjectOptions
            {
                Temperature = request.Temperature,
                MaxTokens = request.MaxTokens,
                MaxRetries = request.MaxRetries ?? 3,
                SchemaWarningThresholdBytes = 2048
            }
        );
        
        return Results.Ok(new
        {
            @object = result.Object,
            usage = result.Usage,
            finishReason = result.FinishReason
        });
    }
    catch (OpenRouterException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.WithName("GenerateObject");

// ✨ Strongly-typed structured outputs - no more JSON parsing!
app.MapPost("/api/triage-bug", async (BugReportRequest request) =>
{
    // ✨ Create client with observability for bug triage
    var client = new OpenRouterClient(new OpenRouterClientOptions
    {
        ApiKey = apiKey,
        Telemetry = new OpenRouterTelemetryOptions
        {
            EnableTelemetry = true,
            CapturePrompts = true,
            CaptureCompletions = true
        }
    });

    var analysis = await client.GenerateObjectAsync<BugAnalysis>(
        prompt: $"Analyze this bug report and extract structured information:\n\n{request.Description}",
        model: "anthropic/claude-sonnet-4.5"
    );

    // Clean, typed access to LLM output - no JsonElement.TryGetProperty() mess
    return Results.Ok(new
    {
        title = analysis.Object.Title,
        severity = analysis.Object.Severity,
        priority = analysis.Object.Priority,
        affectedComponents = analysis.Object.AffectedComponents,
        reproSteps = analysis.Object.ReproductionSteps,
        suggestedOwner = analysis.Object.AffectedComponents.FirstOrDefault() ?? "unassigned"
    });
})
.WithName("TriageBug");

// ✨ Demo of typed tools - clean, type-safe tool registration
app.MapGet("/api/demo-typed-tools", async (HttpContext context) =>
{
    // ✨ Create client with observability for typed tools demo
    var client = new OpenRouterClient(new OpenRouterClientOptions
    {
        ApiKey = apiKey,
        Telemetry = new OpenRouterTelemetryOptions
        {
            EnableTelemetry = true,
            CapturePrompts = true,
            CaptureCompletions = true,
            CaptureToolDetails = true
        }
    });

    // One-line registration for each typed tool!
    client.RegisterTool<CalculateTool>();
    client.RegisterTool<SearchTool>();
    client.RegisterTool<NotifyTool>();

    var request = new ChatCompletionRequest
    {
        Model = "anthropic/claude-sonnet-4.5",
        Messages = new List<Message>
        {
            Message.FromUser("Calculate 42 * 137, then search for 'typed tools', and notify me about the result")
        }
    };

    // Stream the response with typed tools!
    await client.StreamAsSseAsync(request, context.Response);
})
.WithName("DemoTypedTools");

app.MapDelete("/api/dashboard/conversation/{conversationId}", (string conversationId) =>
{
    dashboardConversationStore.TryRemove(conversationId, out _);
    return Results.Ok(new { message = "Dashboard conversation cleared" });
})
.WithName("ClearDashboardConversation");

app.Run();

public record ChatRequest(string Message, string? Model = null, string? ConversationId = null)
{
   public EnabledArtifact[]? EnabledArtifacts { get; init; }
}

public record DashboardChatRequest(string Message, string? Model = null, string? ConversationId = null);

public record BugReportRequest(string Description);

public enum Severity { Low, Medium, High, Critical }
public enum Priority { Low, Medium, High }

public class BugAnalysis
{
    public string Title { get; set; } = string.Empty;
    public Severity Severity { get; set; }
    public Priority Priority { get; set; }
    public List<string> AffectedComponents { get; set; } = new();
    public List<string> ReproductionSteps { get; set; } = new();
}

public record GenerateObjectApiRequest
{
    public JsonElement Schema { get; init; }
    public string Prompt { get; init; } = string.Empty;
    public string? Model { get; init; }
    public double? Temperature { get; init; }
    public int? MaxTokens { get; init; }
    public int? MaxRetries { get; init; }
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

// ✨ Typed Dashboard Tools - Client-side

public class AddWidgetParams
{
    public string ArtifactId { get; set; } = string.Empty;
    public string WidgetId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Size { get; set; } = "medium";
}

public class AddWidgetTool : VoidTool<AddWidgetParams>
{
    public override string Name => "add_widget_to_dashboard";
    public override string Description => "Add a new widget to the dashboard canvas. You must specify the artifactId of the tsx.reactrunner artifact that contains the widget code.";
    public override ToolMode Mode => ToolMode.ClientSide;

    protected override void HandleVoid(AddWidgetParams p) { }
}

public class UpdateWidgetParams
{
    public string WidgetId { get; set; } = string.Empty;
    public string? Title { get; set; }
}

public class UpdateWidgetTool : VoidTool<UpdateWidgetParams>
{
    public override string Name => "update_widget";
    public override string Description => "Update an existing widget on the dashboard";
    public override ToolMode Mode => ToolMode.ClientSide;

    protected override void HandleVoid(UpdateWidgetParams p) { }
}

public class RemoveWidgetParams
{
    public string WidgetId { get; set; } = string.Empty;
}

public class RemoveWidgetTool : VoidTool<RemoveWidgetParams>
{
    public override string Name => "remove_widget";
    public override string Description => "Remove a widget from the dashboard";
    public override ToolMode Mode => ToolMode.ClientSide;

    protected override void HandleVoid(RemoveWidgetParams p) { }
}

// Legacy method-based version (kept for reference)
public class DashboardTools
{
    [ToolMethod("Add a new widget to the dashboard canvas. You must specify the artifactId of the tsx.reactrunner artifact that contains the widget code.")]
    public void AddWidgetToDashboard(
        [ToolParameter("The ID of the artifact containing the widget code (must match the id you used in the <artifact> tag)")] string artifactId,
        [ToolParameter("Unique identifier for the widget on the dashboard")] string widgetId,
        [ToolParameter("Display title for the widget")] string title,
        [ToolParameter("Widget size: 'small', 'medium', or 'large'")] string size = "medium")
    {
    }

    [ToolMethod("Update an existing widget on the dashboard")]
    public void UpdateWidget(
        [ToolParameter("ID of the widget to update")] string widgetId,
        [ToolParameter("New title (optional)")] string? title = null)
    {
    }

    [ToolMethod("Remove a widget from the dashboard")]
    public void RemoveWidget(
        [ToolParameter("ID of the widget to remove")] string widgetId)
    {
    }
}
