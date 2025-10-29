using System.Text;
using OpenRouter.NET.Models;

namespace OpenRouter.NET;

public static class ConversationExtensions
{
    public static void AddUserMessage(this List<Message> history, string content)
    {
        history.Add(Message.FromUser(content));
    }
    
    public static void AddAssistantMessage(this List<Message> history, string content)
    {
        history.Add(Message.FromAssistant(content));
    }
    
    public static void ClearHistory(this List<Message> history, bool keepSystemPrompt = true)
    {
        if (keepSystemPrompt)
        {
            var systemMessage = history.FirstOrDefault(m => m.Role == "system");
            history.Clear();
            if (systemMessage != null)
            {
                history.Add(systemMessage);
            }
        }
        else
        {
            history.Clear();
        }
    }
    
    public static string GetConversationSummary(this List<Message> history)
    {
        var summary = new StringBuilder();
        summary.AppendLine("=== CONVERSATION HISTORY ===");
        
        for (int i = 0; i < history.Count; i++)
        {
            var msg = history[i];
            var preview = msg.Content?.ToString() ?? "";
            if (preview.Length > 100)
            {
                preview = preview.Substring(0, 100) + "...";
            }
            
            summary.AppendLine($"[{i + 1}] {msg.Role}: {preview}");
        }
        
        return summary.ToString();
    }
    
    public static int EstimateTokenCount(this List<Message> history)
    {
        var totalChars = 0;
        foreach (var msg in history)
        {
            var content = msg.Content?.ToString() ?? "";
            totalChars += content.Length;
        }
        
        return (int)(totalChars / 4.0);
    }
    
    public static decimal EstimateCost(this List<Message> history, decimal inputPricePerMillion, decimal outputPricePerMillion, int estimatedOutputTokens = 0)
    {
        var inputTokens = history.EstimateTokenCount();
        var inputCost = (inputTokens / 1_000_000.0m) * inputPricePerMillion;
        var outputCost = (estimatedOutputTokens / 1_000_000.0m) * outputPricePerMillion;
        
        return inputCost + outputCost;
    }
    
    public static int GetMessageCount(this List<Message> history, string? role = null)
    {
        if (string.IsNullOrEmpty(role))
        {
            return history.Count;
        }
        
        return history.Count(m => m.Role?.Equals(role, StringComparison.OrdinalIgnoreCase) == true);
    }
}

