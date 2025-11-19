using OpenRouter.NET.Models;

namespace OpenRouter.NET.Sse;

public class StreamingResult
{
    public List<Message> Messages { get; init; } = new();
    public ResponseUsage? Usage { get; init; }
    public string? FinishReason { get; init; }
    public TimeSpan? TimeToFirstToken { get; init; }
    public TimeSpan TotalElapsed { get; init; }
    public int ChunkCount { get; init; }
    public int ArtifactCount { get; init; }
    public List<ToolExecutionInfo> ToolExecutions { get; init; } = new();
    public string? RequestId { get; init; }
    public string? Model { get; init; }
}
