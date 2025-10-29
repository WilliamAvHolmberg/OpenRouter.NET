using System.Text.Json.Serialization;

namespace OpenRouter.NET.Sse;

public static class SseEventType
{
    public const string Text = "text";
    public const string ToolExecuting = "tool_executing";
    public const string ToolCompleted = "tool_completed";
    public const string ToolError = "tool_error";
    public const string ToolClient = "tool_client";
    public const string ArtifactStarted = "artifact_started";
    public const string ArtifactContent = "artifact_content";
    public const string ArtifactCompleted = "artifact_completed";
    public const string Completion = "completion";
    public const string Error = "error";
}

public abstract class SseEvent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = null!;
    
    [JsonPropertyName("chunkIndex")]
    public int ChunkIndex { get; set; }
    
    [JsonPropertyName("elapsedMs")]
    public double ElapsedMs { get; set; }
}

public class TextEvent : SseEvent
{
    [JsonPropertyName("textDelta")]
    public string TextDelta { get; set; } = null!;
}

public class ToolExecutingEvent : SseEvent
{
    [JsonPropertyName("toolName")]
    public string ToolName { get; set; } = null!;
    
    [JsonPropertyName("toolId")]
    public string ToolId { get; set; } = null!;
    
    [JsonPropertyName("arguments")]
    public string Arguments { get; set; } = null!;
}

public class ToolCompletedEvent : SseEvent
{
    [JsonPropertyName("toolName")]
    public string ToolName { get; set; } = null!;
    
    [JsonPropertyName("toolId")]
    public string ToolId { get; set; } = null!;
    
    [JsonPropertyName("arguments")]
    public string Arguments { get; set; } = null!;
    
    [JsonPropertyName("result")]
    public string? Result { get; set; }
    
    [JsonPropertyName("executionMs")]
    public double? ExecutionMs { get; set; }
}

public class ToolErrorEvent : SseEvent
{
    [JsonPropertyName("toolName")]
    public string ToolName { get; set; } = null!;
    
    [JsonPropertyName("toolId")]
    public string ToolId { get; set; } = null!;
    
    [JsonPropertyName("arguments")]
    public string Arguments { get; set; } = null!;
    
    [JsonPropertyName("error")]
    public string Error { get; set; } = null!;
    
    [JsonPropertyName("executionMs")]
    public double? ExecutionMs { get; set; }
}

public class ToolClientEvent : SseEvent
{
    [JsonPropertyName("toolName")]
    public string ToolName { get; set; } = null!;
    
    [JsonPropertyName("toolId")]
    public string ToolId { get; set; } = null!;
    
    [JsonPropertyName("arguments")]
    public string Arguments { get; set; } = null!;
}

public class ArtifactStartedEvent : SseEvent
{
    [JsonPropertyName("artifactId")]
    public string ArtifactId { get; set; } = null!;
    
    [JsonPropertyName("title")]
    public string Title { get; set; } = null!;
    
    [JsonPropertyName("artifactType")]
    public string ArtifactType { get; set; } = null!;
    
    [JsonPropertyName("language")]
    public string? Language { get; set; }
}

public class ArtifactContentEvent : SseEvent
{
    [JsonPropertyName("artifactId")]
    public string ArtifactId { get; set; } = null!;
    
    [JsonPropertyName("contentDelta")]
    public string ContentDelta { get; set; } = null!;
}

public class ArtifactCompletedEvent : SseEvent
{
    [JsonPropertyName("artifactId")]
    public string ArtifactId { get; set; } = null!;
    
    [JsonPropertyName("title")]
    public string Title { get; set; } = null!;
    
    [JsonPropertyName("artifactType")]
    public string ArtifactType { get; set; } = null!;
    
    [JsonPropertyName("language")]
    public string? Language { get; set; }
    
    [JsonPropertyName("content")]
    public string Content { get; set; } = null!;
}

public class CompletionEvent : SseEvent
{
    [JsonPropertyName("finishReason")]
    public string? FinishReason { get; set; }
    
    [JsonPropertyName("model")]
    public string? Model { get; set; }
    
    [JsonPropertyName("id")]
    public string? Id { get; set; }
}

public class ErrorEvent : SseEvent
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = null!;
    
    [JsonPropertyName("details")]
    public string? Details { get; set; }
}
