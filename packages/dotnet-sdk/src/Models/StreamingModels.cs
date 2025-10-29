using System.Text.Json.Serialization;

namespace OpenRouter.NET.Models;

public class ChatCompletionStreamResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("choices")]
    public List<StreamingChoice>? Choices { get; set; }

    [JsonPropertyName("created")]
    public long Created { get; set; }

    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("object")]
    public string? Object { get; set; }

    [JsonPropertyName("system_fingerprint")]
    public string? SystemFingerprint { get; set; }

    [JsonPropertyName("usage")]
    public ResponseUsage? Usage { get; set; }
}

public class StreamingChoice
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("delta")]
    public MessageDelta? Delta { get; set; }

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }
}

public class MessageDelta
{
    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("tool_calls")]
    public ToolCall[]? ToolCalls { get; set; }
}

