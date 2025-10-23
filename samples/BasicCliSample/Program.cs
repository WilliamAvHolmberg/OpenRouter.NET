using System.Text;
using OpenRouter.NET;
using OpenRouter.NET.Models;
using Spectre.Console;

AnsiConsole.Write(
    new FigletText("OpenRouter.NET")
        .LeftJustified()
        .Color(Color.Blue));

AnsiConsole.MarkupLine("[dim]Basic CLI Sample[/]\n");

var apiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
if (string.IsNullOrEmpty(apiKey))
{
    AnsiConsole.MarkupLine("[red]❌ Please set OPENROUTER_API_KEY environment variable[/]");
    AnsiConsole.MarkupLine("[dim]   Example: export OPENROUTER_API_KEY=\"your-key-here\"[/]\n");
    return;
}

var client = new OpenRouterClient(apiKey);
List<ModelInfo>? cachedModels = null;

while (true)
{
    var choice = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("\n[bold blue]What do you want to do?[/]")
            .PageSize(10)
            .AddChoices(new[] {
                "📊 List available models",
                "💬 Send chat message (streaming)",
                "🔁 Start conversation",
                "🚪 Exit"
            }));
    
    switch (choice)
    {
        case "📊 List available models":
            cachedModels = await ListModels();
            break;
        case "💬 Send chat message (streaming)":
            await SendChatMessage(cachedModels);
            break;
        case "🔁 Start conversation":
            await StartConversation(cachedModels);
            break;
        case "🚪 Exit":
            AnsiConsole.MarkupLine("\n[bold green]👋 Goodbye![/]");
            return;
    }
}

async Task<List<ModelInfo>> ListModels()
{
    var models = await AnsiConsole.Status()
        .StartAsync("📊 Fetching models from OpenRouter...", async ctx =>
        {
            try
            {
                return await client.GetModelsAsync();
            }
            catch (OpenRouterException ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Error: {ex.Message}[/]");
                return new List<ModelInfo>();
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Unexpected error: {ex.Message}[/]");
                return new List<ModelInfo>();
            }
        });
    
    if (models.Count > 0)
    {
        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.AddColumn("[bold]Model ID[/]");
        table.AddColumn("[bold]Name[/]");
        table.AddColumn("[bold]Context Length[/]");
        
        foreach (var model in models.Take(15))
        {
            table.AddRow(
                $"[cyan]{model.Id}[/]",
                model.Name,
                $"[green]{model.ContextLength:N0}[/] tokens"
            );
        }
        
        AnsiConsole.Write(table);
        
        if (models.Count > 15)
        {
            AnsiConsole.MarkupLine($"\n[dim]... and {models.Count - 15} more models[/]");
        }
    }
    
    return models;
}

async Task SendChatMessage(List<ModelInfo>? availableModels)
{
    var userMessage = AnsiConsole.Ask<string>("\n[bold cyan]💬 Enter your message:[/]");
    
    if (string.IsNullOrWhiteSpace(userMessage))
    {
        AnsiConsole.MarkupLine("[red]❌ Message cannot be empty[/]");
        return;
    }
    
    if (availableModels == null || availableModels.Count == 0)
    {
        availableModels = await AnsiConsole.Status()
            .StartAsync("⏳ Fetching models...", async ctx => await FetchModels());
    }
    
    var model = SelectModel(availableModels);
    
    AnsiConsole.MarkupLine($"\n[bold]🔄 Streaming response from[/] [cyan]{model}[/]...\n");
    
    TimeSpan? firstTokenTime = null;
    var responseText = new System.Text.StringBuilder();
    var artifacts = new List<OpenRouter.NET.Models.Artifact>();
    var streamingStarted = false;
    var currentElapsed = TimeSpan.Zero;
    
    try
    {
        var request = new ChatCompletionRequest
        {
            Model = model,
            Messages = new List<Message>
            {
                Message.FromUser(userMessage)
            }
        };
        
        await AnsiConsole.Live(CreateResponseDisplay(responseText.ToString(), currentElapsed, streamingStarted, artifacts.Count))
            .StartAsync(async ctx =>
            {
                await foreach (var chunk in client.StreamAsync(request))
                {
                    currentElapsed = chunk.ElapsedTime;
                    
                    if (!streamingStarted && chunk.TextDelta != null)
                    {
                        firstTokenTime = chunk.ElapsedTime;
                        streamingStarted = true;
                    }
                    
                    // Handle artifacts
                    switch (chunk.Artifact)
                    {
                        case OpenRouter.NET.Models.ArtifactCompleted completed:
                            artifacts.Add(new OpenRouter.NET.Models.Artifact
                            {
                                Id = completed.ArtifactId,
                                Type = completed.Type,
                                Title = completed.Title,
                                Content = completed.Content,
                                Language = completed.Language
                            });
                            break;
                    }
                    
                    if (chunk.TextDelta != null)
                    {
                        responseText.Append(chunk.TextDelta);
                    }
                    
                    ctx.UpdateTarget(CreateResponseDisplay(responseText.ToString(), currentElapsed, streamingStarted, artifacts.Count));
                }
            });
        
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"\n[dim]⏱️  Time to first token: {firstTokenTime?.TotalMilliseconds:F0}ms[/]");
        AnsiConsole.MarkupLine($"[dim]⏱️  Total time: {currentElapsed.TotalSeconds:F2}s[/]");
        
        if (artifacts.Count > 0)
        {
            AnsiConsole.MarkupLine($"[dim]📦 Artifacts received: {artifacts.Count}[/]");
            foreach (var artifact in artifacts)
            {
                AnsiConsole.MarkupLine($"[dim]   • {artifact.Title} ({artifact.Type})[/]");
            }
        }
        
        AnsiConsole.MarkupLine("\n[green]✅ Done![/]");
    }
    catch (Exception ex)
    {
        AnsiConsole.MarkupLine($"\n[red]❌ Error: {ex.Message}[/]");
    }
}

Table CreateResponseDisplay(string content, TimeSpan elapsed, bool receivedFirstToken, int artifactsCount)
{
    var table = new Table().Border(TableBorder.None);
    table.AddColumn(new TableColumn("").NoWrap());
    
    var status = receivedFirstToken 
        ? $"[green]● Streaming[/]" 
        : $"[yellow]● Waiting for response...[/]";
    
    var artifactInfo = artifactsCount > 0 ? $" [blue]📦 {artifactsCount} artifact(s)[/]" : "";
    
    table.AddRow($"[bold yellow]Assistant:[/] {status} [dim]({elapsed.TotalSeconds:F1}s)[/]{artifactInfo}");
    table.AddRow("");
    
    if (!string.IsNullOrEmpty(content))
    {
        table.AddRow(content.Replace("[", "[[").Replace("]", "]]"));
    }
    
    return table;
}

string SelectModel(List<ModelInfo> models)
{
    var popularModels = new[]
    {
        "openai/gpt-3.5-turbo",
        "openai/gpt-4o",
        "anthropic/claude-3.5-sonnet",
        "google/gemini-pro",
        "meta-llama/llama-3.1-70b-instruct"
    };
    
    var modelChoices = models
        .Select(m => m.Id)
        .OrderBy(m =>
        {
            var popularIndex = Array.IndexOf(popularModels, m);
            if (popularIndex != -1) return popularIndex;
            
            var provider = m.Split('/')[0];
            return 100 + string.Compare(provider, "", StringComparison.OrdinalIgnoreCase);
        })
        .ThenBy(m => m)
        .ToList();
    
    var selection = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("\n[bold cyan]🤖 Select a model:[/]")
            .PageSize(10)
            .MoreChoicesText("[grey](Move up and down to see more models)[/]")
            .EnableSearch()
            .SearchPlaceholderText("[grey](Type to search... e.g. 'google/gemini', 'gpt', 'claude')[/]")
            .AddChoices(modelChoices));
    
    return selection;
}

async Task<List<ModelInfo>> FetchModels()
{
    try
    {
        return await client.GetModelsAsync();
    }
    catch
    {
        return new List<ModelInfo>();
    }
}

async Task StartConversation(List<ModelInfo>? models)
{
    AnsiConsole.Clear();
    AnsiConsole.Write(
        new FigletText("Conversation Mode")
            .LeftJustified()
            .Color(Color.Green));
    
    AnsiConsole.MarkupLine("[dim]Multi-turn conversation with memory[/]\n");
    
    if (models == null)
    {
        models = await FetchModels();
    }
    
    if (models.Count == 0)
    {
        AnsiConsole.MarkupLine("[red]❌ Failed to fetch models[/]");
        return;
    }
    
    var selectedModel = SelectModel(models);
    
    var conversationHistory = new List<Message>();
    var totalInputTokens = 0;
    var totalOutputTokens = 0;
    
    AnsiConsole.MarkupLine($"\n[green]✓ Selected model:[/] [cyan]{selectedModel}[/]");
    AnsiConsole.MarkupLine("[dim]Commands: /exit to quit, /clear to reset history, /history to view conversation[/]\n");
    
    while (true)
    {
        var userInput = AnsiConsole.Prompt(
            new TextPrompt<string>("[bold blue]You:[/]")
                .AllowEmpty());
        
        if (string.IsNullOrWhiteSpace(userInput))
        {
            continue;
        }
        
        if (userInput == "/exit")
        {
            AnsiConsole.MarkupLine("\n[green]✓ Exiting conversation mode[/]");
            break;
        }
        
        if (userInput == "/clear")
        {
            conversationHistory.ClearHistory(keepSystemPrompt: true);
            totalInputTokens = 0;
            totalOutputTokens = 0;
            AnsiConsole.MarkupLine("[yellow]✓ Conversation history cleared[/]\n");
            continue;
        }
        
        if (userInput == "/history")
        {
            ShowConversationHistory(conversationHistory, totalInputTokens, totalOutputTokens);
            continue;
        }
        
        conversationHistory.AddUserMessage(userInput);
        
        var request = new ChatCompletionRequest
        {
            Model = selectedModel,
            Messages = conversationHistory
        };
        
        var responseText = new StringBuilder();
        var artifacts = new List<OpenRouter.NET.Models.Artifact>();
        TimeSpan? firstTokenTime = null;
        var currentElapsed = TimeSpan.Zero;
        var streamingStarted = false;
        
        try
        {
            await AnsiConsole.Live(CreateResponseDisplay("", currentElapsed, streamingStarted, artifacts.Count))
                .StartAsync(async ctx =>
                {
                    await foreach (var chunk in client.StreamAsync(request))
                    {
                        currentElapsed = chunk.ElapsedTime;
                        
                        if (firstTokenTime == null && chunk.IsFirstChunk)
                        {
                            firstTokenTime = chunk.ElapsedTime;
                            streamingStarted = true;
                        }
                        
                        switch (chunk.Artifact)
                        {
                            case OpenRouter.NET.Models.ArtifactCompleted completed:
                                artifacts.Add(new OpenRouter.NET.Models.Artifact
                                {
                                    Id = completed.ArtifactId,
                                    Type = completed.Type,
                                    Title = completed.Title,
                                    Content = completed.Content,
                                    Language = completed.Language
                                });
                                break;
                        }
                        
                        if (chunk.TextDelta != null)
                        {
                            responseText.Append(chunk.TextDelta);
                        }
                        
                        ctx.UpdateTarget(CreateResponseDisplay(responseText.ToString(), currentElapsed, streamingStarted, artifacts.Count));
                    }
                });
            
            AnsiConsole.WriteLine();
            
            var fullResponse = responseText.ToString();
            conversationHistory.AddAssistantMessage(fullResponse);
            
            var estimatedInputTokens = conversationHistory.EstimateTokenCount();
            var estimatedOutputTokens = (int)(fullResponse.Length / 4.0);
            
            totalInputTokens = estimatedInputTokens;
            totalOutputTokens += estimatedOutputTokens;
            
            AnsiConsole.MarkupLine($"[dim]⏱️  TTFT: {firstTokenTime?.TotalMilliseconds:F0}ms | Total: {currentElapsed.TotalSeconds:F2}s[/]");
            AnsiConsole.MarkupLine($"[dim]📊 Est. tokens - Input: ~{estimatedInputTokens:N0} | Output: ~{estimatedOutputTokens:N0} | Total conversation: ~{totalInputTokens + totalOutputTokens:N0}[/]");
            
            if (artifacts.Count > 0)
            {
                AnsiConsole.MarkupLine($"[dim]📦 Artifacts: {artifacts.Count}[/]");
                foreach (var artifact in artifacts)
                {
                    AnsiConsole.MarkupLine($"[dim]   • {artifact.Title} ({artifact.Type})[/]");
                }
            }
            
            AnsiConsole.WriteLine();
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"\n[red]❌ Error: {ex.Message}[/]\n");
        }
    }
}

void ShowConversationHistory(List<Message> history, int totalInputTokens, int totalOutputTokens)
{
    AnsiConsole.WriteLine();
    
    var panel = new Panel(history.GetConversationSummary())
    {
        Header = new PanelHeader($"📜 Conversation History ({history.GetMessageCount()} messages)"),
        Border = BoxBorder.Rounded,
        Padding = new Padding(1)
    };
    
    AnsiConsole.Write(panel);
    
    AnsiConsole.MarkupLine($"\n[dim]📊 Statistics:[/]");
    AnsiConsole.MarkupLine($"[dim]   • User messages: {history.GetMessageCount("user")}[/]");
    AnsiConsole.MarkupLine($"[dim]   • Assistant messages: {history.GetMessageCount("assistant")}[/]");
    AnsiConsole.MarkupLine($"[dim]   • System messages: {history.GetMessageCount("system")}[/]");
    AnsiConsole.MarkupLine($"[dim]   • Estimated tokens: ~{totalInputTokens + totalOutputTokens:N0}[/]");
    AnsiConsole.WriteLine();
}

