using OpenRouter.NET.Models;
using OpenRouter.NET.Parsing;

namespace OpenRouter.NET.Tests;

public class ArtifactParserTests
{
    [Fact]
    public void Parse_ShouldDetectSimpleArtifact()
    {
        var parser = new ArtifactParser();
        var input = "Here is code:\n<artifact type=\"code\" title=\"test.js\">console.log('hello');</artifact>\nDone!";
        
        var result = parser.Parse(input);
        
        Assert.Single(result.Artifacts);
        Assert.Equal("code", result.Artifacts[0].Type);
        Assert.Equal("test.js", result.Artifacts[0].Title);
        Assert.Equal("console.log('hello');", result.Artifacts[0].Content);
        Assert.Equal("Here is code:\n\nDone!", result.TextWithoutArtifacts);
    }
    
    [Fact]
    public void Parse_ShouldHandleArtifactWithLanguage()
    {
        var parser = new ArtifactParser();
        var input = "<artifact type=\"react_component\" title=\"Button.tsx\" language=\"typescript\">export const Button = () => <button>Click</button>;</artifact>";
        
        var result = parser.Parse(input);
        
        Assert.Single(result.Artifacts);
        Assert.Equal("typescript", result.Artifacts[0].Language);
    }
    
    [Fact]
    public void Parse_ShouldHandleMultipleArtifacts()
    {
        var parser = new ArtifactParser();
        var input = @"First file:
<artifact type=""code"" title=""file1.js"">const x = 1;</artifact>
Second file:
<artifact type=""code"" title=""file2.js"">const y = 2;</artifact>";
        
        var result = parser.Parse(input);
        
        Assert.Equal(2, result.Artifacts.Count);
        Assert.Equal("file1.js", result.Artifacts[0].Title);
        Assert.Equal("file2.js", result.Artifacts[1].Title);
    }
    
    [Fact]
    public void Parse_ShouldHandleMultilineContent()
    {
        var parser = new ArtifactParser();
        var input = @"<artifact type=""code"" title=""test.js"">
import React from 'react';

export const Component = () => {
  return <div>Hello</div>;
};
</artifact>";
        
        var result = parser.Parse(input);
        
        Assert.Single(result.Artifacts);
        Assert.Contains("import React", result.Artifacts[0].Content);
        Assert.Contains("return <div>Hello</div>", result.Artifacts[0].Content);
    }
    
    [Fact]
    public void Parse_ShouldHandleNoArtifacts()
    {
        var parser = new ArtifactParser();
        var input = "Just plain text with no artifacts";
        
        var result = parser.Parse(input);
        
        Assert.Empty(result.Artifacts);
        Assert.Equal(input, result.TextWithoutArtifacts);
    }
    
    [Fact]
    public void ParseIncremental_ShouldDetectArtifactAcrossMultipleChunks()
    {
        var parser = new ArtifactParser();
        
        // Simulate streaming chunks
        var chunk1 = "Text before <artif";
        var chunk2 = "act type=\"code\" title=\"test.js\">";
        var chunk3 = "console.log('hel";
        var chunk4 = "lo');</artifact> Text after";
        
        var result1 = parser.ParseIncremental(chunk1);
        var result2 = parser.ParseIncremental(chunk2);
        var result3 = parser.ParseIncremental(chunk3);
        var result4 = parser.ParseIncremental(chunk4);
        
        // Should emit started when opening tag complete
        Assert.Null(result1.ArtifactStarted);
        Assert.NotNull(result2.ArtifactStarted);
        Assert.Equal("test.js", result2.ArtifactStarted!.Title);
        
        // Should emit completed when closing tag found
        Assert.NotNull(result4.ArtifactCompleted);
        Assert.Equal("console.log('hello');", result4.ArtifactCompleted!.Content);
        
        // Text before artifact should be emitted
        Assert.Equal("Text before ", result1.TextDelta);
        // Text after artifact should be emitted
        Assert.Equal(" Text after", result4.TextDelta);
    }
    
    [Fact]
    public void ParseIncremental_ShouldEmitTextOutsideArtifacts()
    {
        var parser = new ArtifactParser();
        
        var chunk1 = "Before <artifact type=\"code\" title=\"test.js\">content</artifact> After";
        
        var result = parser.ParseIncremental(chunk1);
        
        Assert.Equal("Before  After", result.TextDelta);
        Assert.NotNull(result.ArtifactCompleted);
    }
    
    [Fact]
    public void ParseIncremental_ShouldHandlePartialOpeningTag()
    {
        var parser = new ArtifactParser();
        
        // Opening tag split across chunks
        var chunk1 = "Text <art";
        var chunk2 = "ifact type=\"code\" tit";
        var chunk3 = "le=\"test.js\">content</artifact>";
        
        var result1 = parser.ParseIncremental(chunk1);
        var result2 = parser.ParseIncremental(chunk2);
        var result3 = parser.ParseIncremental(chunk3);
        
        // Text before artifact should be emitted
        Assert.Equal("Text ", result1.TextDelta);
        
        // Artifact should be detected when complete
        Assert.NotNull(result3.ArtifactCompleted);
        Assert.Equal("test.js", result3.ArtifactCompleted!.Title);
    }
    
    [Fact]
    public void Parse_ShouldPreserveLLMProvidedId()
    {
        var parser = new ArtifactParser();
        var input = "<artifact id=\"widget-revenue-x7k9m\" type=\"code\" title=\"Revenue Widget\">const revenue = 100;</artifact>";
        
        var result = parser.Parse(input);
        
        Assert.Single(result.Artifacts);
        Assert.Equal("widget-revenue-x7k9m", result.Artifacts[0].Id);
    }
    
    [Fact]
    public void Parse_ShouldGenerateFallbackIdWhenMissing()
    {
        var parser = new ArtifactParser();
        var input = "<artifact type=\"code\" title=\"test.js\">console.log('test');</artifact>";
        
        var result = parser.Parse(input);
        
        Assert.Single(result.Artifacts);
        Assert.NotNull(result.Artifacts[0].Id);
        Assert.StartsWith("art_", result.Artifacts[0].Id);
    }
    
    [Fact]
    public void Parse_ShouldHandleAttributesInDifferentOrders()
    {
        var parser = new ArtifactParser();
        
        var input1 = "<artifact title=\"test1.js\" type=\"code\" language=\"javascript\">code1</artifact>";
        var input2 = "<artifact language=\"typescript\" title=\"test2.ts\" type=\"code\">code2</artifact>";
        var input3 = "<artifact type=\"code\" language=\"python\" title=\"test3.py\">code3</artifact>";
        
        var result1 = parser.Parse(input1);
        var result2 = parser.Parse(input2);
        var result3 = parser.Parse(input3);
        
        Assert.Single(result1.Artifacts);
        Assert.Equal("test1.js", result1.Artifacts[0].Title);
        Assert.Equal("code", result1.Artifacts[0].Type);
        Assert.Equal("javascript", result1.Artifacts[0].Language);
        
        Assert.Single(result2.Artifacts);
        Assert.Equal("test2.ts", result2.Artifacts[0].Title);
        Assert.Equal("code", result2.Artifacts[0].Type);
        Assert.Equal("typescript", result2.Artifacts[0].Language);
        
        Assert.Single(result3.Artifacts);
        Assert.Equal("test3.py", result3.Artifacts[0].Title);
        Assert.Equal("code", result3.Artifacts[0].Type);
        Assert.Equal("python", result3.Artifacts[0].Language);
    }
    
    [Fact]
    public void Parse_ShouldHandleIdAtDifferentPositions()
    {
        var parser = new ArtifactParser();
        
        var input1 = "<artifact id=\"test-1\" type=\"code\" title=\"First\">content1</artifact>";
        var input2 = "<artifact type=\"code\" id=\"test-2\" title=\"Second\">content2</artifact>";
        var input3 = "<artifact type=\"code\" title=\"Third\" id=\"test-3\">content3</artifact>";
        
        var result1 = parser.Parse(input1);
        var result2 = parser.Parse(input2);
        var result3 = parser.Parse(input3);
        
        Assert.Equal("test-1", result1.Artifacts[0].Id);
        Assert.Equal("test-2", result2.Artifacts[0].Id);
        Assert.Equal("test-3", result3.Artifacts[0].Id);
    }
    
    [Fact]
    public void Parse_ShouldUseDefaultsForMissingAttributes()
    {
        var parser = new ArtifactParser();
        var input = "<artifact>just content</artifact>";
        
        var result = parser.Parse(input);
        
        Assert.Single(result.Artifacts);
        Assert.Equal("code", result.Artifacts[0].Type);
        Assert.Equal("Untitled", result.Artifacts[0].Title);
        Assert.Null(result.Artifacts[0].Language);
        Assert.StartsWith("art_", result.Artifacts[0].Id);
    }
    
    [Fact]
    public void Parse_ShouldHandleExtraUnknownAttributes()
    {
        var parser = new ArtifactParser();
        var input = "<artifact id=\"test-id\" type=\"code\" title=\"Test\" custom=\"value\" data-test=\"123\">content</artifact>";
        
        var result = parser.Parse(input);
        
        Assert.Single(result.Artifacts);
        Assert.Equal("test-id", result.Artifacts[0].Id);
        Assert.Equal("code", result.Artifacts[0].Type);
        Assert.Equal("Test", result.Artifacts[0].Title);
        Assert.Equal("content", result.Artifacts[0].Content);
    }
    
    [Fact]
    public void ParseIncremental_ShouldPreserveLLMProvidedId()
    {
        var parser = new ArtifactParser();
        var chunk = "<artifact id=\"widget-chart-abc123\" type=\"code\" title=\"Chart Widget\">chart code</artifact>";
        
        var result = parser.ParseIncremental(chunk);
        
        Assert.NotNull(result.ArtifactCompleted);
        Assert.Equal("widget-chart-abc123", result.ArtifactCompleted!.ArtifactId);
    }
    
    [Fact]
    public void ParseIncremental_ShouldGenerateFallbackIdWhenMissing()
    {
        var parser = new ArtifactParser();
        var chunk = "<artifact type=\"code\" title=\"test.js\">test code</artifact>";
        
        var result = parser.ParseIncremental(chunk);
        
        Assert.NotNull(result.ArtifactCompleted);
        Assert.NotNull(result.ArtifactCompleted!.ArtifactId);
        Assert.StartsWith("art_", result.ArtifactCompleted!.ArtifactId);
    }
    
    [Fact]
    public void ParseIncremental_ShouldHandleAttributesInDifferentOrders()
    {
        var parser = new ArtifactParser();
        var chunk = "<artifact title=\"myfile.ts\" language=\"typescript\" type=\"code\" id=\"custom-id\">typescript code</artifact>";
        
        var result = parser.ParseIncremental(chunk);
        
        Assert.NotNull(result.ArtifactStarted);
        Assert.Equal("custom-id", result.ArtifactStarted!.ArtifactId);
        Assert.Equal("code", result.ArtifactStarted!.Type);
        Assert.Equal("myfile.ts", result.ArtifactStarted!.Title);
        Assert.Equal("typescript", result.ArtifactStarted!.Language);
    }
    
    [Fact]
    public void ParseIncremental_ShouldGenerateSequentialIdsForMultipleArtifacts()
    {
        var parser = new ArtifactParser();
        
        var result1 = parser.ParseIncremental("<artifact type=\"code\" title=\"First\">code1</artifact>");
        var result2 = parser.ParseIncremental("<artifact type=\"code\" title=\"Second\">code2</artifact>");
        var result3 = parser.ParseIncremental("<artifact type=\"code\" title=\"Third\">code3</artifact>");
        
        Assert.NotNull(result1.ArtifactCompleted);
        Assert.NotNull(result2.ArtifactCompleted);
        Assert.NotNull(result3.ArtifactCompleted);
        
        Assert.Equal("art_1", result1.ArtifactCompleted!.ArtifactId);
        Assert.Equal("art_2", result2.ArtifactCompleted!.ArtifactId);
        Assert.Equal("art_3", result3.ArtifactCompleted!.ArtifactId);
    }
    
    [Fact]
    public void Parse_ShouldHandleCaseInsensitiveAttributes()
    {
        var parser = new ArtifactParser();
        var input = "<artifact ID=\"test-id\" TYPE=\"code\" TITLE=\"Test\" LANGUAGE=\"python\">content</artifact>";
        
        var result = parser.Parse(input);
        
        Assert.Single(result.Artifacts);
        Assert.Equal("test-id", result.Artifacts[0].Id);
        Assert.Equal("code", result.Artifacts[0].Type);
        Assert.Equal("Test", result.Artifacts[0].Title);
        Assert.Equal("python", result.Artifacts[0].Language);
    }
}

