namespace OpenRouter.NET.Artifacts;

public abstract class ArtifactDefinition
{
    public string Type { get; set; } = "";
    public string? PreferredTitle { get; set; }
    public string? Language { get; set; }
    public string? Instruction { get; set; }
    public string? OutputFormat { get; set; }
    public Dictionary<string, string>? CustomAttributes { get; set; }
    
    public virtual string ToSystemPrompt()
    {
        var prompt = $"When creating {Type} content, wrap it in <artifact type=\"{Type}\"";
        
        if (!string.IsNullOrEmpty(PreferredTitle))
        {
            prompt += $" title=\"{PreferredTitle}\"";
        }
        
        if (!string.IsNullOrEmpty(Language))
        {
            prompt += $" language=\"{Language}\"";
        }
        
        if (CustomAttributes != null)
        {
            foreach (var attr in CustomAttributes)
            {
                prompt += $" {attr.Key}=\"{attr.Value}\"";
            }
        }
        
        prompt += ">";
        
        if (!string.IsNullOrEmpty(Instruction))
        {
            prompt += $" {Instruction}";
        }
        
        if (!string.IsNullOrEmpty(OutputFormat))
        {
            prompt += $" Expected format: {OutputFormat}.";
        }
        
        return prompt;
    }
}

public class GenericArtifact : ArtifactDefinition
{
    public GenericArtifact(string type, string? title = null)
    {
        Type = type;
        PreferredTitle = title;
    }
    
    public GenericArtifact WithLanguage(string language)
    {
        Language = language;
        return this;
    }
    
    public GenericArtifact WithInstruction(string instruction)
    {
        Instruction = instruction;
        return this;
    }
    
    public GenericArtifact WithOutputFormat(string format)
    {
        OutputFormat = format;
        return this;
    }
    
    public GenericArtifact WithAttribute(string key, string value)
    {
        CustomAttributes ??= new Dictionary<string, string>();
        CustomAttributes[key] = value;
        return this;
    }
}

public static class Artifacts
{
    public static GenericArtifact Code(string? title = null, string? language = null)
    {
        var artifact = new GenericArtifact("code", title);
        
        if (!string.IsNullOrEmpty(language))
        {
            artifact.WithLanguage(language);
        }
        
        artifact.Instruction = "Include appropriate file extensions in the title.";
        
        return artifact;
    }
    
    public static GenericArtifact Document(string? title = null, string? format = "markdown")
    {
        var artifact = new GenericArtifact("document", title);
        
        if (!string.IsNullOrEmpty(format))
        {
            artifact.WithLanguage(format);
        }
        
        artifact.Instruction = "Create well-formatted, readable documentation.";
        
        return artifact;
    }
    
    public static GenericArtifact Data(string? title = null, string? format = "json")
    {
        var artifact = new GenericArtifact("data", title);
        
        if (!string.IsNullOrEmpty(format))
        {
            artifact.WithLanguage(format);
        }
        
        artifact.Instruction = "Create structured, well-formatted data.";
        
        return artifact;
    }
    
    public static GenericArtifact Custom(string type, string? title = null)
    {
        return new GenericArtifact(type, title);
    }
}

