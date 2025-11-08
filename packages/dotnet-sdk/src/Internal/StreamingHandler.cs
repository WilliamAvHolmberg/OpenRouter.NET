using System.Diagnostics;
using System.Text;
using System.Text.Json;
using OpenRouter.NET.Models;
using OpenRouter.NET.Observability;
using OpenRouter.NET.Parsing;
using OpenRouter.NET.Streaming;
using OpenRouter.NET.Tools;

namespace OpenRouter.NET.Internal;

/// <summary>
/// Handles streaming chat completions with tool loop orchestration
/// </summary>
internal class StreamingHandler
{
    private readonly HttpRequestHandler _httpHandler;
    private readonly ToolManager _toolManager;
    private readonly Action<string>? _logCallback;
    private readonly OpenRouterTelemetryOptions _telemetryOptions;

    public StreamingHandler(
        HttpRequestHandler httpHandler,
        ToolManager toolManager,
        Action<string>? logCallback = null,
        OpenRouterTelemetryOptions? telemetryOptions = null)
    {
        _httpHandler = httpHandler ?? throw new ArgumentNullException(nameof(httpHandler));
        _toolManager = toolManager ?? throw new ArgumentNullException(nameof(toolManager));
        _logCallback = logCallback;
        _telemetryOptions = telemetryOptions ?? OpenRouterTelemetryOptions.Default;
    }

    /// <summary>
    /// Streams chat completion with optional tool loop support
    /// </summary>
    public async IAsyncEnumerable<StreamChunk> StreamAsync(
        ChatCompletionRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        // Start telemetry span if enabled
        using var activity = _telemetryOptions.EnableTelemetry
            ? OpenRouterActivitySource.Instance.StartActivity(GenAiSemanticConventions.SpanNameStream, ActivityKind.Client)
            : null;

        try
        {
            if (activity != null)
            {
                activity.SetTag(GenAiSemanticConventions.AttributeOperationName, GenAiSemanticConventions.OperationStream);
                activity.SetTag(GenAiSemanticConventions.AttributeSystem, GenAiSemanticConventions.SystemValue);
                activity.SetTag(GenAiSemanticConventions.AttributeServerAddress, GenAiSemanticConventions.ServerAddressValue);

                if (!string.IsNullOrEmpty(request.Model))
                {
                    activity.SetTag(GenAiSemanticConventions.AttributeRequestModel, request.Model);
                }

                // Capture prompts if enabled
                if (_telemetryOptions.CapturePrompts && request.Messages?.Count > 0)
                {
                    var requestJson = System.Text.Json.JsonSerializer.Serialize(request, _httpHandler.JsonOptions);
                    TelemetryHelper.EnrichWithRequest(activity, request, requestJson, _telemetryOptions);
                }
            }

        var config = request.ToolLoopConfig ?? new ToolLoopConfig { Enabled = true, MaxIterations = 5 };

        if (!config.Enabled || _toolManager.ToolCount == 0)
        {
            await foreach (var chunk in StreamAsyncInternal(request, cancellationToken))
            {
                yield return chunk;
            }
            yield break;
        }

        // Apply default reasoning for streaming
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
        var lastElapsedTime = TimeSpan.Zero;

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

                // Track the last elapsed time for use in tool execution chunks
                if (chunk.ElapsedTime > lastElapsedTime)
                {
                    lastElapsedTime = chunk.ElapsedTime;
                }

                if (chunk.TextDelta != null)
                {
                    contentBuilder.Append(chunk.TextDelta);
                }

                if (chunk.ToolCallDelta != null)
                {
                    AccumulateToolCall(toolCallsAccumulator, chunk.ToolCallDelta);
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
                    yield return new StreamChunk
                    {
                        IsFirstChunk = false,
                        ElapsedTime = lastElapsedTime,
                        ChunkIndex = 0,
                        TextDelta = $"\n\n[Max tool iterations ({config.MaxIterations}) reached]",
                        Raw = null!
                    };
                    break;
                }

                // Increment after the check passes
                toolCallsCount++;

                await foreach (var toolChunk in ExecuteToolCalls(completeMessage.ToolCalls, conversationHistory, lastElapsedTime))
                {
                    // Update last elapsed time if tool execution took time
                    if (toolChunk.ElapsedTime > lastElapsedTime)
                    {
                        lastElapsedTime = toolChunk.ElapsedTime;
                    }
                    yield return toolChunk;
                }
            }
            else
            {
                hasToolCalls = false;
            }

        } while (hasToolCalls && toolCallsCount < config.MaxIterations);
        }
        catch (Exception ex)
        {
            if (activity != null)
            {
                TelemetryHelper.RecordException(activity, ex);
            }
            throw;
        }
    }

    /// <summary>
    /// Internal streaming implementation without tool loop
    /// </summary>
    private async IAsyncEnumerable<StreamChunk> StreamAsyncInternal(
        ChatCompletionRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        if (_toolManager.ToolCount > 0 && request.Tools == null)
        {
            request.Tools = _toolManager.GetAllTools();
        }

        request.Stream = true;

        var requestContent = JsonSerializer.Serialize(request, _httpHandler.JsonOptions);

        // Get current activity for enrichment
        var currentActivity = Activity.Current;

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_httpHandler.BaseUrl}/chat/completions")
        {
            Content = new StringContent(requestContent, Encoding.UTF8, "application/json")
        };

        _httpHandler.AddHeaders(httpRequest);

        var response = await _httpHandler.HttpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        await _httpHandler.HandleErrorResponse(response);

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        System.Diagnostics.Stopwatch? stopwatch = null;
        var chunkIndex = 0;
        var isFirstChunk = true;
        var artifactParser = new ArtifactParser();

        // Telemetry tracking
        long? timeToFirstTokenMs = null;
        int totalChunks = 0;
        string? finishReason = null;
        string? responseModel = null;
        int? inputTokens = null;
        int? outputTokens = null;

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

            var chunksToYield = new List<StreamChunk>();

            try
            {
                var rawChunk = JsonSerializer.Deserialize<ChatCompletionStreamResponse>(data, _httpHandler.JsonOptions);
                if (rawChunk == null) continue;

                var choice = rawChunk.Choices?.FirstOrDefault();
                var delta = choice?.Delta;

                // Parse artifacts from text content
                if (delta?.Content != null)
                {
                    ProcessTextContent(delta.Content, rawChunk, choice, artifactParser, ref isFirstChunk, ref chunkIndex, stopwatch, chunksToYield);
                }
                else
                {
                    ProcessNonTextContent(delta, choice, rawChunk, ref isFirstChunk, ref chunkIndex, stopwatch, chunksToYield);
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
                // Telemetry tracking
                if (_telemetryOptions.EnableTelemetry && currentActivity != null)
                {
                    totalChunks++;

                    // Capture time to first token
                    if (!timeToFirstTokenMs.HasValue && chunkToYield.IsFirstChunk && stopwatch != null)
                    {
                        timeToFirstTokenMs = (long)stopwatch.Elapsed.TotalMilliseconds;
                    }

                    // Capture completion metadata
                    if (chunkToYield.Completion != null)
                    {
                        finishReason = chunkToYield.Completion.FinishReason;
                        responseModel = chunkToYield.Completion.Model;
                    }

                    // Capture usage
                    if (chunkToYield.Raw?.Usage != null)
                    {
                        inputTokens = chunkToYield.Raw.Usage.PromptTokens;
                        outputTokens = chunkToYield.Raw.Usage.CompletionTokens;
                    }

                    // Optionally log stream chunks
                    if (_telemetryOptions.CaptureStreamChunks && !string.IsNullOrEmpty(chunkToYield.TextDelta))
                    {
                        currentActivity.AddEvent(new ActivityEvent(GenAiSemanticConventions.EventStreamChunk,
                            tags: new ActivityTagsCollection
                            {
                                { "chunk_index", totalChunks },
                                { "text_delta", chunkToYield.TextDelta },
                                { "elapsed_ms", (long)chunkToYield.ElapsedTime.TotalMilliseconds }
                            }));
                    }
                }

                yield return chunkToYield;
            }
        }

        // Enrich activity with final streaming metrics
        if (_telemetryOptions.EnableTelemetry && currentActivity != null && stopwatch != null)
        {
            var durationMs = (long)stopwatch.Elapsed.TotalMilliseconds;

            if (timeToFirstTokenMs.HasValue)
            {
                currentActivity.SetTag(GenAiSemanticConventions.AttributeStreamTimeToFirstToken, timeToFirstTokenMs.Value);
            }

            currentActivity.SetTag(GenAiSemanticConventions.AttributeStreamTotalChunks, totalChunks);
            currentActivity.SetTag(GenAiSemanticConventions.AttributeStreamDuration, durationMs);

            if (outputTokens.HasValue && durationMs > 0)
            {
                var tokensPerSecond = (outputTokens.Value / (durationMs / 1000.0));
                currentActivity.SetTag(GenAiSemanticConventions.AttributeStreamTokensPerSecond, tokensPerSecond);
            }

            if (!string.IsNullOrEmpty(finishReason))
            {
                currentActivity.SetTag(GenAiSemanticConventions.AttributeStreamFinishReason, finishReason);
            }

            if (!string.IsNullOrEmpty(responseModel))
            {
                currentActivity.SetTag(GenAiSemanticConventions.AttributeResponseModel, responseModel);
            }

            if (inputTokens.HasValue)
            {
                currentActivity.SetTag(GenAiSemanticConventions.AttributeUsageInputTokens, inputTokens.Value);
            }

            if (outputTokens.HasValue)
            {
                currentActivity.SetTag(GenAiSemanticConventions.AttributeUsageOutputTokens, outputTokens.Value);
            }

            currentActivity.SetTag(GenAiSemanticConventions.AttributeHttpStatusCode, 200);
            currentActivity.SetStatus(ActivityStatusCode.Ok);
        }
    }

    /// <summary>
    /// Processes text content including artifact parsing
    /// </summary>
    private void ProcessTextContent(
        string content,
        ChatCompletionStreamResponse rawChunk,
        StreamingChoice? choice,
        ArtifactParser artifactParser,
        ref bool isFirstChunk,
        ref int chunkIndex,
        System.Diagnostics.Stopwatch? stopwatch,
        List<StreamChunk> chunksToYield)
    {
        var parseResult = artifactParser.ParseIncremental(content);
        var currentIsFirstChunk = isFirstChunk;

        // Collect artifact started event
        if (parseResult.ArtifactStarted != null)
        {
            chunksToYield.Add(new StreamChunk
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
            chunksToYield.Add(new StreamChunk
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
            chunksToYield.Add(new StreamChunk
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
            var textChunk = new StreamChunk
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
                    Completion = new CompletionMetadata
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

    /// <summary>
    /// Processes non-text content (tool calls, finish reason)
    /// </summary>
    private void ProcessNonTextContent(
        MessageDelta? delta,
        StreamingChoice? choice,
        ChatCompletionStreamResponse rawChunk,
        ref bool isFirstChunk,
        ref int chunkIndex,
        System.Diagnostics.Stopwatch? stopwatch,
        List<StreamChunk> chunksToYield)
    {
        // Handle tool call deltas
        if (delta?.ToolCalls != null && delta.ToolCalls.Length > 0)
        {
            foreach (var toolCall in delta.ToolCalls)
            {
                chunksToYield.Add(new StreamChunk
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
            chunksToYield.Add(new StreamChunk
            {
                IsFirstChunk = isFirstChunk,
                ElapsedTime = stopwatch?.Elapsed ?? TimeSpan.Zero,
                ChunkIndex = chunkIndex++,
                Completion = new CompletionMetadata
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
            chunksToYield.Add(new StreamChunk
            {
                IsFirstChunk = isFirstChunk,
                ElapsedTime = stopwatch?.Elapsed ?? TimeSpan.Zero,
                ChunkIndex = chunkIndex++,
                Raw = rawChunk
            });
        }
    }

    /// <summary>
    /// Accumulates tool call deltas into complete tool calls
    /// </summary>
    private void AccumulateToolCall(Dictionary<int, ToolCall> accumulator, ToolCall toolCallDelta)
    {
        if (!accumulator.ContainsKey(toolCallDelta.Index))
        {
            accumulator[toolCallDelta.Index] = new ToolCall
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
            var existing = accumulator[toolCallDelta.Index];
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

    /// <summary>
    /// Executes tool calls and yields results as stream chunks
    /// </summary>
    private async IAsyncEnumerable<StreamChunk> ExecuteToolCalls(
        ToolCall[] toolCalls,
        List<Message> conversationHistory,
        TimeSpan baseElapsedTime)
    {
        foreach (var toolCall in toolCalls)
        {
            if (toolCall.Function == null || string.IsNullOrEmpty(toolCall.Function.Name))
                continue;

            var toolName = toolCall.Function.Name;

            if (!_toolManager.TryGetToolRegistration(toolName, out var registration))
            {
                var errorMsg = $"Tool '{toolName}' is not registered";
                yield return new StreamChunk
                {
                    IsFirstChunk = false,
                    ElapsedTime = baseElapsedTime,
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
                yield return new StreamChunk
                {
                    IsFirstChunk = false,
                    ElapsedTime = baseElapsedTime,
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

            yield return new StreamChunk
            {
                IsFirstChunk = false,
                ElapsedTime = baseElapsedTime,
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

            StreamChunk resultChunk;
            Message toolResultMessage;

            try
            {
                var result = _toolManager.ExecuteTool(toolName, toolCall.Function.Arguments!);
                executionStopwatch.Stop();

                var resultString = result is string str
                    ? str
                    : JsonSerializer.Serialize(result, _httpHandler.JsonOptions);

                resultChunk = new StreamChunk
                {
                    IsFirstChunk = false,
                    ElapsedTime = baseElapsedTime + executionStopwatch.Elapsed,
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

                resultChunk = new StreamChunk
                {
                    IsFirstChunk = false,
                    ElapsedTime = baseElapsedTime + executionStopwatch.Elapsed,
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

    private void Log(string message)
    {
        _logCallback?.Invoke(message);
    }
}
