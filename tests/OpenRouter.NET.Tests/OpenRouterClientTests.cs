using OpenRouter.NET;
using OpenRouter.NET.Models;

namespace OpenRouter.NET.Tests;

public class OpenRouterClientTests
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
    public async Task GetModelsAsync_ShouldReturnListOfModels()
    {
        var apiKey = GetApiKey();
        var client = new OpenRouterClient(apiKey);
        
        var models = await client.GetModelsAsync();
        
        Assert.NotNull(models);
        Assert.IsType<List<ModelInfo>>(models);
    }
    
    [Fact]
    public async Task GetModelsAsync_ShouldIncludeModelProperties()
    {
        var apiKey = GetApiKey();
        var client = new OpenRouterClient(apiKey);
        
        var models = await client.GetModelsAsync();
        
        var firstModel = models.FirstOrDefault();
        if (firstModel != null)
        {
            Assert.NotNull(firstModel.Id);
            Assert.NotNull(firstModel.Name);
            Assert.True(firstModel.ContextLength > 0);
        }
    }
    
    [Fact]
    public async Task GetLimitsAsync_ShouldReturnUserLimits()
    {
        var apiKey = GetApiKey();
        var client = new OpenRouterClient(apiKey);
        
        var limits = await client.GetLimitsAsync();
        
        Assert.NotNull(limits);
    }
}

