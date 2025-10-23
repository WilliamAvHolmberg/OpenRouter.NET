using OpenRouter.NET;
using OpenRouter.NET.Artifacts;
using OpenRouter.NET.Models;
using Spectre.Console;
using System.Diagnostics;

namespace DiagnosticSample;

public class BatchReliabilityTester
{
    private readonly string _apiKey;

    public BatchReliabilityTester(string apiKey)
    {
        _apiKey = apiKey;
    }

    public async Task RunBatchTestAsync(string model, int iterations, bool parallel)
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(
            new FigletText("Batch Reliability Test")
                .Centered()
                .Color(Color.Blue));

        // Create output directory
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var outputDir = Path.Combine("diagnostic_output", $"batch_{timestamp}");
        Directory.CreateDirectory(outputDir);

        AnsiConsole.MarkupLine($"[bold]Model:[/] {model}");
        AnsiConsole.MarkupLine($"[bold]Iterations:[/] {iterations}");
        AnsiConsole.MarkupLine($"[bold]Execution:[/] {(parallel ? "Parallel" : "Sequential")}");
        AnsiConsole.MarkupLine($"[bold]Test:[/] Python function with artifact");
        AnsiConsole.MarkupLine($"[bold]Output:[/] {outputDir}\n");

        var results = new List<TestResult>();
        var totalCost = 0.0;

        if (parallel)
        {
            await AnsiConsole.Progress()
                .StartAsync(async ctx =>
                {
                    var task = ctx.AddTask("[green]Running tests in parallel...[/]", maxValue: iterations);

                    var tasks = Enumerable.Range(1, iterations)
                        .Select(i => RunSingleTestAsync(model, i))
                        .ToList();

                    var completedTasks = 0;
                    while (completedTasks < tasks.Count)
                    {
                        var completed = await Task.WhenAny(tasks.Where(t => !t.IsCompleted));
                        completedTasks++;
                        task.Increment(1);
                    }

                    results = (await Task.WhenAll(tasks)).ToList();
                });
        }
        else
        {
            await AnsiConsole.Progress()
                .StartAsync(async ctx =>
                {
                    var task = ctx.AddTask("[green]Running tests sequentially...[/]", maxValue: iterations);

                    for (int i = 0; i < iterations; i++)
                    {
                        var result = await RunSingleTestAsync(model, i + 1);
                        results.Add(result);
                        task.Increment(1);

                        // Small delay to avoid rate limiting
                        await Task.Delay(500);
                    }
                });
        }

        totalCost = results.Sum(r => r.Cost);

        // Display results
        DisplayResults(results, totalCost);

        // Save results to files
        await SaveResultsAsync(outputDir, model, iterations, parallel, results, totalCost);

        AnsiConsole.MarkupLine($"\n[bold green]✓ Results saved to:[/] [link]{outputDir}[/]");
    }

    private async Task<TestResult> RunSingleTestAsync(string model, int runNumber)
    {
        var client = new OpenRouterClient(_apiKey);
        var stopwatch = Stopwatch.StartNew();

        var request = new ChatCompletionRequest
        {
            Model = model,
            Messages = new List<Message>
            {
                Message.FromUser("Create a simple Python function that says hello")
            }
        };

        request.EnableArtifacts(Artifacts.Code(language: "python"));

        var artifactStarted = false;
        var artifactCompleted = false;
        var hasClosingTag = false;
        var responseText = new System.Text.StringBuilder();
        var rawStreamText = new System.Text.StringBuilder(); // Capture raw stream including artifacts
        var chunkCount = 0;
        var error = string.Empty;
        var inputTokens = 0;
        var outputTokens = 0;
        var systemPrompt = string.Empty;
        var completedArtifacts = new List<ArtifactCompleted>();

        // Get system prompt from request
        var systemMessage = request.Messages.FirstOrDefault(m => m.Role == "system");
        if (systemMessage != null && systemMessage.Content is string content)
        {
            systemPrompt = content;
        }

        try
        {
            await foreach (var chunk in client.StreamAsync(request))
            {
                chunkCount++;

                if (chunk.TextDelta != null)
                {
                    responseText.Append(chunk.TextDelta);
                }

                // Capture raw stream data from chunk.Raw
                var deltaContent = chunk.Raw?.Choices?.FirstOrDefault()?.Delta?.Content;
                if (deltaContent != null)
                {
                    rawStreamText.Append(deltaContent);
                }

                if (chunk.Artifact != null)
                {
                    if (chunk.Artifact is ArtifactStarted)
                        artifactStarted = true;
                    else if (chunk.Artifact is ArtifactCompleted completed)
                    {
                        artifactCompleted = true;
                        completedArtifacts.Add(completed);
                    }
                }

                if (chunk.Completion?.Usage != null)
                {
                    inputTokens = chunk.Completion.Usage.PromptTokens;
                    outputTokens = chunk.Completion.Usage.CompletionTokens;
                }
            }

            hasClosingTag = rawStreamText.ToString().Contains("</artifact>");
        }
        catch (Exception ex)
        {
            error = ex.Message;
        }

        stopwatch.Stop();

        // Calculate approximate cost (rough estimates)
        var cost = CalculateCost(model, inputTokens, outputTokens);

        return new TestResult
        {
            RunNumber = runNumber,
            Success = artifactCompleted,
            ArtifactStarted = artifactStarted,
            ArtifactCompleted = artifactCompleted,
            HasClosingTag = hasClosingTag,
            ChunkCount = chunkCount,
            ResponseLength = rawStreamText.Length,
            ResponseText = responseText.ToString(), // Parsed text (without artifact XML)
            RawStreamText = rawStreamText.ToString(), // Raw stream with artifact XML
            Duration = stopwatch.Elapsed,
            Error = error,
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            Cost = cost,
            SystemPrompt = systemPrompt,
            Artifacts = completedArtifacts
        };
    }

    private double CalculateCost(string model, int inputTokens, int outputTokens)
    {
        // Rough cost estimates per million tokens (as of 2025)
        var (inputCost, outputCost) = model.ToLower() switch
        {
            var m when m.Contains("haiku") => (0.25, 1.25),
            var m when m.Contains("sonnet") => (3.0, 15.0),
            var m when m.Contains("opus") => (15.0, 75.0),
            _ => (1.0, 3.0) // Default estimate
        };

        var inputCostTotal = (inputTokens / 1_000_000.0) * inputCost;
        var outputCostTotal = (outputTokens / 1_000_000.0) * outputCost;

        return inputCostTotal + outputCostTotal;
    }

    private async Task SaveResultsAsync(string outputDir, string model, int iterations, bool parallel, List<TestResult> results, double totalCost)
    {
        // Save summary
        var summaryPath = Path.Combine(outputDir, "summary.txt");
        var summary = new System.Text.StringBuilder();
        summary.AppendLine("=== BATCH RELIABILITY TEST SUMMARY ===");
        summary.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        summary.AppendLine($"Model: {model}");
        summary.AppendLine($"Iterations: {iterations}");
        summary.AppendLine($"Execution: {(parallel ? "Parallel" : "Sequential")}");
        summary.AppendLine();
        summary.AppendLine($"Total Tests: {results.Count}");
        summary.AppendLine($"Succeeded: {results.Count(r => r.Success)}");
        summary.AppendLine($"Failed: {results.Count(r => !r.Success)}");
        summary.AppendLine($"Success Rate: {(results.Count(r => r.Success) / (double)results.Count * 100):F1}%");
        summary.AppendLine($"Total Cost: ${totalCost:F4}");
        await File.WriteAllTextAsync(summaryPath, summary.ToString());

        // Save detailed results as CSV
        var csvPath = Path.Combine(outputDir, "results.csv");
        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Run,Success,Started,Completed,HasClosingTag,Duration_ms,Chunks,ResponseLength,InputTokens,OutputTokens,Cost,Error");
        foreach (var r in results)
        {
            csv.AppendLine($"{r.RunNumber},{r.Success},{r.ArtifactStarted},{r.ArtifactCompleted},{r.HasClosingTag},{r.Duration.TotalMilliseconds:F0},{r.ChunkCount},{r.ResponseLength},{r.InputTokens},{r.OutputTokens},{r.Cost:F5},\"{r.Error}\"");
        }
        await File.WriteAllTextAsync(csvPath, csv.ToString());

        // Save system prompt (from first result)
        if (results.Any() && !string.IsNullOrEmpty(results[0].SystemPrompt))
        {
            var promptPath = Path.Combine(outputDir, "system_prompt.txt");
            await File.WriteAllTextAsync(promptPath, results[0].SystemPrompt);
        }

        // Save failed responses
        var failedResults = results.Where(r => !r.Success).ToList();
        if (failedResults.Any())
        {
            var failedDir = Path.Combine(outputDir, "failed_responses");
            Directory.CreateDirectory(failedDir);

            foreach (var failed in failedResults)
            {
                var runDir = Path.Combine(failedDir, $"run_{failed.RunNumber:D3}");
                Directory.CreateDirectory(runDir);

                var failedSummaryPath = Path.Combine(runDir, "summary.txt");
                var content = new System.Text.StringBuilder();
                content.AppendLine($"=== Run #{failed.RunNumber} - FAILED ===");
                content.AppendLine($"Started: {failed.ArtifactStarted}");
                content.AppendLine($"Completed: {failed.ArtifactCompleted}");
                content.AppendLine($"Has Closing Tag: {failed.HasClosingTag}");
                content.AppendLine($"Duration: {failed.Duration.TotalMilliseconds:F0}ms");
                content.AppendLine($"Chunks: {failed.ChunkCount}");
                content.AppendLine($"Tokens: {failed.InputTokens + failed.OutputTokens}");
                content.AppendLine();
                content.AppendLine("=== PARSED RESPONSE (TextDelta only) ===");
                content.AppendLine(failed.ResponseText);
                content.AppendLine();
                content.AppendLine("=== RAW STREAM (including artifact XML) ===");
                content.AppendLine(failed.RawStreamText);
                await File.WriteAllTextAsync(failedSummaryPath, content.ToString());

                // Save artifacts if any
                if (failed.Artifacts.Any())
                {
                    var artifactsDir = Path.Combine(runDir, "artifacts");
                    Directory.CreateDirectory(artifactsDir);

                    foreach (var artifact in failed.Artifacts)
                    {
                        var artifactFilename = artifact.Title;
                        if (string.IsNullOrEmpty(artifactFilename))
                        {
                            artifactFilename = $"{artifact.Type}_{artifact.ArtifactId}";
                        }
                        var artifactPath = Path.Combine(artifactsDir, artifactFilename);
                        await File.WriteAllTextAsync(artifactPath, artifact.Content);
                    }
                }
            }
        }

        // Save ALL successful responses
        var successResults = results.Where(r => r.Success).ToList();
        if (successResults.Any())
        {
            var successDir = Path.Combine(outputDir, "success_responses");
            Directory.CreateDirectory(successDir);

            foreach (var success in successResults)
            {
                var runDir = Path.Combine(successDir, $"run_{success.RunNumber:D3}");
                Directory.CreateDirectory(runDir);

                var successSummaryPath = Path.Combine(runDir, "summary.txt");
                var content = new System.Text.StringBuilder();
                content.AppendLine($"=== Run #{success.RunNumber} - SUCCESS ===");
                content.AppendLine($"Duration: {success.Duration.TotalMilliseconds:F0}ms");
                content.AppendLine($"Chunks: {success.ChunkCount}");
                content.AppendLine($"Tokens: {success.InputTokens + success.OutputTokens}");
                content.AppendLine($"Artifacts: {success.Artifacts.Count}");
                content.AppendLine();
                content.AppendLine("=== PARSED RESPONSE (TextDelta only) ===");
                content.AppendLine(success.ResponseText);
                content.AppendLine();
                content.AppendLine("=== RAW STREAM (including artifact XML) ===");
                content.AppendLine(success.RawStreamText);
                await File.WriteAllTextAsync(successSummaryPath, content.ToString());

                // Save artifacts
                if (success.Artifacts.Any())
                {
                    var artifactsDir = Path.Combine(runDir, "artifacts");
                    Directory.CreateDirectory(artifactsDir);

                    foreach (var artifact in success.Artifacts)
                    {
                        var artifactFilename = artifact.Title;
                        if (string.IsNullOrEmpty(artifactFilename))
                        {
                            artifactFilename = $"{artifact.Type}_{artifact.ArtifactId}";
                        }
                        var artifactPath = Path.Combine(artifactsDir, artifactFilename);
                        await File.WriteAllTextAsync(artifactPath, artifact.Content);
                    }
                }
            }
        }
    }

    private void DisplayResults(List<TestResult> results, double totalCost)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[bold green]Results[/]").RuleStyle("grey"));
        AnsiConsole.WriteLine();

        var successCount = results.Count(r => r.Success);
        var failureCount = results.Count - successCount;
        var successRate = (successCount / (double)results.Count) * 100;

        // Summary statistics
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Metric[/]")
            .AddColumn("[bold]Value[/]");

        table.AddRow("Total Tests", results.Count.ToString());
        table.AddRow("✅ Succeeded", $"[green]{successCount}[/]");
        table.AddRow("❌ Failed", $"[red]{failureCount}[/]");
        table.AddRow("Success Rate", $"{successRate:F1}%");
        table.AddRow("Avg Duration", $"{results.Average(r => r.Duration.TotalMilliseconds):F0}ms");
        table.AddRow("Avg Chunks", $"{results.Average(r => r.ChunkCount):F1}");
        table.AddRow("Avg Response Length", $"{results.Average(r => r.ResponseLength):F0} chars");
        table.AddRow("Total Cost", $"${totalCost:F4}");

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        // Detailed results
        var detailsTable = new Table()
            .Border(TableBorder.Square)
            .AddColumn("[bold]#[/]")
            .AddColumn("[bold]Status[/]")
            .AddColumn("[bold]Started[/]")
            .AddColumn("[bold]Completed[/]")
            .AddColumn("[bold]Has </tag>[/]")
            .AddColumn("[bold]Duration[/]")
            .AddColumn("[bold]Chunks[/]")
            .AddColumn("[bold]Tokens[/]")
            .AddColumn("[bold]Cost[/]");

        foreach (var result in results)
        {
            var status = result.Success ? "[green]✓[/]" : "[red]✗[/]";
            var started = result.ArtifactStarted ? "✓" : "✗";
            var completed = result.ArtifactCompleted ? "✓" : "✗";
            var hasTag = result.HasClosingTag ? "✓" : "✗";

            detailsTable.AddRow(
                result.RunNumber.ToString(),
                status,
                started,
                completed,
                hasTag,
                $"{result.Duration.TotalMilliseconds:F0}ms",
                result.ChunkCount.ToString(),
                $"{result.InputTokens + result.OutputTokens}",
                $"${result.Cost:F5}"
            );
        }

        AnsiConsole.Write(detailsTable);
        AnsiConsole.WriteLine();

        // Failure analysis
        if (failureCount > 0)
        {
            AnsiConsole.Write(new Rule("[bold red]Failure Analysis[/]").RuleStyle("red"));
            AnsiConsole.WriteLine();

            var failedTests = results.Where(r => !r.Success).ToList();

            var noStarted = failedTests.Count(r => !r.ArtifactStarted);
            var noCompleted = failedTests.Count(r => r.ArtifactStarted && !r.ArtifactCompleted);
            var noClosingTag = failedTests.Count(r => !r.HasClosingTag);
            var hasClosingTagButNoCompleted = failedTests.Count(r => r.HasClosingTag && !r.ArtifactCompleted);

            AnsiConsole.MarkupLine($"[yellow]No STARTED event:[/] {noStarted}");
            AnsiConsole.MarkupLine($"[yellow]STARTED but no COMPLETED event:[/] {noCompleted}");
            AnsiConsole.MarkupLine($"[yellow]No closing </artifact> tag:[/] {noClosingTag}");
            AnsiConsole.MarkupLine($"[red]Has closing tag but parser didn't emit COMPLETED:[/] {hasClosingTagButNoCompleted}");

            if (hasClosingTagButNoCompleted > 0)
            {
                AnsiConsole.MarkupLine("\n[bold red]⚠️  PARSER BUG DETECTED![/]");
                AnsiConsole.MarkupLine("[red]The closing tag is present but the parser isn't emitting COMPLETED events.[/]");
            }
            else if (noClosingTag == failureCount)
            {
                AnsiConsole.MarkupLine("\n[yellow]💡 All failures are due to missing closing tags (LLM behavior issue)[/]");
            }
        }
        else
        {
            AnsiConsole.MarkupLine("[bold green]🎉 All tests passed! The model is reliably closing artifact tags.[/]");
        }
    }

    public static async Task<string> SelectModelAsync(string apiKey)
    {
        var client = new OpenRouterClient(apiKey);

        List<OpenRouter.NET.Models.ModelInfo>? models = null;

        await AnsiConsole.Status()
            .StartAsync("Loading available models...", async ctx =>
            {
                ctx.Spinner(Spinner.Known.Dots);
                ctx.SpinnerStyle(Style.Parse("green"));
                models = await client.GetModelsAsync();
            });

        if (models == null || models.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]Failed to load models. Using default.[/]");
            return "anthropic/claude-3.5-sonnet";
        }

        // Popular models to show at top
        var popularModels = new[]
        {
            "anthropic/claude-3.5-sonnet",
            "anthropic/claude-haiku-4.5",
            "anthropic/claude-opus-4",
            "openai/gpt-4o",
            "openai/gpt-4o-mini",
            "google/gemini-2.0-flash-exp:free"
        };

        var modelChoices = models
            .OrderBy(m =>
            {
                var index = Array.IndexOf(popularModels, m.Id);
                return index == -1 ? int.MaxValue : index;
            })
            .ThenBy(m => m.Id)
            .Select(m => m.Id)
            .ToList();

        var model = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold blue]Select model to test:[/]")
                .PageSize(15)
                .MoreChoicesText("[grey](Move up and down to see more models)[/]")
                .AddChoices(modelChoices));

        return model;
    }

    public static int SelectIterations()
    {
        var iterations = AnsiConsole.Prompt(
            new TextPrompt<int>("[bold blue]How many iterations?[/]")
                .DefaultValue(10)
                .ValidationErrorMessage("[red]Please enter a number between 1 and 100[/]")
                .Validate(n => n is >= 1 and <= 100));

        return iterations;
    }

    public static bool SelectParallel()
    {
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold blue]Execution mode:[/]")
                .AddChoices(new[]
                {
                    "⚡ Parallel (faster, may hit rate limits)",
                    "🐌 Sequential (slower, safer)"
                }));

        return choice.StartsWith("⚡");
    }
}

public class TestResult
{
    public int RunNumber { get; set; }
    public bool Success { get; set; }
    public bool ArtifactStarted { get; set; }
    public bool ArtifactCompleted { get; set; }
    public bool HasClosingTag { get; set; }
    public int ChunkCount { get; set; }
    public int ResponseLength { get; set; }
    public string ResponseText { get; set; } = string.Empty; // Parsed response (TextDelta only)
    public string RawStreamText { get; set; } = string.Empty; // Raw stream with artifact XML
    public TimeSpan Duration { get; set; }
    public string Error { get; set; } = string.Empty;
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public double Cost { get; set; }
    public string SystemPrompt { get; set; } = string.Empty;
    public List<ArtifactCompleted> Artifacts { get; set; } = new();
}
