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
<artifact id=""unique-id-abc12"" type=""code"" title=""filename.ext"" language=""typescript"">
your content here
</artifact>

ARTIFACT ID GUIDELINES (OPTIONAL BUT RECOMMENDED):
- Include an 'id' attribute in the <artifact> tag for better tracking and reference
- Make IDs descriptive and unique (e.g., ""widget-chart-x7k9m"", ""script-parser-r4t2y"")
- Use lowercase, hyphens, and add random characters for uniqueness
- If using client-side tools that reference artifacts, the ID is REQUIRED

CRITICAL ARTIFACT RULES:

1. WHEN TO USE ARTIFACTS:
   - Complete, standalone files the user can save directly (e.g., Component.tsx, script.py, config.json)
   - Self-contained documents or code that represent a single deliverable

2. WHEN NOT TO USE ARTIFACTS:
   - Explanations of how to use code
   - Usage examples or import statements
   - Conversational code snippets
   - Multiple related files in one artifact
   - ANY explanatory text or questions to the user

3. STRUCTURAL REQUIREMENTS:
   - Every <artifact> tag MUST have a matching </artifact> tag
   - NEVER write </artifact> as part of conversational text
   - The closing tag is a structural element, not content
   - Each artifact should contain ONE complete file only

4. CONTENT BOUNDARIES:
   - Artifact content ENDS at </artifact> - nothing after the tag should be inside
   - If you want to explain usage, write it OUTSIDE the artifact tags as normal text
   - Example of CORRECT usage:
     <artifact type=""code"" title=""Component.tsx"" language=""typescript"">
     export default function Component() { return <div>Hello</div>; }
     </artifact>

     To use this component, import it like: import Component from './Component';

   - Example of INCORRECT usage (DO NOT DO THIS):
     <artifact type=""code"" title=""App.tsx"" language=""typescript"">
     import Component from './Component';
     export default App;
     </artifact>

     Would you like me to modify this? ← THIS TEXT SHOULD NOT BE IN THE ARTIFACT

5. GUARANTEE CLOSURE:
   - Before ending your response, ensure all opened artifacts are closed
   - Never leave an artifact tag unclosed

Use artifacts ONLY for complete, self-contained deliverables. For all explanations, usage examples, or conversation, respond normally without artifacts.";

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

ARTIFACT ID GUIDELINES (OPTIONAL BUT RECOMMENDED):
- Include an 'id' attribute in the <artifact> tag for better tracking and reference
- Make IDs descriptive and unique (e.g., ""widget-chart-x7k9m"", ""script-parser-r4t2y"")
- Use lowercase, hyphens, and add random characters for uniqueness
- If using client-side tools that reference artifacts, the ID is REQUIRED

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
                baseInstructions += $"<artifact id=\"unique-id-abc12\" type=\"{typeGroup.Key}\" title=\"filename\" language=\"<language>\">\n";
                baseInstructions += "your content here\n";
                baseInstructions += "</artifact>\n";
            }
            else
            {
                baseInstructions += $"For {typeGroup.Key} content:\n";
                baseInstructions += $"<artifact id=\"unique-id-abc12\" type=\"{typeGroup.Key}\" title=\"title\">\n";
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

        baseInstructions += @"CRITICAL ARTIFACT RULES:

1. STRUCTURAL REQUIREMENTS:
   - Every <artifact> tag MUST have a matching </artifact> tag
   - NEVER write </artifact> as part of conversational text or content
   - The closing tag is a structural element, not content
   - Each artifact contains ONE complete file only

2. WHEN TO USE ARTIFACTS:
   - Complete, standalone files the user can save directly
   - Self-contained documents or code that represent a single deliverable

3. WHEN NOT TO USE ARTIFACTS:
   - Explanations of how to use code (explain OUTSIDE the artifact)
   - Usage examples or import statements (show OUTSIDE the artifact)
   - Conversational text or questions to the user
   - Multiple related files in one artifact

4. CONTENT BOUNDARIES:
   - Artifact content ENDS at </artifact> - nothing after should be inside
   - If explaining usage, write it as normal text OUTSIDE the artifact tags
   - Never include conversational text inside artifact content

5. GUARANTEE CLOSURE:
   - Before ending your response, ensure all opened artifacts are closed
   - Never leave an artifact tag unclosed

Include proper title attribute with file extension for code. Use artifacts ONLY for complete, self-contained deliverables.";

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

