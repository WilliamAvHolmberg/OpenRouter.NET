using OpenRouter.NET;
using OpenRouter.NET.Models;
using Xunit.Abstractions;

namespace OpenRouter.NET.Tests.Integration;

public abstract class IntegrationTestBase
{
    protected readonly ITestOutputHelper Output;
    protected const string TestModel = "anthropic/claude-haiku-4.5";

    protected IntegrationTestBase(ITestOutputHelper output)
    {
        Output = output;
    }

    protected static string GetApiKey()
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException(
                "OPENROUTER_API_KEY environment variable is required for integration tests. " +
                "Set it with: export OPENROUTER_API_KEY=your-key-here");
        }
        return apiKey;
    }

    protected OpenRouterClient CreateClient()
    {
        var apiKey = GetApiKey();
        return new OpenRouterClient(apiKey);
    }

    protected void LogInfo(string message)
    {
        Output.WriteLine($"[INFO] {message}");
    }

    protected void LogSuccess(string message)
    {
        Output.WriteLine($"[✓] {message}");
    }

    protected void LogWarning(string message)
    {
        Output.WriteLine($"[⚠] {message}");
    }

    protected void LogError(string message)
    {
        Output.WriteLine($"[✗] {message}");
    }

    protected void LogChunk(int index, string type, string? content = null)
    {
        var msg = $"Chunk {index}: {type}";
        if (content != null)
        {
            msg += $" - {content.Substring(0, Math.Min(50, content.Length))}...";
        }
        Output.WriteLine($"  [{index}] {type}");
    }
}

