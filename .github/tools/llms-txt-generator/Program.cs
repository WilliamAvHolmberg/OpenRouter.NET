using LlmsTxtGenerator;
using System.CommandLine;

var pathOption = new Option<string>(
    name: "--path",
    description: "Path to the codebase to analyze (e.g., /path/to/sdk or .)") 
{ 
    IsRequired = true 
};

var outputOption = new Option<string>(
    name: "--output",
    description: "Output path for llms.txt (default: llms.txt in current directory)",
    getDefaultValue: () => Path.Combine(Directory.GetCurrentDirectory(), "llms.txt"));

var modelOption = new Option<string>(
    name: "--model",
    description: "OpenRouter model to use",
    getDefaultValue: () => "anthropic/claude-haiku-4.5");

var maxIterationsOption = new Option<int>(
    name: "--max-iterations",
    description: "Maximum iterations for the agent",
    getDefaultValue: () => 30);

var apiKeyOption = new Option<string?>(
    name: "--api-key",
    description: "OpenRouter API key (or set OPENROUTER_API_KEY env var)");

var rootCommand = new RootCommand("🤖 LLMs.txt Generator - Agentic documentation generator for codebases")
{
    pathOption,
    outputOption,
    modelOption,
    maxIterationsOption,
    apiKeyOption
};

rootCommand.SetHandler(async (path, output, model, maxIterations, apiKey) =>
{
    try
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║          🤖 LLMs.txt Generator - Agentic Edition          ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        var resolvedApiKey = apiKey ?? Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
        
        if (string.IsNullOrEmpty(resolvedApiKey))
        {
            Console.WriteLine("❌ Error: OpenRouter API key not provided!");
            Console.WriteLine();
            Console.WriteLine("Please either:");
            Console.WriteLine("  1. Set OPENROUTER_API_KEY environment variable");
            Console.WriteLine("  2. Use --api-key option");
            Console.WriteLine();
            Console.WriteLine("Example:");
            Console.WriteLine("  export OPENROUTER_API_KEY='your-key-here'");
            Console.WriteLine("  dotnet run -- --path ./src");
            Environment.Exit(1);
            return;
        }

        var fullPath = Path.GetFullPath(path);
        var fullOutput = Path.GetFullPath(output);

        Console.WriteLine($"📂 Analyzing: {fullPath}");
        Console.WriteLine($"📝 Output to: {fullOutput}");
        Console.WriteLine($"🤖 Model: {model}");
        Console.WriteLine($"🔄 Max iterations: {maxIterations}");
        Console.WriteLine();

        if (!Directory.Exists(fullPath))
        {
            Console.WriteLine($"❌ Error: Directory does not exist: {fullPath}");
            Environment.Exit(1);
            return;
        }

        Console.WriteLine("🚀 Initializing agent...");
        Console.WriteLine();

        var client = new OpenRouter.NET.OpenRouterClient(resolvedApiKey);
        var agent = new Agent(
            client,
            fullPath,
            fullOutput,
            model,
            maxIterations);

        var success = await agent.RunAsync();

        Console.WriteLine();
        
        if (success)
        {
            Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                    ✅ SUCCESS!                             ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
            Console.WriteLine();
            Console.WriteLine($"📄 Generated: {fullOutput}");
            
            if (File.Exists(fullOutput))
            {
                var fileInfo = new FileInfo(fullOutput);
                Console.WriteLine($"📊 Size: {fileInfo.Length:N0} bytes");
                
                var lines = File.ReadLines(fullOutput).Count();
                Console.WriteLine($"📏 Lines: {lines:N0}");
            }
            
            Console.WriteLine();
            Console.WriteLine("🎉 Your llms.txt is ready!");
            Environment.Exit(0);
        }
        else
        {
            Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                    ⚠️  WARNING                            ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
            Console.WriteLine();
            Console.WriteLine("❌ Agent did not complete successfully.");
            Console.WriteLine("Consider increasing --max-iterations or checking for errors above.");
            Environment.Exit(1);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine();
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                      ❌ ERROR                              ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        Console.WriteLine($"Error: {ex.Message}");
        Console.WriteLine();
        Console.WriteLine("Stack trace:");
        Console.WriteLine(ex.StackTrace);
        Environment.Exit(1);
    }
}, pathOption, outputOption, modelOption, maxIterationsOption, apiKeyOption);

return await rootCommand.InvokeAsync(args);
