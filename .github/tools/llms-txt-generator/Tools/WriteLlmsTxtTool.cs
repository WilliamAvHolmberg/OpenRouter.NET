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

    [ToolMethod("Write the final llms.txt content. Call this ONLY ONCE when you have completed your analysis and are ready to output the complete documentation. After calling this successfully, your task is COMPLETE and you should stop.")]
    public string WriteLlmsTxt(
        [ToolParameter("The complete llms.txt content with all sections, examples, and documentation. Must be comprehensive (2000+ lines recommended).")] string content)
    {
        try
        {
            if (_hasWritten)
            {
                return "❌ ERROR: llms.txt has already been written successfully. " +
                       "Tool can only be called once per execution. " +
                       "Your task is COMPLETE. Do not call this tool again.";
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                return "❌ ERROR: Content cannot be empty. Please provide the complete llms.txt documentation.";
            }

            if (content.Length < 1000)
            {
                return $"❌ ERROR: Content is too short ({content.Length} characters). " +
                       "llms.txt should be comprehensive documentation (at least 10,000 characters recommended, 50,000+ for thorough docs). " +
                       "Did you complete your analysis? Have you read all the extension methods, samples, and existing llms.txt?";
            }

            // Write the file
            File.WriteAllText(_outputPath, content);
            _hasWritten = true;

            var lines = content.Split('\n').Length;
            var chars = content.Length;
            var fileSize = new FileInfo(_outputPath).Length;
            
            return $"🎉 SUCCESS! llms.txt has been written successfully!\n" +
                   $"\n" +
                   $"📄 Location: {_outputPath}\n" +
                   $"📏 Lines: {lines:N0}\n" +
                   $"📝 Characters: {chars:N0}\n" +
                   $"💾 File size: {fileSize:N0} bytes\n" +
                   $"\n" +
                   $"✅ Your task is COMPLETE. The documentation has been generated.\n" +
                   $"✅ You should now STOP. Do not continue exploring or call tools.\n" +
                   $"✅ Do not call WriteLlmsTxt again.\n" +
                   $"\n" +
                   $"The agent can now terminate successfully.";
        }
        catch (Exception ex)
        {
            _hasWritten = false; // Allow retry if there was an error
            return $"❌ ERROR writing llms.txt: {ex.Message}\n" +
                   $"Stack trace: {ex.StackTrace}\n" +
                   $"Please try again after fixing the issue.";
        }
    }

    public bool HasWritten => _hasWritten;
}
