using OpenRouter.NET.Models;

namespace OpenRouter.NET;

public static class ResponseExtensions
{
    public static string? GetContent(this ChatCompletionResponse response)
    {
        var content = response.Choices?.FirstOrDefault()?.Message?.Content;
        return content?.ToString();
    }
    
    public static List<ToolCall>? GetToolCalls(this ChatCompletionResponse response)
    {
        var toolCalls = response.Choices?.FirstOrDefault()?.Message?.ToolCalls;
        return toolCalls?.ToList();
    }
    
    public static string? GetFinishReason(this ChatCompletionResponse response)
    {
        return response.Choices?.FirstOrDefault()?.FinishReason;
    }
    
    public static Message? GetMessage(this ChatCompletionResponse response)
    {
        return response.Choices?.FirstOrDefault()?.Message;
    }
    
    public static Choice? GetFirstChoice(this ChatCompletionResponse response)
    {
        return response.Choices?.FirstOrDefault();
    }
    
    public static bool HasToolCalls(this ChatCompletionResponse response)
    {
        var toolCalls = response.GetToolCalls();
        return toolCalls != null && toolCalls.Count > 0;
    }
}

