using System.Text;

namespace DiagnosticSample.Models;

/// <summary>
/// Contains the results and statistics from a diagnostic test run
/// </summary>
public class DiagnosticResult
{
    public int ChunkCount { get; set; }
    public int TextChunks { get; set; }
    public int ArtifactEvents { get; set; }
    public List<OpenRouter.NET.Models.Artifact> Artifacts { get; } = new();
    public StringBuilder ResponseText { get; } = new();
    public List<DiagnosticServerToolCall> ServerToolCalls { get; } = new();
    public List<DiagnosticClientToolCall> ClientToolCalls { get; } = new();
    public TimeSpan? TimeToFirstToken { get; set; }
    public Exception? Error { get; set; }
}

/// <summary>
/// Represents a server-side tool call execution result
/// </summary>
public record DiagnosticServerToolCall(
    string Name,
    string State,
    string? Result,
    string? Error,
    TimeSpan? Duration);

/// <summary>
/// Represents a client-side tool call result
/// </summary>
public record DiagnosticClientToolCall(
    string Name,
    string Arguments);
