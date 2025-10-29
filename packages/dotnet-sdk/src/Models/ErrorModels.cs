using System.Text.Json.Serialization;

namespace OpenRouter.NET.Models;

public class ErrorResponse
{
    [JsonPropertyName("error")]
    public ErrorInfo? Error { get; set; }
}

public class ErrorInfo
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }
}

