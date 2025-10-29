using OpenRouter.NET.Tools;
using System.Text;

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
        // First, validate and save diagnostics
        var diagnosticInfo = new StringBuilder();
        diagnosticInfo.AppendLine("=".PadRight(70, '='));
        diagnosticInfo.AppendLine("WriteLlmsTxt DIAGNOSTIC INFORMATION");
        diagnosticInfo.AppendLine("=".PadRight(70, '='));
        diagnosticInfo.AppendLine($"Output path: {_outputPath}");
        diagnosticInfo.AppendLine($"Already written: {_hasWritten}");
        diagnosticInfo.AppendLine($"Content provided: {content != null}");
        diagnosticInfo.AppendLine($"Content length: {content?.Length ?? 0} characters");
        diagnosticInfo.AppendLine($"Content lines: {(content?.Split('\n').Length ?? 0)}");
        diagnosticInfo.AppendLine($"Current directory: {Directory.GetCurrentDirectory()}");
        
        var outputDir = Path.GetDirectoryName(_outputPath);
        if (!string.IsNullOrEmpty(outputDir))
        {
            diagnosticInfo.AppendLine($"Output directory: {outputDir}");
            diagnosticInfo.AppendLine($"Directory exists: {Directory.Exists(outputDir)}");
        }
        
        try
        {
            // Validation checks
            if (_hasWritten)
            {
                var errorMsg = "❌ CRITICAL ERROR: llms.txt has already been written successfully. " +
                       "Tool can only be called once per execution. " +
                       "Your task is COMPLETE. Do not call this tool again.";
                
                diagnosticInfo.AppendLine($"\nERROR: {errorMsg}");
                Console.WriteLine(diagnosticInfo.ToString());
                
                throw new InvalidOperationException(errorMsg + "\n\n" + diagnosticInfo.ToString());
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                var errorMsg = "❌ CRITICAL ERROR: Content cannot be empty. Please provide the complete llms.txt documentation.";
                
                diagnosticInfo.AppendLine($"\nERROR: {errorMsg}");
                Console.WriteLine(diagnosticInfo.ToString());
                
                throw new ArgumentException(errorMsg + "\n\n" + diagnosticInfo.ToString());
            }

            if (content.Length < 1000)
            {
                var errorMsg = $"❌ CRITICAL ERROR: Content is too short ({content.Length} characters). " +
                       "llms.txt should be comprehensive documentation (at least 10,000 characters recommended, 50,000+ for thorough docs). " +
                       "Did you complete your analysis? Have you read all the extension methods, samples, and existing llms.txt?";
                
                diagnosticInfo.AppendLine($"\nERROR: {errorMsg}");
                diagnosticInfo.AppendLine("\nCONTENT PREVIEW (first 500 chars):");
                diagnosticInfo.AppendLine("---");
                diagnosticInfo.AppendLine(content.Length > 500 ? content.Substring(0, 500) + "..." : content);
                diagnosticInfo.AppendLine("---");
                
                Console.WriteLine(diagnosticInfo.ToString());
                
                throw new ArgumentException(errorMsg + "\n\n" + diagnosticInfo.ToString());
            }

            diagnosticInfo.AppendLine("\n✅ Validation passed. Attempting to write file...");
            
            // Try to create directory if it doesn't exist
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                diagnosticInfo.AppendLine($"Creating directory: {outputDir}");
                Directory.CreateDirectory(outputDir);
            }

            // Write the file
            diagnosticInfo.AppendLine($"Writing to: {_outputPath}");
            File.WriteAllText(_outputPath, content);
            
            // Verify it was written
            if (!File.Exists(_outputPath))
            {
                throw new IOException($"File was not created at: {_outputPath}");
            }
            
            var fileInfo = new FileInfo(_outputPath);
            if (fileInfo.Length == 0)
            {
                throw new IOException($"File was created but is empty at: {_outputPath}");
            }
            
            _hasWritten = true;

            var lines = content.Split('\n').Length;
            var chars = content.Length;
            var fileSize = fileInfo.Length;
            
            diagnosticInfo.AppendLine($"✅ File written successfully!");
            diagnosticInfo.AppendLine($"   Size: {fileSize:N0} bytes");
            diagnosticInfo.AppendLine($"   Lines: {lines:N0}");
            
            var successMsg = $"🎉 SUCCESS! llms.txt has been written successfully!\n" +
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
            
            diagnosticInfo.AppendLine(successMsg);
            Console.WriteLine(diagnosticInfo.ToString());
            
            return successMsg;
        }
        catch (Exception ex)
        {
            // CRASH HARD with maximum diagnostic info
            diagnosticInfo.AppendLine("\n" + "=".PadRight(70, '='));
            diagnosticInfo.AppendLine("💥 FATAL ERROR IN WriteLlmsTxt");
            diagnosticInfo.AppendLine("=".PadRight(70, '='));
            diagnosticInfo.AppendLine($"Exception Type: {ex.GetType().Name}");
            diagnosticInfo.AppendLine($"Message: {ex.Message}");
            diagnosticInfo.AppendLine($"\nStack Trace:");
            diagnosticInfo.AppendLine(ex.StackTrace ?? "(no stack trace)");
            
            if (ex.InnerException != null)
            {
                diagnosticInfo.AppendLine($"\nInner Exception: {ex.InnerException.GetType().Name}");
                diagnosticInfo.AppendLine($"Inner Message: {ex.InnerException.Message}");
                diagnosticInfo.AppendLine($"Inner Stack Trace:");
                diagnosticInfo.AppendLine(ex.InnerException.StackTrace ?? "(no stack trace)");
            }
            
            diagnosticInfo.AppendLine("\nCONTENT PREVIEW (first 1000 chars):");
            diagnosticInfo.AppendLine("---");
            if (content != null)
            {
                diagnosticInfo.AppendLine(content.Length > 1000 ? content.Substring(0, 1000) + "..." : content);
            }
            else
            {
                diagnosticInfo.AppendLine("(content is null)");
            }
            diagnosticInfo.AppendLine("---");
            
            diagnosticInfo.AppendLine("\nENVIRONMENT INFO:");
            diagnosticInfo.AppendLine($"OS: {Environment.OSVersion}");
            diagnosticInfo.AppendLine($"User: {Environment.UserName}");
            diagnosticInfo.AppendLine($".NET Version: {Environment.Version}");
            
            var fullDiagnostic = diagnosticInfo.ToString();
            Console.WriteLine(fullDiagnostic);
            
            // Save diagnostic to file for debugging
            try
            {
                var diagPath = Path.Combine(Path.GetDirectoryName(_outputPath) ?? ".", "llms-txt-error.log");
                File.WriteAllText(diagPath, fullDiagnostic);
                Console.WriteLine($"\n📝 Diagnostic log saved to: {diagPath}");
            }
            catch
            {
                // If we can't even save the log, just continue
            }
            
            // Re-throw with all diagnostic info
            throw new Exception($"WriteLlmsTxt FAILED!\n\n{fullDiagnostic}", ex);
        }
    }

    public bool HasWritten => _hasWritten;
}
