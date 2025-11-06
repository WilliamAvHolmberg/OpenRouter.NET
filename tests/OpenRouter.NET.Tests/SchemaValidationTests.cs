using OpenRouter.NET;
using System.Reflection;
using System.Text.Json;

namespace OpenRouter.NET.Tests;

public class SchemaValidationTests
{
    private static void InvokeValidateMethod(OpenRouterClient client, JsonElement schema, JsonElement generatedObject)
    {
        // Access the ObjectGenerator through the internal testing property
        var objectGenerator = client.ObjectGeneratorForTesting;
        objectGenerator.ValidateJsonAgainstSchema(schema, generatedObject);
    }

    [Fact]
    public void ValidateJsonAgainstSchema_WithValidObject_DoesNotThrow()
    {
        var client = new OpenRouterClient("test-key");

        var schemaJson = @"{
            ""type"": ""object"",
            ""properties"": {
                ""name"": { ""type"": ""string"" },
                ""age"": { ""type"": ""number"" }
            },
            ""required"": [""name"", ""age""]
        }";

        var objectJson = @"{
            ""name"": ""John Doe"",
            ""age"": 30
        }";

        var schema = JsonSerializer.Deserialize<JsonElement>(schemaJson);
        var obj = JsonSerializer.Deserialize<JsonElement>(objectJson);

        var exception = Record.Exception(() => InvokeValidateMethod(client, schema, obj));
        
        Assert.Null(exception);
    }

    [Fact]
    public void ValidateJsonAgainstSchema_WithMissingRequiredField_Throws()
    {
        var client = new OpenRouterClient("test-key");

        var schemaJson = @"{
            ""type"": ""object"",
            ""properties"": {
                ""name"": { ""type"": ""string"" },
                ""age"": { ""type"": ""number"" }
            },
            ""required"": [""name"", ""age""]
        }";

        var objectJson = @"{
            ""name"": ""John Doe""
        }";

        var schema = JsonSerializer.Deserialize<JsonElement>(schemaJson);
        var obj = JsonSerializer.Deserialize<JsonElement>(objectJson);

        var exception = Assert.Throws<TargetInvocationException>(() => 
            InvokeValidateMethod(client, schema, obj)
        );

        Assert.NotNull(exception.InnerException);
        Assert.IsType<OpenRouterException>(exception.InnerException);
        Assert.Contains("does not match schema", exception.InnerException.Message);
    }

    [Fact]
    public void ValidateJsonAgainstSchema_WithWrongType_Throws()
    {
        var client = new OpenRouterClient("test-key");

        var schemaJson = @"{
            ""type"": ""object"",
            ""properties"": {
                ""name"": { ""type"": ""string"" },
                ""age"": { ""type"": ""number"" }
            },
            ""required"": [""name"", ""age""]
        }";

        var objectJson = @"{
            ""name"": ""John Doe"",
            ""age"": ""thirty""
        }";

        var schema = JsonSerializer.Deserialize<JsonElement>(schemaJson);
        var obj = JsonSerializer.Deserialize<JsonElement>(objectJson);

        var exception = Assert.Throws<TargetInvocationException>(() => 
            InvokeValidateMethod(client, schema, obj)
        );

        Assert.NotNull(exception.InnerException);
        Assert.IsType<OpenRouterException>(exception.InnerException);
        Assert.Contains("does not match schema", exception.InnerException.Message);
    }

    [Fact]
    public void ValidateJsonAgainstSchema_WithNestedObject_ValidatesCorrectly()
    {
        var client = new OpenRouterClient("test-key");

        var schemaJson = @"{
            ""type"": ""object"",
            ""properties"": {
                ""user"": {
                    ""type"": ""object"",
                    ""properties"": {
                        ""name"": { ""type"": ""string"" },
                        ""email"": { ""type"": ""string"" }
                    },
                    ""required"": [""name"", ""email""]
                }
            },
            ""required"": [""user""]
        }";

        var objectJson = @"{
            ""user"": {
                ""name"": ""John Doe"",
                ""email"": ""john@example.com""
            }
        }";

        var schema = JsonSerializer.Deserialize<JsonElement>(schemaJson);
        var obj = JsonSerializer.Deserialize<JsonElement>(objectJson);

        var exception = Record.Exception(() => InvokeValidateMethod(client, schema, obj));
        
        Assert.Null(exception);
    }

    [Fact]
    public void ValidateJsonAgainstSchema_WithNestedObjectMissingField_Throws()
    {
        var client = new OpenRouterClient("test-key");

        var schemaJson = @"{
            ""type"": ""object"",
            ""properties"": {
                ""user"": {
                    ""type"": ""object"",
                    ""properties"": {
                        ""name"": { ""type"": ""string"" },
                        ""email"": { ""type"": ""string"" }
                    },
                    ""required"": [""name"", ""email""]
                }
            },
            ""required"": [""user""]
        }";

        var objectJson = @"{
            ""user"": {
                ""name"": ""John Doe""
            }
        }";

        var schema = JsonSerializer.Deserialize<JsonElement>(schemaJson);
        var obj = JsonSerializer.Deserialize<JsonElement>(objectJson);

        var exception = Assert.Throws<TargetInvocationException>(() => 
            InvokeValidateMethod(client, schema, obj)
        );

        Assert.NotNull(exception.InnerException);
        Assert.IsType<OpenRouterException>(exception.InnerException);
    }

    [Fact]
    public void ValidateJsonAgainstSchema_WithArray_ValidatesCorrectly()
    {
        var client = new OpenRouterClient("test-key");

        var schemaJson = @"{
            ""type"": ""object"",
            ""properties"": {
                ""items"": {
                    ""type"": ""array"",
                    ""items"": {
                        ""type"": ""object"",
                        ""properties"": {
                            ""id"": { ""type"": ""number"" },
                            ""name"": { ""type"": ""string"" }
                        },
                        ""required"": [""id"", ""name""]
                    }
                }
            },
            ""required"": [""items""]
        }";

        var objectJson = @"{
            ""items"": [
                { ""id"": 1, ""name"": ""Item 1"" },
                { ""id"": 2, ""name"": ""Item 2"" }
            ]
        }";

        var schema = JsonSerializer.Deserialize<JsonElement>(schemaJson);
        var obj = JsonSerializer.Deserialize<JsonElement>(objectJson);

        var exception = Record.Exception(() => InvokeValidateMethod(client, schema, obj));
        
        Assert.Null(exception);
    }

    [Fact]
    public void ValidateJsonAgainstSchema_WithArrayItemMissingField_Throws()
    {
        var client = new OpenRouterClient("test-key");

        var schemaJson = @"{
            ""type"": ""object"",
            ""properties"": {
                ""items"": {
                    ""type"": ""array"",
                    ""items"": {
                        ""type"": ""object"",
                        ""properties"": {
                            ""id"": { ""type"": ""number"" },
                            ""name"": { ""type"": ""string"" }
                        },
                        ""required"": [""id"", ""name""]
                    }
                }
            },
            ""required"": [""items""]
        }";

        var objectJson = @"{
            ""items"": [
                { ""id"": 1, ""name"": ""Item 1"" },
                { ""id"": 2 }
            ]
        }";

        var schema = JsonSerializer.Deserialize<JsonElement>(schemaJson);
        var obj = JsonSerializer.Deserialize<JsonElement>(objectJson);

        var exception = Assert.Throws<TargetInvocationException>(() => 
            InvokeValidateMethod(client, schema, obj)
        );

        Assert.NotNull(exception.InnerException);
        Assert.IsType<OpenRouterException>(exception.InnerException);
    }

    [Fact]
    public void ValidateJsonAgainstSchema_WithOptionalFields_AllowsMissingOptional()
    {
        var client = new OpenRouterClient("test-key");

        var schemaJson = @"{
            ""type"": ""object"",
            ""properties"": {
                ""name"": { ""type"": ""string"" },
                ""age"": { ""type"": ""number"" },
                ""email"": { ""type"": ""string"" }
            },
            ""required"": [""name""]
        }";

        var objectJson = @"{
            ""name"": ""John Doe""
        }";

        var schema = JsonSerializer.Deserialize<JsonElement>(schemaJson);
        var obj = JsonSerializer.Deserialize<JsonElement>(objectJson);

        var exception = Record.Exception(() => InvokeValidateMethod(client, schema, obj));
        
        Assert.Null(exception);
    }

    [Fact]
    public void ValidateJsonAgainstSchema_WithEnum_ValidatesCorrectly()
    {
        var client = new OpenRouterClient("test-key");

        var schemaJson = @"{
            ""type"": ""object"",
            ""properties"": {
                ""status"": { 
                    ""type"": ""string"",
                    ""enum"": [""active"", ""inactive"", ""pending""]
                }
            },
            ""required"": [""status""]
        }";

        var objectJson = @"{
            ""status"": ""active""
        }";

        var schema = JsonSerializer.Deserialize<JsonElement>(schemaJson);
        var obj = JsonSerializer.Deserialize<JsonElement>(objectJson);

        var exception = Record.Exception(() => InvokeValidateMethod(client, schema, obj));
        
        Assert.Null(exception);
    }

    [Fact]
    public void ValidateJsonAgainstSchema_WithInvalidEnumValue_Throws()
    {
        var client = new OpenRouterClient("test-key");

        var schemaJson = @"{
            ""type"": ""object"",
            ""properties"": {
                ""status"": { 
                    ""type"": ""string"",
                    ""enum"": [""active"", ""inactive"", ""pending""]
                }
            },
            ""required"": [""status""]
        }";

        var objectJson = @"{
            ""status"": ""unknown""
        }";

        var schema = JsonSerializer.Deserialize<JsonElement>(schemaJson);
        var obj = JsonSerializer.Deserialize<JsonElement>(objectJson);

        var exception = Assert.Throws<TargetInvocationException>(() => 
            InvokeValidateMethod(client, schema, obj)
        );

        Assert.NotNull(exception.InnerException);
        Assert.IsType<OpenRouterException>(exception.InnerException);
    }

    [Fact]
    public void ValidateJsonAgainstSchema_WithBoolean_ValidatesCorrectly()
    {
        var client = new OpenRouterClient("test-key");

        var schemaJson = @"{
            ""type"": ""object"",
            ""properties"": {
                ""isActive"": { ""type"": ""boolean"" },
                ""count"": { ""type"": ""integer"" }
            },
            ""required"": [""isActive"", ""count""]
        }";

        var objectJson = @"{
            ""isActive"": true,
            ""count"": 42
        }";

        var schema = JsonSerializer.Deserialize<JsonElement>(schemaJson);
        var obj = JsonSerializer.Deserialize<JsonElement>(objectJson);

        var exception = Record.Exception(() => InvokeValidateMethod(client, schema, obj));
        
        Assert.Null(exception);
    }

    [Fact]
    public void ValidateJsonAgainstSchema_WithComplexRealWorldSchema_ValidatesCorrectly()
    {
        var client = new OpenRouterClient("test-key");

        var schemaJson = @"{
            ""type"": ""object"",
            ""properties"": {
                ""translations"": {
                    ""type"": ""array"",
                    ""items"": {
                        ""type"": ""object"",
                        ""properties"": {
                            ""language"": { ""type"": ""string"" },
                            ""languageCode"": { ""type"": ""string"" },
                            ""translatedText"": { ""type"": ""string"" }
                        },
                        ""required"": [""language"", ""languageCode"", ""translatedText""]
                    }
                }
            },
            ""required"": [""translations""]
        }";

        var objectJson = @"{
            ""translations"": [
                {
                    ""language"": ""Spanish"",
                    ""languageCode"": ""es"",
                    ""translatedText"": ""Hola Mundo""
                },
                {
                    ""language"": ""French"",
                    ""languageCode"": ""fr"",
                    ""translatedText"": ""Bonjour le monde""
                }
            ]
        }";

        var schema = JsonSerializer.Deserialize<JsonElement>(schemaJson);
        var obj = JsonSerializer.Deserialize<JsonElement>(objectJson);

        var exception = Record.Exception(() => InvokeValidateMethod(client, schema, obj));
        
        Assert.Null(exception);
    }
}


