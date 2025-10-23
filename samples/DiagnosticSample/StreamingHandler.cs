using DiagnosticSample.Models;
using DiagnosticSample.Output;
using OpenRouter.NET;
using OpenRouter.NET.Models;
using OpenRouter.NET.Streaming;
using Spectre.Console;

namespace DiagnosticSample.Streaming;

/// <summary>
/// Handles streaming response processing and event tracking
/// </summary>
public class StreamingHandler
{
    private readonly OutputManager _outputManager;
    private readonly DiagnosticResult _result;

    private string _currentArtifact = "";
    private string _currentTool = "";

    public string CurrentArtifact => _currentArtifact;
    public string CurrentTool => _currentTool;

    public StreamingHandler(OutputManager outputManager, DiagnosticResult result)
    {
        _outputManager = outputManager;
        _result = result;
    }

    public async Task ProcessChunkAsync(StreamChunk chunk)
    {
        _result.ChunkCount++;

        // Log raw chunk
        await _outputManager.LogRawChunkAsync(chunk);

        // Track first chunk
        if (_result.TimeToFirstToken == null && _result.ChunkCount == 1)
        {
            _result.TimeToFirstToken = chunk.ElapsedTime;
            await _outputManager.LogEventAsync($"⚡ FIRST CHUNK at {chunk.ElapsedTime.TotalMilliseconds:F0}ms (IsFirstChunk={chunk.IsFirstChunk})");
        }

        // Handle text
        if (chunk.TextDelta != null)
        {
            await ProcessTextDeltaAsync(chunk);
        }

        // Handle artifacts
        if (chunk.Artifact != null)
        {
            await ProcessArtifactAsync(chunk);
        }

        // Handle server-side tool calls
        if (chunk.ServerTool != null)
        {
            await ProcessServerToolAsync(chunk);
        }

        // Handle client-side tool calls
        if (chunk.ClientTool != null)
        {
            await ProcessClientToolAsync(chunk);
        }

        // Handle completion
        if (chunk.Completion != null)
        {
            await _outputManager.LogEventAsync($"[{chunk.ChunkIndex}] COMPLETION: {chunk.Completion.FinishReason}");
        }
    }

    private async Task ProcessTextDeltaAsync(StreamChunk chunk)
    {
        _result.TextChunks++;
        _result.ResponseText.Append(chunk.TextDelta);
        await _outputManager.LogEventAsync($"[{chunk.ChunkIndex}] TEXT: {chunk.TextDelta!.Replace("\n", "\\n")}");
    }

    private async Task ProcessArtifactAsync(StreamChunk chunk)
    {
        _result.ArtifactEvents++;

        switch (chunk.Artifact)
        {
            case ArtifactStarted started:
                _currentArtifact = started.Title;
                await _outputManager.LogEventAsync($"[{chunk.ChunkIndex}] ARTIFACT_STARTED: {started.Title} (type={started.Type}, lang={started.Language})");
                break;

            case ArtifactContent content:
                await _outputManager.LogEventAsync($"[{chunk.ChunkIndex}] ARTIFACT_CONTENT: {content.ContentDelta.Length} chars");
                break;

            case ArtifactCompleted completed:
                _currentArtifact = "";
                _result.Artifacts.Add(new Artifact
                {
                    Id = completed.ArtifactId,
                    Type = completed.Type,
                    Title = completed.Title,
                    Content = completed.Content,
                    Language = completed.Language
                });

                await _outputManager.SaveArtifactAsync(completed.Title, completed.Content);
                await _outputManager.LogEventAsync($"[{chunk.ChunkIndex}] ARTIFACT_COMPLETED: {completed.Title} (size={completed.Content.Length})");
                await _outputManager.LogEventAsync($"   Content preview: {completed.Content.Substring(0, Math.Min(100, completed.Content.Length))}...");
                break;
        }
    }

    private async Task ProcessServerToolAsync(StreamChunk chunk)
    {
        var tool = chunk.ServerTool!;
        await _outputManager.LogEventAsync($"[{chunk.ChunkIndex}] SERVER_TOOL_{tool.State.ToString().ToUpper()}: {tool.ToolName}");
        await _outputManager.LogEventAsync($"   Arguments: {tool.Arguments}");

        switch (tool.State)
        {
            case ToolCallState.Completed:
                await _outputManager.LogEventAsync($"   Result: {tool.Result}");
                await _outputManager.LogEventAsync($"   Duration: {tool.ExecutionTime?.TotalMilliseconds:F0}ms");
                _result.ServerToolCalls.Add(new DiagnosticServerToolCall(tool.ToolName, "Completed", tool.Result, null, tool.ExecutionTime));
                _currentTool = "";
                break;

            case ToolCallState.Error:
                await _outputManager.LogEventAsync($"   Error: {tool.Error}");
                await _outputManager.LogEventAsync($"   Duration: {tool.ExecutionTime?.TotalMilliseconds:F0}ms");
                _result.ServerToolCalls.Add(new DiagnosticServerToolCall(tool.ToolName, "Error", null, tool.Error, tool.ExecutionTime));
                _currentTool = "";
                break;

            case ToolCallState.Executing:
                _result.ServerToolCalls.Add(new DiagnosticServerToolCall(tool.ToolName, "Executing", null, null, null));
                _currentTool = $"🔧 {tool.ToolName}";
                break;
        }
    }

    private async Task ProcessClientToolAsync(StreamChunk chunk)
    {
        var tool = chunk.ClientTool!;
        await _outputManager.LogEventAsync($"[{chunk.ChunkIndex}] CLIENT_TOOL: {tool.ToolName}");
        await _outputManager.LogEventAsync($"   Arguments: {tool.Arguments}");
        _result.ClientToolCalls.Add(new DiagnosticClientToolCall(tool.ToolName, tool.Arguments));
        _currentTool = $"📞 {tool.ToolName} (client)";
    }

    public async Task<DiagnosticResult> StreamResponseAsync(
        OpenRouterClient client,
        ChatCompletionRequest request,
        Func<string, TimeSpan, bool, int, string, string, Table> createDisplay)
    {
        try
        {
            AnsiConsole.MarkupLine("🔄 Streaming response...\n");

            var currentElapsed = TimeSpan.Zero;
            var streamingStarted = false;

            await AnsiConsole.Live(createDisplay("", currentElapsed, streamingStarted, 0, "", ""))
                .StartAsync(async ctx =>
                {
                    await foreach (var chunk in client.StreamAsync(request))
                    {
                        currentElapsed = chunk.ElapsedTime;

                        if (_result.ChunkCount == 0)
                        {
                            streamingStarted = true;
                        }

                        await ProcessChunkAsync(chunk);

                        // Update display
                        ctx.UpdateTarget(createDisplay(
                            _result.ResponseText.ToString(),
                            currentElapsed,
                            streamingStarted,
                            _result.Artifacts.Count,
                            _currentArtifact,
                            _currentTool));
                    }
                });
        }
        catch (Exception ex)
        {
            _result.Error = ex;
        }

        return _result;
    }
}
