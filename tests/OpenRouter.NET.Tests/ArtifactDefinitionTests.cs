using OpenRouter.NET.Artifacts;
using OpenRouter.NET.Models;

namespace OpenRouter.NET.Tests;

public class ArtifactDefinitionTests
{
    [Fact]
    public void GenericArtifact_ShouldGenerateBasicPrompt()
    {
        var artifact = new GenericArtifact("code", "test.js");
        
        var prompt = artifact.ToSystemPrompt();
        
        Assert.Contains("type=\"code\"", prompt);
        Assert.Contains("title=\"test.js\"", prompt);
    }
    
    [Fact]
    public void GenericArtifact_WithLanguage_ShouldIncludeLanguageAttribute()
    {
        var artifact = new GenericArtifact("code", "test.ts")
            .WithLanguage("typescript");
        
        var prompt = artifact.ToSystemPrompt();
        
        Assert.Contains("language=\"typescript\"", prompt);
    }
    
    [Fact]
    public void GenericArtifact_WithInstruction_ShouldAppendInstruction()
    {
        var artifact = new GenericArtifact("code")
            .WithInstruction("Use functional programming style");
        
        var prompt = artifact.ToSystemPrompt();
        
        Assert.Contains("Use functional programming style", prompt);
    }
    
    [Fact]
    public void GenericArtifact_ShouldSupportFluentChaining()
    {
        var artifact = new GenericArtifact("code", "app.py")
            .WithLanguage("python")
            .WithInstruction("Include type hints")
            .WithOutputFormat("PEP 8 compliant");
        
        Assert.Equal("code", artifact.Type);
        Assert.Equal("app.py", artifact.PreferredTitle);
        Assert.Equal("python", artifact.Language);
        Assert.Contains("type hints", artifact.Instruction);
        Assert.Contains("PEP 8", artifact.OutputFormat);
    }
    
    [Fact]
    public void ArtifactsFactory_Code_ShouldCreateCodeArtifact()
    {
        var artifact = OpenRouter.NET.Artifacts.Artifacts.Code("Button.tsx", "typescript");
        
        Assert.Equal("code", artifact.Type);
        Assert.Equal("Button.tsx", artifact.PreferredTitle);
        Assert.Equal("typescript", artifact.Language);
    }
    
    [Fact]
    public void ArtifactsFactory_Document_ShouldCreateDocumentArtifact()
    {
        var artifact = OpenRouter.NET.Artifacts.Artifacts.Document("README.md");
        
        Assert.Equal("document", artifact.Type);
        Assert.Equal("README.md", artifact.PreferredTitle);
        Assert.Equal("markdown", artifact.Language);
    }
    
    [Fact]
    public void ArtifactsFactory_Data_ShouldCreateDataArtifact()
    {
        var artifact = OpenRouter.NET.Artifacts.Artifacts.Data("config.json", "json");
        
        Assert.Equal("data", artifact.Type);
        Assert.Equal("config.json", artifact.PreferredTitle);
        Assert.Equal("json", artifact.Language);
    }
    
    [Fact]
    public void EnableArtifactSupport_ShouldAddSystemMessage()
    {
        var request = new ChatCompletionRequest
        {
            Model = "test-model",
            Messages = new List<Message>
            {
                Message.FromUser("Hello")
            }
        };
        
        request.EnableArtifactSupport();
        
        Assert.Equal(2, request.Messages.Count);
        Assert.Equal("system", request.Messages[0].Role);
        Assert.Contains("ability to create artifacts", request.Messages[0].Content?.ToString());
    }
    
    [Fact]
    public void EnableArtifacts_WithDefinitions_ShouldIncludeInstructions()
    {
        var request = new ChatCompletionRequest
        {
            Model = "test-model",
            Messages = new List<Message> { Message.FromUser("Hello") }
        };
        
        request.EnableArtifacts(
            OpenRouter.NET.Artifacts.Artifacts.Code("test.ts", "typescript"),
            OpenRouter.NET.Artifacts.Artifacts.Document("README.md")
        );
        
        var systemMessage = request.Messages.First(m => m.Role == "system");
        var content = systemMessage.Content?.ToString() ?? "";
        
        Assert.Contains("code", content);
        Assert.Contains("document", content);
        Assert.Contains("typescript", content);
    }
    
    [Fact]
    public void EnableCodeArtifacts_ShouldEnableCodeWithLanguages()
    {
        var request = new ChatCompletionRequest
        {
            Model = "test-model",
            Messages = new List<Message> { Message.FromUser("Hello") }
        };
        
        request.EnableCodeArtifacts("typescript", "python", "rust");
        
        var systemMessage = request.Messages.First(m => m.Role == "system");
        var content = systemMessage.Content?.ToString() ?? "";
        
        Assert.Contains("code", content);
    }
}

