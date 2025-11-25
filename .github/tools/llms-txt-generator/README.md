# 🤖 LLMs.txt Generator - Agentic Edition

An intelligent agent that explores your codebase and generates comprehensive LLM-friendly documentation (`llms.txt`).

## What Does It Do?

Instead of manually maintaining `llms.txt`, this tool:

1. **Explores** your codebase autonomously using filesystem tools
2. **Understands** your SDK by reading source files, README, examples
3. **Generates** a comprehensive, structured `llms.txt` documentation file
4. **Uses your own OpenRouter.NET SDK** with tool calling (dogfooding!)

## Features

✅ **Agentic Exploration** - The LLM agent explores your codebase systematically  
✅ **Smart Analysis** - Understands code structure, patterns, and usage  
✅ **Tool Calling** - Uses real tools to read files, search, and analyze  
✅ **XML Structure** - Outputs structured XML for easy section updates  
✅ **Comprehensive** - Generates complete documentation, not just summaries  
✅ **Extensible** - Easy to add new modes (git-diff, incremental updates)  

## Quick Start

### 1. Set API Key

```bash
export OPENROUTER_API_KEY="sk-or-v1-..."
```

### 2. Run the Generator

```bash
cd .github/tools/llms-txt-generator
dotnet run -- --path ../../..
```

That's it! The agent will explore your codebase and generate `llms.txt`.

## Usage

### Basic Usage

```bash
# Analyze entire workspace (RECOMMENDED)
dotnet run -- --path /path/to/workspace

# Or from tool directory, workspace root
dotnet run -- --path ../../..
```

### Full Options

```bash
dotnet run -- \
  --path ../../.. \
  --output ../../../llms.txt \
  --model anthropic/claude-3.5-sonnet \
  --max-iterations 30 \
  --api-key sk-or-v1-...
```

**💡 Pro Tip**: Always point to the **workspace root**, not just `src/`.
The agent needs access to:
- Source code (`src/`)
- Sample projects (`samples/`)
- Existing documentation (`llms.txt`, `README.md`)
- Project files (`.csproj`)

The agent will explore intelligently and focus on what matters.

### Options

| Option | Description | Default |
|--------|-------------|---------|
| `--path` | Path to codebase (required) | - |
| `--output` | Output file path | `./llms.txt` |
| `--model` | OpenRouter model to use | `anthropic/claude-3.5-sonnet` |
| `--max-iterations` | Max agent iterations | `30` |
| `--api-key` | OpenRouter API key | `$OPENROUTER_API_KEY` |

## How It Works

### 1. Agent Initialization

The agent starts with a comprehensive system prompt that instructs it to:
- Explore the codebase systematically
- Read relevant source files
- Understand the SDK's purpose and patterns
- Generate complete documentation

### 2. Tool Calling

The agent has access to these tools:

- **`ListDirectory(path)`** - List files/folders
- **`ReadFile(filePath)`** - Read a single file
- **`ReadFiles(filePathsJson)`** - Read multiple files efficiently
- **`SearchFiles(pattern)`** - Find files by pattern (e.g., `*.cs`)
- **`GetDirectoryTree(path, maxDepth)`** - Get tree view of structure
- **`GetCodebaseStats(path)`** - Get file statistics
- **`WriteLlmsTxt(content)`** - Output the final documentation (called last)

### 3. Agentic Loop

The agent iteratively:
1. Calls tools to explore and understand the codebase
2. Builds up knowledge about the SDK
3. When ready, generates and outputs the complete `llms.txt`

### 4. Output

The generated `llms.txt` uses XML structure for easy parsing:

```xml
<llms-txt>
  <metadata>...</metadata>
  <section id="getting-started">...</section>
  <section id="core-client">...</section>
  ...
</llms-txt>
```

## Example Run

```bash
$ cd .github/tools/llms-txt-generator
$ export OPENROUTER_API_KEY="sk-or-v1-..."
$ dotnet run -- --path ../../../src

╔════════════════════════════════════════════════════════════╗
║          🤖 LLMs.txt Generator - Agentic Edition          ║
╚════════════════════════════════════════════════════════════╝

📂 Analyzing: /workspace/src
📝 Output to: /workspace/llms.txt
🤖 Model: anthropic/claude-3.5-sonnet
🔄 Max iterations: 30

🚀 Initializing agent...

🤖 Agent Starting...
📦 Model: anthropic/claude-3.5-sonnet
🔄 Max iterations: 30

============================================================
🔄 Iteration 1/30
============================================================

Let me start by exploring the codebase structure...

🔧 Executing tool: GetDirectoryTree
✅ Tool completed in 45ms

🔧 Executing tool: ReadFile
✅ Tool completed in 12ms

[... agent continues exploring ...]

============================================================
✅ AGENT COMPLETED SUCCESSFULLY!
============================================================

📝 Files read during analysis: 23
🔄 Iterations used: 12/30

🎉 llms.txt has been generated!

╔════════════════════════════════════════════════════════════╗
║                    ✅ SUCCESS!                             ║
╚════════════════════════════════════════════════════════════╝

📄 Generated: /workspace/llms.txt
📊 Size: 127,584 bytes
📏 Lines: 2,341

🎉 Your llms.txt is ready!
```

## Recommended Models

| Model | Speed | Quality | Cost | Use Case |
|-------|-------|---------|------|----------|
| `anthropic/claude-3.5-sonnet` | Medium | Excellent | $$$ | **Recommended** - Best quality |
| `openai/gpt-4o` | Fast | Excellent | $$ | Great balance |
| `openai/gpt-4o-mini` | Very Fast | Good | $ | Quick iterations |
| `anthropic/claude-3-opus` | Slow | Best | $$$$ | Maximum quality |

## Cost Estimation

Typical run for a medium SDK (~50 source files):
- **Input tokens**: ~15,000 (reading files, context)
- **Output tokens**: ~10,000 (generating docs)
- **Total cost**: ~$0.10-0.30 per run (with Claude 3.5 Sonnet)

Very affordable for automated documentation!

## Future Modes

This tool is designed to be extended with additional modes:

### Coming Soon

- **`--mode diff`** - Generate updates based on git diff
- **`--mode incremental`** - Update only changed sections
- **`--mode validate`** - Check if existing llms.txt is up to date
- **`--format markdown`** - Output in markdown instead of XML

### Example Future Usage

```bash
# Generate based on recent changes only
dotnet run -- --path ./src --mode diff --since HEAD~5

# Update specific sections only
dotnet run -- --path ./src --mode incremental --sections "tools,streaming"

# Validate current llms.txt
dotnet run -- --path ./src --mode validate --input llms.txt
```

## Troubleshooting

### "Agent did not complete successfully"

The agent might need more iterations. Try:
```bash
dotnet run -- --path ./src --max-iterations 50
```

### "Directory does not exist"

Make sure the path is correct:
```bash
dotnet run -- --path $(pwd)/src
```

### API Key Issues

Verify your API key:
```bash
echo $OPENROUTER_API_KEY
```

Should start with `sk-or-v1-`

### Agent is too slow

Try a faster model:
```bash
dotnet run -- --path ./src --model openai/gpt-4o-mini
```

## Development

### Project Structure

```
.github/tools/llms-txt-generator/
├── Program.cs                    # CLI entry point
├── Agent.cs                      # Agent orchestration
├── Tools/
│   ├── FileSystemTools.cs        # File reading/exploring tools
│   └── WriteLlmsTxtTool.cs       # Output generation tool
├── Prompts/
│   └── SystemPrompt.cs           # Agent instructions
├── llms-txt-generator.csproj     # Project file
└── README.md                     # This file
```

### Adding New Tools

1. Create tool method in `Tools/FileSystemTools.cs`
2. Add `[ToolMethod]` attribute with description
3. Add `[ToolParameter]` attributes to parameters
4. Register tool in `Agent.cs` constructor

Example:
```csharp
[ToolMethod("Get git commit history")]
public string GetGitLog(
    [ToolParameter("Number of commits")] int count = 10)
{
    // Implementation
}
```

Then register:
```csharp
_client.RegisterTool(_fileTools, nameof(_fileTools.GetGitLog));
```

## Why This Approach?

### Traditional Approach ❌
- Manual updates required
- Easy to forget or skip
- Inconsistent documentation
- Time-consuming maintenance

### Agentic Approach ✅
- **Autonomous** - Agent explores and understands
- **Comprehensive** - Doesn't miss important features
- **Consistent** - Same quality every time
- **Extensible** - Easy to add new capabilities

## Contributing

Ideas for improvements:
- Add more filesystem tools
- Implement git-diff mode
- Add validation mode
- Support multiple output formats
- Add caching to reduce API calls

## License

Same as OpenRouter.NET (MIT)
