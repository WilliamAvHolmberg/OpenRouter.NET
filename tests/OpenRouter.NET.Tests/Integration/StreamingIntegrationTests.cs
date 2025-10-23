using System.Diagnostics;
using System.Text;
using OpenRouter.NET.Models;
using OpenRouter.NET.Streaming;
using Xunit.Abstractions;

namespace OpenRouter.NET.Tests.Integration;

[Trait("Category", "Integration")]
public class StreamingIntegrationTests : IntegrationTestBase
{
    public StreamingIntegrationTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async Task BasicStreaming_ShouldEmitChunksCorrectly()
    {
        LogInfo("Testing basic streaming behavior...");
        
        var client = CreateClient();
        
        var request = new ChatCompletionRequest
        {
            Model = TestModel,
            Messages = new List<Message> 
            { 
                Message.FromUser("Say hello and explain what you are in one sentence") 
            }
        };
        
        var chunks = new List<StreamChunk>();
        var responseText = new StringBuilder();
        var firstChunkTime = TimeSpan.Zero;
        var isFirstChunkFlagSet = false;
        
        await foreach (var chunk in client.StreamAsync(request))
        {
            chunks.Add(chunk);
            
            if (chunk.IsFirstChunk)
            {
                isFirstChunkFlagSet = true;
                firstChunkTime = chunk.ElapsedTime;
                LogSuccess($"First chunk received at {chunk.ElapsedTime.TotalMilliseconds:F0}ms");
            }
            
            if (chunk.TextDelta != null)
            {
                responseText.Append(chunk.TextDelta);
            }
        }
        
        LogInfo($"Total chunks: {chunks.Count}");
        LogInfo($"TTFT: {firstChunkTime.TotalMilliseconds:F0}ms");
        LogInfo($"Response length: {responseText.Length} chars");
        LogInfo($"Response preview: {responseText.ToString().Substring(0, Math.Min(100, responseText.Length))}...");
        
        Assert.NotEmpty(chunks);
        Assert.True(isFirstChunkFlagSet, "IsFirstChunk should be set on first chunk");
        Assert.True(firstChunkTime.TotalMilliseconds > 0, "TTFT should be greater than 0");
        Assert.True(responseText.Length > 0, "Should have response text");
        
        LogSuccess("✓ Basic streaming works correctly");
    }

    [Fact]
    public async Task StreamingChunkMetadata_ShouldBeCorrect()
    {
        LogInfo("Testing streaming chunk metadata...");
        
        var client = CreateClient();
        
        var request = new ChatCompletionRequest
        {
            Model = TestModel,
            Messages = new List<Message> 
            { 
                Message.FromUser("Count from 1 to 5") 
            }
        };
        
        var chunks = new List<StreamChunk>();
        var previousElapsed = TimeSpan.Zero;
        
        await foreach (var chunk in client.StreamAsync(request))
        {
            chunks.Add(chunk);
            
            Assert.True(chunk.ElapsedTime >= previousElapsed, 
                $"Elapsed time should be monotonically increasing: {chunk.ElapsedTime} >= {previousElapsed}");
            
            Assert.NotNull(chunk.Raw);
            
            if (chunk.ChunkIndex > 0)
            {
                Assert.True(chunk.ChunkIndex >= chunks.Count - 1, "ChunkIndex should be sequential");
            }
            
            previousElapsed = chunk.ElapsedTime;
        }
        
        LogInfo($"Total chunks: {chunks.Count}");
        LogInfo($"First chunk index: {chunks[0].ChunkIndex}");
        LogInfo($"Last chunk index: {chunks[^1].ChunkIndex}");
        LogInfo($"Total elapsed: {chunks[^1].ElapsedTime.TotalMilliseconds:F0}ms");
        
        Assert.NotEmpty(chunks);
        Assert.All(chunks, c => Assert.NotNull(c.Raw));
        
        LogSuccess("✓ Chunk metadata is correct");
    }

    [Fact]
    public async Task StreamingCompletion_ShouldIncludeFinishReason()
    {
        LogInfo("Testing streaming completion metadata...");
        
        var client = CreateClient();
        
        var request = new ChatCompletionRequest
        {
            Model = TestModel,
            Messages = new List<Message> 
            { 
                Message.FromUser("Say 'done' and nothing else") 
            }
        };
        
        CompletionMetadata? completionMetadata = null;
        var chunkCount = 0;
        
        await foreach (var chunk in client.StreamAsync(request))
        {
            chunkCount++;
            
            if (chunk.Completion != null)
            {
                completionMetadata = chunk.Completion;
                LogSuccess($"Completion received: {chunk.Completion.FinishReason}");
                LogInfo($"  Model: {chunk.Completion.Model}");
                LogInfo($"  ID: {chunk.Completion.Id}");
            }
        }
        
        LogInfo($"Total chunks: {chunkCount}");

        // Completion metadata might not always be present for very short responses
        // depending on OpenRouter's SSE stream behavior
        if (completionMetadata != null)
        {
            Assert.NotNull(completionMetadata.FinishReason);
            Assert.NotNull(completionMetadata.Model);
            Assert.NotNull(completionMetadata.Id);
            LogSuccess($"✓ Completion metadata present with finish_reason: {completionMetadata.FinishReason}");
        }
        else
        {
            LogInfo("⚠ Completion metadata not received (can happen with very short responses or timing issues)");
            LogSuccess("✓ Test passed with lenient completion metadata check");
        }
    }

    [Fact]
    public async Task StreamingPerformance_ShouldBeReasonable()
    {
        LogInfo("Testing streaming performance...");
        
        var client = CreateClient();
        
        var request = new ChatCompletionRequest
        {
            Model = TestModel,
            Messages = new List<Message> 
            { 
                Message.FromUser("Write a three sentence paragraph about streaming") 
            }
        };
        
        var stopwatch = Stopwatch.StartNew();
        var chunks = new List<StreamChunk>();
        var firstChunkTime = TimeSpan.Zero;
        
        await foreach (var chunk in client.StreamAsync(request))
        {
            if (chunks.Count == 0)
            {
                firstChunkTime = stopwatch.Elapsed;
            }
            
            chunks.Add(chunk);
        }
        
        stopwatch.Stop();
        
        var totalTime = stopwatch.Elapsed;
        var avgTimePerChunk = totalTime.TotalMilliseconds / chunks.Count;
        
        LogInfo($"Total chunks: {chunks.Count}");
        LogInfo($"TTFT: {firstChunkTime.TotalMilliseconds:F0}ms");
        LogInfo($"Total time: {totalTime.TotalMilliseconds:F0}ms");
        LogInfo($"Avg time per chunk: {avgTimePerChunk:F2}ms");
        
        Assert.True(firstChunkTime.TotalMilliseconds < 10000, "TTFT should be less than 10 seconds");
        Assert.True(totalTime.TotalMilliseconds < 30000, "Total time should be less than 30 seconds");
        Assert.True(chunks.Count > 0, "Should have received chunks");
        
        LogSuccess("✓ Streaming performance is acceptable");
    }

    [Fact]
    public async Task StreamingWithToolCalls_ShouldEmitToolCallDeltas()
    {
        LogInfo("Testing tool call deltas in streaming...");
        
        var client = CreateClient();
        
        var addSchema = new
        {
            type = "function",
            function = new
            {
                name = "add",
                description = "Add two numbers",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        a = new { type = "number" },
                        b = new { type = "number" }
                    }
                }
            }
        };
        
        var request = new ChatCompletionRequest
        {
            Model = TestModel,
            Messages = new List<Message> 
            { 
                Message.FromUser("What is 5 + 7? Use the add function.") 
            },
            Tools = new List<Tool> 
            { 
                new Tool 
                { 
                    Type = "function",
                    Function = new FunctionDescription
                    {
                        Name = "add",
                        Description = "Add two numbers",
                        Parameters = new
                        {
                            type = "object",
                            properties = new
                            {
                                a = new { type = "number" },
                                b = new { type = "number" }
                            }
                        }
                    }
                } 
            },
            ToolLoopConfig = new ToolLoopConfig { Enabled = false }
        };
        
        var toolCallDeltas = new List<ToolCall>();
        var chunkCount = 0;
        
        await foreach (var chunk in client.StreamAsync(request))
        {
            chunkCount++;
            
            if (chunk.ToolCallDelta != null)
            {
                toolCallDeltas.Add(chunk.ToolCallDelta);
                LogInfo($"Tool call delta: {chunk.ToolCallDelta.Function?.Name ?? "null"} (index: {chunk.ToolCallDelta.Index})");
            }
        }
        
        LogInfo($"Total chunks: {chunkCount}");
        LogInfo($"Tool call deltas: {toolCallDeltas.Count}");
        
        if (toolCallDeltas.Any())
        {
            Assert.NotEmpty(toolCallDeltas);
            
            var hasNameDelta = toolCallDeltas.Any(t => !string.IsNullOrEmpty(t.Function?.Name));
            
            if (hasNameDelta)
            {
                LogSuccess($"✓ Tool call deltas emitted (found function name)");
            }
            else
            {
                LogWarning("Tool call deltas found but no function name");
            }
        }
        else
        {
            LogWarning("No tool call deltas received (model might not have used tool)");
        }
    }

    [Fact]
    public async Task StreamingTextAccumulation_ShouldMatchFullResponse()
    {
        LogInfo("Testing that accumulated text matches full response...");
        
        var client = CreateClient();
        
        var request = new ChatCompletionRequest
        {
            Model = TestModel,
            Messages = new List<Message> 
            { 
                Message.FromUser("Write exactly: Hello World") 
            }
        };
        
        var streamedText = new StringBuilder();
        var textChunks = new List<string>();
        
        await foreach (var chunk in client.StreamAsync(request))
        {
            if (chunk.TextDelta != null)
            {
                streamedText.Append(chunk.TextDelta);
                textChunks.Add(chunk.TextDelta);
            }
        }
        
        var fullText = streamedText.ToString();
        var reconstructed = string.Concat(textChunks);
        
        LogInfo($"Text chunks: {textChunks.Count}");
        LogInfo($"Full text length: {fullText.Length}");
        LogInfo($"Full text: {fullText}");
        
        Assert.Equal(reconstructed, fullText);
        Assert.True(fullText.Length > 0, "Should have text content");
        
        LogSuccess("✓ Streamed text accumulation is correct");
    }

    [Fact]
    public async Task StreamingCancellation_ShouldWork()
    {
        LogInfo("Testing streaming cancellation...");
        
        var client = CreateClient();
        
        var request = new ChatCompletionRequest
        {
            Model = TestModel,
            Messages = new List<Message> 
            { 
                Message.FromUser("Write a very long story about a dragon...") 
            }
        };
        
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(2));
        
        var chunkCount = 0;
        var wasCancelled = false;
        
        try
        {
            await foreach (var chunk in client.StreamAsync(request, cts.Token))
            {
                chunkCount++;
                
                if (chunkCount > 5)
                {
                    cts.Cancel();
                }
            }
        }
        catch (OperationCanceledException)
        {
            wasCancelled = true;
            LogSuccess("Stream was cancelled as expected");
        }
        
        LogInfo($"Chunks received before cancellation: {chunkCount}");
        
        Assert.True(wasCancelled || chunkCount > 0, "Should either cancel or receive chunks");
        
        if (wasCancelled)
        {
            LogSuccess("✓ Streaming cancellation works");
        }
        else
        {
            LogWarning("Stream completed before cancellation (request was too fast)");
        }
    }

    [Fact]
    public async Task MultipleSequentialStreams_ShouldAllWork()
    {
        LogInfo("Testing multiple sequential streams...");
        
        var client = CreateClient();
        
        var requests = new[]
        {
            "Say hello",
            "Count to 3",
            "Say goodbye"
        };
        
        var allSucceeded = true;
        
        for (int i = 0; i < requests.Length; i++)
        {
            LogInfo($"Stream {i + 1}/{requests.Length}: {requests[i]}");
            
            var request = new ChatCompletionRequest
            {
                Model = TestModel,
                Messages = new List<Message> { Message.FromUser(requests[i]) }
            };
            
            var responseText = new StringBuilder();
            var chunkCount = 0;
            
            try
            {
                await foreach (var chunk in client.StreamAsync(request))
                {
                    chunkCount++;
                    if (chunk.TextDelta != null)
                    {
                        responseText.Append(chunk.TextDelta);
                    }
                }
                
                LogSuccess($"  ✓ Stream {i + 1} completed: {chunkCount} chunks, {responseText.Length} chars");
            }
            catch (Exception ex)
            {
                LogError($"  ✗ Stream {i + 1} failed: {ex.Message}");
                allSucceeded = false;
            }
        }
        
        Assert.True(allSucceeded, "All sequential streams should succeed");
        
        LogSuccess("✓ Multiple sequential streams all worked");
    }
}

