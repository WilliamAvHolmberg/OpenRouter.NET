using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenRouter.NET.Models;

public class GenerateObjectRequest
{
    [JsonPropertyName("schema")]
    public JsonElement Schema { get; set; }
    
    [JsonPropertyName("prompt")]
    public string Prompt { get; set; } = string.Empty;
    
    [JsonPropertyName("model")]
    public string? Model { get; set; }
    
    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }
}

public class GenerateObjectResult
{
    [JsonPropertyName("object")]
    public JsonElement Object { get; set; }

    [JsonPropertyName("usage")]
    public ResponseUsage? Usage { get; set; }

    [JsonPropertyName("finishReason")]
    public string? FinishReason { get; set; }
}

public class GenerateObjectResult<T> where T : class
{
    public T Object { get; set; } = null!;

    public ResponseUsage? Usage { get; set; }

    public string? FinishReason { get; set; }
}

public class GenerateObjectOptions
{
    public int? MaxTokens { get; set; }
    public double? Temperature { get; set; }
    public int MaxRetries { get; set; } = 3;
    public int SchemaWarningThresholdBytes { get; set; } = 2048;
}
