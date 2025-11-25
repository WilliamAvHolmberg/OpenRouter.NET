namespace OpenRouterWebApi.Models;

public record ChatRequest(
    string Model,
    string Message,
    string? SystemPrompt = null,
    float? Temperature = null,
    int? MaxTokens = null
);

public record ChatResponse(
    string Content,
    string Model,
    string? FinishReason,
    TokenUsage? Usage
);

public record TokenUsage(
    int? TotalTokens,
    int? PromptTokens,
    int? CompletionTokens
);

public record StreamChatRequest(
    string Model,
    string Message,
    string? SystemPrompt = null
);

public record ConversationRequest(
    string Model,
    List<ConversationMessage> Messages,
    float? Temperature = null
);

public record ConversationMessage(
    string Role,
    string Content
);

public record ArtifactRequest(
    string Model,
    string Prompt,
    string ArtifactType = "code"
);

public record ArtifactResponse(
    string Text,
    List<ArtifactInfo> Artifacts
);

public record ArtifactInfo(
    string Title,
    string Type,
    string Content,
    string? Language = null
);

public record ToolRequest(
    string Model,
    string Message
);

public record MultimodalRequest(
    string Model,
    string Message,
    List<string> ImageUrls
);

