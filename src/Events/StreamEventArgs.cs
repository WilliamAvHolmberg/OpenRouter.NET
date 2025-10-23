using OpenRouter.NET.Models;

namespace OpenRouter.NET.Events;

public class StreamEventArgs : EventArgs
{
    public StreamEventType EventType { get; set; }
    public StreamState State { get; set; }
    public string? ToolName { get; set; }
    public string? TextDelta { get; set; }
    public ToolCall? ToolCall { get; set; }
    public string? ToolResult { get; set; }
    public object? OriginalResponse { get; set; }
}

