using OpenRouter.NET;
using OpenRouter.NET.Models;
using OpenRouter.NET.Tools;
using LlmsTxtGenerator.Tools;
using LlmsTxtGenerator.Prompts;
using System.Text;

namespace LlmsTxtGenerator;

public class Agent
{
    private readonly OpenRouterClient _client;
    private readonly FileSystemTools _fileTools;
    private readonly WriteLlmsTxtTool _writeTool;
    private readonly List<Message> _conversationHistory;
    private readonly string _model;
    private readonly int _maxIterations;

    private readonly List<Artifact> _artifacts = new();

    public Agent(
        OpenRouterClient client,
        string basePath,
        string outputPath,
        string model = "anthropic/claude-3.5-sonnet",
        int maxIterations = 30)
    {
        _client = client;
        _fileTools = new FileSystemTools(basePath);
        _writeTool = new WriteLlmsTxtTool(outputPath, () => _artifacts);
        _model = model;
        _maxIterations = maxIterations;

        _conversationHistory = new List<Message>
        {
            Message.FromSystem(SystemPrompt.GetPrompt(basePath))
        };

        RegisterTools();
    }

    private void RegisterTools()
    {
        _client.RegisterTool(_fileTools, nameof(_fileTools.ListDirectory));
        _client.RegisterTool(_fileTools, nameof(_fileTools.ReadFile));
        _client.RegisterTool(_fileTools, nameof(_fileTools.ReadFiles));
        _client.RegisterTool(_fileTools, nameof(_fileTools.SearchFiles));
        _client.RegisterTool(_fileTools, nameof(_fileTools.GetDirectoryTree));
        _client.RegisterTool(_fileTools, nameof(_fileTools.GetCodebaseStats));
        _client.RegisterTool(_writeTool, nameof(_writeTool.WriteLlmsTxt));
    }

    public async Task<bool> RunAsync()
    {
        Console.WriteLine("🤖 Agent Starting...");
        Console.WriteLine($"📦 Model: {_model}");
        Console.WriteLine($"🔄 Max iterations: {_maxIterations}");
        Console.WriteLine();

        _conversationHistory.Add(Message.FromUser(
            "You are documenting the OpenRouter.NET SDK (the C#/.NET SDK in the src/ directory).\n\n" +
            "Follow this approach:\n\n" +
            "1. FIRST: Read llms.txt if it exists to understand expected depth and style\n" +
            "2. Explore the codebase structure (GetDirectoryTree)\n" +
            "3. Read README.md and OpenRouter.NET.csproj\n" +
            "4. Read ALL files in src/Extensions/ - these are critical!\n" +
            "5. Read ALL files in src/Sse/ if it exists\n" +
            "6. Explore samples/ directory for real-world C# usage patterns\n" +
            "7. Read other important source files in src/\n" +
            "8. Generate the complete documentation as an ARTIFACT using <artifact> tags\n" +
            "9. Finally, call WriteLlmsTxt with the artifact ID\n\n" +
            "CRITICAL OUTPUT FORMAT:\n" +
            "<artifact type=\"document\" language=\"text\" title=\"llms.txt\">\n" +
            "[Your complete 50,000+ character documentation here]\n" +
            "</artifact>\n\n" +
            "Then: WriteLlmsTxt(artifactId: \"artifact-id\")\n\n" +
            "After successfully calling WriteLlmsTxt, you are DONE. Do not continue.\n\n" +
            "Take your time and be thorough. Quality over speed!"));

        int iteration = 0;

        while (iteration < _maxIterations && !_writeTool.HasWritten)
        {
            iteration++;
            Console.WriteLine($"\n{'=',60}");
            Console.WriteLine($"🔄 Iteration {iteration}/{_maxIterations}");
            Console.WriteLine($"{'=',60}\n");

            var request = new ChatCompletionRequest
            {
                Model = _model,
                Messages = _conversationHistory,
                Temperature = 0.3f,
                MaxTokens = 8000  // Increased for artifact generation
            };
            
            // Enable artifact support for documentation generation
            request.EnableArtifactSupport();

            var response = new StringBuilder();
            var toolCalls = new List<(string name, string result)>();
            bool hasContent = false;

            try
            {
                await foreach (var chunk in _client.StreamAsync(request))
                {
                    if (chunk.TextDelta != null)
                    {
                        response.Append(chunk.TextDelta);
                        Console.Write(chunk.TextDelta);
                        hasContent = true;
                    }
                    
                    // Handle artifacts
                    if (chunk.Artifact != null)
                    {
                        if (chunk.Artifact is ArtifactStarted started)
                        {
                            Console.WriteLine($"\n\n📦 Artifact started: {started.Title} (Type: {started.Type})");
                        }
                        else if (chunk.Artifact is ArtifactCompleted completed)
                        {
                            Console.WriteLine($"\n\n✅ Artifact completed: {completed.Title}");
                            Console.WriteLine($"   ID: {completed.ArtifactId}");
                            Console.WriteLine($"   Size: {completed.Content?.Length ?? 0} characters");
                            
                            // Store artifact
                            var artifact = new Artifact
                            {
                                Id = completed.ArtifactId,
                                Type = completed.Type,
                                Title = completed.Title,
                                Content = completed.Content ?? "",
                                Language = completed.Language
                            };
                            _artifacts.Add(artifact);
                        }
                    }

                    if (chunk.ServerTool != null)
                    {
                        switch (chunk.ServerTool.State)
                        {
                            case ToolCallState.Executing:
                                Console.WriteLine($"\n\n🔧 Executing tool: {chunk.ServerTool.ToolName}");
                                
                                // Special handling for WriteLlmsTxt
                                if (chunk.ServerTool.ToolName == "WriteLlmsTxt")
                                {
                                    Console.WriteLine("   ⚠️  WRITING FINAL OUTPUT - This may take a moment...");
                                }
                                else
                                {
                                    Console.WriteLine($"   Arguments: {TruncateForDisplay(chunk.ServerTool.Arguments, 100)}");
                                }
                                break;

                            case ToolCallState.Completed:
                                var result = chunk.ServerTool.Result ?? "No result";
                                Console.WriteLine($"✅ Tool completed in {chunk.ServerTool.ExecutionTime?.TotalMilliseconds:F0}ms");
                                
                                // Special handling for WriteLlmsTxt success
                                if (chunk.ServerTool.ToolName == "WriteLlmsTxt")
                                {
                                    Console.WriteLine("\n" + "=".PadRight(60, '='));
                                    Console.WriteLine("🎉 WriteLlmsTxt SUCCEEDED!");
                                    Console.WriteLine("=".PadRight(60, '='));
                                    Console.WriteLine(result);
                                    
                                    // Force break on next check
                                }
                                else
                                {
                                    Console.WriteLine($"   Result preview: {TruncateForDisplay(result, 200)}");
                                }
                                
                                toolCalls.Add((chunk.ServerTool.ToolName, result));
                                break;

                            case ToolCallState.Error:
                                Console.WriteLine($"❌ Tool error: {chunk.ServerTool.Error}");
                                
                                // If WriteLlmsTxt failed, show prominent error
                                if (chunk.ServerTool.ToolName == "WriteLlmsTxt")
                                {
                                    Console.WriteLine("\n⚠️⚠️⚠️ WriteLlmsTxt FAILED! ⚠️⚠️⚠️");
                                    Console.WriteLine("The agent will likely retry. Check the error above.");
                                }
                                break;
                        }
                    }

                    if (chunk.Completion != null)
                    {
                        Console.WriteLine($"\n\n📊 Finish reason: {chunk.Completion.FinishReason}");
                        if (chunk.Completion.Usage != null)
                        {
                            Console.WriteLine($"   Tokens - Input: {chunk.Completion.Usage.PromptTokens}, " +
                                            $"Output: {chunk.Completion.Usage.CompletionTokens}, " +
                                            $"Total: {chunk.Completion.Usage.TotalTokens}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n\n" + "=".PadRight(70, '='));
                Console.WriteLine("💥 FATAL ERROR DURING AGENT EXECUTION");
                Console.WriteLine("=".PadRight(70, '='));
                Console.WriteLine($"Exception Type: {ex.GetType().Name}");
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine($"\nStack Trace:");
                Console.WriteLine(ex.StackTrace ?? "(no stack trace)");
                
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"\nInner Exception: {ex.InnerException.GetType().Name}");
                    Console.WriteLine($"Inner Message: {ex.InnerException.Message}");
                    Console.WriteLine($"Inner Stack Trace:");
                    Console.WriteLine(ex.InnerException.StackTrace ?? "(no stack trace)");
                }
                
                Console.WriteLine("\nCONTEXT:");
                Console.WriteLine($"Iteration: {iteration}/{_maxIterations}");
                Console.WriteLine($"Model: {_model}");
                Console.WriteLine($"Files read so far: {_fileTools.GetReadFiles().Count()}");
                Console.WriteLine($"Has written llms.txt: {_writeTool.HasWritten}");
                Console.WriteLine($"Conversation messages: {_conversationHistory.Count}");
                
                Console.WriteLine("\nLAST TOOL CALLS:");
                foreach (var (name, result) in toolCalls.TakeLast(5))
                {
                    Console.WriteLine($"  - {name}: {TruncateForDisplay(result, 100)}");
                }
                
                Console.WriteLine("\n" + "=".PadRight(70, '='));
                
                // Re-throw to crash the application
                throw;
            }

            var responseText = response.ToString();
            
            if (hasContent || toolCalls.Any())
            {
                _conversationHistory.Add(Message.FromAssistant(responseText));
            }

            if (_writeTool.HasWritten)
            {
                Console.WriteLine("\n\n" + "=".PadRight(60, '='));
                Console.WriteLine("✅ AGENT COMPLETED SUCCESSFULLY!");
                Console.WriteLine("=".PadRight(60, '='));
                Console.WriteLine($"\n📝 Files read during analysis: {_fileTools.GetReadFiles().Count()}");
                Console.WriteLine($"🔄 Iterations used: {iteration}/{_maxIterations}");
                Console.WriteLine($"\n🎉 llms.txt has been generated!");
                
                // Give the agent one more chance to see the success message
                if (!string.IsNullOrEmpty(responseText) || hasContent)
                {
                    Console.WriteLine($"\n📋 Final agent response: {TruncateForDisplay(responseText, 300)}");
                }
                
                return true;
            }

            // Check if we're stuck (agent keeps trying after WriteLlmsTxt was called)
            if (iteration > 5 && toolCalls.Any(t => t.name == "WriteLlmsTxt"))
            {
                Console.WriteLine("\n⚠️  Warning: WriteLlmsTxt was called but agent hasn't stopped.");
                Console.WriteLine("Checking if file was actually written...");
                
                if (_writeTool.HasWritten)
                {
                    Console.WriteLine("✅ File WAS written successfully. Stopping agent.");
                    return true;
                }
            }

            if (iteration >= _maxIterations)
            {
                Console.WriteLine("\n\n⚠️  Reached maximum iterations without completing.");
                Console.WriteLine("The agent may need more iterations or there might be an issue.");
                return false;
            }
        }

        return false;
    }

    private static string TruncateForDisplay(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text))
            return "(empty)";

        text = text.Replace("\n", " ").Replace("\r", "");
        
        if (text.Length <= maxLength)
            return text;

        return text.Substring(0, maxLength) + "...";
    }
}
