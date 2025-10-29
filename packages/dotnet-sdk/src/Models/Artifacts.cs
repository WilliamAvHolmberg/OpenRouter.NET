namespace OpenRouter.NET.Models;

public abstract record ArtifactEvent
{
    public string ArtifactId { get; init; } = "";
    public string Type { get; init; } = "";
}

public record ArtifactStarted : ArtifactEvent
{
    public string Title { get; init; } = "";
    public string? Language { get; init; }
    
    public ArtifactStarted(string artifactId, string type, string title, string? language = null)
    {
        ArtifactId = artifactId;
        Type = type;
        Title = title;
        Language = language;
    }
}

public record ArtifactContent : ArtifactEvent
{
    public string ContentDelta { get; init; } = "";
    
    public ArtifactContent(string artifactId, string type, string contentDelta)
    {
        ArtifactId = artifactId;
        Type = type;
        ContentDelta = contentDelta;
    }
}

public record ArtifactCompleted : ArtifactEvent
{
    public string Title { get; init; } = "";
    public string Content { get; init; } = "";
    public string? Language { get; init; }
    
    public ArtifactCompleted(string artifactId, string type, string title, string content, string? language = null)
    {
        ArtifactId = artifactId;
        Type = type;
        Title = title;
        Content = content;
        Language = language;
    }
}

public class Artifact
{
    public string Id { get; init; } = "";
    public string Type { get; init; } = "";
    public string Title { get; init; } = "";
    public string Content { get; init; } = "";
    public string? Language { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
    
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

