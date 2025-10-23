namespace OpenRouter.NET.Models;

public class ModelInfo
{
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string Id { get; set; } = "";
    
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; } = "";
    
    [System.Text.Json.Serialization.JsonPropertyName("context_length")]
    public int ContextLength { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("pricing")]
    public ModelPricing? Pricing { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("top_provider")]
    public ProviderInfo? TopProvider { get; set; }
}

public class ModelPricing
{
    [System.Text.Json.Serialization.JsonPropertyName("prompt")]
    public string Prompt { get; set; } = "";
    
    [System.Text.Json.Serialization.JsonPropertyName("completion")]
    public string Completion { get; set; } = "";
    
    [System.Text.Json.Serialization.JsonPropertyName("image")]
    public string? Image { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("request")]
    public string? Request { get; set; }
}

public class ProviderInfo
{
    [System.Text.Json.Serialization.JsonPropertyName("max_completion_tokens")]
    public int? MaxCompletionTokens { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("is_moderated")]
    public bool IsModerated { get; set; }
}

public class ModelsResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("data")]
    public List<ModelInfo> Data { get; set; } = new();
}

public class UserLimits
{
    [System.Text.Json.Serialization.JsonPropertyName("credits_used")]
    public decimal CreditsUsed { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("credits_limit")]
    public decimal? CreditsLimit { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("rate_limit")]
    public RateLimitInfo? RateLimit { get; set; }
}

public class RateLimitInfo
{
    [System.Text.Json.Serialization.JsonPropertyName("requests")]
    public int Requests { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("interval")]
    public string Interval { get; set; } = "";
}

public class GenerationInfo
{
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string Id { get; set; } = "";
    
    [System.Text.Json.Serialization.JsonPropertyName("model")]
    public string Model { get; set; } = "";
    
    [System.Text.Json.Serialization.JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("tokens_prompt")]
    public int TokensPrompt { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("tokens_completion")]
    public int TokensCompletion { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("native_tokens_prompt")]
    public int? NativeTokensPrompt { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("native_tokens_completion")]
    public int? NativeTokensCompletion { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("total_cost")]
    public decimal TotalCost { get; set; }
}

