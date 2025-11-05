using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Json.Schema;
using OpenRouter.NET.Events;
using OpenRouter.NET.Models;
using OpenRouter.NET.Tools;

namespace OpenRouter.NET;

public class OpenRouterClient
{
    private readonly HttpClient _httpClient;
    private readonly bool _disposeHttpClient;
    private readonly string _apiKey;
    private readonly string? _siteUrl;
    private readonly string? _siteName;
    private readonly string _baseUrl;
    private readonly Action<string>? _logCallback;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly List<Tool> _tools = new List<Tool>();
    private readonly Dictionary<string, Func<string, object>> _toolImplementations = new Dictionary<string, Func<string, object>>();
    private readonly Dictionary<string, ToolRegistration> _toolRegistry = new Dictionary<string, ToolRegistration>();
    private static readonly ConcurrentDictionary<Type, JsonElement> _schemaCache = new ConcurrentDictionary<Type, JsonElement>();
    
    public event EventHandler<StreamEventArgs>? OnStreamEvent;
    public static string Version => "0.1.0";

    public OpenRouterClient(string apiKey) : this(new OpenRouterClientOptions { ApiKey = apiKey })
    {
    }

    public OpenRouterClient(OpenRouterClientOptions options)
    {
        if (string.IsNullOrEmpty(options.ApiKey))
        {
            throw new ArgumentException("API key is required", nameof(options));
        }

        _apiKey = options.ApiKey;
        _siteUrl = options.SiteUrl;
        _siteName = options.SiteName;
        _baseUrl = options.BaseUrl;
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
    }

    public OpenRouterClient RegisterTool(
        string name,
        Func<string, object>? implementation,
        string description,
        object parameters,
        ToolMode mode = ToolMode.AutoExecute)
    {
        var tool = Tool.CreateFunctionTool(name, description, parameters);
        _tools.Add(tool);
        
        var registration = new ToolRegistration
        {
            Name = name,
            Mode = mode,
            Handler = implementation,
            Schema = parameters
        };
        
        _toolRegistry[name] = registration;
        
        if (implementation != null && mode == ToolMode.AutoExecute)
        {
            _toolImplementations[name] = implementation;
        }
        
        return this;
    }

    public object ExecuteTool(string name, string arguments)
    {
        if (!_toolImplementations.TryGetValue(name, out var implementation))
        {
            throw new InvalidOperationException($"Tool '{name}' is not registered");
        }

        if (string.IsNullOrEmpty(arguments))
        {
            arguments = "{}";
        }

        if (!arguments.Trim().StartsWith("{") && !arguments.Trim().StartsWith("["))
        {
            arguments = "{" + arguments + "}";
        }

        try
        {
            using (var doc = JsonDocument.Parse(arguments)) { }
        }
        catch (JsonException)
        {
            arguments = EnsureValidJson(arguments);
        }

        return implementation(arguments);
    }

    private string EnsureValidJson(string json)
    {
        int openBraces = json.Count(c => c == '{');
        int closeBraces = json.Count(c => c == '}');

        if (openBraces > closeBraces)
        {
            json += new string('}', openBraces - closeBraces);
        }

        return json;
    }

    public List<Tool> GetTools() => _tools.ToList();

    public async Task<ChatCompletionResponse> CreateChatCompletionAsync(
        ChatCompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        if (_tools.Count > 0 && request.Tools == null)
        {
            request.Tools = _tools;
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

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/chat/completions")
        {
            Content = new StringContent(requestContent, Encoding.UTF8, "application/json")
        };

        AddHeaders(httpRequest);

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync();

        Log($"OpenRouter API Response: {responseContent}");

        await HandleErrorResponse(response);

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
        if (request == null) throw new ArgumentNullException(nameof(request));

        var config = request.ToolLoopConfig ?? new ToolLoopConfig { Enabled = true, MaxIterations = 5 };
        
        if (!config.Enabled || _tools.Count == 0)
        {
            await foreach (var chunk in StreamAsyncInternal(request, cancellationToken))
            {
                yield return chunk;
            }
            yield break;
        }

        // Apply default reasoning for streaming as well
        if (request.Reasoning == null)
        {
            request.Reasoning = new Models.ReasoningConfig
            {
                Effort = "low",
                Exclude = true,
                Enabled = true
            };
        }

        var toolCallsCount = 0;
        var hasToolCalls = false;
        var conversationHistory = request.Messages.ToList();

        do
        {
            request.Messages = conversationHistory;
            
            var completeMessage = new Message
            {
                Role = "assistant",
                Content = "",
                ToolCalls = null
            };
            
            var contentBuilder = new StringBuilder();
            var toolCallsAccumulator = new Dictionary<int, ToolCall>();

            await foreach (var chunk in StreamAsyncInternal(request, cancellationToken))
            {
                yield return chunk;

                if (chunk.TextDelta != null)
                {
                    contentBuilder.Append(chunk.TextDelta);
                }

                if (chunk.ToolCallDelta != null)
                {
                    var toolCallDelta = chunk.ToolCallDelta;
                    if (!toolCallsAccumulator.ContainsKey(toolCallDelta.Index))
                    {
                        toolCallsAccumulator[toolCallDelta.Index] = new ToolCall
                        {
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
                        if (toolCallDelta.Function?.Name != null)
                        {
                            existing.Function!.Name = toolCallDelta.Function.Name;
                        }
                        if (toolCallDelta.Function?.Arguments != null)
                        {
                            existing.Function!.Arguments += toolCallDelta.Function.Arguments;
                        }
                    }
                }

                if (chunk.Completion?.FinishReason == "tool_calls")
                {
                    hasToolCalls = true;
                }
            }

            completeMessage.Content = contentBuilder.ToString();
            
            if (toolCallsAccumulator.Count > 0)
            {
                completeMessage.ToolCalls = toolCallsAccumulator.Values.ToArray();
                hasToolCalls = true;
            }
            else
            {
                hasToolCalls = false;
            }

            conversationHistory.Add(completeMessage);
        

            if (hasToolCalls && completeMessage.ToolCalls != null)
            {
                // Check if we've already hit max iterations before executing
                if (toolCallsCount >= config.MaxIterations)
                {
                    yield return new Streaming.StreamChunk
                    {
                        IsFirstChunk = false,
                        ElapsedTime = TimeSpan.Zero,
                        ChunkIndex = 0,
                        TextDelta = $"\n\n[Max tool iterations ({config.MaxIterations}) reached]",
                        Raw = null!
                    };
                    break;
                }

                // Increment after the check passes
                toolCallsCount++;

                foreach (var toolCall in completeMessage.ToolCalls)
                {
                    if (toolCall.Function == null || string.IsNullOrEmpty(toolCall.Function.Name))
                        continue;

                    var toolName = toolCall.Function.Name;
                    
                    if (!_toolRegistry.TryGetValue(toolName, out var registration))
                    {
                        var errorMsg = $"Tool '{toolName}' is not registered";
                        yield return new Streaming.StreamChunk
                        {
                            IsFirstChunk = false,
                            ElapsedTime = TimeSpan.Zero,
                            ChunkIndex = 0,
                            ServerTool = new ServerToolCall
                            {
                                ToolName = toolName,
                                ToolId = toolCall.Id!,
                                Arguments = toolCall.Function.Arguments!,
                                State = ToolCallState.Error,
                                Error = errorMsg
                            },
                            Raw = null!
                        };
                        
                        conversationHistory.Add(new Message
                        {
                            Role = "tool",
                            ToolCallId = toolCall.Id,
                            Content = errorMsg
                        });
                        continue;
                    }

                    if (registration.Mode == ToolMode.ClientSide)
                    {
                        yield return new Streaming.StreamChunk
                        {
                            IsFirstChunk = false,
                            ElapsedTime = TimeSpan.Zero,
                            ChunkIndex = 0,
                            ClientTool = new ClientToolCall
                            {
                                ToolName = toolName,
                                ToolId = toolCall.Id!,
                                Arguments = toolCall.Function.Arguments!
                            },
                            Raw = null!
                        };

                        conversationHistory.Add(new Message
                        {
                            Role = "tool",
                            ToolCallId = toolCall.Id,
                            Content = "Client tool executed"
                        });
                        continue;
                    }

                    var executionStopwatch = System.Diagnostics.Stopwatch.StartNew();
                    
                    yield return new Streaming.StreamChunk
                    {
                        IsFirstChunk = false,
                        ElapsedTime = TimeSpan.Zero,
                        ChunkIndex = 0,
                        ServerTool = new ServerToolCall
                        {
                            ToolName = toolName,
                            ToolId = toolCall.Id!,
                            Arguments = toolCall.Function.Arguments!,
                            State = ToolCallState.Executing
                        },
                        Raw = null!
                    };

                    Streaming.StreamChunk resultChunk;
                    Message toolResultMessage;
                    
                    try
                    {
                        var result = ExecuteTool(toolName, toolCall.Function.Arguments!);
                        executionStopwatch.Stop();

                        var resultString = result is string str
                            ? str
                            : JsonSerializer.Serialize(result, _jsonOptions);

                        resultChunk = new Streaming.StreamChunk
                        {
                            IsFirstChunk = false,
                            ElapsedTime = TimeSpan.Zero,
                            ChunkIndex = 0,
                            ServerTool = new ServerToolCall
                            {
                                ToolName = toolName,
                                ToolId = toolCall.Id!,
                                Arguments = toolCall.Function.Arguments!,
                                State = ToolCallState.Completed,
                                Result = resultString,
                                ExecutionTime = executionStopwatch.Elapsed
                            },
                            Raw = null!
                        };

                        toolResultMessage = new Message
                        {
                            Role = "tool",
                            ToolCallId = toolCall.Id,
                            Content = resultString
                        };
                    }
                    catch (Exception ex)
                    {
                        executionStopwatch.Stop();
                        var errorMessage = $"Error executing tool: {ex.Message}";

                        resultChunk = new Streaming.StreamChunk
                        {
                            IsFirstChunk = false,
                            ElapsedTime = TimeSpan.Zero,
                            ChunkIndex = 0,
                            ServerTool = new ServerToolCall
                            {
                                ToolName = toolName,
                                ToolId = toolCall.Id!,
                                Arguments = toolCall.Function.Arguments!,
                                State = ToolCallState.Error,
                                Error = errorMessage,
                                ExecutionTime = executionStopwatch.Elapsed
                            },
                            Raw = null!
                        };

                        toolResultMessage = new Message
                        {
                            Role = "tool",
                            ToolCallId = toolCall.Id,
                            Content = errorMessage
                        };
                    }
                    
                    yield return resultChunk;
                    conversationHistory.Add(toolResultMessage);
                }
            }
            else
            {
                hasToolCalls = false;
            }

        } while (hasToolCalls && toolCallsCount < config.MaxIterations);
    }

    private async IAsyncEnumerable<Streaming.StreamChunk> StreamAsyncInternal(
        ChatCompletionRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        if (_tools.Count > 0 && request.Tools == null)
        {
            request.Tools = _tools;
        }

        request.Stream = true;

        var requestContent = JsonSerializer.Serialize(request, _jsonOptions);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/chat/completions")
        {
            Content = new StringContent(requestContent, Encoding.UTF8, "application/json")
        };

        AddHeaders(httpRequest);

        var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        await HandleErrorResponse(response);

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        System.Diagnostics.Stopwatch? stopwatch = null;
        var chunkIndex = 0;
        var isFirstChunk = true;
        var artifactParser = new Parsing.ArtifactParser();

        string? line;
        while ((line = await reader.ReadLineAsync()) != null && !cancellationToken.IsCancellationRequested)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith(":"))
                continue;

            if (!line.StartsWith("data: "))
                continue;

            var data = line.Substring("data: ".Length);

            if (data == "[DONE]")
            {
                break;
            }

            // Start stopwatch on first actual data chunk
            if (stopwatch == null)
            {
                stopwatch = System.Diagnostics.Stopwatch.StartNew();
            }

            var chunksToYield = new List<Streaming.StreamChunk>();
            
            try
            {
                var rawChunk = JsonSerializer.Deserialize<ChatCompletionStreamResponse>(data, _jsonOptions);
                if (rawChunk == null) continue;

                var choice = rawChunk.Choices?.FirstOrDefault();
                var delta = choice?.Delta;

                // Parse artifacts from text content
                if (delta?.Content != null)
                {
                    var parseResult = artifactParser.ParseIncremental(delta.Content);
                    var currentIsFirstChunk = isFirstChunk;
                    
                    // Collect artifact started event
                    if (parseResult.ArtifactStarted != null)
                    {
                        chunksToYield.Add(new Streaming.StreamChunk
                        {
                            IsFirstChunk = currentIsFirstChunk,
                            ElapsedTime = stopwatch?.Elapsed ?? TimeSpan.Zero,
                            ChunkIndex = chunkIndex++,
                            Artifact = parseResult.ArtifactStarted,
                            Raw = rawChunk
                        });
                        currentIsFirstChunk = false;
                    }
                    
                    // Collect artifact content
                    if (parseResult.ArtifactContent != null)
                    {
                        chunksToYield.Add(new Streaming.StreamChunk
                        {
                            IsFirstChunk = false,
                            ElapsedTime = stopwatch?.Elapsed ?? TimeSpan.Zero,
                            ChunkIndex = chunkIndex++,
                            Artifact = parseResult.ArtifactContent,
                            Raw = rawChunk
                        });
                    }
                    
                    // Collect artifact completed
                    if (parseResult.ArtifactCompleted != null)
                    {
                        chunksToYield.Add(new Streaming.StreamChunk
                        {
                            IsFirstChunk = false,
                            ElapsedTime = stopwatch?.Elapsed ?? TimeSpan.Zero,
                            ChunkIndex = chunkIndex++,
                            Artifact = parseResult.ArtifactCompleted,
                            Raw = rawChunk
                        });
                    }
                    
                    // Collect text chunk (without artifact XML)
                    if (!string.IsNullOrEmpty(parseResult.TextDelta))
                    {
                        var textChunk = new Streaming.StreamChunk
                        {
                            IsFirstChunk = currentIsFirstChunk,
                            ElapsedTime = stopwatch?.Elapsed ?? TimeSpan.Zero,
                            ChunkIndex = chunkIndex++,
                            TextDelta = parseResult.TextDelta,
                            Raw = rawChunk
                        };
                        
                        if (choice?.FinishReason != null)
                        {
                            textChunk = textChunk with
                            {
                                Completion = new Streaming.CompletionMetadata
                                {
                                    FinishReason = choice.FinishReason,
                                    Model = rawChunk.Model,
                                    Id = rawChunk.Id
                                }
                            };
                        }
                        
                        chunksToYield.Add(textChunk);
                        currentIsFirstChunk = false;
                    }
                    
                    isFirstChunk = currentIsFirstChunk;
                }
                else
                {
                    // No text content, check for other delta properties (tool calls, finish reason)
                    
                    // Handle tool call deltas
                    if (delta?.ToolCalls != null && delta.ToolCalls.Length > 0)
                    {
                        foreach (var toolCall in delta.ToolCalls)
                        {
                            chunksToYield.Add(new Streaming.StreamChunk
                            {
                                IsFirstChunk = isFirstChunk,
                                ElapsedTime = stopwatch?.Elapsed ?? TimeSpan.Zero,
                                ChunkIndex = chunkIndex++,
                                ToolCallDelta = toolCall,
                                Raw = rawChunk
                            });
                        }
                    }
                    
                    // Handle finish reason - always emit if present, even with tool calls
                    if (choice?.FinishReason != null)
                    {
                        chunksToYield.Add(new Streaming.StreamChunk
                        {
                            IsFirstChunk = isFirstChunk,
                            ElapsedTime = stopwatch?.Elapsed ?? TimeSpan.Zero,
                            ChunkIndex = chunkIndex++,
                            Completion = new Streaming.CompletionMetadata
                            {
                                FinishReason = choice.FinishReason,
                                Model = rawChunk.Model,
                                Id = rawChunk.Id
                            },
                            Raw = rawChunk
                        });
                    }
                    else if (delta?.ToolCalls == null || delta.ToolCalls.Length == 0)
                    {
                        // Emit empty chunk only if no content and no finish reason
                        chunksToYield.Add(new Streaming.StreamChunk
                        {
                            IsFirstChunk = isFirstChunk,
                            ElapsedTime = stopwatch?.Elapsed ?? TimeSpan.Zero,
                            ChunkIndex = chunkIndex++,
                            Raw = rawChunk
                        });
                    }
                }

                // Only set isFirstChunk to false if we actually yielded chunks
                if (chunksToYield.Count > 0)
                {
                    isFirstChunk = false;
                }
            }
            catch (JsonException)
            {
                // Skip malformed chunks
                continue;
            }

            foreach (var chunkToYield in chunksToYield)
            {
                yield return chunkToYield;
            }
        }
    }

    public async Task<List<ModelInfo>> GetModelsAsync(CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/models");
        AddHeaders(request);

        try
        {
            var response = await _httpClient.SendAsync(request, cancellationToken);
            await HandleErrorResponse(response);
            
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
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/auth/key");
        AddHeaders(request);

        try
        {
            var response = await _httpClient.SendAsync(request, cancellationToken);
            await HandleErrorResponse(response);
            
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
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/generation?id={generationId}");
        AddHeaders(request);

        try
        {
            var response = await _httpClient.SendAsync(request, cancellationToken);
            await HandleErrorResponse(response);
            
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

    private async Task HandleErrorResponse(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        var content = await response.Content.ReadAsStringAsync();
        ErrorResponse? errorResponse = null;

        try
        {
            errorResponse = JsonSerializer.Deserialize<ErrorResponse>(content, _jsonOptions);
        }
        catch
        {
        }

        var errorMessage = errorResponse?.Error?.Message ?? response.ReasonPhrase ?? "Unknown error";

        switch ((int)response.StatusCode)
        {
            case 401:
                throw new OpenRouterAuthException($"Authentication failed: {errorMessage}");
            case 429:
                int? retryAfter = null;
                if (response.Headers.TryGetValues("Retry-After", out var values))
                {
                    if (int.TryParse(values.First(), out var seconds))
                    {
                        retryAfter = seconds;
                    }
                }
                throw new OpenRouterRateLimitException($"Rate limit exceeded: {errorMessage}", retryAfter);
            case 400:
                throw new OpenRouterBadRequestException($"Bad request: {errorMessage}");
            case 404:
                throw new OpenRouterModelNotFoundException("unknown", $"Not found: {errorMessage}");
            case >= 500:
                throw new OpenRouterServerException($"Server error: {errorMessage}");
            default:
                throw new OpenRouterException($"Request failed with status {response.StatusCode}: {errorMessage}", (int)response.StatusCode);
        }
    }

    private void AddHeaders(HttpRequestMessage request)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (!string.IsNullOrEmpty(_siteUrl))
        {
            request.Headers.Add("HTTP-Referer", _siteUrl);
        }

        if (!string.IsNullOrEmpty(_siteName))
        {
            request.Headers.Add("X-Title", _siteName);
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
        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("Prompt cannot be empty", nameof(prompt));
        
        if (string.IsNullOrWhiteSpace(model))
            throw new ArgumentException("Model cannot be empty", nameof(model));

        options ??= new GenerateObjectOptions();

        var schemaJson = schema.GetRawText();
        var schemaSizeBytes = Encoding.UTF8.GetByteCount(schemaJson);
        
        if (schemaSizeBytes > options.SchemaWarningThresholdBytes)
        {
            Log($"WARNING: Schema size ({schemaSizeBytes} bytes) exceeds threshold ({options.SchemaWarningThresholdBytes} bytes). This may impact performance.");
        }

        var toolName = "generate_structured_output";
        var tool = Tool.CreateFunctionTool(
            name: toolName,
            description: "Generate structured output matching the provided schema",
            parameters: schema
        );

        Exception? lastException = null;

        for (int attempt = 0; attempt < options.MaxRetries; attempt++)
        {
            try
            {
                var request = new ChatCompletionRequest
                {
                    Model = model,
                    Messages = new List<Message>
                    {
                        Message.FromSystem("You are a helpful assistant that generates structured data exactly matching the provided schema. Follow the schema strictly and include all required fields."),
                        Message.FromUser(prompt)
                    },
                    Tools = new List<Tool> { tool },
                    ToolChoice = new { type = "function", function = new { name = toolName } },
                    Temperature = (float?)(options.Temperature ?? 0.7),
                    MaxTokens = options.MaxTokens
                };

                Log($"GenerateObject attempt {attempt + 1}/{options.MaxRetries}");

                var response = await CreateChatCompletionAsync(request, cancellationToken);
                
                var choice = response.Choices?.FirstOrDefault();
                var toolCall = choice?.Message?.ToolCalls?.FirstOrDefault();
                
                if (toolCall?.Function?.Arguments == null)
                {
                    throw new OpenRouterException("No structured output generated by the model");
                }

                var generatedObject = JsonSerializer.Deserialize<JsonElement>(toolCall.Function.Arguments);

                ValidateJsonAgainstSchema(schema, generatedObject);

                Log($"Successfully generated structured object on attempt {attempt + 1}");
                
                return new GenerateObjectResult
                {
                    Object = generatedObject,
                    Usage = response.Usage,
                    FinishReason = choice?.FinishReason
                };
            }
            catch (Exception ex) when (attempt < options.MaxRetries - 1)
            {
                lastException = ex;
                var delayMs = (int)Math.Pow(2, attempt) * 1000;
                Log($"GenerateObject attempt {attempt + 1} failed: {ex.Message}. Retrying in {delayMs}ms...");
                await Task.Delay(delayMs, cancellationToken);
            }
        }

        throw new OpenRouterException(
            $"Failed to generate structured object after {options.MaxRetries} attempts. Last error: {lastException?.Message}",
            lastException!);
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
        // Get or generate schema from type
        var schema = _schemaCache.GetOrAdd(typeof(T), type =>
        {
            var schemaObj = SchemaGenerator.GenerateSchema(type);
            var schemaJson = JsonSerializer.Serialize(schemaObj, _jsonOptions);
            return JsonSerializer.Deserialize<JsonElement>(schemaJson);
        });

        // Call the existing untyped method
        var result = await GenerateObjectAsync(schema, prompt, model, options, cancellationToken);

        // Deserialize to typed object
        var typedObject = JsonSerializer.Deserialize<T>(result.Object.GetRawText(), _jsonOptions);

        if (typedObject == null)
        {
            throw new OpenRouterException("Failed to deserialize generated object to target type");
        }

        return new GenerateObjectResult<T>
        {
            Object = typedObject,
            Usage = result.Usage,
            FinishReason = result.FinishReason
        };
    }

    private void ValidateJsonAgainstSchema(JsonElement schema, JsonElement generatedObject)
    {
        var schemaObj = JsonSerializer.Deserialize<JsonSchema>(schema.GetRawText());
        if (schemaObj == null)
        {
            throw new OpenRouterException("Invalid schema provided for validation");
        }

        var results = schemaObj.Evaluate(generatedObject, new EvaluationOptions 
        { 
            OutputFormat = OutputFormat.Hierarchical 
        });

        if (!results.IsValid)
        {
            var errors = new StringBuilder();
            errors.AppendLine("Generated object does not match schema:");
            
            CollectValidationErrors(results, errors, "");

            throw new OpenRouterException(errors.ToString().TrimEnd());
        }
    }

    private void CollectValidationErrors(EvaluationResults results, StringBuilder errors, string path)
    {
        if (results.Errors != null)
        {
            foreach (var (key, value) in results.Errors)
            {
                errors.AppendLine($"  - {path}{key}: {value}");
            }
        }

        if (results.Details != null)
        {
            foreach (var detail in results.Details)
            {
                var newPath = string.IsNullOrEmpty(path) 
                    ? detail.InstanceLocation.ToString() 
                    : $"{path}/{detail.InstanceLocation}";
                CollectValidationErrors(detail, errors, newPath);
            }
        }
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
