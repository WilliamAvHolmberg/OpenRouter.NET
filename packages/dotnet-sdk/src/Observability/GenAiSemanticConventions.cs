namespace OpenRouter.NET.Observability;

/// <summary>
/// OpenTelemetry semantic conventions for Generative AI operations.
/// Based on: https://opentelemetry.io/docs/specs/semconv/gen-ai/
/// </summary>
internal static class GenAiSemanticConventions
{
    // ========== Span Names ==========
    public const string SpanNameChat = "gen_ai.client.chat";
    public const string SpanNameStream = "gen_ai.client.stream";
    public const string SpanNameGenerateObject = "gen_ai.client.generate_object";
    public const string SpanNameToolCall = "gen_ai.tool.call";
    public const string SpanNameHttp = "http";

    // ========== General Attributes ==========
    public const string AttributeSystem = "gen_ai.system";
    public const string AttributeOperationName = "gen_ai.operation.name";
    public const string AttributeServerAddress = "server.address";

    // ========== Request Attributes ==========
    public const string AttributeRequestModel = "gen_ai.request.model";
    public const string AttributeRequestMaxTokens = "gen_ai.request.max_tokens";
    public const string AttributeRequestTemperature = "gen_ai.request.temperature";
    public const string AttributeRequestTopP = "gen_ai.request.top_p";
    public const string AttributeRequestFrequencyPenalty = "gen_ai.request.frequency_penalty";
    public const string AttributeRequestPresencePenalty = "gen_ai.request.presence_penalty";
    public const string AttributeRequestSizeBytes = "gen_ai.request.size_bytes";

    // ========== Response Attributes ==========
    public const string AttributeResponseId = "gen_ai.response.id";
    public const string AttributeResponseModel = "gen_ai.response.model";
    public const string AttributeResponseFinishReasons = "gen_ai.response.finish_reasons";
    public const string AttributeResponseSizeBytes = "gen_ai.response.size_bytes";

    // ========== Usage/Token Attributes ==========
    public const string AttributeUsageInputTokens = "gen_ai.usage.input_tokens";
    public const string AttributeUsageOutputTokens = "gen_ai.usage.output_tokens";

    // ========== HTTP Attributes ==========
    public const string AttributeHttpStatusCode = "http.response.status_code";
    public const string AttributeHttpMethod = "http.request.method";
    public const string AttributeHttpUrl = "url.full";
    public const string AttributeHttpRetryAfter = "http.response.retry_after";

    // ========== Streaming Attributes ==========
    public const string AttributeStreamTimeToFirstToken = "gen_ai.stream.time_to_first_token_ms";
    public const string AttributeStreamTotalChunks = "gen_ai.stream.total_chunks";
    public const string AttributeStreamDuration = "gen_ai.stream.duration_ms";
    public const string AttributeStreamTokensPerSecond = "gen_ai.stream.tokens_per_second";
    public const string AttributeStreamFinishReason = "gen_ai.stream.finish_reason";

    // ========== Tool Attributes ==========
    public const string AttributeToolName = "gen_ai.tool.name";
    public const string AttributeToolId = "gen_ai.tool.id";
    public const string AttributeToolExecutionMode = "gen_ai.tool.execution_mode";
    public const string AttributeToolArguments = "gen_ai.tool.arguments";
    public const string AttributeToolResult = "gen_ai.tool.result";
    public const string AttributeToolError = "gen_ai.tool.error";
    public const string AttributeToolDuration = "gen_ai.tool.duration_ms";
    public const string AttributeToolLoopIteration = "gen_ai.tool.loop_iteration";

    // ========== Schema/Validation Attributes ==========
    public const string AttributeSchemaSizeBytes = "gen_ai.schema.size_bytes";
    public const string AttributeValidationAttempt = "gen_ai.validation.attempt";
    public const string AttributeValidationSuccess = "gen_ai.validation.success";
    public const string AttributeValidationError = "gen_ai.validation.error";

    // ========== Event Names ==========
    public const string EventPrompt = "gen_ai.client.input";
    public const string EventCompletion = "gen_ai.client.output";
    public const string EventStreamChunk = "gen_ai.client.stream.chunk";
    public const string EventException = "exception";

    // ========== Event Attribute Names ==========
    public const string EventAttributePrompt = "gen_ai.prompt";
    public const string EventAttributeCompletion = "gen_ai.completion";
    public const string EventAttributeRawRequest = "gen_ai.request.raw_json";
    public const string EventAttributeRawResponse = "gen_ai.response.raw_json";

    // ========== Exception Attributes ==========
    public const string AttributeExceptionType = "exception.type";
    public const string AttributeExceptionMessage = "exception.message";
    public const string AttributeExceptionStacktrace = "exception.stacktrace";

    // ========== Constant Values ==========
    public const string SystemValue = "openrouter";
    public const string OperationChat = "chat";
    public const string OperationStream = "stream";
    public const string OperationGenerateObject = "generate_object";
    public const string ServerAddressValue = "openrouter.ai";
}
