using System.Diagnostics;
using System.Text;
using System.Text.Json;
using OpenRouter.NET.Models;

namespace OpenRouter.NET.Observability;

/// <summary>
/// Helper methods for enriching activities with telemetry data.
/// </summary>
internal static class TelemetryHelper
{
    /// <summary>
    /// Enriches an activity with request-level attributes from a ChatCompletionRequest.
    /// </summary>
    public static void EnrichWithRequest(
        Activity activity,
        ChatCompletionRequest request,
        string? requestJson,
        OpenRouterTelemetryOptions options)
    {
        if (activity == null) return;

        // Core request attributes - always capture
        activity.SetTag(GenAiSemanticConventions.AttributeSystem, GenAiSemanticConventions.SystemValue);
        activity.SetTag(GenAiSemanticConventions.AttributeServerAddress, GenAiSemanticConventions.ServerAddressValue);

        if (!string.IsNullOrEmpty(request.Model))
        {
            activity.SetTag(GenAiSemanticConventions.AttributeRequestModel, request.Model);
        }

        if (request.MaxTokens.HasValue)
        {
            activity.SetTag(GenAiSemanticConventions.AttributeRequestMaxTokens, request.MaxTokens.Value);
        }

        if (request.Temperature.HasValue)
        {
            activity.SetTag(GenAiSemanticConventions.AttributeRequestTemperature, request.Temperature.Value);
        }

        if (request.TopP.HasValue)
        {
            activity.SetTag(GenAiSemanticConventions.AttributeRequestTopP, request.TopP.Value);
        }

        if (request.FrequencyPenalty.HasValue)
        {
            activity.SetTag(GenAiSemanticConventions.AttributeRequestFrequencyPenalty, request.FrequencyPenalty.Value);
        }

        if (request.PresencePenalty.HasValue)
        {
            activity.SetTag(GenAiSemanticConventions.AttributeRequestPresencePenalty, request.PresencePenalty.Value);
        }

        // Request size
        if (!string.IsNullOrEmpty(requestJson))
        {
            activity.SetTag(GenAiSemanticConventions.AttributeRequestSizeBytes, Encoding.UTF8.GetByteCount(requestJson));
        }

        // Opt-in: Capture raw request
        if (options.CaptureRawRequests && !string.IsNullOrEmpty(requestJson))
        {
            var truncated = TruncateIfNeeded(requestJson, options.MaxEventBodySize);
            activity.AddEvent(new ActivityEvent(GenAiSemanticConventions.EventPrompt,
                tags: new ActivityTagsCollection
                {
                    { GenAiSemanticConventions.EventAttributeRawRequest, truncated }
                }));
        }

        // Opt-in: Capture structured prompts
        if (options.CapturePrompts && request.Messages?.Count > 0)
        {
            var promptData = SerializePrompts(request.Messages, options);
            if (!string.IsNullOrEmpty(promptData))
            {
                activity.AddEvent(new ActivityEvent(GenAiSemanticConventions.EventPrompt,
                    tags: new ActivityTagsCollection
                    {
                        { GenAiSemanticConventions.EventAttributePrompt, promptData }
                    }));
            }
        }
    }

    /// <summary>
    /// Enriches an activity with response-level attributes from a ChatCompletionResponse.
    /// </summary>
    public static void EnrichWithResponse(
        Activity activity,
        ChatCompletionResponse response,
        string? responseJson,
        OpenRouterTelemetryOptions options)
    {
        if (activity == null) return;

        // Core response attributes - always capture
        if (!string.IsNullOrEmpty(response.Id))
        {
            activity.SetTag(GenAiSemanticConventions.AttributeResponseId, response.Id);
        }

        if (!string.IsNullOrEmpty(response.Model))
        {
            activity.SetTag(GenAiSemanticConventions.AttributeResponseModel, response.Model);
        }

        // Finish reasons
        if (response.Choices?.Count > 0)
        {
            var finishReasons = response.Choices
                .Where(c => !string.IsNullOrEmpty(c.FinishReason))
                .Select(c => c.FinishReason!)
                .ToArray();

            if (finishReasons.Length > 0)
            {
                activity.SetTag(GenAiSemanticConventions.AttributeResponseFinishReasons, finishReasons);
            }
        }

        // Token usage
        if (response.Usage != null)
        {
            activity.SetTag(GenAiSemanticConventions.AttributeUsageInputTokens, response.Usage.PromptTokens);
            activity.SetTag(GenAiSemanticConventions.AttributeUsageOutputTokens, response.Usage.CompletionTokens);
        }

        // Response size
        if (!string.IsNullOrEmpty(responseJson))
        {
            activity.SetTag(GenAiSemanticConventions.AttributeResponseSizeBytes, Encoding.UTF8.GetByteCount(responseJson));
        }

        // Opt-in: Capture raw response
        if (options.CaptureRawResponses && !string.IsNullOrEmpty(responseJson))
        {
            var truncated = TruncateIfNeeded(responseJson, options.MaxEventBodySize);
            activity.AddEvent(new ActivityEvent(GenAiSemanticConventions.EventCompletion,
                tags: new ActivityTagsCollection
                {
                    { GenAiSemanticConventions.EventAttributeRawResponse, truncated }
                }));
        }

        // Opt-in: Capture structured completions
        if (options.CaptureCompletions && response.Choices?.Count > 0)
        {
            var completionData = SerializeCompletions(response.Choices, options);
            if (!string.IsNullOrEmpty(completionData))
            {
                activity.AddEvent(new ActivityEvent(GenAiSemanticConventions.EventCompletion,
                    tags: new ActivityTagsCollection
                    {
                        { GenAiSemanticConventions.EventAttributeCompletion, completionData }
                    }));
            }
        }
    }

    /// <summary>
    /// Enriches an activity with HTTP-level attributes.
    /// </summary>
    public static void EnrichWithHttp(
        Activity activity,
        string method,
        string url,
        int statusCode,
        int? retryAfterSeconds = null)
    {
        if (activity == null) return;

        activity.SetTag(GenAiSemanticConventions.AttributeHttpMethod, method);
        activity.SetTag(GenAiSemanticConventions.AttributeHttpUrl, url);
        activity.SetTag(GenAiSemanticConventions.AttributeHttpStatusCode, statusCode);

        if (retryAfterSeconds.HasValue)
        {
            activity.SetTag(GenAiSemanticConventions.AttributeHttpRetryAfter, retryAfterSeconds.Value);
        }
    }

    /// <summary>
    /// Enriches an activity with streaming-specific metrics.
    /// </summary>
    public static void EnrichWithStreaming(
        Activity activity,
        long timeToFirstTokenMs,
        int totalChunks,
        long durationMs,
        double tokensPerSecond,
        string? finishReason)
    {
        if (activity == null) return;

        activity.SetTag(GenAiSemanticConventions.AttributeStreamTimeToFirstToken, timeToFirstTokenMs);
        activity.SetTag(GenAiSemanticConventions.AttributeStreamTotalChunks, totalChunks);
        activity.SetTag(GenAiSemanticConventions.AttributeStreamDuration, durationMs);
        activity.SetTag(GenAiSemanticConventions.AttributeStreamTokensPerSecond, tokensPerSecond);

        if (!string.IsNullOrEmpty(finishReason))
        {
            activity.SetTag(GenAiSemanticConventions.AttributeStreamFinishReason, finishReason);
        }
    }

    /// <summary>
    /// Records an exception on an activity.
    /// </summary>
    public static void RecordException(Activity activity, Exception exception)
    {
        if (activity == null) return;

        activity.SetStatus(ActivityStatusCode.Error, exception.Message);
        activity.AddEvent(new ActivityEvent(GenAiSemanticConventions.EventException,
            tags: new ActivityTagsCollection
            {
                { GenAiSemanticConventions.AttributeExceptionType, exception.GetType().FullName ?? exception.GetType().Name },
                { GenAiSemanticConventions.AttributeExceptionMessage, exception.Message },
                { GenAiSemanticConventions.AttributeExceptionStacktrace, exception.StackTrace ?? string.Empty }
            }));
    }

    /// <summary>
    /// Serializes prompt messages to JSON.
    /// </summary>
    private static string? SerializePrompts(List<Message> messages, OpenRouterTelemetryOptions options)
    {
        try
        {
            var simplified = messages.Select(m => new
            {
                role = m.Role,
                content = options.SanitizePrompt?.Invoke(GetMessageContent(m)) ?? GetMessageContent(m)
            }).ToList();

            var json = JsonSerializer.Serialize(simplified);
            return TruncateIfNeeded(json, options.MaxEventBodySize);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Serializes completion choices to JSON.
    /// </summary>
    private static string? SerializeCompletions(List<Choice> choices, OpenRouterTelemetryOptions options)
    {
        try
        {
            var simplified = choices.Select(c => new
            {
                index = c.Index,
                content = options.SanitizeCompletion?.Invoke(GetMessageContent(c.Message)) ?? GetMessageContent(c.Message),
                finish_reason = c.FinishReason
            }).ToList();

            var json = JsonSerializer.Serialize(simplified);
            return TruncateIfNeeded(json, options.MaxEventBodySize);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Extracts text content from a message.
    /// </summary>
    private static string GetMessageContent(Message? message)
    {
        if (message == null) return string.Empty;

        // If Content is a string, return it directly
        if (message.Content is string str)
        {
            return str;
        }

        // If Content is a list of ContentPart, concatenate text parts
        if (message.Content is List<ContentPart> parts)
        {
            return string.Join(" ", parts
                .OfType<TextContent>()
                .Select(p => p.Text ?? string.Empty));
        }

        return string.Empty;
    }

    /// <summary>
    /// Truncates a string if it exceeds the maximum size.
    /// </summary>
    private static string TruncateIfNeeded(string content, int maxSize)
    {
        if (maxSize <= 0) return content;

        var bytes = Encoding.UTF8.GetBytes(content);
        if (bytes.Length <= maxSize)
        {
            return content;
        }

        // Truncate to maxSize bytes and add indicator
        var truncated = Encoding.UTF8.GetString(bytes, 0, Math.Max(0, maxSize - 20));
        return truncated + "... [TRUNCATED]";
    }
}
