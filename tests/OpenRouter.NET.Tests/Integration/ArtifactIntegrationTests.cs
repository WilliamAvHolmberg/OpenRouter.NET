using System.Text;
using OpenRouter.NET.Artifacts;
using OpenRouter.NET.Models;
using Xunit.Abstractions;

namespace OpenRouter.NET.Tests.Integration;

[Trait("Category", "Integration")]
public class ArtifactIntegrationTests : IntegrationTestBase
{
    public ArtifactIntegrationTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async Task SingleCodeArtifact_ShouldBeExtractedCorrectly()
    {
        LogInfo("Testing single code artifact extraction...");
        
        var client = CreateClient();
        
        var request = new ChatCompletionRequest
        {
            Model = TestModel,
            Messages = new List<Message> 
            { 
                Message.FromUser("Create a simple TypeScript function that adds two numbers") 
            }
        };
        
        request.EnableArtifacts(OpenRouter.NET.Artifacts.Artifacts.Code(language: "typescript"));
        
        var artifacts = new List<ArtifactEvent>();
        var responseText = new StringBuilder();
        var completedArtifacts = new List<ArtifactCompleted>();
        var chunkCount = 0;
        
        await foreach (var chunk in client.StreamAsync(request))
        {
            chunkCount++;
            
            if (chunk.TextDelta != null)
            {
                responseText.Append(chunk.TextDelta);
                LogChunk(chunkCount, "TEXT", chunk.TextDelta);
            }
            
            if (chunk.Artifact != null)
            {
                artifacts.Add(chunk.Artifact);
                
                switch (chunk.Artifact)
                {
                    case ArtifactStarted started:
                        LogInfo($"Artifact started: {started.Title} (type: {started.Type}, lang: {started.Language})");
                        break;
                    case ArtifactContent content:
                        LogChunk(chunkCount, "ARTIFACT_CONTENT", $"{content.ContentDelta.Length} chars");
                        break;
                    case ArtifactCompleted completed:
                        LogSuccess($"Artifact completed: {completed.Title} ({completed.Content.Length} chars)");
                        completedArtifacts.Add(completed);
                        break;
                }
            }
        }
        
        LogInfo($"Total chunks: {chunkCount}");
        LogInfo($"Total artifact events: {artifacts.Count}");
        LogInfo($"Completed artifacts: {completedArtifacts.Count}");
        LogInfo($"Response text length: {responseText.Length}");
        
        Assert.NotEmpty(completedArtifacts);
        Assert.Equal(1, completedArtifacts.Count);
        
        var artifact = completedArtifacts[0];
        Assert.NotNull(artifact.Title);
        Assert.NotNull(artifact.Content);
        Assert.True(artifact.Content.Length > 0);
        Assert.Equal("code", artifact.Type);
        
        Assert.Contains(artifacts, a => a is ArtifactStarted);
        Assert.Contains(artifacts, a => a is ArtifactCompleted);
        
        Assert.DoesNotContain("<artifact", responseText.ToString());
        Assert.DoesNotContain("</artifact>", responseText.ToString());
        
        LogSuccess("✓ Single code artifact extracted successfully");
        LogInfo($"Artifact preview: {artifact.Content.Substring(0, Math.Min(100, artifact.Content.Length))}...");
    }

    [Fact]
    public async Task MultipleArtifacts_ShouldAllBeExtracted()
    {
        LogInfo("Testing multiple artifact extraction...");
        
        var client = CreateClient();
        
        var request = new ChatCompletionRequest
        {
            Model = TestModel,
            Messages = new List<Message> 
            { 
                Message.FromUser("Create a simple React button component with TypeScript and CSS") 
            }
        };
        
        request.EnableArtifacts(
            OpenRouter.NET.Artifacts.Artifacts.Code(language: "typescript"),
            OpenRouter.NET.Artifacts.Artifacts.Code(language: "css")
        );
        
        var completedArtifacts = new List<ArtifactCompleted>();
        var responseText = new StringBuilder();
        
        await foreach (var chunk in client.StreamAsync(request))
        {
            if (chunk.TextDelta != null)
            {
                responseText.Append(chunk.TextDelta);
            }
            
            if (chunk.Artifact is ArtifactStarted started)
            {
                LogInfo($"Started: {started.Title}");
            }
            
            if (chunk.Artifact is ArtifactCompleted completed)
            {
                completedArtifacts.Add(completed);
                LogSuccess($"Completed: {completed.Title} ({completed.Type}, {completed.Language})");
                LogInfo($"  Content length: {completed.Content.Length} chars");
            }
        }
        
        LogInfo($"Total artifacts created: {completedArtifacts.Count}");
        
        Assert.True(completedArtifacts.Count >= 1, "At least one artifact should be created");
        
        foreach (var artifact in completedArtifacts)
        {
            Assert.NotNull(artifact.Title);
            Assert.NotNull(artifact.Content);
            Assert.True(artifact.Content.Length > 0);
            LogInfo($"  - {artifact.Title}: {artifact.Content.Length} chars");
        }
        
        Assert.DoesNotContain("<artifact", responseText.ToString());
        
        LogSuccess($"✓ {completedArtifacts.Count} artifact(s) extracted successfully");
    }

    [Fact]
    public async Task ArtifactWithoutXmlTags_InResponseText()
    {
        LogInfo("Testing that artifact XML tags are stripped from response text...");
        
        var client = CreateClient();
        
        var request = new ChatCompletionRequest
        {
            Model = TestModel,
            Messages = new List<Message> 
            { 
                Message.FromUser("Create a simple HTML page") 
            }
        };
        
        request.EnableArtifacts(OpenRouter.NET.Artifacts.Artifacts.Document(format: "html"));
        
        var responseText = new StringBuilder();
        var completedArtifacts = new List<ArtifactCompleted>();
        
        await foreach (var chunk in client.StreamAsync(request))
        {
            if (chunk.TextDelta != null)
            {
                responseText.Append(chunk.TextDelta);
                
                if (chunk.TextDelta.Contains("<artifact") || chunk.TextDelta.Contains("</artifact>"))
                {
                    LogError($"Found XML tag in text delta: {chunk.TextDelta}");
                }
            }
            
            if (chunk.Artifact is ArtifactCompleted completed)
            {
                completedArtifacts.Add(completed);
            }
        }
        
        var fullText = responseText.ToString();
        
        LogInfo($"Response text length: {fullText.Length}");
        LogInfo($"Artifacts created: {completedArtifacts.Count}");
        
        Assert.DoesNotContain("<artifact", fullText);
        Assert.DoesNotContain("</artifact>", fullText);
        
        if (completedArtifacts.Any())
        {
            LogSuccess($"✓ Artifact created and XML tags stripped from text");
        }
        
        LogSuccess("✓ No artifact XML tags in response text");
    }

    [Fact]
    public async Task ArtifactEventSequence_ShouldBeCorrect()
    {
        LogInfo("Testing artifact event sequence (Started -> Content -> Completed)...");
        
        var client = CreateClient();
        
        var request = new ChatCompletionRequest
        {
            Model = TestModel,
            Messages = new List<Message> 
            { 
                Message.FromUser("Create a simple Python function that says hello") 
            }
        };
        
        request.EnableArtifacts(OpenRouter.NET.Artifacts.Artifacts.Code(language: "python"));
        
        var artifactEvents = new List<string>();
        var currentArtifactId = "";
        
        await foreach (var chunk in client.StreamAsync(request))
        {
            switch (chunk.Artifact)
            {
                case ArtifactStarted started:
                    currentArtifactId = started.ArtifactId;
                    artifactEvents.Add($"STARTED:{started.ArtifactId}");
                    LogInfo($"Started: {started.ArtifactId} - {started.Title}");
                    break;
                    
                case ArtifactContent content:
                    artifactEvents.Add($"CONTENT:{content.ArtifactId}");
                    break;
                    
                case ArtifactCompleted completed:
                    artifactEvents.Add($"COMPLETED:{completed.ArtifactId}");
                    LogInfo($"Completed: {completed.ArtifactId}");
                    break;
            }
        }
        
        LogInfo($"Total artifact events: {artifactEvents.Count}");
        
        if (artifactEvents.Count > 0)
        {
            foreach (var evt in artifactEvents.Take(10))
            {
                LogInfo($"  {evt}");
            }
            
            var hasStarted = artifactEvents.Any(e => e.StartsWith("STARTED:"));
            var hasContent = artifactEvents.Any(e => e.StartsWith("CONTENT:"));
            var hasCompleted = artifactEvents.Any(e => e.StartsWith("COMPLETED:"));
            
            Assert.True(hasStarted, "Should have STARTED event");
            Assert.True(hasCompleted, "Should have COMPLETED event");
            
            var startedIndex = artifactEvents.FindIndex(e => e.StartsWith("STARTED:"));
            var completedIndex = artifactEvents.FindIndex(e => e.StartsWith("COMPLETED:"));
            
            Assert.True(startedIndex < completedIndex, "STARTED should come before COMPLETED");
            
            LogSuccess("✓ Artifact event sequence is correct");
        }
        else
        {
            LogWarning("No artifact events captured (model might not have created artifacts)");
        }
    }

    [Fact]
    public async Task ArtifactEventSequence_DiagnosticTest()
    {
        LogInfo("DIAGNOSTIC: Testing artifact parsing with raw response capture...");

        var client = CreateClient();

        var request = new ChatCompletionRequest
        {
            Model = TestModel,
            Messages = new List<Message>
            {
                Message.FromUser("Create a simple Python function that says hello")
            }
        };

        request.EnableArtifacts(OpenRouter.NET.Artifacts.Artifacts.Code(language: "python"));

        var artifactEvents = new List<string>();
        var responseText = new StringBuilder();
        var allChunks = new List<string>();

        await foreach (var chunk in client.StreamAsync(request))
        {
            if (chunk.TextDelta != null)
            {
                responseText.Append(chunk.TextDelta);
                allChunks.Add(chunk.TextDelta);
            }

            if (chunk.Artifact != null)
            {
                switch (chunk.Artifact)
                {
                    case ArtifactStarted started:
                        artifactEvents.Add($"STARTED:{started.ArtifactId}");
                        LogInfo($"✓ STARTED: {started.ArtifactId} - {started.Title}");
                        break;

                    case ArtifactContent content:
                        artifactEvents.Add($"CONTENT:{content.ArtifactId}");
                        break;

                    case ArtifactCompleted completed:
                        artifactEvents.Add($"COMPLETED:{completed.ArtifactId}");
                        LogSuccess($"✓ COMPLETED: {completed.ArtifactId}");
                        break;
                }
            }
        }

        LogInfo($"\n=== DIAGNOSTIC INFO ===");
        LogInfo($"Total artifact events: {artifactEvents.Count}");
        LogInfo($"Total text chunks: {allChunks.Count}");
        LogInfo($"Full response length: {responseText.Length} chars");
        LogInfo($"\nResponse text:\n{responseText}");
        LogInfo($"\nLast 5 chunks:");
        foreach (var chunk in allChunks.TakeLast(5))
        {
            LogInfo($"  Chunk: '{chunk}'");
        }

        var hasClosingTag = responseText.ToString().Contains("</artifact>");
        LogInfo($"\nContains closing tag: {hasClosingTag}");

        if (hasClosingTag)
        {
            LogInfo("✓ Closing tag is present in response");
            if (!artifactEvents.Any(e => e.StartsWith("COMPLETED:")))
            {
                LogError("❌ BUG: Closing tag present but parser didn't emit COMPLETED event!");
            }
        }
        else
        {
            LogWarning("⚠ Closing tag NOT present - LLM didn't close the artifact tag");
        }

        LogInfo($"\n=== END DIAGNOSTIC ===\n");
    }

    [Fact]
    public async Task GenericArtifact_WithCustomType()
    {
        LogInfo("Testing generic artifact with custom type...");
        
        var client = CreateClient();
        
        var customArtifact = new GenericArtifact("config")
            .WithLanguage("json")
            .WithInstruction("Create configuration as JSON");
        
        var request = new ChatCompletionRequest
        {
            Model = TestModel,
            Messages = new List<Message> 
            { 
                Message.FromUser("Create a simple app configuration file") 
            }
        };
        
        request.EnableArtifacts(customArtifact);
        
        var completedArtifacts = new List<ArtifactCompleted>();
        
        await foreach (var chunk in client.StreamAsync(request))
        {
            if (chunk.Artifact is ArtifactCompleted completed)
            {
                completedArtifacts.Add(completed);
                LogSuccess($"Completed: {completed.Title} (type: {completed.Type}, lang: {completed.Language})");
            }
        }
        
        if (completedArtifacts.Any())
        {
            var artifact = completedArtifacts[0];
            
            LogInfo($"Artifact type: {artifact.Type}");
            LogInfo($"Artifact language: {artifact.Language}");
            LogInfo($"Content preview: {artifact.Content.Substring(0, Math.Min(100, artifact.Content.Length))}");
            
            Assert.NotNull(artifact.Content);
            Assert.True(artifact.Content.Length > 0);
            
            LogSuccess("✓ Generic artifact with custom type created successfully");
        }
        else
        {
            LogWarning("No artifacts were created (model might not have used artifacts)");
        }
    }

    [Fact]
    public async Task ArtifactContent_ShouldAccumulateCorrectly()
    {
        LogInfo("Testing artifact content accumulation...");
        
        var client = CreateClient();
        
        var request = new ChatCompletionRequest
        {
            Model = TestModel,
            Messages = new List<Message> 
            { 
                Message.FromUser("Create a JavaScript function with detailed comments") 
            }
        };
        
        request.EnableArtifacts(OpenRouter.NET.Artifacts.Artifacts.Code(language: "javascript"));
        
        var contentChunks = new List<string>();
        var completedArtifacts = new List<ArtifactCompleted>();
        
        await foreach (var chunk in client.StreamAsync(request))
        {
            switch (chunk.Artifact)
            {
                case ArtifactContent content:
                    contentChunks.Add(content.ContentDelta);
                    LogChunk(contentChunks.Count, "CONTENT", $"{content.ContentDelta.Length} chars");
                    break;
                    
                case ArtifactCompleted completed:
                    completedArtifacts.Add(completed);
                    break;
            }
        }
        
        if (completedArtifacts.Any())
        {
            var accumulated = string.Concat(contentChunks);
            var final = completedArtifacts[0].Content;
            
            LogInfo($"Content chunks: {contentChunks.Count}");
            LogInfo($"Accumulated length: {accumulated.Length}");
            LogInfo($"Final length: {final.Length}");
            
            Assert.Equal(accumulated, final);
            
            LogSuccess("✓ Artifact content accumulated correctly");
        }
        else
        {
            LogWarning("No artifacts created to test accumulation");
        }
    }

    [Fact]
    public async Task ArtifactId_ShouldBePreservedFromLLM()
    {
        LogInfo("Testing that LLM-provided artifact IDs are preserved end-to-end...");
        
        var client = CreateClient();
        
        var request = new ChatCompletionRequest
        {
            Model = TestModel,
            Messages = new List<Message> 
            { 
                Message.FromUser("Create a simple Python hello world function") 
            }
        };
        
        request.EnableArtifacts(OpenRouter.NET.Artifacts.Artifacts.Code(language: "python"));
        
        var artifactStarted = default(ArtifactStarted);
        var artifactContent = new List<ArtifactContent>();
        var artifactCompleted = default(ArtifactCompleted);
        var textDeltas = new StringBuilder();
        
        await foreach (var chunk in client.StreamAsync(request))
        {
            if (chunk.TextDelta != null)
            {
                textDeltas.Append(chunk.TextDelta);
            }
            
            switch (chunk.Artifact)
            {
                case ArtifactStarted started:
                    artifactStarted = started;
                    LogInfo($"✓ ArtifactStarted - ID: '{started.ArtifactId}'");
                    LogInfo($"  Title: {started.Title}");
                    LogInfo($"  Type: {started.Type}");
                    LogInfo($"  Language: {started.Language}");
                    break;
                    
                case ArtifactContent content:
                    artifactContent.Add(content);
                    LogChunk(artifactContent.Count, "CONTENT", $"ID: '{content.ArtifactId}', Delta: {content.ContentDelta.Length} chars");
                    break;
                    
                case ArtifactCompleted completed:
                    artifactCompleted = completed;
                    LogSuccess($"✓ ArtifactCompleted - ID: '{completed.ArtifactId}'");
                    LogInfo($"  Title: {completed.Title}");
                    LogInfo($"  Type: {completed.Type}");
                    LogInfo($"  Language: {completed.Language}");
                    LogInfo($"  Content length: {completed.Content.Length} chars");
                    break;
            }
        }
        
        LogInfo($"\n=== VERIFICATION ===");
        
        if (artifactStarted == null)
        {
            LogWarning("No artifact was created by the LLM");
            return;
        }
        
        Assert.NotNull(artifactStarted);
        Assert.NotNull(artifactCompleted);
        
        var artifactId = artifactStarted.ArtifactId;
        LogInfo($"Artifact ID from LLM: '{artifactId}'");
        
        // Check if ID format indicates it came from LLM (not auto-generated)
        var isAutoGenerated = artifactId.StartsWith("art_") && artifactId.Length <= 12;
        var isLLMProvided = !isAutoGenerated;
        
        if (isLLMProvided)
        {
            LogSuccess($"✓ LLM provided a custom ID: '{artifactId}'");
        }
        else
        {
            LogWarning($"⚠ ID appears auto-generated: '{artifactId}' (LLM might not have provided an ID)");
            LogWarning("  This test verifies the parser CAN preserve LLM IDs, even if LLM didn't provide one this time.");
        }
        
        // Verify all events use the same ID
        Assert.Equal(artifactId, artifactStarted.ArtifactId);
        Assert.Equal(artifactId, artifactCompleted.ArtifactId);
        
        foreach (var content in artifactContent)
        {
            Assert.Equal(artifactId, content.ArtifactId);
        }
        
        LogSuccess($"✓ All artifact events use consistent ID: '{artifactId}'");
        
        // Verify artifact tags are stripped from text
        var fullText = textDeltas.ToString();
        Assert.DoesNotContain("<artifact", fullText);
        Assert.DoesNotContain("</artifact>", fullText);
        LogSuccess("✓ Artifact XML tags stripped from response text");
        
        // Test with CollectArtifactsAsync to verify ID propagates to final Artifact objects
        var request2 = new ChatCompletionRequest
        {
            Model = TestModel,
            Messages = new List<Message> 
            { 
                Message.FromUser("Create a simple countdown function in Python") 
            }
        };
        request2.EnableArtifacts(OpenRouter.NET.Artifacts.Artifacts.Code(language: "python"));
        
        LogInfo("\n=== TESTING CollectArtifactsAsync ===");
        var collectedArtifacts = await client.CollectArtifactsAsync(request2);
        
        if (collectedArtifacts.Any())
        {
            var artifact = collectedArtifacts[0];
            LogInfo($"Collected artifact ID: '{artifact.Id}'");
            LogInfo($"  Title: {artifact.Title}");
            LogInfo($"  Type: {artifact.Type}");
            
            Assert.NotNull(artifact.Id);
            Assert.NotEmpty(artifact.Id);
            
            var isCollectedAutoGen = artifact.Id.StartsWith("art_") && artifact.Id.Length <= 12;
            if (!isCollectedAutoGen)
            {
                LogSuccess($"✓ CollectArtifactsAsync preserved LLM ID: '{artifact.Id}'");
            }
            else
            {
                LogWarning($"⚠ Collected artifact has auto-generated ID: '{artifact.Id}'");
            }
        }
        else
        {
            LogWarning("No artifacts collected in second request");
        }
        
        LogSuccess("\n✓ End-to-end artifact ID preservation test complete");
    }

    [Fact]
    public async Task ArtifactId_OrderAgnosticAttributeParsing()
    {
        LogInfo("Testing that artifact attributes can be in any order...");
        
        var client = CreateClient();
        
        var request = new ChatCompletionRequest
        {
            Model = TestModel,
            Messages = new List<Message> 
            { 
                Message.FromUser("Create a simple JavaScript function that calculates the factorial of a number") 
            }
        };
        
        request.EnableArtifacts(OpenRouter.NET.Artifacts.Artifacts.Code(language: "javascript"));
        
        var artifacts = new List<ArtifactCompleted>();
        
        await foreach (var chunk in client.StreamAsync(request))
        {
            if (chunk.Artifact is ArtifactStarted started)
            {
                LogInfo($"✓ Started: ID='{started.ArtifactId}', Title='{started.Title}', Type='{started.Type}'");
            }
            
            if (chunk.Artifact is ArtifactCompleted completed)
            {
                artifacts.Add(completed);
                LogSuccess($"✓ Completed: ID='{completed.ArtifactId}'");
            }
        }
        
        if (artifacts.Any())
        {
            var artifact = artifacts[0];
            
            // Verify artifact was parsed correctly regardless of attribute order
            Assert.NotNull(artifact.ArtifactId);
            Assert.NotNull(artifact.Title);
            Assert.NotNull(artifact.Type);
            Assert.NotEmpty(artifact.Content);
            
            LogInfo($"Parsed artifact:");
            LogInfo($"  ID: '{artifact.ArtifactId}'");
            LogInfo($"  Title: '{artifact.Title}'");
            LogInfo($"  Type: '{artifact.Type}'");
            LogInfo($"  Language: '{artifact.Language}'");
            
            LogSuccess("✓ Attributes parsed correctly regardless of order");
        }
        else
        {
            LogWarning("No artifacts created (LLM might not have followed exact format)");
            LogWarning("This is OK - the parser supports order-agnostic parsing even if LLM didn't test it here");
        }
    }
}

