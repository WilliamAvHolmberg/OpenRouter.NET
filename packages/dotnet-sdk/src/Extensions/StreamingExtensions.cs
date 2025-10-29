using OpenRouter.NET.Models;
using OpenRouter.NET.Streaming;

namespace OpenRouter.NET;

public static class StreamingExtensions
{
    public static async Task StreamTextAsync(
        this OpenRouterClient client,
        ChatCompletionRequest request,
        Action<string> onText,
        CancellationToken cancellationToken = default)
    {
        await foreach (var chunk in client.StreamAsync(request, cancellationToken))
        {
            if (chunk.TextDelta != null)
            {
                onText(chunk.TextDelta);
            }
        }
    }
    
    public static async Task StreamTextAsync(
        this OpenRouterClient client,
        ChatCompletionRequest request,
        Action<string> onText,
        Action<StreamChunk>? onFirstChunk = null,
        Action<CompletionMetadata>? onComplete = null,
        CancellationToken cancellationToken = default)
    {
        await foreach (var chunk in client.StreamAsync(request, cancellationToken))
        {
            if (chunk.IsFirstChunk)
            {
                onFirstChunk?.Invoke(chunk);
            }
            
            if (chunk.TextDelta != null)
            {
                onText(chunk.TextDelta);
            }
            
            if (chunk.Completion != null)
            {
                onComplete?.Invoke(chunk.Completion);
            }
        }
    }
    
    public static async Task<List<Artifact>> CollectArtifactsAsync(
        this OpenRouterClient client,
        ChatCompletionRequest request,
        Action<string>? onText = null,
        CancellationToken cancellationToken = default)
    {
        var artifacts = new List<Artifact>();
        
        await foreach (var chunk in client.StreamAsync(request, cancellationToken))
        {
            if (chunk.TextDelta != null)
            {
                onText?.Invoke(chunk.TextDelta);
            }
            
            if (chunk.Artifact is ArtifactCompleted completed)
            {
                artifacts.Add(new Artifact
                {
                    Id = completed.ArtifactId,
                    Type = completed.Type,
                    Title = completed.Title,
                    Content = completed.Content,
                    Language = completed.Language
                });
            }
        }
        
        return artifacts;
    }
    
    public static async IAsyncEnumerable<string> StreamTextChunksAsync(
        this OpenRouterClient client,
        ChatCompletionRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var chunk in client.StreamAsync(request, cancellationToken))
        {
            if (chunk.TextDelta != null)
            {
                yield return chunk.TextDelta;
            }
        }
    }
    
    public static async Task<Message> StreamAndAccumulateAsync(
        this OpenRouterClient client,
        ChatCompletionRequest request,
        Action<Streaming.StreamChunk>? onChunk = null,
        CancellationToken cancellationToken = default)
    {
        var contentBuilder = new System.Text.StringBuilder();
        var toolCallsById = new Dictionary<string, ToolCall>();
        
        await foreach (var chunk in client.StreamAsync(request, cancellationToken))
        {
            onChunk?.Invoke(chunk);
            
            if (chunk.TextDelta != null)
            {
                contentBuilder.Append(chunk.TextDelta);
            }
            
            if (chunk.ToolCallDelta != null)
            {
                var toolCall = chunk.ToolCallDelta;
                if (!string.IsNullOrEmpty(toolCall.Id))
                {
                    if (!toolCallsById.ContainsKey(toolCall.Id))
                    {
                        toolCallsById[toolCall.Id] = toolCall;
                    }
                }
            }
        }
        
        var message = new Message
        {
            Role = "assistant",
            Content = contentBuilder.ToString()
        };
        
        if (toolCallsById.Count > 0)
        {
            message.ToolCalls = toolCallsById.Values.ToArray();
        }
        
        return message;
    }
}

