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
}

