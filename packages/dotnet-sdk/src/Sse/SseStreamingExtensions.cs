using Microsoft.AspNetCore.Http;
using OpenRouter.NET.Models;
using System.Text;
using System.Text.Json;

namespace OpenRouter.NET.Sse;

public static class SseStreamingExtensions
{
    public static async Task<List<Message>> StreamAsSseAsync(
        this OpenRouterClient client,
        ChatCompletionRequest request,
        HttpResponse response,
        CancellationToken cancellationToken = default)
    {
        return await StreamAsSseAsync(client, request, response, null, cancellationToken);
    }

    public static async Task<List<Message>> StreamAsSseAsync(
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
        var lastElapsedMs = 0.0;

        // Message accumulation
        var messages = new List<Message>();
        var contentBuilder = new StringBuilder();
        var toolCallsAccumulator = new Dictionary<int, ToolCall>();
        var hasContent = false;
        var currentMessageAdded = false;
        var pendingClientToolResponses = new List<Message>();

        try
        {
            await foreach (var chunk in client.StreamAsync(request, cancellationToken))
            {
                var events = SseChunkMapper.MapChunk(chunk);
                lastChunkIndex = chunk.ChunkIndex;
                lastElapsedMs = chunk.ElapsedTime.TotalMilliseconds;

                // Accumulate text content
                if (chunk.TextDelta != null)
                {
                    contentBuilder.Append(chunk.TextDelta);
                    hasContent = true;

                    // If we're seeing text after tools were processed, reset for new assistant message
                    if (currentMessageAdded)
                    {
                        contentBuilder.Clear();
                        contentBuilder.Append(chunk.TextDelta);
                        toolCallsAccumulator.Clear();
                        currentMessageAdded = false;
                        pendingClientToolResponses.Clear();
                    }
                }

                // Accumulate tool call deltas
                if (chunk.ToolCallDelta != null)
                {
                    var toolCallDelta = chunk.ToolCallDelta;
                    if (!toolCallsAccumulator.ContainsKey(toolCallDelta.Index))
                    {
                        toolCallsAccumulator[toolCallDelta.Index] = new ToolCall
                        {
                            Index = toolCallDelta.Index,
                            Id = toolCallDelta.Id ?? "",
                            Type = toolCallDelta.Type ?? "function",
                            Function = new FunctionCall
                            {
                                Name = toolCallDelta.Function?.Name ?? "",
                                Arguments = toolCallDelta.Function?.Arguments ?? ""
                            }
                        };
                    }
                    else
                    {
                        var existing = toolCallsAccumulator[toolCallDelta.Index];
                        if (toolCallDelta.Id != null)
                        {
                            existing.Id = toolCallDelta.Id;
                        }
                        if (toolCallDelta.Function?.Name != null)
                        {
                            existing.Function!.Name = toolCallDelta.Function.Name;
                        }
                        if (toolCallDelta.Function?.Arguments != null)
                        {
                            existing.Function!.Arguments += toolCallDelta.Function.Arguments;
                        }
                    }
                    hasContent = true;
                }

                // When we see a ServerTool or ClientTool chunk, finalize the current assistant message (if not already done)
                if ((chunk.ServerTool != null || chunk.ClientTool != null) && hasContent && !currentMessageAdded)
                {
                    var assistantMessage = new Message
                    {
                        Role = "assistant",
                        Content = contentBuilder.ToString()
                    };

                    if (toolCallsAccumulator.Count > 0)
                    {
                        assistantMessage.ToolCalls = toolCallsAccumulator.Values.ToArray();
                    }

                    messages.Add(assistantMessage);
                    currentMessageAdded = true;

                    // Add any pending client tool responses now that assistant message is added
                    messages.AddRange(pendingClientToolResponses);
                    pendingClientToolResponses.Clear();
                }

                // Add tool result messages for server-side tools
                if (chunk.ServerTool?.State == ToolCallState.Completed)
                {
                    messages.Add(new Message
                    {
                        Role = "tool",
                        ToolCallId = chunk.ServerTool.ToolId,
                        Content = chunk.ServerTool.Result ?? ""
                    });
                }
                else if (chunk.ServerTool?.State == ToolCallState.Error)
                {
                    messages.Add(new Message
                    {
                        Role = "tool",
                        ToolCallId = chunk.ServerTool.ToolId,
                        Content = chunk.ServerTool.Error ?? "Unknown error"
                    });
                }

                // Queue tool result messages for client-side tools (will be added after assistant message)
                if (chunk.ClientTool != null)
                {
                    var toolResponse = new Message
                    {
                        Role = "tool",
                        ToolCallId = chunk.ClientTool.ToolId,
                        Content = $"Client-side tool '{chunk.ClientTool.ToolName}' invoked with arguments: {chunk.ClientTool.Arguments}"
                    };

                    // If assistant message already added, add immediately; otherwise queue it
                    if (currentMessageAdded)
                    {
                        messages.Add(toolResponse);
                    }
                    else
                    {
                        pendingClientToolResponses.Add(toolResponse);
                    }
                }

                foreach (var sseEvent in events)
                {
                    await writer.WriteEventAsync(sseEvent, cancellationToken);

                    if (sseEvent is CompletionEvent)
                    {
                        completionSent = true;
                    }
                }
            }

            // Add any remaining assistant message (final response without tool calls)
            if (hasContent && !currentMessageAdded)
            {
                var assistantMessage = new Message
                {
                    Role = "assistant",
                    Content = contentBuilder.ToString()
                };

                if (toolCallsAccumulator.Count > 0)
                {
                    assistantMessage.ToolCalls = toolCallsAccumulator.Values.ToArray();
                }

                messages.Add(assistantMessage);
            }

            // Ensure a completion event is always sent
            if (!completionSent)
            {
                var completionEvent = new CompletionEvent
                {
                    Type = SseEventType.Completion,
                    ChunkIndex = lastChunkIndex + 1,
                    ElapsedMs = lastElapsedMs,
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

        return messages;
    }

    public static async IAsyncEnumerable<SseEvent> StreamAsSseEventsAsync(
        this OpenRouterClient client,
        ChatCompletionRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var completionSent = false;
        var lastChunkIndex = 0;
        var lastElapsedMs = 0.0;

        await foreach (var chunk in client.StreamAsync(request, cancellationToken))
        {
            var events = SseChunkMapper.MapChunk(chunk);
            lastChunkIndex = chunk.ChunkIndex;
            lastElapsedMs = chunk.ElapsedTime.TotalMilliseconds;

            foreach (var sseEvent in events)
            {
                if (sseEvent is CompletionEvent)
                {
                    completionSent = true;
                }

                yield return sseEvent;
            }
        }

        // Ensure a completion event is always sent
        if (!completionSent)
        {
            yield return new CompletionEvent
            {
                Type = SseEventType.Completion,
                ChunkIndex = lastChunkIndex + 1,
                ElapsedMs = lastElapsedMs,
                FinishReason = "stop"
            };
        }
    }
}
