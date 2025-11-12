# 🏗️ Architecture Overview

## System Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                         Program.cs                          │
│                  (CLI Entry Point)                          │
│                                                             │
│  • Parse command line arguments                            │
│  • Initialize OpenRouterClient                             │
│  • Create Agent instance                                   │
│  • Handle errors and output                                │
└─────────────────────┬───────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────┐
│                         Agent.cs                            │
│              (Agent Orchestration)                          │
│                                                             │
│  • Manages conversation history                            │
│  • Registers tools with OpenRouterClient                   │
│  • Runs agentic loop (up to max iterations)               │
│  • Streams responses and handles tool calls                │
│  • Monitors for completion (WriteLlmsTxt called)          │
└─────┬───────────────────────────────────┬─────────────────┘
      │                                   │
      │                                   │
      ▼                                   ▼
┌─────────────────────┐          ┌──────────────────────────┐
│  FileSystemTools    │          │   WriteLlmsTxtTool       │
│                     │          │                          │
│  📁 ListDirectory   │          │  ✍️ WriteLlmsTxt         │
│  📄 ReadFile        │          │     (called once)        │
│  📚 ReadFiles       │          │                          │
│  🔍 SearchFiles     │          │  • Validates content     │
│  🌲 GetDirectoryTree│          │  • Writes output file    │
│  📊 GetCodebaseStats│          │  • Marks completion      │
└─────────────────────┘          └──────────────────────────┘
```

## Agent Flow

```
                    ┌──────────────┐
                    │   START      │
                    └──────┬───────┘
                           │
                           ▼
                ┌──────────────────────┐
                │ Load System Prompt   │
                │ (Instructions to     │
                │  explore & document) │
                └──────────┬───────────┘
                           │
                           ▼
        ┌──────────────────────────────────────┐
        │         AGENTIC LOOP                 │
        │  (Iteration 1 to MaxIterations)      │
        │                                      │
        │  ┌────────────────────────────────┐ │
        │  │  1. LLM decides next action    │ │
        │  │     (via OpenRouter.NET)       │ │
        │  └────────────┬───────────────────┘ │
        │               │                      │
        │               ▼                      │
        │  ┌────────────────────────────────┐ │
        │  │  2. Calls tool (if needed)     │ │
        │  │     • ListDirectory            │ │
        │  │     • ReadFile                 │ │
        │  │     • SearchFiles              │ │
        │  │     • etc.                     │ │
        │  └────────────┬───────────────────┘ │
        │               │                      │
        │               ▼                      │
        │  ┌────────────────────────────────┐ │
        │  │  3. Tool executes & returns    │ │
        │  │     result to LLM              │ │
        │  └────────────┬───────────────────┘ │
        │               │                      │
        │               ▼                      │
        │  ┌────────────────────────────────┐ │
        │  │  4. LLM processes result       │ │
        │  │     (builds understanding)     │ │
        │  └────────────┬───────────────────┘ │
        │               │                      │
        │               ▼                      │
        │     ┌─────────────────────┐         │
        │     │ WriteLlmsTxt called?│         │
        │     └─────┬───────────┬───┘         │
        │           │ No        │ Yes         │
        │           │           │             │
        │      ─────┘           └────────────▶│
        │     │                               │
        │     └─ Continue loop                │
        │                                      │
        └──────────────────────────────────────┘
                           │
                           ▼
                ┌──────────────────────┐
                │  WriteLlmsTxt called │
                │  Output file created │
                └──────────┬───────────┘
                           │
                           ▼
                    ┌──────────────┐
                    │   SUCCESS!   │
                    └──────────────┘
```

## Tool Calling Flow

```
Agent
  │
  │ RegisterTool(fileTools, "ReadFile")
  ▼
OpenRouterClient
  │
  │ ChatCompletionRequest with tools[]
  ▼
OpenRouter API
  │
  │ Response with tool_calls[]
  ▼
SDK Tool Execution
  │
  │ Execute: fileTools.ReadFile(path)
  ▼
Tool Returns Result
  │
  │ Result sent back to LLM
  ▼
LLM Continues
  │
  └─▶ Next iteration or completion
```

## Data Flow

```
User Input (--path)
       │
       ▼
   File System  ─────▶  FileSystemTools
       │                     │
       │                     │ Read/List/Search
       │                     │
       │                     ▼
       │              OpenRouterClient
       │                     │
       │                     │ API Request
       │                     │
       │                     ▼
       │              OpenRouter API
       │                     │
       │                     │ LLM Processing
       │                     │
       │                     ▼
       │              Tool Calls + Responses
       │                     │
       │              ┌──────┴──────┐
       │              │             │
       │         (Loop until     (When ready)
       │          complete)          │
       │              │             │
       └──────────────┘             ▼
                            WriteLlmsTxtTool
                                   │
                                   │ Write
                                   ▼
                              llms.txt file
```

## Key Components

### 1. Program.cs
- **Purpose**: CLI interface
- **Responsibilities**:
  - Parse arguments
  - Validate inputs
  - Initialize components
  - Display results

### 2. Agent.cs
- **Purpose**: Agent orchestration
- **Responsibilities**:
  - Manage conversation state
  - Register tools
  - Run agentic loop
  - Stream and handle responses
  - Detect completion

### 3. FileSystemTools.cs
- **Purpose**: Codebase exploration
- **Tools**:
  - `ListDirectory` - Browse files
  - `ReadFile` - Read single file
  - `ReadFiles` - Read multiple files
  - `SearchFiles` - Find by pattern
  - `GetDirectoryTree` - Tree view
  - `GetCodebaseStats` - Statistics

### 4. WriteLlmsTxtTool.cs
- **Purpose**: Final output
- **Responsibilities**:
  - Validate content
  - Write file
  - Signal completion

### 5. SystemPrompt.cs
- **Purpose**: Agent instructions
- **Content**:
  - Mission statement
  - Tool descriptions
  - Process steps
  - Output format
  - Requirements

## Extension Points

### Adding New Tools

```csharp
// 1. Add method to FileSystemTools.cs
[ToolMethod("Description")]
public string MyNewTool([ToolParameter("param")] string param)
{
    // Implementation
}

// 2. Register in Agent.cs
_client.RegisterTool(_fileTools, nameof(_fileTools.MyNewTool));
```

### Adding New Modes

```csharp
// Future: git-diff mode
public class GitTools
{
    [ToolMethod("Get git diff")]
    public string GetDiff(string since) { ... }
}

// Register conditionally based on mode
if (mode == "diff")
{
    var gitTools = new GitTools();
    _client.RegisterTool(gitTools, nameof(gitTools.GetDiff));
}
```

## Performance Considerations

- **Max Iterations**: Balance between thoroughness and cost
- **Model Selection**: Faster models = quicker runs, may be less thorough
- **Tool Efficiency**: ReadFiles() is more efficient than multiple ReadFile() calls
- **Caching**: Future enhancement to cache file reads

## Cost Analysis

Per run (typical SDK):
- **Context**: ~10k-20k tokens (files read, conversation)
- **Output**: ~10k tokens (generated docs)
- **Total**: ~$0.10-0.30 with Claude 3.5 Sonnet

Very affordable for maintaining documentation!
