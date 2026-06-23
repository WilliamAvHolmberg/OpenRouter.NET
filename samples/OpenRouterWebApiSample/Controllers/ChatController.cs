using Microsoft.AspNetCore.Mvc;
using OpenRouter.NET;
using OpenRouter.NET.Models;
using OpenRouter.NET.Tools;
using OpenRouter.NET.Events;
using OpenRouterWebApi.Models;
using System.Text;

namespace OpenRouterWebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly OpenRouterClient _client;
    private readonly ILogger<ChatController> _logger;

    public ChatController(OpenRouterClient client, ILogger<ChatController> logger)
    {
        _client = client;
        _logger = logger;
    }

    [HttpPost("basic")]
    public async Task<ActionResult<ChatResponse>> BasicChat([FromBody] ChatRequest request)
    {
        try
        {
            var messages = new List<Message>();

            if (!string.IsNullOrEmpty(request.SystemPrompt))
            {
                messages.Add(Message.FromSystem(request.SystemPrompt));
            }

            messages.Add(Message.FromUser(request.Message));

            var chatRequest = new ChatCompletionRequest
            {
                Model = request.Model,
                Messages = messages,
                Temperature = request.Temperature,
                MaxTokens = request.MaxTokens
            };

            var response = await _client.CreateChatCompletionAsync(chatRequest);

            return Ok(new ChatResponse(
                Content: response.Choices?[0]?.Message?.Content?.ToString() ?? "No response",
                Model: response.Model ?? request.Model,
                FinishReason: response.Choices?[0]?.FinishReason,
                Usage: response.Usage != null ? new TokenUsage(
                    response.Usage.TotalTokens,
                    response.Usage.PromptTokens,
                    response.Usage.CompletionTokens
                ) : null
            ));
        }
        catch (OpenRouterException ex)
        {
            _logger.LogError(ex, "OpenRouter API error");
            return StatusCode(ex.StatusCode ?? 500, new { error = ex.Message, code = ex.ErrorCode });
        }
    }

    [HttpPost("stream")]
    public async Task StreamChat([FromBody] StreamChatRequest request)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        try
        {
            var messages = new List<Message>();

            if (!string.IsNullOrEmpty(request.SystemPrompt))
            {
                messages.Add(Message.FromSystem(request.SystemPrompt));
            }

            messages.Add(Message.FromUser(request.Message));

            var chatRequest = new ChatCompletionRequest
            {
                Model = request.Model,
                Messages = messages
            };

            await foreach (var chunk in _client.StreamAsync(chatRequest))
            {
                if (chunk.TextDelta != null)
                {
                    await Response.WriteAsync($"data: {chunk.TextDelta}\n\n");
                    await Response.Body.FlushAsync();
                }

                if (chunk.Completion != null)
                {
                    await Response.WriteAsync($"data: [DONE] {chunk.Completion.FinishReason}\n\n");
                    await Response.Body.FlushAsync();
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Streaming error");
            await Response.WriteAsync($"data: [ERROR] {ex.Message}\n\n");
        }
    }

    [HttpPost("conversation")]
    public async Task<ActionResult<ChatResponse>> Conversation([FromBody] ConversationRequest request)
    {
        try
        {
            var messages = request.Messages.Select(m => new Message
            {
                Role = m.Role,
                Content = m.Content
            }).ToList();

            var chatRequest = new ChatCompletionRequest
            {
                Model = request.Model,
                Messages = messages,
                Temperature = request.Temperature
            };

            var response = await _client.CreateChatCompletionAsync(chatRequest);

            return Ok(new ChatResponse(
                Content: response.Choices?[0]?.Message?.Content?.ToString() ?? "No response",
                Model: response.Model ?? request.Model,
                FinishReason: response.Choices?[0]?.FinishReason,
                Usage: response.Usage != null ? new TokenUsage(
                    response.Usage.TotalTokens,
                    response.Usage.PromptTokens,
                    response.Usage.CompletionTokens
                ) : null
            ));
        }
        catch (OpenRouterException ex)
        {
            _logger.LogError(ex, "OpenRouter API error");
            return StatusCode(ex.StatusCode ?? 500, new { error = ex.Message, code = ex.ErrorCode });
        }
    }

    [HttpPost("artifacts")]
    public async Task<ActionResult<ArtifactResponse>> GenerateWithArtifacts([FromBody] ArtifactRequest request)
    {
        try
        {
            var chatRequest = new ChatCompletionRequest
            {
                Model = request.Model,
                Messages = new List<Message> { Message.FromUser(request.Prompt) }
            };

            chatRequest.EnableArtifactSupport();

            var responseText = new StringBuilder();
            var artifacts = new List<ArtifactInfo>();

            await foreach (var chunk in _client.StreamAsync(chatRequest))
            {
                if (chunk.TextDelta != null)
                {
                    responseText.Append(chunk.TextDelta);
                }

                if (chunk.Artifact is ArtifactCompleted completed)
                {
                    artifacts.Add(new ArtifactInfo(
                        Title: completed.Title ?? "Untitled",
                        Type: completed.Type ?? "unknown",
                        Content: completed.Content ?? "",
                        Language: completed.Language
                    ));
                }
            }

            return Ok(new ArtifactResponse(
                Text: responseText.ToString(),
                Artifacts: artifacts
            ));
        }
        catch (OpenRouterException ex)
        {
            _logger.LogError(ex, "OpenRouter API error");
            return StatusCode(ex.StatusCode ?? 500, new { error = ex.Message, code = ex.ErrorCode });
        }
    }

    [HttpPost("multimodal")]
    public async Task<ActionResult<ChatResponse>> MultimodalChat([FromBody] MultimodalRequest request)
    {
        try
        {
            var contentParts = new List<ContentPart>
            {
                new TextContent(request.Message)
            };

            foreach (var imageUrl in request.ImageUrls)
            {
                contentParts.Add(new ImageContent(imageUrl));
            }

            var chatRequest = new ChatCompletionRequest
            {
                Model = request.Model,
                Messages = new List<Message>
                {
                    Message.FromUser(contentParts)
                }
            };

            var response = await _client.CreateChatCompletionAsync(chatRequest);

            return Ok(new ChatResponse(
                Content: response.Choices?[0]?.Message?.Content?.ToString() ?? "No response",
                Model: response.Model ?? request.Model,
                FinishReason: response.Choices?[0]?.FinishReason,
                Usage: response.Usage != null ? new TokenUsage(
                    response.Usage.TotalTokens,
                    response.Usage.PromptTokens,
                    response.Usage.CompletionTokens
                ) : null
            ));
        }
        catch (OpenRouterException ex)
        {
            _logger.LogError(ex, "OpenRouter API error");
            return StatusCode(ex.StatusCode ?? 500, new { error = ex.Message, code = ex.ErrorCode });
        }
    }

    [HttpPost("tools")]
    public async Task<ActionResult<string>> ChatWithTools([FromBody] ToolRequest request)
    {
        try
        {
            var calculator = new OpenRouterWebApi.Tools.CalculatorTools();

            _client.RegisterTool(calculator, nameof(calculator.Add));
            _client.RegisterTool(calculator, nameof(calculator.Multiply));
            _client.RegisterTool(calculator, nameof(calculator.Subtract));
            _client.RegisterTool(calculator, nameof(calculator.Divide));

            var chatRequest = new ChatCompletionRequest
            {
                Model = request.Model,
                Messages = new List<Message> { Message.FromUser(request.Message) }
            };

            var responseText = new StringBuilder();

            await foreach (var chunk in _client.StreamAsync(chatRequest))
            {
                if (chunk.ServerTool != null)
                {
                    _logger.LogInformation(
                        "Tool {ToolName} - State: {State}, Result: {Result}",
                        chunk.ServerTool.ToolName,
                        chunk.ServerTool.State,
                        chunk.ServerTool.Result
                    );
                }

                if (chunk.TextDelta != null)
                {
                    responseText.Append(chunk.TextDelta);
                }
            }

            return Ok(responseText.ToString());
        }
        catch (OpenRouterException ex)
        {
            _logger.LogError(ex, "OpenRouter API error");
            return StatusCode(ex.StatusCode ?? 500, new { error = ex.Message, code = ex.ErrorCode });
        }
    }

    [HttpPost("tools/stream")]
    public async Task StreamChatWithTools([FromBody] ToolRequest request)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        try
        {
            _logger.LogInformation("Starting tool streaming for model: {Model}, message: {Message}", 
                request.Model, request.Message);

            var calculator = new OpenRouterWebApi.Tools.CalculatorTools();

            _client.RegisterTool(calculator, nameof(calculator.Add));
            _client.RegisterTool(calculator, nameof(calculator.Multiply));
            _client.RegisterTool(calculator, nameof(calculator.Subtract));
            _client.RegisterTool(calculator, nameof(calculator.Divide));

            _logger.LogInformation("Registered 4 calculator tools");

            // Subscribe to streaming events
            _client.OnStreamEvent += async (sender, e) =>
            {
                _logger.LogInformation("Stream Event: {EventType}, State: {State}", e.EventType, e.State);

                var eventData = e.EventType switch
                {
                    StreamEventType.StateChange => (object)new
                    {
                        type = "stateChange",
                        state = e.State.ToString()
                    },
                    StreamEventType.TextContent => (object)new
                    {
                        type = "text",
                        content = e.TextDelta
                    },
                    StreamEventType.ToolCall => (object)new
                    {
                        type = "toolCall",
                        toolName = e.ToolName,
                        toolCall = e.ToolCall
                    },
                    StreamEventType.ToolResult => (object)new
                    {
                        type = "toolResult",
                        toolName = e.ToolName,
                        result = e.ToolResult
                    },
                    StreamEventType.Error => (object)new
                    {
                        type = "error",
                        error = e.ToolResult
                    },
                    _ => null
                };

                if (eventData != null)
                {
                    var eventJson = System.Text.Json.JsonSerializer.Serialize(eventData);
                    await Response.WriteAsync($"event: {e.EventType}\ndata: {eventJson}\n\n");
                    await Response.Body.FlushAsync();
                }
            };

            var chatRequest = new ChatCompletionRequest
            {
                Model = request.Model,
                Messages = new List<Message> { Message.FromUser(request.Message) },
                Stream = true
            };

            _logger.LogInformation("Starting ProcessMessageAsync with event handler...");
            
            var (response, history) = await _client.ProcessMessageAsync(chatRequest, maxToolCalls: 5);

            _logger.LogInformation("ProcessMessageAsync completed. History count: {Count}", history.Count);

            var completeEvent = new
            {
                type = "complete",
                content = response.GetContent(),
                finishReason = response.GetFinishReason()
            };

            var completeJson = System.Text.Json.JsonSerializer.Serialize(completeEvent);
            await Response.WriteAsync($"event: complete\ndata: {completeJson}\n\n");
            await Response.Body.FlushAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ERROR in streaming with tools: {Message}", ex.Message);
            var errorEvent = new
            {
                type = "error",
                message = ex.Message,
                stackTrace = ex.StackTrace
            };
            var eventJson = System.Text.Json.JsonSerializer.Serialize(errorEvent);
            await Response.WriteAsync($"event: error\ndata: {eventJson}\n\n");
        }
    }
}

