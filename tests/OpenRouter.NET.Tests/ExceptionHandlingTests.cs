using OpenRouter.NET;
using OpenRouter.NET.Models;

namespace OpenRouter.NET.Tests;

public class ExceptionHandlingTests
{
    private static string GetApiKey()
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException(
                "OPENROUTER_API_KEY environment variable is required for integration tests. " +
                "Set it with: export OPENROUTER_API_KEY=your-key-here");
        }
        return apiKey;
    }

    [Fact]
    public async Task CreateChatCompletion_WhenUnauthorized_ShouldThrowOpenRouterAuthException()
    {
        var client = new OpenRouterClient("invalid-key");
        var request = new ChatCompletionRequest
        {
            Model = "openai/gpt-3.5-turbo",
            Messages = new List<Message> { Message.FromUser("test") }
        };
        
        await Assert.ThrowsAsync<OpenRouterAuthException>(async () =>
        {
            await client.CreateChatCompletionAsync(request);
        });
    }
    
    [Fact]
    public async Task CreateChatCompletion_WhenModelNotFound_ShouldThrowBadRequestException()
    {
        var apiKey = GetApiKey();
        var client = new OpenRouterClient(apiKey);
        var request = new ChatCompletionRequest
        {
            Model = "non-existent-model-12345",
            Messages = new List<Message> { Message.FromUser("test") }
        };
        
        var exception = await Assert.ThrowsAsync<OpenRouterBadRequestException>(async () =>
        {
            await client.CreateChatCompletionAsync(request);
        });
        
        Assert.Contains("not a valid model ID", exception.Message);
    }
    
    [Fact]
    public void RateLimitException_ShouldStoreRetryAfterSeconds()
    {
        var exception = new OpenRouterRateLimitException("Rate limit exceeded", 60);
        
        Assert.Equal(60, exception.RetryAfterSeconds);
        Assert.Contains("Rate limit", exception.Message);
    }
}

