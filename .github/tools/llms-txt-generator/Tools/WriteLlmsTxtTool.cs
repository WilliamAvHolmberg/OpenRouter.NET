using OpenRouter.NET.Tools;
using OpenRouter.NET.Models;
using System.Text;

namespace LlmsTxtGenerator.Tools;

public class WriteLlmsTxtTool
{
    private readonly string _outputPath;
    private bool _hasWritten = false;
    private readonly Func<List<Artifact>> _getArtifacts;

    public WriteLlmsTxtTool(string outputPath, Func<List<Artifact>> getArtifacts)
    {
        _outputPath = outputPath;
        _getArtifacts = getArtifacts;
    }

    [ToolMethod("Write the final llms.txt content from an artifact. First generate the documentation as an artifact (using <artifact> tags), then call this tool with the artifact ID. After calling this successfully, your task is COMPLETE and you should stop.")]
    public string WriteLlmsTxt(
        [ToolParameter("The artifact ID containing the complete llms.txt documentation. You must generate an artifact first before calling this. Check the artifact ID carefully!")] string artifactId)
    {
        // First, validate and save diagnostics
        var diagnosticInfo = new StringBuilder();
        diagnosticInfo.AppendLine("=".PadRight(70, '='));
        diagnosticInfo.AppendLine("WriteLlmsTxt DIAGNOSTIC INFORMATION");
        diagnosticInfo.AppendLine("=".PadRight(70, '='));
        diagnosticInfo.AppendLine($"Output path: {_outputPath}");
        diagnosticInfo.AppendLine($"Already written: {_hasWritten}");
        diagnosticInfo.AppendLine($"Artifact ID provided: {artifactId}");
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

            // Get artifacts
            var artifacts = _getArtifacts();
            diagnosticInfo.AppendLine($"Available artifacts: {artifacts.Count}");
            foreach (var art in artifacts)
            {
                diagnosticInfo.AppendLine($"  - {art.Id}: {art.Title} ({art.Content?.Length ?? 0} chars)");
            }
            
            // If artifactId is empty but there's exactly one artifact, use it
            Artifact? artifact = null;
            
            if (string.IsNullOrWhiteSpace(artifactId))
            {
                if (artifacts.Count == 1)
                {
                    artifact = artifacts[0];
                    diagnosticInfo.AppendLine($"\n✅ No artifact ID provided, but found exactly one artifact. Using: {artifact.Id}");
                }
                else
                {
                    var errorMsg = "❌ CRITICAL ERROR: artifactId cannot be empty. " +
                                  "You must first generate the documentation as an artifact using <artifact> tags, " +
                                  "then call this tool with the artifact ID.";
                    
                    diagnosticInfo.AppendLine($"\nERROR: {errorMsg}");
                    Console.WriteLine(diagnosticInfo.ToString());
                    
                    throw new ArgumentException(errorMsg + "\n\n" + diagnosticInfo.ToString());
                }
            }
            else
            {
                // Try exact match first
                artifact = artifacts.FirstOrDefault(a => a.Id == artifactId);
                
                // If not found, try case-insensitive match
                if (artifact == null)
                {
                    artifact = artifacts.FirstOrDefault(a => 
                        string.Equals(a.Id, artifactId, StringComparison.OrdinalIgnoreCase));
                    
                    if (artifact != null)
                    {
                        diagnosticInfo.AppendLine($"\n⚠️  Artifact ID mismatch (case difference). Found: {artifact.Id}");
                    }
                }
                
                // If still not found but there's only one artifact, use it
                if (artifact == null && artifacts.Count == 1)
                {
                    artifact = artifacts[0];
                    diagnosticInfo.AppendLine($"\n⚠️  Artifact ID '{artifactId}' not found, but only one artifact exists. Using: {artifact.Id}");
                }
                
                if (artifact == null)
                {
                    var errorMsg = $"❌ CRITICAL ERROR: Artifact '{artifactId}' not found. " +
                                  $"Available artifacts: {string.Join(", ", artifacts.Select(a => a.Id))}. " +
                                  "Check the artifact ID carefully!";
                    
                    diagnosticInfo.AppendLine($"\nERROR: {errorMsg}");
                    Console.WriteLine(diagnosticInfo.ToString());
                    
                    throw new ArgumentException(errorMsg + "\n\n" + diagnosticInfo.ToString());
                }
            }
            
            var content = artifact.Content;
            diagnosticInfo.AppendLine($"\n✅ Found artifact: {artifact.Title}");
            diagnosticInfo.AppendLine($"   Content length: {content?.Length ?? 0} characters");
            diagnosticInfo.AppendLine($"   Content lines: {(content?.Split('\n').Length ?? 0)}");

            if (string.IsNullOrWhiteSpace(content))
            {
                var errorMsg = "❌ CRITICAL ERROR: Artifact content is empty.";
                
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
                   $"📦 Source artifact: {artifact.Title} ({artifactId})\n" +
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
            
            diagnosticInfo.AppendLine("\nAVAILABLE ARTIFACTS:");
            var artifacts = _getArtifacts();
            foreach (var art in artifacts)
            {
                diagnosticInfo.AppendLine($"  - {art.Id}: {art.Title}");
                if (art.Content != null)
                {
                    diagnosticInfo.AppendLine($"    Content preview: {(art.Content.Length > 200 ? art.Content.Substring(0, 200) + "..." : art.Content)}");
                }
            }
            
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
