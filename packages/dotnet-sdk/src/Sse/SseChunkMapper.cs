using OpenRouter.NET.Models;
using OpenRouter.NET.Streaming;

namespace OpenRouter.NET.Sse;

public static class SseChunkMapper
{
    public static IEnumerable<SseEvent> MapChunk(StreamChunk chunk)
    {
        var events = new List<SseEvent>();
        var elapsedMs = chunk.ElapsedTime.TotalMilliseconds;

        if (chunk.TextDelta != null)
        {
            events.Add(new TextEvent
            {
                Type = SseEventType.Text,
                ChunkIndex = chunk.ChunkIndex,
                ElapsedMs = elapsedMs,
                TextDelta = chunk.TextDelta
            });
        }

        if (chunk.ServerTool != null)
        {
            var tool = chunk.ServerTool;
            
            switch (tool.State)
            {
                case ToolCallState.Executing:
                    events.Add(new ToolExecutingEvent
                    {
                        Type = SseEventType.ToolExecuting,
                        ChunkIndex = chunk.ChunkIndex,
                        ElapsedMs = elapsedMs,
                        ToolName = tool.ToolName,
                        ToolId = tool.ToolId,
                        Arguments = tool.Arguments
                    });
                    break;

                case ToolCallState.Completed:
                    events.Add(new ToolCompletedEvent
                    {
                        Type = SseEventType.ToolCompleted,
                        ChunkIndex = chunk.ChunkIndex,
                        ElapsedMs = elapsedMs,
                        ToolName = tool.ToolName,
                        ToolId = tool.ToolId,
                        Arguments = tool.Arguments,
                        Result = tool.Result,
                        ExecutionMs = tool.ExecutionTime?.TotalMilliseconds
                    });
                    break;

                case ToolCallState.Error:
                    events.Add(new ToolErrorEvent
                    {
                        Type = SseEventType.ToolError,
                        ChunkIndex = chunk.ChunkIndex,
                        ElapsedMs = elapsedMs,
                        ToolName = tool.ToolName,
                        ToolId = tool.ToolId,
                        Arguments = tool.Arguments,
                        Error = tool.Error ?? "Unknown error",
                        ExecutionMs = tool.ExecutionTime?.TotalMilliseconds
                    });
                    break;
            }
        }

        if (chunk.ClientTool != null)
        {
            var tool = chunk.ClientTool;
            events.Add(new ToolClientEvent
            {
                Type = SseEventType.ToolClient,
                ChunkIndex = chunk.ChunkIndex,
                ElapsedMs = elapsedMs,
                ToolName = tool.ToolName,
                ToolId = tool.ToolId,
                Arguments = tool.Arguments
            });
        }

        if (chunk.Artifact != null)
        {
            switch (chunk.Artifact)
            {
                case ArtifactStarted started:
                    events.Add(new ArtifactStartedEvent
                    {
                        Type = SseEventType.ArtifactStarted,
                        ChunkIndex = chunk.ChunkIndex,
                        ElapsedMs = elapsedMs,
                        ArtifactId = started.ArtifactId,
                        Title = started.Title,
                        ArtifactType = started.Type,
                        Language = started.Language
                    });
                    break;

                case ArtifactContent content:
                    events.Add(new ArtifactContentEvent
                    {
                        Type = SseEventType.ArtifactContent,
                        ChunkIndex = chunk.ChunkIndex,
                        ElapsedMs = elapsedMs,
                        ArtifactId = content.ArtifactId,
                        ContentDelta = content.ContentDelta
                    });
                    break;

                case ArtifactCompleted completed:
                    events.Add(new ArtifactCompletedEvent
                    {
                        Type = SseEventType.ArtifactCompleted,
                        ChunkIndex = chunk.ChunkIndex,
                        ElapsedMs = elapsedMs,
                        ArtifactId = completed.ArtifactId,
                        Title = completed.Title,
                        ArtifactType = completed.Type,
                        Language = completed.Language,
                        Content = completed.Content
                    });
                    break;
            }
        }

        if (chunk.Completion != null)
        {
            events.Add(new CompletionEvent
            {
                Type = SseEventType.Completion,
                ChunkIndex = chunk.ChunkIndex,
                ElapsedMs = elapsedMs,
                FinishReason = chunk.Completion.FinishReason,
                Model = chunk.Completion.Model,
                Id = chunk.Completion.Id
            });
        }

        return events;
    }
}
