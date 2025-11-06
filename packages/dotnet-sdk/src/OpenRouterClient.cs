using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenRouter.NET.Events;
using OpenRouter.NET.Internal;
using OpenRouter.NET.Models;
using OpenRouter.NET.Tools;

namespace OpenRouter.NET;

public class OpenRouterClient
{
    private readonly HttpClient _httpClient;
    private readonly bool _disposeHttpClient;
    private readonly Action<string>? _logCallback;
    private readonly JsonSerializerOptions _jsonOptions;

    // Handler classes for specialized functionality
    private readonly HttpRequestHandler _httpHandler;
    private readonly ToolManager _toolManager;
    private readonly StreamingHandler _streamingHandler;
    private readonly ObjectGenerator _objectGenerator;

    public event EventHandler<StreamEventArgs>? OnStreamEvent;
    public static string Version => "0.1.0";

    // Internal accessor for testing
    internal ObjectGenerator ObjectGeneratorForTesting => _objectGenerator;

    public OpenRouterClient(string apiKey) : this(new OpenRouterClientOptions { ApiKey = apiKey })
    {
    }

    public OpenRouterClient(OpenRouterClientOptions options)
    {
        if (string.IsNullOrEmpty(options.ApiKey))
        {
            throw new ArgumentException("API key is required", nameof(options));
        }

        _logCallback = options.OnLogMessage;

        if (options.HttpClient != null)
        {
            _httpClient = options.HttpClient;
            _disposeHttpClient = false;
        }
        else
        {
            _httpClient = new HttpClient();
            _disposeHttpClient = true;
        }

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

        // Initialize handler classes
        _httpHandler = new HttpRequestHandler(
            _httpClient,
            options.ApiKey,
            options.BaseUrl,
            options.SiteUrl,
            options.SiteName,
            _jsonOptions);

        _toolManager = new ToolManager(_jsonOptions);
        _streamingHandler = new StreamingHandler(_httpHandler, _toolManager, _logCallback);
        _objectGenerator = new ObjectGenerator(CreateChatCompletionAsync, _jsonOptions, _logCallback);
    }

    public OpenRouterClient RegisterTool(
        string name,
        Func<string, object>? implementation,
        string description,
        object parameters,
        ToolMode mode = ToolMode.AutoExecute)
    {
        _toolManager.RegisterTool(name, implementation, description, parameters, mode);
        return this;
    }

    public object ExecuteTool(string name, string arguments)
    {
        return _toolManager.ExecuteTool(name, arguments);
    }

    public List<Tool> GetTools() => _toolManager.GetAllTools();

    public async Task<ChatCompletionResponse> CreateChatCompletionAsync(
        ChatCompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        if (_toolManager.ToolCount > 0 && request.Tools == null)
        {
            request.Tools = _toolManager.GetAllTools();
        }

        // Apply default reasoning: lowest effort and excluded, if not specified
        if (request.Reasoning == null)
        {
            request.Reasoning = new Models.ReasoningConfig
            {
                Effort = "low",
                Exclude = true,
                Enabled = true
            };
        }

        var requestContent = JsonSerializer.Serialize(request, _jsonOptions);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_httpHandler.BaseUrl}/chat/completions")
        {
            Content = new StringContent(requestContent, Encoding.UTF8, "application/json")
        };

        _httpHandler.AddHeaders(httpRequest);

        var response = await _httpHandler.HttpClient.SendAsync(httpRequest, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync();

        Log($"OpenRouter API Response: {responseContent}");

        await _httpHandler.HandleErrorResponse(response);

        try
        {
            var result = JsonSerializer.Deserialize<ChatCompletionResponse>(responseContent, _jsonOptions)!;
            Log($"Deserialized response - Choices count: {result?.Choices?.Count ?? 0}");
            return result;
        }
        catch (JsonException ex)
        {
            Log($"JSON parsing error: {ex.Message}");
            throw new OpenRouterException($"Failed to parse OpenRouter API response: {ex.Message}. Response content: '{responseContent}'", ex);
        }
    }

    public async IAsyncEnumerable<Streaming.StreamChunk> StreamAsync(
        ChatCompletionRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var chunk in _streamingHandler.StreamAsync(request, cancellationToken))
        {
            yield return chunk;
        }
    }

    public async Task<List<ModelInfo>> GetModelsAsync(CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_httpHandler.BaseUrl}/models");
        _httpHandler.AddHeaders(request);

        try
        {
            var response = await _httpHandler.HttpClient.SendAsync(request, cancellationToken);
            await _httpHandler.HandleErrorResponse(response);

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var modelsResponse = JsonSerializer.Deserialize<ModelsResponse>(json, _jsonOptions);

            return modelsResponse?.Data ?? new List<ModelInfo>();
        }
        catch (OpenRouterException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new OpenRouterException("Failed to fetch models", ex);
        }
    }

    public async Task<UserLimits> GetLimitsAsync(CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_httpHandler.BaseUrl}/auth/key");
        _httpHandler.AddHeaders(request);

        try
        {
            var response = await _httpHandler.HttpClient.SendAsync(request, cancellationToken);
            await _httpHandler.HandleErrorResponse(response);

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, _jsonOptions);

            if (data != null && data.ContainsKey("data"))
            {
                var limitsJson = data["data"].GetRawText();
                var limits = JsonSerializer.Deserialize<UserLimits>(limitsJson, _jsonOptions);
                return limits ?? new UserLimits();
            }

            return new UserLimits();
        }
        catch (OpenRouterException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new OpenRouterException("Failed to fetch limits", ex);
        }
    }

    public async Task<GenerationInfo> GetGenerationAsync(string generationId, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_httpHandler.BaseUrl}/generation?id={generationId}");
        _httpHandler.AddHeaders(request);

        try
        {
            var response = await _httpHandler.HttpClient.SendAsync(request, cancellationToken);
            await _httpHandler.HandleErrorResponse(response);

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, _jsonOptions);

            if (data != null && data.ContainsKey("data"))
            {
                var generationJson = data["data"].GetRawText();
                var generation = JsonSerializer.Deserialize<GenerationInfo>(generationJson, _jsonOptions);
                return generation ?? throw new OpenRouterException("Failed to deserialize generation info");
            }

            throw new OpenRouterException("Invalid response format");
        }
        catch (OpenRouterException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new OpenRouterException("Failed to fetch generation info", ex);
        }
    }

    private void RaiseEvent(
        StreamEventType eventType,
        StreamState state,
        string? toolName = null,
        string? textDelta = null,
        ToolCall? toolCall = null,
        string? toolResult = null,
        object? originalResponse = null)
    {
        OnStreamEvent?.Invoke(this, new StreamEventArgs
        {
            EventType = eventType,
            State = state,
            ToolName = toolName,
            TextDelta = textDelta,
            ToolCall = toolCall,
            ToolResult = toolResult,
            OriginalResponse = originalResponse
        });
    }

    public async Task<GenerateObjectResult> GenerateObjectAsync(
        JsonElement schema,
        string prompt,
        string model,
        GenerateObjectOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return await _objectGenerator.GenerateObjectAsync(schema, prompt, model, options, cancellationToken);
    }

    /// <summary>
    /// Generates a strongly-typed object matching the provided C# type using LLM structured output.
    /// </summary>
    public async Task<GenerateObjectResult<T>> GenerateObjectAsync<T>(
        string prompt,
        string model,
        GenerateObjectOptions? options = null,
        CancellationToken cancellationToken = default) where T : class
    {
        return await _objectGenerator.GenerateObjectAsync<T>(prompt, model, options, cancellationToken);
    }

    private void Log(string message)
    {
        _logCallback?.Invoke(message);
    }

    public void Dispose()
    {
        if (_disposeHttpClient)
        {
            _httpClient?.Dispose();
        }
    }
}
