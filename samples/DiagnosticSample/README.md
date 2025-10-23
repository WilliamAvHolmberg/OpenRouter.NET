# Diagnostic Sample

A comprehensive diagnostic tool for testing and debugging OpenRouter.NET streaming, artifacts, and tool calling.

## 🎯 Purpose

This sample provides multiple diagnostic modes:
- 📝 Log every raw SSE chunk from streaming sessions
- 🔍 Show all parsed events in detail
- 📦 Extract and save artifacts automatically
- 🔧 Test tool calling with server and client modes
- 📊 Batch reliability testing for artifact completion
- 🐛 Debug streaming issues with detailed output

## 🚀 Usage

```bash
export OPENROUTER_API_KEY="your-key-here"
cd samples/DiagnosticSample
dotnet run
```

### Diagnostic Modes

The sample offers four modes:

1. **Interactive Mode** - Configure request, model, and prompt manually
2. **Quick Test** - Hardcoded React button example for fast testing
3. **Tool Call Test** - Test both server-side and client-side tool execution
4. **Batch Reliability Test** - Run multiple iterations to test artifact completion reliability

## 📁 Output Files

### Single Test Output

The sample creates a timestamped directory with:

```
diagnostic_output_2025-10-22_14-30-45/
├── raw_stream.txt          # Complete SSE stream (JSON chunks)
├── parsed_events.txt       # Sequential event log
├── summary.txt             # Statistics and full response
└── artifacts/              # Extracted artifact files
    ├── Button.tsx
    └── styles.css
```

### `raw_stream.txt`
Complete SSE stream with every chunk as JSON.

### `parsed_events.txt`
Event-by-event breakdown showing how the SDK interpreted each chunk.

### `summary.txt`
Overview with statistics, artifacts list, and full response text.

### `artifacts/`
Extracted artifact files saved with their original names.

### Batch Test Output

Batch reliability tests create structured output:

```
diagnostic_output/batch_2025-10-23_13-33-15/
├── summary.txt              # Overall success rate and statistics
├── results.csv              # Detailed CSV with all runs
├── system_prompt.txt        # System prompt used for testing
├── failed_responses/        # Failed runs with diagnostics
│   └── run_XXX/
│       ├── summary.txt      # Parsed and raw response text
│       └── artifacts/       # Any artifacts that were extracted
└── success_responses/       # Successful runs
    └── run_XXX/
        ├── summary.txt      # Parsed and raw response text
        └── artifacts/       # Extracted artifact files
```

Use batch testing to:
- Validate artifact completion reliability across models
- Compare success rates between different system prompts
- Identify patterns in failures
- Test parallel vs sequential execution

## 🧪 What This Tests

✅ **Artifact parsing** - Creates React component wrapped in `<artifact>` tags  
✅ **Tool calling** - Uses `get_weather` tool  
✅ **Streaming** - Tests chunk-by-chunk delivery  
✅ **Edge cases** - Tags split across chunks  
✅ **Performance** - TTFT and total time tracking  

## 🐛 Debugging

When something goes wrong, share the entire output directory to diagnose:
- `raw_stream.txt` - What the API actually sent
- `parsed_events.txt` - How the SDK interpreted it
- `summary.txt` - The final result

## 💡 Example Output

```
🔍 OpenRouter.NET Diagnostic Sample
=====================================

📁 Output directory: ./diagnostic_output_2025-10-22_14-30-45

📤 Sending request...
   Model: anthropic/claude-3.5-sonnet

🔄 Streaming response...
