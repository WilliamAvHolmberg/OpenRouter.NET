using System.Text;
using OpenRouter.NET.Models;
using OpenRouter.NET.Sse;
using Xunit.Abstractions;

namespace OpenRouter.NET.Tests.Integration;

[Trait("Category", "Integration")]
public class SseStreamingIntegrationTests : IntegrationTestBase
{
    public SseStreamingIntegrationTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async Task StreamAsSseEventsAsync_WithArtifact_ProducesCompleteEventSequence()
    {
        LogInfo("=== COMPREHENSIVE SSE STREAMING TEST ===");
        LogInfo("Testing: Event sequence, artifact generation, and completion");
        LogInfo("");

        var client = CreateClient();
        
        var request = new ChatCompletionRequest
        {
            Model = TestModel,
            Messages = new List<Message> 
            { 
                Message.FromUser("Create a simple Python hello world function. Please use an artifact for the code.")
            }
        };
        
        request.EnableArtifactSupport();

        LogInfo($"📤 Request: {request.Messages[0].Content}");
        LogInfo($"🤖 Model: {request.Model}");
        LogInfo($"📦 Artifact support: Enabled");
        LogInfo("");
        LogInfo("🔄 Starting stream...");
        LogInfo("");

        var events = new List<SseEvent>();
        var textEvents = new List<TextEvent>();
        var artifactStartedEvents = new List<ArtifactStartedEvent>();
        var artifactContentEvents = new List<ArtifactContentEvent>();
        var artifactCompletedEvents = new List<ArtifactCompletedEvent>();
        var completionEvents = new List<CompletionEvent>();

        var fullText = new StringBuilder();
        var eventLog = new StringBuilder();

        try
        {
            await foreach (var sseEvent in client.StreamAsSseEventsAsync(request))
            {
                events.Add(sseEvent);
                
                switch (sseEvent)
                {
                    case TextEvent textEvent:
                        textEvents.Add(textEvent);
                        fullText.Append(textEvent.TextDelta);
                        eventLog.AppendLine($"[{events.Count:000}] [{textEvent.ElapsedMs:0000}ms] TEXT: {textEvent.TextDelta.Replace("\n", "\\n")}");
                        break;

                    case ArtifactStartedEvent artifactStarted:
                        artifactStartedEvents.Add(artifactStarted);
                        eventLog.AppendLine($"[{events.Count:000}] [{artifactStarted.ElapsedMs:0000}ms] ARTIFACT_STARTED: {artifactStarted.Title} (id={artifactStarted.ArtifactId}, type={artifactStarted.ArtifactType}, lang={artifactStarted.Language})");
                        LogSuccess($"🎨 Artifact started: {artifactStarted.Title}");
                        break;

                    case ArtifactContentEvent artifactContent:
                        artifactContentEvents.Add(artifactContent);
                        eventLog.AppendLine($"[{events.Count:000}] [{artifactContent.ElapsedMs:0000}ms] ARTIFACT_CONTENT: {artifactContent.ContentDelta.Length} chars (id={artifactContent.ArtifactId})");
                        break;

                    case ArtifactCompletedEvent artifactCompleted:
                        artifactCompletedEvents.Add(artifactCompleted);
                        eventLog.AppendLine($"[{events.Count:000}] [{artifactCompleted.ElapsedMs:0000}ms] ARTIFACT_COMPLETED: {artifactCompleted.Title} (id={artifactCompleted.ArtifactId}, size={artifactCompleted.Content.Length})");
                        LogSuccess($"✅ Artifact completed: {artifactCompleted.Title} ({artifactCompleted.Content.Length} chars)");
                        break;

                    case CompletionEvent completion:
                        completionEvents.Add(completion);
                        eventLog.AppendLine($"[{events.Count:000}] [{completion.ElapsedMs:0000}ms] COMPLETION: reason={completion.FinishReason}, model={completion.Model}");
                        LogSuccess($"🏁 Stream completed: {completion.FinishReason}");
                        break;

                    case ToolExecutingEvent toolExecuting:
                        eventLog.AppendLine($"[{events.Count:000}] [{toolExecuting.ElapsedMs:0000}ms] TOOL_EXECUTING: {toolExecuting.ToolName}");
                        LogInfo($"🔧 Tool executing: {toolExecuting.ToolName}");
                        break;

                    case ToolCompletedEvent toolCompleted:
                        eventLog.AppendLine($"[{events.Count:000}] [{toolCompleted.ElapsedMs:0000}ms] TOOL_COMPLETED: {toolCompleted.ToolName}");
                        LogInfo($"✅ Tool completed: {toolCompleted.ToolName}");
                        break;

                    case ToolErrorEvent toolError:
                        eventLog.AppendLine($"[{events.Count:000}] [{toolError.ElapsedMs:0000}ms] TOOL_ERROR: {toolError.ToolName} - {toolError.Error}");
                        LogWarning($"❌ Tool error: {toolError.ToolName}");
                        break;

                    case ToolClientEvent toolClient:
                        eventLog.AppendLine($"[{events.Count:000}] [{toolClient.ElapsedMs:0000}ms] TOOL_CLIENT: {toolClient.ToolName}");
                        LogInfo($"📞 Client tool: {toolClient.ToolName}");
                        break;

                    case ErrorEvent error:
                        eventLog.AppendLine($"[{events.Count:000}] [{error.ElapsedMs:0000}ms] ERROR: {error.Message}");
                        LogError($"❌ Error: {error.Message}");
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            LogError($"Stream failed: {ex.Message}");
            LogError($"Exception: {ex}");
            throw;
        }

        LogInfo("");
        LogInfo("=== STREAM COMPLETED ===");
        LogInfo("");
        LogInfo("📊 EVENT SUMMARY:");
        LogInfo($"  Total events: {events.Count}");
        LogInfo($"  Text events: {textEvents.Count}");
        LogInfo($"  Artifact started: {artifactStartedEvents.Count}");
        LogInfo($"  Artifact content: {artifactContentEvents.Count}");
        LogInfo($"  Artifact completed: {artifactCompletedEvents.Count}");
        LogInfo($"  Completion events: {completionEvents.Count}");
        LogInfo("");

        if (fullText.Length > 0)
        {
            var preview = fullText.ToString();
            LogInfo($"📝 FULL TEXT ({preview.Length} chars):");
            LogInfo(preview.Length > 500 ? preview.Substring(0, 500) + "..." : preview);
            LogInfo("");
        }

        if (artifactCompletedEvents.Any())
        {
            foreach (var artifact in artifactCompletedEvents)
            {
                LogInfo($"📦 ARTIFACT: {artifact.Title}");
                LogInfo($"   Type: {artifact.ArtifactType}");
                LogInfo($"   Language: {artifact.Language}");
                LogInfo($"   Size: {artifact.Content.Length} chars");
                var contentPreview = artifact.Content.Length > 200 
                    ? artifact.Content.Substring(0, 200) + "..." 
                    : artifact.Content;
                LogInfo($"   Content preview:");
                LogInfo($"   {contentPreview}");
                LogInfo("");
            }
        }

        LogInfo("📋 DETAILED EVENT LOG:");
        Output.WriteLine(eventLog.ToString());
        LogInfo("");

        // ========================================
        // ASSERTIONS
        // ========================================

        LogInfo("🧪 RUNNING ASSERTIONS...");
        LogInfo("");

        // 1. Basic sanity checks
        Assert.NotEmpty(events);
        LogSuccess("✓ Received events");

        Assert.NotEmpty(textEvents);
        LogSuccess($"✓ Received {textEvents.Count} text events");

        // 2. Completion event check
        Assert.NotEmpty(completionEvents);
        LogSuccess("✓ Received completion event");

        var completion = completionEvents[0];
        Assert.NotNull(completion.FinishReason);
        LogSuccess($"✓ Completion has finish reason: {completion.FinishReason}");

        // 3. Artifact sequence check
        Assert.NotEmpty(artifactStartedEvents);
        LogSuccess("✓ Artifact started event received");

        Assert.NotEmpty(artifactCompletedEvents);
        LogSuccess("✓ Artifact completed event received");

        // 4. Artifact sequence order
        var firstArtifactStartIndex = events.IndexOf(artifactStartedEvents[0]);
        var firstArtifactCompleteIndex = events.IndexOf(artifactCompletedEvents[0]);

        Assert.True(firstArtifactCompleteIndex > firstArtifactStartIndex,
            $"Artifact completion (index {firstArtifactCompleteIndex}) should come after start (index {firstArtifactStartIndex})");
        LogSuccess($"✓ Artifact sequence correct: started at [{firstArtifactStartIndex}], completed at [{firstArtifactCompleteIndex}]");

        // 5. Artifact content events between start and complete
        foreach (var contentEvent in artifactContentEvents)
        {
            var contentIndex = events.IndexOf(contentEvent);
            Assert.True(contentIndex > firstArtifactStartIndex && contentIndex < firstArtifactCompleteIndex,
                $"Artifact content (index {contentIndex}) should be between start ({firstArtifactStartIndex}) and completion ({firstArtifactCompleteIndex})");
        }
        LogSuccess($"✓ All {artifactContentEvents.Count} artifact content events are in correct position");

        // 6. Artifact ID consistency
        var startedId = artifactStartedEvents[0].ArtifactId;
        var completedId = artifactCompletedEvents[0].ArtifactId;
        Assert.Equal(startedId, completedId);
        LogSuccess($"✓ Artifact IDs match: {startedId}");

        foreach (var contentEvent in artifactContentEvents)
        {
            Assert.Equal(startedId, contentEvent.ArtifactId);
        }
        LogSuccess($"✓ All artifact content events have matching ID");

        // 7. Artifact has actual content
        var artifactContent = artifactCompletedEvents[0].Content;
        Assert.NotEmpty(artifactContent);
        LogSuccess($"✓ Artifact has content ({artifactContent.Length} chars)");

        // Verify it looks like Python code
        Assert.Contains("def", artifactContent);
        LogSuccess("✓ Artifact contains Python code (has 'def')");

        // 8. Completion event is at or near the end
        var completionIndex = events.IndexOf(completionEvents[0]);
        var lastEventIndex = events.Count - 1;
        Assert.True(completionIndex >= lastEventIndex - 5,
            $"Completion event should be near the end (at {completionIndex} of {lastEventIndex})");
        LogSuccess($"✓ Completion event at correct position: [{completionIndex}/{lastEventIndex}]");

        // 9. Chunk indices are valid and increase
        for (int i = 1; i < events.Count; i++)
        {
            Assert.True(events[i].ChunkIndex >= 0, "ChunkIndex should be non-negative");
            Assert.True(events[i].ChunkIndex >= events[i-1].ChunkIndex, 
                $"ChunkIndex should not decrease: {events[i-1].ChunkIndex} -> {events[i].ChunkIndex}");
        }
        LogSuccess($"✓ All {events.Count} chunk indices are valid and monotonic");

        // 10. Elapsed time is monotonic
        for (int i = 1; i < events.Count; i++)
        {
            Assert.True(events[i].ElapsedMs >= events[i-1].ElapsedMs,
                $"ElapsedMs should increase: {events[i-1].ElapsedMs} -> {events[i].ElapsedMs}");
        }
        LogSuccess($"✓ Elapsed time is monotonically increasing");

        // 11. All events have proper type
        foreach (var evt in events)
        {
            Assert.NotNull(evt.Type);
            Assert.NotEmpty(evt.Type);
        }
        LogSuccess($"✓ All {events.Count} events have valid type");

        // 12. No error events
        var errorEvents = events.OfType<ErrorEvent>().ToList();
        Assert.Empty(errorEvents);
        LogSuccess("✓ No error events received");

        // 13. Text content accumulation
        var accumulatedText = fullText.ToString();
        Assert.True(accumulatedText.Length > 0, "Should have accumulated text");
        LogSuccess($"✓ Accumulated text: {accumulatedText.Length} chars");

        LogInfo("");
        LogSuccess("🎉 ALL ASSERTIONS PASSED!");
        LogInfo("");
        LogInfo("=== TEST COMPLETE ===");
    }
}
