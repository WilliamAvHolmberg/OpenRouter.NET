using System.Text.Json.Serialization;

namespace OpenRouter.NET.Models;

public class Tool
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";

    [JsonPropertyName("function")]
    public FunctionDescription? Function { get; set; }

    public static Tool CreateFunctionTool(string name, string description, object parameters)
    {
        return new Tool
        {
            Type = "function",
            Function = new FunctionDescription
            {
                Name = name,
                Description = description,
                Parameters = parameters
            }
        };
    }
}

public class FunctionDescription
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("parameters")]
    public object? Parameters { get; set; }
}

public class ToolCall
{
    [JsonPropertyName("index")]
    public int Index { get; set; }
    
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";

    [JsonPropertyName("function")]
    public FunctionCall? Function { get; set; }
}

public class FunctionCall
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("arguments")]
    public string? Arguments { get; set; }
}

