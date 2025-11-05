using OpenRouter.NET;
using OpenRouter.NET.Models;
using System.Text.Json;

namespace OpenRouter.NET.Tests;

public class GenerateObjectTests
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
    public async Task GenerateObjectAsync_WithSimpleSchema_ReturnsValidObject()
    {
        var apiKey = GetApiKey();
        var client = new OpenRouterClient(apiKey);

        var schemaJson = @"{
            ""type"": ""object"",
            ""properties"": {
                ""name"": { 
                    ""type"": ""string"",
                    ""description"": ""Person's full name""
                },
                ""age"": { 
                    ""type"": ""number"",
                    ""description"": ""Person's age in years""
                }
            },
            ""required"": [""name"", ""age""]
        }";

        var schema = JsonSerializer.Deserialize<JsonElement>(schemaJson);

        var result = await client.GenerateObjectAsync(
            schema: schema,
            prompt: "Generate information about a person named John Smith who is 30 years old",
            model: "openai/gpt-4o-mini"
        );

        Assert.NotNull(result);
        Assert.NotEqual(default(JsonElement), result.Object);
        
        var hasName = result.Object.TryGetProperty("name", out var nameProperty);
        var hasAge = result.Object.TryGetProperty("age", out var ageProperty);
        
        Assert.True(hasName);
        Assert.True(hasAge);
        Assert.Equal(JsonValueKind.String, nameProperty.ValueKind);
        Assert.True(ageProperty.ValueKind == JsonValueKind.Number);
    }

    [Fact]
    public async Task GenerateObjectAsync_WithArraySchema_ReturnsValidArray()
    {
        var apiKey = GetApiKey();
        var client = new OpenRouterClient(apiKey);

        var schemaJson = @"{
            ""type"": ""object"",
            ""properties"": {
                ""translations"": {
                    ""type"": ""array"",
                    ""items"": {
                        ""type"": ""object"",
                        ""properties"": {
                            ""language"": { 
                                ""type"": ""string"",
                                ""description"": ""The target language name""
                            },
                            ""languageCode"": { 
                                ""type"": ""string"",
                                ""description"": ""ISO 639-1 language code""
                            },
                            ""translatedText"": { 
                                ""type"": ""string"",
                                ""description"": ""The translated text""
                            }
                        },
                        ""required"": [""language"", ""languageCode"", ""translatedText""]
                    }
                }
            },
            ""required"": [""translations""]
        }";

        var schema = JsonSerializer.Deserialize<JsonElement>(schemaJson);

        var result = await client.GenerateObjectAsync(
            schema: schema,
            prompt: "Translate 'Hello World' into Spanish, French, and German",
            model: "openai/gpt-4o-mini"
        );

        Assert.NotNull(result);
        
        var hasTranslations = result.Object.TryGetProperty("translations", out var translations);
        Assert.True(hasTranslations);
        Assert.Equal(JsonValueKind.Array, translations.ValueKind);
        
        var translationsArray = translations.EnumerateArray().ToList();
        Assert.True(translationsArray.Count >= 3);

        foreach (var translation in translationsArray)
        {
            Assert.True(translation.TryGetProperty("language", out _));
            Assert.True(translation.TryGetProperty("languageCode", out _));
            Assert.True(translation.TryGetProperty("translatedText", out _));
        }
    }

    [Fact]
    public async Task GenerateObjectAsync_WithEmptyPrompt_ThrowsArgumentException()
    {
        var apiKey = GetApiKey();
        var client = new OpenRouterClient(apiKey);

        var schemaJson = @"{""type"": ""object"", ""properties"": {""test"": {""type"": ""string""}}}";
        var schema = JsonSerializer.Deserialize<JsonElement>(schemaJson);

        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await client.GenerateObjectAsync(
                schema: schema,
                prompt: "",
                model: "openai/gpt-4o-mini"
            );
        });
    }

    [Fact]
    public async Task GenerateObjectAsync_WithEmptyModel_ThrowsArgumentException()
    {
        var apiKey = GetApiKey();
        var client = new OpenRouterClient(apiKey);

        var schemaJson = @"{""type"": ""object"", ""properties"": {""test"": {""type"": ""string""}}}";
        var schema = JsonSerializer.Deserialize<JsonElement>(schemaJson);

        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await client.GenerateObjectAsync(
                schema: schema,
                prompt: "Test prompt",
                model: ""
            );
        });
    }

    [Fact]
    public async Task GenerateObjectAsync_WithCustomTemperature_UsesProvidedValue()
    {
        var apiKey = GetApiKey();
        var client = new OpenRouterClient(apiKey);

        var schemaJson = @"{
            ""type"": ""object"",
            ""properties"": {
                ""randomNumber"": { 
                    ""type"": ""number"",
                    ""description"": ""A random number between 1 and 10""
                }
            },
            ""required"": [""randomNumber""]
        }";

        var schema = JsonSerializer.Deserialize<JsonElement>(schemaJson);

        var result = await client.GenerateObjectAsync(
            schema: schema,
            prompt: "Generate a random number between 1 and 10",
            model: "openai/gpt-4o-mini",
            options: new GenerateObjectOptions
            {
                Temperature = 0.0
            }
        );

        Assert.NotNull(result);
        Assert.NotEqual(default(JsonElement), result.Object);
    }

    [Fact]
    public async Task GenerateObjectAsync_ReturnsUsageInformation()
    {
        var apiKey = GetApiKey();
        var client = new OpenRouterClient(apiKey);

        var schemaJson = @"{
            ""type"": ""object"",
            ""properties"": {
                ""message"": { ""type"": ""string"" }
            },
            ""required"": [""message""]
        }";

        var schema = JsonSerializer.Deserialize<JsonElement>(schemaJson);

        var result = await client.GenerateObjectAsync(
            schema: schema,
            prompt: "Generate a message saying 'Hello'",
            model: "openai/gpt-4o-mini"
        );

        Assert.NotNull(result.Usage);
        Assert.True(result.Usage.PromptTokens > 0);
        Assert.True(result.Usage.CompletionTokens > 0);
        Assert.True(result.Usage.TotalTokens > 0);
    }

    [Theory]
    [InlineData("openai/gpt-4o-mini")]
    [InlineData("anthropic/claude-3.5-haiku")]
    public async Task GenerateObjectAsync_WorksWithDifferentModels(string model)
    {
        var apiKey = GetApiKey();
        var client = new OpenRouterClient(apiKey);

        var schemaJson = @"{
            ""type"": ""object"",
            ""properties"": {
                ""color"": { 
                    ""type"": ""string"",
                    ""description"": ""A color name""
                }
            },
            ""required"": [""color""]
        }";

        var schema = JsonSerializer.Deserialize<JsonElement>(schemaJson);

        var result = await client.GenerateObjectAsync(
            schema: schema,
            prompt: "Generate a color: blue",
            model: model
        );

        Assert.NotNull(result);
        Assert.NotEqual(default(JsonElement), result.Object);
        
        var hasColor = result.Object.TryGetProperty("color", out var colorProperty);
        Assert.True(hasColor);
    }

    [Fact]
    public async Task GenerateObjectAsync_WithLargeSchema_LogsWarning()
    {
        var apiKey = GetApiKey();
        var logMessages = new List<string>();
        
        var client = new OpenRouterClient(new OpenRouterClientOptions
        {
            ApiKey = apiKey,
            OnLogMessage = (msg) => logMessages.Add(msg)
        });

        var largeSchemaJson = @"{
            ""type"": ""object"",
            ""properties"": {
                ""field1"": { ""type"": ""string"", ""description"": """ + new string('A', 500) + @""" },
                ""field2"": { ""type"": ""string"", ""description"": """ + new string('B', 500) + @""" },
                ""field3"": { ""type"": ""string"", ""description"": """ + new string('C', 500) + @""" },
                ""field4"": { ""type"": ""string"", ""description"": """ + new string('D', 500) + @""" },
                ""field5"": { ""type"": ""string"", ""description"": """ + new string('E', 500) + @""" }
            },
            ""required"": [""field1""]
        }";

        var schema = JsonSerializer.Deserialize<JsonElement>(largeSchemaJson);

        var result = await client.GenerateObjectAsync(
            schema: schema,
            prompt: "Generate data for field1",
            model: "openai/gpt-4o-mini",
            options: new GenerateObjectOptions
            {
                SchemaWarningThresholdBytes = 1024
            }
        );

        Assert.NotNull(result);
        Assert.Contains(logMessages, msg => msg.Contains("WARNING: Schema size"));
    }

    [Fact]
    public async Task GenerateObjectAsync_WithCancellationToken_CanBeCancelled()
    {
        var apiKey = GetApiKey();
        var client = new OpenRouterClient(apiKey);

        var schemaJson = @"{""type"": ""object"", ""properties"": {""test"": {""type"": ""string""}}}";
        var schema = JsonSerializer.Deserialize<JsonElement>(schemaJson);

        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await client.GenerateObjectAsync(
                schema: schema,
                prompt: "Test prompt",
                model: "openai/gpt-4o-mini",
                cancellationToken: cts.Token
            );
        });
    }
}
