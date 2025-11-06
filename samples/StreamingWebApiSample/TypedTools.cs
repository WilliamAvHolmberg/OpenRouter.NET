using OpenRouter.NET.Models;
using OpenRouter.NET.Tools;

namespace StreamingWebApiSample;

// Example parameters and tools using the new typed pattern

public class CalculateParams
{
    public int A { get; set; }
    public int B { get; set; }
    public string Operation { get; set; } = "add";
}

public class CalculateTool : Tool<CalculateParams, int>
{
    public override string Name => "calculate";
    public override string Description => "Perform a mathematical operation on two numbers";

    protected override int Handle(CalculateParams p)
    {
        return p.Operation.ToLower() switch
        {
            "add" => p.A + p.B,
            "subtract" => p.A - p.B,
            "multiply" => p.A * p.B,
            "divide" => p.B != 0 ? p.A / p.B : throw new ArgumentException("Cannot divide by zero"),
            _ => throw new ArgumentException($"Unknown operation: {p.Operation}")
        };
    }
}

public class SearchParams
{
    public string Query { get; set; } = string.Empty;
    public int MaxResults { get; set; } = 10;
}

public class SearchResult
{
    public List<string> Items { get; set; } = new();
    public int TotalCount { get; set; }
}

public class SearchTool : Tool<SearchParams, SearchResult>
{
    public override string Name => "search";
    public override string Description => "Search for items matching a query";

    protected override SearchResult Handle(SearchParams p)
    {
        // Mock search implementation
        var items = new List<string>();
        for (int i = 0; i < Math.Min(p.MaxResults, 5); i++)
        {
            items.Add($"Result {i + 1} for '{p.Query}'");
        }

        return new SearchResult
        {
            Items = items,
            TotalCount = items.Count
        };
    }
}

public class NotifyParams
{
    public string Message { get; set; } = string.Empty;
    public string Level { get; set; } = "info";
}

public class NotifyTool : VoidTool<NotifyParams>
{
    public override string Name => "notify";
    public override string Description => "Send a notification message";
    public override ToolMode Mode => ToolMode.ClientSide;

    protected override void HandleVoid(NotifyParams p)
    {
        // Just log for demo - in real app this would emit an event
        Console.WriteLine($"[{p.Level.ToUpper()}] {p.Message}");
    }
}
