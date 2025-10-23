namespace OpenRouter.NET.Models;

public enum ToolMode
{
    AutoExecute,
    ClientSide
}

public enum ToolCallState
{
    Executing,
    Completed,
    Error
}

public record ServerToolCall
{
    public string ToolName { get; init; } = "";
    public string ToolId { get; init; } = "";
    public string Arguments { get; init; } = "";
    public ToolCallState State { get; init; }
    public string? Result { get; init; }
    public string? Error { get; init; }
    public TimeSpan? ExecutionTime { get; init; }
}

public record ClientToolCall
{
    public string ToolName { get; init; } = "";
    public string ToolId { get; init; } = "";
    public string Arguments { get; init; } = "";
}

public class ToolLoopConfig
{
    public bool Enabled { get; set; } = true;
    public int MaxIterations { get; set; } = 5;
    public TimeSpan TimeoutPerCall { get; set; } = TimeSpan.FromSeconds(30);
}

public class ToolRegistration
{
    public string Name { get; set; } = "";
    public ToolMode Mode { get; set; }
    public Func<string, object>? Handler { get; set; }
    public object? Schema { get; set; }
}

