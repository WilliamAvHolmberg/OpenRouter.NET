# 🚀 Quick Start Guide

## Step 1: Set Your API Key

```bash
export OPENROUTER_API_KEY="sk-or-v1-your-key-here"
```

Get your API key from: https://openrouter.ai/keys

## Step 2: Run the Generator

### Option A: Using the quick script (easiest)

```bash
cd .github/tools/llms-txt-generator
./run.sh
```

This will:
- Restore dependencies
- Build the project
- Analyze **entire workspace** (src/, samples/, llms.txt, README.md)
- Generate `llms.txt` in project root

### Option B: Manual run with custom options

```bash
cd .github/tools/llms-txt-generator

# Restore and build
dotnet restore
dotnet build

# Run - analyze entire workspace (RECOMMENDED)
dotnet run -- --path ../../.. --output ../../../llms.txt

# Or analyze just src/ (will miss samples and existing llms.txt)
dotnet run -- --path ../../../src --output ../../../llms.txt
```

**💡 Important**: For best results, point to the **workspace root**, not just `src/`.
This gives the agent access to:
- ✅ Source code (`src/`)
- ✅ Sample projects (`samples/`)
- ✅ Existing `llms.txt` (for reference)
- ✅ README.md (for overview)
- ✅ .csproj files (for dependencies)

### Option C: Analyze a specific package

```bash
# Analyze just the react-sdk package (will miss broader context)
cd .github/tools/llms-txt-generator
dotnet run -- --path ../../../packages/react-sdk --output react-sdk-llms.txt

# Better: Analyze workspace but focus output on react-sdk
# (Agent can see everything for context, but documents react-sdk)
dotnet run -- --path ../../.. --output react-sdk-llms.txt
# Then instruct in prompt which package to focus on
```

## Step 3: Watch It Work!

The agent will:

1. **🔍 Explore** - List directories and files
2. **📖 Read** - Read source files, README, examples
3. **🧠 Understand** - Build mental model of SDK
4. **✍️ Generate** - Create comprehensive llms.txt
5. **✅ Done!** - Output saved

Example output:
```
🤖 Agent Starting...
📦 Model: anthropic/claude-3.5-sonnet
🔄 Max iterations: 30

============================================================
🔄 Iteration 1/30
============================================================

Let me start by exploring the codebase structure...

🔧 Executing tool: GetDirectoryTree
   Arguments: {"path":".","maxDepth":3}
✅ Tool completed in 45ms

🔧 Executing tool: ReadFile
   Arguments: {"filePath":"README.md"}
✅ Tool completed in 12ms

[... continues exploring ...]

✅ AGENT COMPLETED SUCCESSFULLY!

📄 Generated: /workspace/llms.txt
📊 Size: 127,584 bytes
📏 Lines: 2,341
```

## What Gets Generated?

The tool creates a comprehensive `llms.txt` with:

- ✅ Complete API reference
- ✅ Usage patterns and examples
- ✅ Configuration options
- ✅ Error handling guide
- ✅ Best practices
- ✅ Common issues and solutions
- ✅ XML-structured sections

## Customization

### Use a Different Model

```bash
# Faster, cheaper
dotnet run -- --path ./src --model openai/gpt-4o-mini

# Best quality
dotnet run -- --path ./src --model anthropic/claude-3-opus
```

### Increase Iterations

If agent doesn't finish:

```bash
dotnet run -- --path ./src --max-iterations 50
```

### Custom Output Location

```bash
dotnet run -- --path ./src --output ./docs/llms.txt
```

## Troubleshooting

### "API key not set"
```bash
# Check it's set
echo $OPENROUTER_API_KEY

# If not, set it
export OPENROUTER_API_KEY="sk-or-v1-..."
```

### "Directory does not exist"
```bash
# Use absolute path
dotnet run -- --path /workspace/src

# Or relative from tool directory
dotnet run -- --path ../../../src
```

### Agent runs out of iterations

Increase max iterations:
```bash
dotnet run -- --path ./src --max-iterations 50
```

Or use a more capable model:
```bash
dotnet run -- --path ./src --model anthropic/claude-3-opus
```

## Next Steps

1. ✅ Review the generated `llms.txt`
2. ✅ Test with your LLM of choice
3. ✅ Add to your repository
4. ✅ Set up GitHub Action (coming soon!)

## Need Help?

See the full [README.md](README.md) for:
- Detailed architecture
- Adding custom tools
- Future roadmap
- Development guide
