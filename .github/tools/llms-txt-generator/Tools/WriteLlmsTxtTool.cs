using OpenRouter.NET.Tools;

namespace LlmsTxtGenerator.Tools;

public class WriteLlmsTxtTool
{
    private readonly string _outputPath;
    private bool _hasWritten = false;

    public WriteLlmsTxtTool(string outputPath)
    {
        _outputPath = outputPath;
    }

    [ToolMethod("Write the final llms.txt content. Call this ONLY when you have completed your analysis and are ready to generate the complete documentation.")]
    public string WriteLlmsTxt(
        [ToolParameter("The complete llms.txt content with all sections, examples, and documentation")] string content)
    {
        try
        {
            if (_hasWritten)
            {
                return "❌ llms.txt has already been written. Tool can only be called once per execution.";
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                return "❌ Content cannot be empty";
            }

            if (content.Length < 1000)
            {
                return "❌ Content seems too short. llms.txt should be comprehensive documentation (at least 1000 characters). Did you complete your analysis?";
            }

            File.WriteAllText(_outputPath, content);
            _hasWritten = true;

            var lines = content.Split('\n').Length;
            var chars = content.Length;
            
            return $"✅ Successfully wrote llms.txt!\n" +
                   $"   Location: {_outputPath}\n" +
                   $"   Lines: {lines:N0}\n" +
                   $"   Characters: {chars:N0}\n" +
                   $"\n" +
                   $"The documentation has been generated successfully.";
        }
        catch (Exception ex)
        {
            return $"❌ Error writing llms.txt: {ex.Message}";
        }
    }

    public bool HasWritten => _hasWritten;
}
