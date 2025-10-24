using Microsoft.AspNetCore.Http;
using OpenRouter.NET.Models;
using System.Text.Json;

namespace OpenRouter.NET.Sse;

public static class SseStreamingExtensions
{
    public static async Task StreamAsSseAsync(
        this OpenRouterClient client,
        ChatCompletionRequest request,
        HttpResponse response,
        CancellationToken cancellationToken = default)
    {
        await StreamAsSseAsync(client, request, response, null, cancellationToken);
    }

    public static async Task StreamAsSseAsync(
        this OpenRouterClient client,
        ChatCompletionRequest request,
        HttpResponse response,
        JsonSerializerOptions? jsonOptions,
        CancellationToken cancellationToken = default)
    {
        SseWriter.SetupSseHeaders(response);

        var writer = new SseWriter(response.Body, jsonOptions);
        var completionSent = false;
        var lastChunkIndex = 0;

        try
        {
            await foreach (var chunk in client.StreamAsync(request, cancellationToken))
            {
                var events = SseChunkMapper.MapChunk(chunk);
                lastChunkIndex = chunk.ChunkIndex;

                foreach (var sseEvent in events)
                {
                    await writer.WriteEventAsync(sseEvent, cancellationToken);

                    if (sseEvent is CompletionEvent)
                    {
                        completionSent = true;
                    }
                }
            }

            // Ensure a completion event is always sent
            if (!completionSent)
            {
                var completionEvent = new CompletionEvent
                {
                    Type = SseEventType.Completion,
                    ChunkIndex = lastChunkIndex + 1,
                    ElapsedMs = 0,
                    FinishReason = "stop"
                };

                await writer.WriteEventAsync(completionEvent, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            var errorEvent = new ErrorEvent
            {
                Type = SseEventType.Error,
                ChunkIndex = -1,
                ElapsedMs = 0,
                Message = ex.Message,
                Details = ex.ToString()
            };

            await writer.WriteEventAsync(errorEvent, cancellationToken);
        }
    }

    public static async IAsyncEnumerable<SseEvent> StreamAsSseEventsAsync(
        this OpenRouterClient client,
        ChatCompletionRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var chunk in client.StreamAsync(request, cancellationToken))
        {
            var events = SseChunkMapper.MapChunk(chunk);
            
            foreach (var sseEvent in events)
            {
                yield return sseEvent;
            }
        }
    }
}
