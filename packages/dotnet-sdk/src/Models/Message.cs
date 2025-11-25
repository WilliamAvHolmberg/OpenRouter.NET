using System.Text.Json.Serialization;

namespace OpenRouter.NET.Models;

public class Message
{
    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("content")]
    public object? Content { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("tool_call_id")]
    public string? ToolCallId { get; set; }

    [JsonPropertyName("tool_calls")]
    public ToolCall[]? ToolCalls { get; set; }

    public static Message FromUser(string content, string? name = null)
    {
        return new Message { Role = "user", Content = content, Name = name };
    }

    public static Message FromUser(List<ContentPart> content, string? name = null)
    {
        return new Message { Role = "user", Content = content, Name = name };
    }

    public static Message FromSystem(string content)
    {
        return new Message { Role = "system", Content = content };
    }

    public static Message FromAssistant(string content, string? name = null)
    {
        return new Message { Role = "assistant", Content = content, Name = name };
    }

    public static Message FromTool(string content, string toolCallId)
    {
        return new Message { Role = "tool", Content = content, ToolCallId = toolCallId };
    }
}

[JsonDerivedType(typeof(TextContent), "text")]
[JsonDerivedType(typeof(ImageContent), "image_url")]
public abstract class ContentPart
{
    [JsonPropertyName("type")]
    public abstract string Type { get; }
}

public class TextContent : ContentPart
{
    [JsonPropertyName("type")]
    public override string Type => "text";

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    public TextContent(string text)
    {
        Text = text;
    }
}

public class ImageContent : ContentPart
{
    [JsonPropertyName("type")]
    public override string Type => "image_url";

    [JsonPropertyName("image_url")]
    public ImageUrl? ImageUrl { get; set; }

    public ImageContent(string url, string detail = "auto")
    {
        ImageUrl = new ImageUrl { Url = url, Detail = detail };
    }
}

public class ImageUrl
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("detail")]
    public string Detail { get; set; } = "auto";
}

