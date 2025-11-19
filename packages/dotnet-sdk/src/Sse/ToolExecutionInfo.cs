namespace OpenRouter.NET.Sse;

public class ToolExecutionInfo
{
    public string ToolName { get; init; } = string.Empty;
    public string Arguments { get; init; } = string.Empty;
    public string? Result { get; init; }
    public TimeSpan? ExecutionTime { get; init; }
    public string? ToolId { get; init; }
}
