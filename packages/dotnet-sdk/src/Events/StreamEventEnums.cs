namespace OpenRouter.NET.Events;

public enum StreamEventType
{
    StateChange,
    TextContent,
    ToolCall,
    ToolResult,
    Error
}

public enum StreamState
{
    Loading,
    Text,
    ToolCall,
    Complete
}

