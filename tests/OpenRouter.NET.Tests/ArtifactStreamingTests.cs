using OpenRouter.NET.Models;
using OpenRouter.NET.Streaming;

namespace OpenRouter.NET.Tests;

public class ArtifactStreamingTests
{
    [Fact]
    public void ArtifactStarted_ShouldContainRequiredProperties()
    {
        var artifact = new ArtifactStarted(
            artifactId: "art_123",
            type: "react_component",
            title: "Button.tsx",
            language: "typescript"
        );
        
        Assert.Equal("art_123", artifact.ArtifactId);
        Assert.Equal("react_component", artifact.Type);
        Assert.Equal("Button.tsx", artifact.Title);
        Assert.Equal("typescript", artifact.Language);
    }
    
    [Fact]
    public void ArtifactContent_ShouldAccumulateContent()
    {
        var content1 = new ArtifactContent("art_123", "react_component", "import React");
        var content2 = new ArtifactContent("art_123", "react_component", " from 'react';");
        
        var accumulated = content1.ContentDelta + content2.ContentDelta;
        
        Assert.Equal("import React from 'react';", accumulated);
    }
    
    [Fact]
    public void ArtifactCompleted_ShouldHaveFullContent()
    {
        var completed = new ArtifactCompleted(
            artifactId: "art_123",
            type: "react_component",
            title: "Button.tsx",
            content: "import React from 'react';\n\nexport const Button = () => <button>Click</button>;",
            language: "typescript"
        );
        
        Assert.Contains("import React", completed.Content);
        Assert.Contains("export const Button", completed.Content);
    }
    
    [Fact]
    public void StreamChunk_CanContainArtifactEvent()
    {
        var artifact = new ArtifactStarted("art_123", "react_component", "Button.tsx");
        
        var chunk = new StreamChunk
        {
            IsFirstChunk = false,
            ElapsedTime = TimeSpan.FromMilliseconds(100),
            ChunkIndex = 5,
            Artifact = artifact
        };
        
        Assert.NotNull(chunk.Artifact);
        Assert.IsType<ArtifactStarted>(chunk.Artifact);
        Assert.Equal("art_123", chunk.Artifact.ArtifactId);
    }
    
    [Fact]
    public void StreamChunk_CanContainBothTextAndArtifact()
    {
        var chunk = new StreamChunk
        {
            TextDelta = "Here's a component: ",
            Artifact = new ArtifactStarted("art_123", "react_component", "Button.tsx")
        };
        
        Assert.NotNull(chunk.TextDelta);
        Assert.NotNull(chunk.Artifact);
    }
}

