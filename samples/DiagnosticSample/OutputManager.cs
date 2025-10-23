using System.Text;
using System.Text.Json;
using DiagnosticSample.Models;
using OpenRouter.NET;
using OpenRouter.NET.Models;
using OpenRouter.NET.Streaming;

namespace DiagnosticSample.Output;

/// <summary>
/// Manages file output and logging for diagnostic tests
/// </summary>
public class OutputManager : IDisposable
{
    private readonly string _outputDir;
    private readonly StreamWriter _rawStreamWriter;
    private readonly StreamWriter _eventsWriter;
    private readonly StringBuilder _summaryBuilder;

    public string OutputDirectory => _outputDir;
    public string ArtifactsDirectory { get; }

    public OutputManager(string testDescription)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var baseDir = "./diagnostic_output";
        Directory.CreateDirectory(baseDir);
        _outputDir = Path.Combine(baseDir, timestamp);
        Directory.CreateDirectory(_outputDir);

        ArtifactsDirectory = Path.Combine(_outputDir, "artifacts");
        Directory.CreateDirectory(ArtifactsDirectory);

        var rawStreamFile = Path.Combine(_outputDir, "raw_stream.txt");
        var eventsFile = Path.Combine(_outputDir, "parsed_events.txt");

        _rawStreamWriter = new StreamWriter(rawStreamFile);
        _eventsWriter = new StreamWriter(eventsFile);
        _summaryBuilder = new StringBuilder();

        _summaryBuilder.AppendLine("=== DIAGNOSTIC SUMMARY ===");
        _summaryBuilder.AppendLine($"Test: {testDescription}");
        _summaryBuilder.AppendLine($"Timestamp: {timestamp}");
        _summaryBuilder.AppendLine();
    }

    public async Task LogRawChunkAsync(StreamChunk chunk)
    {
        await _rawStreamWriter.WriteLineAsync($"=== CHUNK {chunk.ChunkIndex} (Elapsed: {chunk.ElapsedTime.TotalMilliseconds:F0}ms) ===");
        await _rawStreamWriter.WriteLineAsync(JsonSerializer.Serialize(chunk.Raw, new JsonSerializerOptions { WriteIndented = true }));
        await _rawStreamWriter.WriteLineAsync();
        await _rawStreamWriter.FlushAsync();
    }

    public async Task LogEventAsync(string message)
    {
        await _eventsWriter.WriteLineAsync(message);
    }

    public async Task SaveSystemPromptAsync(ChatCompletionRequest request)
    {
        var systemMsg = request.Messages.FirstOrDefault(m => m.Role == "system");
        if (systemMsg != null)
        {
            var systemPromptFile = Path.Combine(_outputDir, "system_prompt.txt");
            await File.WriteAllTextAsync(systemPromptFile, systemMsg.Content?.ToString() ?? "");
        }
    }

    public async Task SaveArtifactAsync(string title, string content)
    {
        var artifactFile = Path.Combine(ArtifactsDirectory, title);
        await File.WriteAllTextAsync(artifactFile, content);
    }

    public async Task SaveSummaryAsync(DiagnosticResult result, ChatCompletionRequest request)
    {
        _summaryBuilder.AppendLine($"Model: {request.Model}");
        _summaryBuilder.AppendLine($"Prompt: {request.Messages.Last().Content}");
        _summaryBuilder.AppendLine();

        _summaryBuilder.AppendLine("=== STATISTICS ===");
        _summaryBuilder.AppendLine($"Total chunks: {result.ChunkCount}");
        _summaryBuilder.AppendLine($"Text chunks: {result.TextChunks}");
        _summaryBuilder.AppendLine($"Artifact events: {result.ArtifactEvents}");
        _summaryBuilder.AppendLine($"Server tool calls: {result.ServerToolCalls.Count(t => t.State == "Completed" || t.State == "Error")}");
        _summaryBuilder.AppendLine($"Client tool calls: {result.ClientToolCalls.Count}");
        _summaryBuilder.AppendLine($"TTFT: {result.TimeToFirstToken?.TotalMilliseconds:F0}ms");
        _summaryBuilder.AppendLine();

        if (result.ServerToolCalls.Any())
        {
            _summaryBuilder.AppendLine("=== SERVER TOOL CALLS ===");
            foreach (var call in result.ServerToolCalls.Where(t => t.State == "Completed" || t.State == "Error"))
            {
                _summaryBuilder.AppendLine($"- {call.Name} ({call.State})");
                if (call.State == "Completed")
                {
                    _summaryBuilder.AppendLine($"  Result: {call.Result}");
                }
                else
                {
                    _summaryBuilder.AppendLine($"  Error: {call.Error}");
                }
                _summaryBuilder.AppendLine($"  Duration: {call.Duration?.TotalMilliseconds:F0}ms");
            }
            _summaryBuilder.AppendLine();
        }

        if (result.ClientToolCalls.Any())
        {
            _summaryBuilder.AppendLine("=== CLIENT TOOL CALLS ===");
            foreach (var call in result.ClientToolCalls)
            {
                _summaryBuilder.AppendLine($"- {call.Name}");
                _summaryBuilder.AppendLine($"  Arguments: {call.Arguments}");
            }
            _summaryBuilder.AppendLine();
        }

        _summaryBuilder.AppendLine("=== ARTIFACTS CREATED ===");
        foreach (var artifact in result.Artifacts)
        {
            _summaryBuilder.AppendLine($"- {artifact.Title} ({artifact.Type}, {artifact.Content.Length} chars)");
        }
        _summaryBuilder.AppendLine();

        _summaryBuilder.AppendLine("=== FULL RESPONSE TEXT ===");
        _summaryBuilder.AppendLine(result.ResponseText.ToString());

        if (result.Error != null)
        {
            _summaryBuilder.AppendLine();
            _summaryBuilder.AppendLine("=== ERROR ===");
            _summaryBuilder.AppendLine($"Type: {result.Error.GetType().FullName}");
            _summaryBuilder.AppendLine($"Message: {result.Error.Message}");
            _summaryBuilder.AppendLine($"Stack trace:\n{result.Error.StackTrace}");
        }

        var summaryFile = Path.Combine(_outputDir, "summary.txt");
        await File.WriteAllTextAsync(summaryFile, _summaryBuilder.ToString());
    }

    public void Dispose()
    {
        _rawStreamWriter?.Dispose();
        _eventsWriter?.Dispose();
    }
}
