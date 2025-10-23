using System.Text.Json.Serialization;

namespace OpenRouter.NET.Models;

public class ChatCompletionRequest
{
    [JsonPropertyName("messages")]
    public List<Message> Messages { get; set; } = new List<Message>();

    [JsonPropertyName("prompt")]
    public string? Prompt { get; set; }

    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("response_format")]
    public ResponseFormat? ResponseFormat { get; set; }

    [JsonPropertyName("stop")]
    public object? Stop { get; set; }

    [JsonPropertyName("stream")]
    public bool? Stream { get; set; }

    [JsonPropertyName("max_tokens")]
    public int? MaxTokens { get; set; }

    [JsonPropertyName("temperature")]
    public float? Temperature { get; set; }

    [JsonPropertyName("tools")]
    public List<Tool>? Tools { get; set; }

    [JsonPropertyName("tool_choice")]
    public object? ToolChoice { get; set; }

    [JsonPropertyName("seed")]
    public int? Seed { get; set; }

    [JsonPropertyName("top_p")]
    public float? TopP { get; set; }

    [JsonPropertyName("top_k")]
    public int? TopK { get; set; }

    [JsonPropertyName("frequency_penalty")]
    public float? FrequencyPenalty { get; set; }

    [JsonPropertyName("presence_penalty")]
    public float? PresencePenalty { get; set; }

    [JsonPropertyName("repetition_penalty")]
    public float? RepetitionPenalty { get; set; }

    [JsonPropertyName("logit_bias")]
    public Dictionary<int, float>? LogitBias { get; set; }

    [JsonPropertyName("top_logprobs")]
    public int? TopLogprobs { get; set; }

    [JsonPropertyName("min_p")]
    public float? MinP { get; set; }

    [JsonPropertyName("top_a")]
    public float? TopA { get; set; }

    [JsonPropertyName("prediction")]
    public Prediction? Prediction { get; set; }

    [JsonPropertyName("transforms")]
    public List<string>? Transforms { get; set; }

    [JsonPropertyName("models")]
    public List<string>? Models { get; set; }

    [JsonPropertyName("route")]
    public string? Route { get; set; }

    [JsonPropertyName("provider")]
    public object? Provider { get; set; }
    
    [JsonIgnore]
    public ToolLoopConfig? ToolLoopConfig { get; set; }
}

public class ResponseFormat
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "json_object";
}

public class Prediction
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "content";

    [JsonPropertyName("content")]
    public string? Content { get; set; }
}

