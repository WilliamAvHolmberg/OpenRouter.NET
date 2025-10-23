using DiagnosticSample;
using DiagnosticSample.Configuration;
using DiagnosticSample.Display;
using DiagnosticSample.Models;
using DiagnosticSample.Output;
using DiagnosticSample.Streaming;
using OpenRouter.NET;
using OpenRouter.NET.Models;
using Spectre.Console;

DisplayManager.ShowHeader();

// Get API key
var apiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
if (string.IsNullOrEmpty(apiKey))
{
    AnsiConsole.MarkupLine("[red]❌ Please set OPENROUTER_API_KEY environment variable[/]");
    return;
}

// Choose mode
var mode = ConfigurationManager.SelectMode();
if (mode == null)
{
    return;
}

// Handle batch reliability test separately
if (mode == "📊 Batch Reliability Test (run 10 artifact tests)")
{
    var model = await BatchReliabilityTester.SelectModelAsync(apiKey);
    var iterations = BatchReliabilityTester.SelectIterations();
    var parallel = BatchReliabilityTester.SelectParallel();

    var tester = new BatchReliabilityTester(apiKey);
    await tester.RunBatchTestAsync(model, iterations, parallel);

    DisplayManager.ShowDoneMessage();
    return;
}

// Configure request
var (request, testDescription, client) = ConfigurationManager.ConfigureRequest(mode, apiKey);

// Create output manager
using var outputManager = new OutputManager(testDescription);
DisplayManager.ShowOutputDirectory(outputManager.OutputDirectory);

// Save system prompt if present
await outputManager.SaveSystemPromptAsync(request);

// Create result and streaming handler
var result = new DiagnosticResult();
var streamingHandler = new StreamingHandler(outputManager, result);

// Stream response
var streamingClient = client ?? new OpenRouterClient(apiKey);
await streamingHandler.StreamResponseAsync(
    streamingClient,
    request,
    DisplayManager.CreateResponseDisplay);

// Save summary
await outputManager.SaveSummaryAsync(result, request);

// Display results
if (result.Error != null)
{
    DisplayManager.ShowError(result.Error);
}
else
{
    DisplayManager.ShowCompletionMessage();
    DisplayManager.ShowStatisticsTable(result);
    DisplayManager.ShowServerToolCalls(result);
    DisplayManager.ShowClientToolCalls(result);
    DisplayManager.ShowArtifacts(result);
}

// Show output files
var hasSystemPrompt = request.Messages.Any(m => m.Role == "system");
DisplayManager.ShowOutputFiles(outputManager.OutputDirectory, result, hasSystemPrompt);

DisplayManager.ShowDoneMessage();
