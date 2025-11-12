using OpenRouter.NET.Tools;
using System.Text.Json;

namespace LlmsTxtGenerator.Tools;

public class FileSystemTools
{
    private readonly string _basePath;
    private readonly HashSet<string> _readFiles = new();

    public FileSystemTools(string basePath)
    {
        _basePath = Path.GetFullPath(basePath);
        
        if (!Directory.Exists(_basePath))
        {
            throw new DirectoryNotFoundException($"Base path does not exist: {_basePath}");
        }
    }

    [ToolMethod("List all files and directories in a given path. Returns names only for overview.")]
    public string ListDirectory(
        [ToolParameter("Relative path from the base directory (use '.' for root)")] string path = ".")
    {
        try
        {
            var fullPath = GetFullPath(path);
            
            if (!Directory.Exists(fullPath))
            {
                return $"❌ Directory not found: {path}";
            }

            var items = new List<string>();
            
            var dirs = Directory.GetDirectories(fullPath)
                .Select(d => $"📁 {Path.GetFileName(d)}/")
                .OrderBy(x => x);
            
            var files = Directory.GetFiles(fullPath)
                .Select(f => $"📄 {Path.GetFileName(f)}")
                .OrderBy(x => x);
            
            items.AddRange(dirs);
            items.AddRange(files);

            if (!items.Any())
            {
                return $"📂 {path}: (empty directory)";
            }

            return $"📂 {path}:\n" + string.Join("\n", items);
        }
        catch (Exception ex)
        {
            return $"❌ Error listing directory: {ex.Message}";
        }
    }

    [ToolMethod("Read the complete content of a single file")]
    public string ReadFile(
        [ToolParameter("Relative path to the file from base directory")] string filePath)
    {
        try
        {
            var fullPath = GetFullPath(filePath);
            
            if (!File.Exists(fullPath))
            {
                return $"❌ File not found: {filePath}";
            }

            var content = File.ReadAllText(fullPath);
            _readFiles.Add(filePath);
            
            var lines = content.Split('\n').Length;
            return $"📄 File: {filePath} ({lines} lines)\n" +
                   $"---\n{content}\n---";
        }
        catch (Exception ex)
        {
            return $"❌ Error reading file: {ex.Message}";
        }
    }

    [ToolMethod("Read multiple files at once. Efficient for reading several related files.")]
    public string ReadFiles(
        [ToolParameter("Array of relative file paths as JSON array, e.g. [\"file1.cs\", \"dir/file2.cs\"]")] string filePathsJson)
    {
        try
        {
            var filePaths = JsonSerializer.Deserialize<string[]>(filePathsJson);
            
            if (filePaths == null || filePaths.Length == 0)
            {
                return "❌ No file paths provided or invalid JSON format";
            }

            var results = new List<string>();
            
            foreach (var filePath in filePaths)
            {
                var fullPath = GetFullPath(filePath);
                
                if (!File.Exists(fullPath))
                {
                    results.Add($"❌ {filePath}: File not found");
                    continue;
                }

                var content = File.ReadAllText(fullPath);
                _readFiles.Add(filePath);
                
                var lines = content.Split('\n').Length;
                results.Add($"📄 {filePath} ({lines} lines)\n---\n{content}\n---");
            }

            return string.Join("\n\n", results);
        }
        catch (Exception ex)
        {
            return $"❌ Error reading files: {ex.Message}";
        }
    }

    [ToolMethod("Search for files matching a pattern (e.g., '*.cs', '**/Models/*.cs')")]
    public string SearchFiles(
        [ToolParameter("Glob pattern to search for files")] string pattern)
    {
        try
        {
            var searchPath = _basePath;
            var searchPattern = pattern;

            if (pattern.Contains('/'))
            {
                var parts = pattern.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 1)
                {
                    searchPath = Path.Combine(_basePath, string.Join('/', parts.Take(parts.Length - 1)));
                    searchPattern = parts.Last();
                }
            }

            var files = Directory.GetFiles(searchPath, searchPattern, SearchOption.AllDirectories)
                .Select(f => Path.GetRelativePath(_basePath, f))
                .OrderBy(f => f)
                .ToList();

            if (!files.Any())
            {
                return $"No files found matching pattern: {pattern}";
            }

            return $"🔍 Found {files.Count} file(s) matching '{pattern}':\n" +
                   string.Join("\n", files.Select(f => $"  • {f}"));
        }
        catch (Exception ex)
        {
            return $"❌ Error searching files: {ex.Message}";
        }
    }

    [ToolMethod("Get a tree view of the directory structure")]
    public string GetDirectoryTree(
        [ToolParameter("Relative path from base (use '.' for root)")] string path = ".",
        [ToolParameter("Maximum depth to traverse (default 3)")] int maxDepth = 3)
    {
        try
        {
            var fullPath = GetFullPath(path);
            
            if (!Directory.Exists(fullPath))
            {
                return $"❌ Directory not found: {path}";
            }

            var tree = new List<string> { $"📂 {path}/" };
            BuildTree(fullPath, "", 0, maxDepth, tree);
            
            return string.Join("\n", tree);
        }
        catch (Exception ex)
        {
            return $"❌ Error building tree: {ex.Message}";
        }
    }

    [ToolMethod("Get summary statistics about the codebase")]
    public string GetCodebaseStats(
        [ToolParameter("Relative path to analyze (use '.' for entire codebase)")] string path = ".")
    {
        try
        {
            var fullPath = GetFullPath(path);
            
            if (!Directory.Exists(fullPath))
            {
                return $"❌ Directory not found: {path}";
            }

            var allFiles = Directory.GetFiles(fullPath, "*.*", SearchOption.AllDirectories);
            
            var stats = allFiles
                .GroupBy(f => Path.GetExtension(f).ToLowerInvariant())
                .Select(g => new
                {
                    Extension = string.IsNullOrEmpty(g.Key) ? "(no extension)" : g.Key,
                    Count = g.Count(),
                    TotalLines = g.Sum(f => File.ReadLines(f).Count())
                })
                .OrderByDescending(x => x.Count)
                .ToList();

            var result = new List<string>
            {
                $"📊 Codebase Statistics for: {path}",
                $"Total files: {allFiles.Length}",
                "",
                "Breakdown by file type:"
            };

            foreach (var stat in stats)
            {
                result.Add($"  {stat.Extension}: {stat.Count} files, {stat.TotalLines:N0} lines");
            }

            return string.Join("\n", result);
        }
        catch (Exception ex)
        {
            return $"❌ Error calculating stats: {ex.Message}";
        }
    }

    public IEnumerable<string> GetReadFiles() => _readFiles;

    private string GetFullPath(string relativePath)
    {
        if (relativePath == ".")
            return _basePath;

        var fullPath = Path.Combine(_basePath, relativePath);
        var normalizedPath = Path.GetFullPath(fullPath);

        if (!normalizedPath.StartsWith(_basePath))
        {
            throw new UnauthorizedAccessException("Access outside base path is not allowed");
        }

        return normalizedPath;
    }

    private void BuildTree(string path, string indent, int depth, int maxDepth, List<string> output)
    {
        if (depth >= maxDepth)
            return;

        try
        {
            var dirs = Directory.GetDirectories(path).OrderBy(d => d).ToList();
            var files = Directory.GetFiles(path).OrderBy(f => f).ToList();

            for (int i = 0; i < dirs.Count; i++)
            {
                var isLast = i == dirs.Count - 1 && files.Count == 0;
                var dirName = Path.GetFileName(dirs[i]);
                output.Add($"{indent}{(isLast ? "└── " : "├── ")}📁 {dirName}/");
                BuildTree(dirs[i], indent + (isLast ? "    " : "│   "), depth + 1, maxDepth, output);
            }

            for (int i = 0; i < files.Count; i++)
            {
                var isLast = i == files.Count - 1;
                var fileName = Path.GetFileName(files[i]);
                output.Add($"{indent}{(isLast ? "└── " : "├── ")}📄 {fileName}");
            }
        }
        catch
        {
            // Skip directories we can't access
        }
    }
}
