using OpenRouter.NET.Models;

namespace OpenRouter.NET.Streaming;

public record StreamChunk
{
    public bool IsFirstChunk { get; init; }
    public TimeSpan ElapsedTime { get; init; }
    public int ChunkIndex { get; init; }
    
    public string? TextDelta { get; init; }
    
    public ToolCall? ToolCallDelta { get; init; }
    
    public ArtifactEvent? Artifact { get; init; }
    
    public ServerToolCall? ServerTool { get; init; }
    
    public ClientToolCall? ClientTool { get; init; }
    
    public CompletionMetadata? Completion { get; init; }
    
    public ChatCompletionStreamResponse Raw { get; init; } = null!;
}

public record CompletionMetadata
{
    public string? FinishReason { get; init; }
    public ResponseUsage? Usage { get; init; }
    public string? Model { get; init; }
    public string? Id { get; init; }
}

