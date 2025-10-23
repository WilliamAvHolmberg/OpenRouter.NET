using OpenRouter.NET;
using OpenRouter.NET.Artifacts;
using OpenRouter.NET.Models;
using OpenRouter.NET.Tools;
using Spectre.Console;

namespace DiagnosticSample.Configuration;

/// <summary>
/// Handles user input and configuration for diagnostic tests
/// </summary>
public static class ConfigurationManager
{
    public static string? SelectMode()
    {
        var mode = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold blue]Select mode:[/]")
                .AddChoices(new[] {
                    "🎮 Interactive Mode (configure everything)",
                    "⚡ Quick Test (hardcoded React button)",
                    "🔧 Tool Call Test (test server & client tools)",
                    "📊 Batch Reliability Test (run 10 artifact tests)",
                    "🚪 Exit"
                }));

        return mode == "🚪 Exit" ? null : mode;
    }

    public static (ChatCompletionRequest Request, string Description, OpenRouterClient? Client)
        ConfigureRequest(string mode, string apiKey)
    {
        return mode switch
        {
            "⚡ Quick Test (hardcoded React button)" => ConfigureQuickTest(),
            "🔧 Tool Call Test (test server & client tools)" => ConfigureToolCallTest(apiKey),
            _ => ConfigureInteractiveMode(apiKey)
        };
    }

    private static (ChatCompletionRequest, string, OpenRouterClient?) ConfigureQuickTest()
    {
        var request = new ChatCompletionRequest
        {
            Model = "anthropic/claude-3.5-sonnet",
            Messages = new List<Message>
            {
                Message.FromUser("Create a simple React button component with TypeScript and CSS")
            }
        };

        request.EnableArtifacts(
            Artifacts.Code(language: "typescript"),
            Artifacts.Code(language: "css")
        );

        return (request, "Quick React Button Test", null);
    }

    private static (ChatCompletionRequest, string, OpenRouterClient?) ConfigureToolCallTest(string apiKey)
    {
        var client = new OpenRouterClient(apiKey);

        // Register server-side tools
        client.RegisterTool(ToolDefinitions.GetWeather, ToolMode.AutoExecute);
        client.RegisterTool(ToolDefinitions.Calculate, ToolMode.AutoExecute);

        // Register client-side tool
        var notificationSchema = new
        {
            type = "object",
            properties = new
            {
                message = new { type = "string" },
                level = new { type = "string" }
            }
        };
        client.RegisterClientTool("show_notification", "Display notification", notificationSchema);

        var request = new ChatCompletionRequest
        {
            Model = "anthropic/claude-3.5-sonnet",
            Messages = new List<Message>
            {
                Message.FromUser("What's the weather in Stockholm? Then calculate 42 * 1337. Finally show me a success notification.")
            },
            ToolLoopConfig = new ToolLoopConfig
            {
                Enabled = true,
                MaxIterations = 5
            }
        };

        return (request, "Tool Call Test", client);
    }

    private static (ChatCompletionRequest, string, OpenRouterClient?) ConfigureInteractiveMode(string apiKey)
    {
        var testDescription = AnsiConsole.Ask<string>("\n[bold cyan]Test description:[/]", "Interactive Test");

        // Select model
        AnsiConsole.MarkupLine("\n[bold]📡 Fetching models...[/]");
        var tempClient = new OpenRouterClient(apiKey);
        var models = tempClient.GetModelsAsync().Result;

        var popularModels = new[]
        {
            "anthropic/claude-3.5-sonnet",
            "openai/gpt-4o",
            "openai/gpt-3.5-turbo",
            "google/gemini-pro",
            "meta-llama/llama-3.1-70b-instruct"
        };

        var modelChoices = models
            .Select(m => m.Id)
            .OrderBy(m =>
            {
                var index = Array.IndexOf(popularModels, m);
                return index == -1 ? 999 : index;
            })
            .ThenBy(m => m)
            .ToList();

        var selectedModel = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("\n[bold cyan]🤖 Select model:[/]")
                .PageSize(10)
                .EnableSearch()
                .SearchPlaceholderText("[grey](Type to search...)[/]")
                .AddChoices(modelChoices));

        // Get user message
        var userMessage = AnsiConsole.Ask<string>("\n[bold cyan]💬 Your message/request:[/]",
            "Create a React button component");

        // Configure artifacts
        var enableArtifacts = AnsiConsole.Confirm("\n[bold]📦 Enable artifact support?[/]", true);

        var request = new ChatCompletionRequest
        {
            Model = selectedModel,
            Messages = new List<Message> { Message.FromUser(userMessage) }
        };

        if (enableArtifacts)
        {
            ConfigureArtifacts(request);
        }

        return (request, testDescription, null);
    }

    private static void ConfigureArtifacts(ChatCompletionRequest request)
    {
        var artifactChoices = AnsiConsole.Prompt(
            new MultiSelectionPrompt<string>()
                .Title("[bold]Select artifact types to enable:[/]")
                .PageSize(10)
                .InstructionsText("[grey](Space to toggle, Enter to confirm)[/]")
                .AddChoices(new[] {
                    "Code (TypeScript)",
                    "Code (Python)",
                    "Code (JavaScript)",
                    "Code (CSS)",
                    "Code (Generic)",
                    "Document (Markdown)",
                    "Document (HTML)",
                    "Data (JSON)",
                    "Data (CSV)",
                    "Custom type..."
                }));

        var artifactDefs = new List<ArtifactDefinition>();

        foreach (var choice in artifactChoices)
        {
            switch (choice)
            {
                case "Code (TypeScript)":
                    artifactDefs.Add(Artifacts.Code(language: "typescript"));
                    break;
                case "Code (Python)":
                    artifactDefs.Add(Artifacts.Code(language: "python"));
                    break;
                case "Code (JavaScript)":
                    artifactDefs.Add(Artifacts.Code(language: "javascript"));
                    break;
                case "Code (CSS)":
                    artifactDefs.Add(Artifacts.Code(language: "css"));
                    break;
                case "Code (Generic)":
                    artifactDefs.Add(Artifacts.Code());
                    break;
                case "Document (Markdown)":
                    artifactDefs.Add(Artifacts.Document(format: "markdown"));
                    break;
                case "Document (HTML)":
                    artifactDefs.Add(Artifacts.Document(format: "html"));
                    break;
                case "Data (JSON)":
                    artifactDefs.Add(Artifacts.Data(format: "json"));
                    break;
                case "Data (CSV)":
                    artifactDefs.Add(Artifacts.Data(format: "csv"));
                    break;
                case "Custom type...":
                    var customType = AnsiConsole.Ask<string>("Enter custom artifact type:");
                    var customLang = AnsiConsole.Ask<string>("Enter language (optional):", "");
                    var customInstruction = AnsiConsole.Ask<string>("Enter instruction (optional):", "");

                    var custom = new GenericArtifact(customType)
                        .WithLanguage(customLang)
                        .WithInstruction(customInstruction);
                    artifactDefs.Add(custom);
                    break;
            }
        }

        var customInstructions = AnsiConsole.Ask<string>("\n[bold]Additional instructions (optional):[/]", "");

        if (artifactDefs.Count > 0)
        {
            request.EnableArtifacts(artifactDefs.ToArray());
        }
        else
        {
            request.EnableArtifactSupport(string.IsNullOrEmpty(customInstructions) ? null : customInstructions);
        }
    }
}
