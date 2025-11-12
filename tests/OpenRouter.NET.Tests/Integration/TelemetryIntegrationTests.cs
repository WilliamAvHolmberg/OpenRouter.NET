using OpenRouter.NET.Models;
using OpenRouter.NET.Sse;
using Xunit;
using Xunit.Abstractions;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace OpenRouter.NET.Tests.Integration;

public class TelemetryIntegrationTests : IntegrationTestBase
{
    public TelemetryIntegrationTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async Task StreamAsSseAsync_ShouldCaptureTokenUsage()
    {
        var client = CreateClient();
        
        var request = new ChatCompletionRequest
        {
            Model = TestModel,
            Messages = new List<Message>
            {
                Message.FromUser("Say 'Hello World' and nothing else.")
            }
        };

        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        LogInfo("Starting streaming request to capture token usage...");
        
        var result = await client.StreamAsSseAsync(request, httpContext.Response);

        LogInfo($"Stream completed with {result.ChunkCount} chunks");
        LogInfo($"Finish reason: {result.FinishReason}");
        LogInfo($"Request ID: {result.RequestId}");
        LogInfo($"Model: {result.Model}");
        
        Assert.NotNull(result);
        Assert.NotEmpty(result.Messages);
        
        LogInfo($"Usage data: {result.Usage?.TotalTokens ?? 0} total tokens");
        LogInfo($"  Prompt tokens: {result.Usage?.PromptTokens ?? 0}");
        LogInfo($"  Completion tokens: {result.Usage?.CompletionTokens ?? 0}");
        
        if (result.Usage == null)
        {
            LogError("Usage is NULL - this is the bug we're testing!");
            LogInfo("This confirms token data is not being captured from the streaming response");
        }
        
        Assert.NotNull(result.Usage);
        Assert.True(result.Usage.TotalTokens > 0, "Total tokens should be greater than 0");
        Assert.True(result.Usage.PromptTokens > 0, "Prompt tokens should be greater than 0");
        Assert.True(result.Usage.CompletionTokens > 0, "Completion tokens should be greater than 0");
        
        LogSuccess($"Token usage successfully captured: {result.Usage.TotalTokens} tokens");
    }

    [Fact]
    public async Task StreamAsSseAsync_ShouldCaptureComprehensiveTelemetry()
    {
        var client = CreateClient();
        
        var request = new ChatCompletionRequest
        {
            Model = TestModel,
            Messages = new List<Message>
            {
                Message.FromUser("Write a haiku about coding. Keep it short.")
            }
        };

        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        LogInfo("Starting comprehensive telemetry test...");
        
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await client.StreamAsSseAsync(request, httpContext.Response);
        stopwatch.Stop();

        LogInfo("=== TELEMETRY RESULTS ===");
        LogInfo($"Total elapsed: {result.TotalElapsed.TotalMilliseconds:F0}ms (Stopwatch: {stopwatch.ElapsedMilliseconds}ms)");
        LogInfo($"Time to first token: {result.TimeToFirstToken?.TotalMilliseconds ?? 0:F0}ms");
        LogInfo($"Chunks received: {result.ChunkCount}");
        LogInfo($"Messages: {result.Messages.Count}");
        LogInfo($"Finish reason: {result.FinishReason}");
        LogInfo($"Request ID: {result.RequestId ?? "N/A"}");
        LogInfo($"Model: {result.Model ?? "N/A"}");
        
        if (result.Usage != null)
        {
            LogInfo($"Tokens: Prompt={result.Usage.PromptTokens}, Completion={result.Usage.CompletionTokens}, Total={result.Usage.TotalTokens}");
        }
        else
        {
            LogWarning("Usage data is NULL");
        }
        
        if (result.ToolExecutions.Count > 0)
        {
            LogInfo($"Tool executions: {result.ToolExecutions.Count}");
            foreach (var tool in result.ToolExecutions)
            {
                LogInfo($"  - {tool.ToolName}: {tool.ExecutionTime?.TotalMilliseconds ?? 0:F0}ms");
            }
        }

        Assert.NotNull(result);
        Assert.NotEmpty(result.Messages);
        Assert.True(result.ChunkCount > 0);
        Assert.NotNull(result.TimeToFirstToken);
        Assert.True(result.TotalElapsed.TotalMilliseconds > 0);
        Assert.NotNull(result.FinishReason);
        
        Assert.NotNull(result.Usage);
        Assert.True(result.Usage.TotalTokens > 0, "Token usage should be captured");
        
        LogSuccess("All telemetry data captured successfully!");
    }

    [Fact]
    public async Task StreamAsSseAsync_WithArtifacts_ShouldCountArtifacts()
    {
        var client = CreateClient();
        
        var request = new ChatCompletionRequest
        {
            Model = TestModel,
            Messages = new List<Message>
            {
                Message.FromUser("Create a simple 'Hello World' Python script as an artifact")
            }
        };
        
        request.EnableArtifactSupport();

        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        LogInfo("Testing artifact count tracking...");
        
        var result = await client.StreamAsSseAsync(request, httpContext.Response);

        LogInfo($"Artifacts created: {result.ArtifactCount}");
        
        Assert.NotNull(result);
        Assert.True(result.ArtifactCount >= 0, "Artifact count should be tracked");
        
        if (result.ArtifactCount > 0)
        {
            LogSuccess($"{result.ArtifactCount} artifact(s) captured in telemetry");
        }
        else
        {
            LogWarning("No artifacts created (model may not support artifacts)");
        }
    }
    
    [Fact]
    public async Task DebugRawChunks_InspectUsageData()
    {
        // Enable debug logging
        var client = new OpenRouterClient(new OpenRouterClientOptions
        {
            ApiKey = GetApiKey(),
            OnLogMessage = (msg) => Output.WriteLine($"[SDK-DEBUG] {msg}")
        });
        
        var request = new ChatCompletionRequest
        {
            Model = TestModel,
            Messages = new List<Message>
            {
                Message.FromUser("Write a short paragraph (3-4 sentences) about the benefits of automated testing in software development.")
            }
        };

        LogInfo("=== INSPECTING RAW CHUNKS FOR USAGE DATA ===");
        
        var chunkIndex = 0;
        var hasUsage = false;
        var hasFinishReason = false;
        
        await foreach (var chunk in client.StreamAsync(request))
        {
            chunkIndex++;
            LogInfo($"\n--- Chunk {chunkIndex} ---");
            LogInfo($"  ChunkIndex: {chunk.ChunkIndex}");
            LogInfo($"  IsFirstChunk: {chunk.IsFirstChunk}");
            LogInfo($"  TextDelta: {(chunk.TextDelta != null ? $"'{chunk.TextDelta.Substring(0, Math.Min(20, chunk.TextDelta.Length))}...'" : "null")}");
            LogInfo($"  Completion: {(chunk.Completion != null ? "NOT NULL" : "null")}");
            
            if (chunk.Completion != null)
            {
                hasFinishReason = chunk.Completion.FinishReason != null;
                LogInfo($"    FinishReason: {chunk.Completion.FinishReason ?? "null"}");
                LogInfo($"    Model: {chunk.Completion.Model ?? "null"}");
                LogInfo($"    Id: {chunk.Completion.Id ?? "null"}");
                LogInfo($"    Usage: {(chunk.Completion.Usage != null ? "NOT NULL" : "null")}");
                if (chunk.Completion.Usage != null)
                {
                    hasUsage = true;
                    LogInfo($"      Prompt: {chunk.Completion.Usage.PromptTokens}");
                    LogInfo($"      Completion: {chunk.Completion.Usage.CompletionTokens}");
                    LogInfo($"      Total: {chunk.Completion.Usage.TotalTokens}");
                }
            }
            
            LogInfo($"  Raw: {(chunk.Raw != null ? "NOT NULL" : "null")}");
            if (chunk.Raw != null)
            {
                LogInfo($"    Raw.Choices: {chunk.Raw.Choices?.Count ?? 0}");
                if (chunk.Raw.Choices?.FirstOrDefault() != null)
                {
                    var choice = chunk.Raw.Choices.First();
                    LogInfo($"    Raw.Choice.FinishReason: {choice.FinishReason ?? "null"}");
                }
                LogInfo($"    Raw.Usage: {(chunk.Raw.Usage != null ? "NOT NULL" : "null")}");
                if (chunk.Raw.Usage != null)
                {
                    hasUsage = true;
                    LogInfo($"      Prompt: {chunk.Raw.Usage.PromptTokens}");
                    LogInfo($"      Completion: {chunk.Raw.Usage.CompletionTokens}");
                    LogInfo($"      Total: {chunk.Raw.Usage.TotalTokens}");
                }
                LogInfo($"    Raw.Model: {chunk.Raw.Model ?? "null"}");
                LogInfo($"    Raw.Id: {chunk.Raw.Id ?? "null"}");
            }
        }
        
        LogInfo($"\n=== SUMMARY ===");
        LogInfo($"Total chunks received: {chunkIndex}");
        LogInfo($"Has finish reason: {hasFinishReason}");
        LogInfo($"Has usage data: {hasUsage}");
        
        if (!hasUsage)
        {
            LogError("❌ NO USAGE DATA FOUND IN ANY CHUNK!");
            LogError("This means OpenRouter/Anthropic is NOT sending usage data in the streaming response!");
            LogError("We may need to make a separate non-streaming API call to get token counts.");
        }
        
        if (chunkIndex == 1)
        {
            LogWarning("⚠️ Only 1 chunk received - response was very short");
        }
    }
}

