using OpenRouter.NET.Artifacts;
using OpenRouter.NET.Models;

namespace OpenRouter.NET;

public static class ArtifactRequestExtensions
{
    public static ChatCompletionRequest EnableArtifactSupport(
        this ChatCompletionRequest request,
        string? customInstructions = null)
    {
        var instructions = @"You have the ability to create artifacts when providing code, documents, or other deliverables to the user.

When creating artifacts, wrap them in tags like this:
<artifact type=""code"" title=""filename.ext"" language=""typescript"">
your content here
</artifact>

Use artifacts when you're delivering complete, self-contained code or documents that the user can save and use directly. For explanations or conversation, respond normally without artifacts.";

        if (!string.IsNullOrEmpty(customInstructions))
        {
            instructions += $"\n\nAdditional guidance: {customInstructions}";
        }

        PrependOrAppendToSystemMessage(request, instructions);
        
        return request;
    }
    
    public static ChatCompletionRequest EnableArtifacts(
        this ChatCompletionRequest request,
        params ArtifactDefinition[] artifactDefinitions)
    {
        if (artifactDefinitions.Length == 0)
        {
            return request.EnableArtifactSupport();
        }
        
        var baseInstructions = @"You have the ability to create artifacts when providing deliverables to the user.

Available artifact types:
";

        var typesByCategory = artifactDefinitions
            .GroupBy(d => d.Type)
            .Select(g => new
            {
                Type = g.Key,
                Languages = g.Where(d => !string.IsNullOrEmpty(d.Language))
                             .Select(d => d.Language!)
                             .Distinct()
                             .ToList()
            })
            .ToList();

        foreach (var category in typesByCategory)
        {
            var desc = $"- {category.Type}";
            if (category.Languages.Count > 0)
            {
                desc += $" (languages: {string.Join(", ", category.Languages)})";
            }
            baseInstructions += desc + "\n";
        }

        baseInstructions += @"
CRITICAL: You MUST use the exact XML format below. Always include BOTH opening and closing tags.

";

        var instructionsByType = artifactDefinitions
            .GroupBy(d => d.Type)
            .ToList();

        foreach (var typeGroup in instructionsByType)
        {
            var hasLanguages = typeGroup.Any(d => !string.IsNullOrEmpty(d.Language));

            if (hasLanguages)
            {
                baseInstructions += $"For {typeGroup.Key} content:\n";
                baseInstructions += $"<artifact type=\"{typeGroup.Key}\" title=\"filename\" language=\"<language>\">\n";
                baseInstructions += "your content here\n";
                baseInstructions += "</artifact>\n";
            }
            else
            {
                baseInstructions += $"For {typeGroup.Key} content:\n";
                baseInstructions += $"<artifact type=\"{typeGroup.Key}\" title=\"title\">\n";
                baseInstructions += "your content here\n";
                baseInstructions += "</artifact>\n";
            }

            var firstDef = typeGroup.First();
            if (!string.IsNullOrEmpty(firstDef.Instruction))
            {
                baseInstructions += $"{firstDef.Instruction}\n";
            }

            baseInstructions += "\n";
        }

        baseInstructions += @"IMPORTANT RULES:
1. ALWAYS close your artifact tags with </artifact>
2. Include proper title attribute with file extension for code
3. Use artifacts for complete, self-contained deliverables
4. Use normal text for explanations and conversation";
        
        PrependOrAppendToSystemMessage(request, baseInstructions);
        
        return request;
    }
    
    public static ChatCompletionRequest EnableCodeArtifacts(
        this ChatCompletionRequest request,
        params string[] languages)
    {
        var artifact = Artifacts.Artifacts.Code();
        if (languages.Length > 0)
        {
            artifact.WithLanguage(string.Join(", ", languages));
        }
        return request.EnableArtifacts(artifact);
    }
    
    public static ChatCompletionRequest EnableDocumentArtifacts(
        this ChatCompletionRequest request,
        params string[] formats)
    {
        var artifact = Artifacts.Artifacts.Document();
        if (formats.Length > 0)
        {
            artifact.WithLanguage(string.Join(", ", formats));
        }
        return request.EnableArtifacts(artifact);
    }
    
    private static void PrependOrAppendToSystemMessage(ChatCompletionRequest request, string instructions)
    {
        var systemMessage = request.Messages.FirstOrDefault(m => m.Role == "system");
        
        if (systemMessage != null)
        {
            var existingContent = systemMessage.Content?.ToString() ?? "";
            systemMessage.Content = $"{existingContent}\n\n{instructions}";
        }
        else
        {
            request.Messages.Insert(0, Message.FromSystem(instructions));
        }
    }
}

