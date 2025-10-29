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

    public Agent(
        OpenRouterClient client,
        string basePath,
        string outputPath,
        string model = "anthropic/claude-3.5-sonnet",
        int maxIterations = 30)
    {
        _client = client;
        _fileTools = new FileSystemTools(basePath);
        _writeTool = new WriteLlmsTxtTool(outputPath);
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
            "Begin your analysis. Start by exploring the codebase structure, then read relevant files, " +
            "and finally generate the comprehensive llms.txt documentation."));

        int iteration = 0;

        while (iteration < _maxIterations && !_writeTool.HasWritten)
        {
            iteration++;
            Console.WriteLine($"\n{'='​,60}");
            Console.WriteLine($"🔄 Iteration {iteration}/{_maxIterations}");
            Console.WriteLine($"{'='​,60}\n");

            var request = new ChatCompletionRequest
            {
                Model = _model,
                Messages = _conversationHistory,
                Temperature = 0.3f,
                MaxTokens = 4000
            };

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

                    if (chunk.ServerTool != null)
                    {
                        switch (chunk.ServerTool.State)
                        {
                            case ToolCallState.Executing:
                                Console.WriteLine($"\n\n🔧 Executing tool: {chunk.ServerTool.ToolName}");
                                Console.WriteLine($"   Arguments: {TruncateForDisplay(chunk.ServerTool.Arguments, 100)}");
                                break;

                            case ToolCallState.Completed:
                                var result = chunk.ServerTool.Result ?? "No result";
                                Console.WriteLine($"✅ Tool completed in {chunk.ServerTool.ExecutionTime?.TotalMilliseconds:F0}ms");
                                Console.WriteLine($"   Result preview: {TruncateForDisplay(result, 200)}");
                                toolCalls.Add((chunk.ServerTool.ToolName, result));
                                break;

                            case ToolCallState.Error:
                                Console.WriteLine($"❌ Tool error: {chunk.ServerTool.Error}");
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
                Console.WriteLine($"\n\n❌ Error during streaming: {ex.Message}");
                return false;
            }

            var responseText = response.ToString();
            
            if (hasContent || toolCalls.Any())
            {
                _conversationHistory.Add(Message.FromAssistant(responseText));
            }

            if (_writeTool.HasWritten)
            {
                Console.WriteLine("\n\n" + "="​.PadRight(60, '='));
                Console.WriteLine("✅ AGENT COMPLETED SUCCESSFULLY!");
                Console.WriteLine("="​.PadRight(60, '='));
                Console.WriteLine($"\n📝 Files read during analysis: {_fileTools.GetReadFiles().Count()}");
                Console.WriteLine($"🔄 Iterations used: {iteration}/{_maxIterations}");
                Console.WriteLine($"\n🎉 llms.txt has been generated!");
                return true;
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
