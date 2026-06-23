# 💥 Crash Diagnostics - WriteLlmsTxt Failures

## Updated Behavior: FAIL LOUD 🔊

The tool now **crashes hard** with **maximum diagnostic information** if WriteLlmsTxt fails.

No more silent failures or confusing behavior!

## What Happens on Failure

### 1. Validation Failures

If content is invalid, the tool throws an exception immediately:

```
❌ CRITICAL ERROR: Content is too short (523 characters).
llms.txt should be comprehensive documentation (at least 10,000 characters 
recommended, 50,000+ for thorough docs).
```

**Throws**: `ArgumentException` with full diagnostic info

### 2. File System Failures

If file cannot be written:

```
💥 FATAL ERROR IN WriteLlmsTxt
Exception Type: IOException
Message: Access denied to path '/workspace/llms.txt'
```

**Throws**: `Exception` with wrapped inner exception

### 3. Diagnostic Information Dumped

On ANY failure, you'll see:

```
======================================================================
WriteLlmsTxt DIAGNOSTIC INFORMATION
======================================================================
Output path: /workspace/llms.txt
Already written: False
Content provided: True
Content length: 523 characters
Content lines: 15
Current directory: /workspace/.github/tools/llms-txt-generator
Output directory: /workspace
Directory exists: True

❌ CRITICAL ERROR: Content is too short (523 characters)

CONTENT PREVIEW (first 500 chars):
---
# OpenRouter.NET SDK

This is incomplete documentation...
---

======================================================================
💥 FATAL ERROR IN WriteLlmsTxt
======================================================================
Exception Type: ArgumentException
Message: ❌ CRITICAL ERROR: Content is too short...

Stack Trace:
   at LlmsTxtGenerator.Tools.WriteLlmsTxtTool.WriteLlmsTxt(String content)
   at ...

CONTENT PREVIEW (first 1000 chars):
---
[full content preview]
---

ENVIRONMENT INFO:
OS: Unix 6.1.147.0
User: ubuntu
.NET Version: 9.0.0

📝 Diagnostic log saved to: /workspace/llms-txt-error.log
```

### 4. Error Log File

A detailed log is saved to `llms-txt-error.log` in the output directory with:
- Full stack trace
- Content preview
- Environment information
- All diagnostic data

### 5. Application Crash

The exception is **re-thrown** and **not caught**, causing:
- Agent stops immediately
- Application exits with error code
- Full stack trace printed to console

## Example Failure Output

### Too Short Content

```
🔧 Executing tool: WriteLlmsTxt
   ⚠️  WRITING FINAL OUTPUT - This may take a moment...

======================================================================
WriteLlmsTxt DIAGNOSTIC INFORMATION
======================================================================
Output path: /workspace/llms.txt
Already written: False
Content provided: True
Content length: 523 characters
Content lines: 15
Current directory: /workspace/.github/tools/llms-txt-generator
Output directory: /workspace
Directory exists: True

ERROR: ❌ CRITICAL ERROR: Content is too short (523 characters). 
llms.txt should be comprehensive documentation (at least 10,000 characters 
recommended, 50,000+ for thorough docs). Did you complete your analysis? 
Have you read all the extension methods, samples, and existing llms.txt?

CONTENT PREVIEW (first 500 chars):
---
# OpenRouter.NET SDK

Basic usage...
---

======================================================================
💥 FATAL ERROR IN WriteLlmsTxt
======================================================================
Exception Type: ArgumentException
Message: ❌ CRITICAL ERROR: Content is too short (523 characters)...

Stack Trace:
   at LlmsTxtGenerator.Tools.WriteLlmsTxtTool.WriteLlmsTxt(String content)
   at OpenRouter.NET.Tools.ToolRegistry.ExecuteTool...

📝 Diagnostic log saved to: /workspace/llms-txt-error.log

======================================================================
💥 FATAL ERROR DURING AGENT EXECUTION
======================================================================
Exception Type: Exception
Message: WriteLlmsTxt FAILED!

[full diagnostic info repeated]

CONTEXT:
Iteration: 8/30
Model: anthropic/claude-3.5-sonnet
Files read so far: 12
Has written llms.txt: False
Conversation messages: 17

LAST TOOL CALLS:
  - ReadFile: [content of file]
  - ListDirectory: [directory listing]
  - WriteLlmsTxt: [error]

======================================================================

Unhandled exception. System.Exception: WriteLlmsTxt FAILED!
[application exits with error code]
```

## Why This is Better

### Before ❌
- Tool returned error string
- Agent might ignore it or misunderstand
- Agent continued exploring
- Hard to debug what went wrong
- Unclear if content was too short or file permission issue

### After ✅
- **Immediate crash** - no confusion
- **Full diagnostic dump** - see exactly what happened
- **Error log saved** - easy to review later
- **Content preview** - see what was attempted
- **Environment info** - helpful for debugging
- **Clear error messages** - know exactly what to fix

## Common Failure Scenarios

### 1. Content Too Short

**Cause**: Agent tried to write before completing analysis

**Diagnostic Shows**:
```
Content length: 523 characters
Content lines: 15

CONTENT PREVIEW:
# OpenRouter.NET SDK
Basic usage...
```

**Solution**: Increase `--max-iterations` or improve prompt to ensure thorough exploration

### 2. Permission Denied

**Cause**: Cannot write to output path

**Diagnostic Shows**:
```
Exception Type: UnauthorizedAccessException
Message: Access to the path '/root/llms.txt' is denied.
Output directory: /root
```

**Solution**: Change output path or fix permissions

### 3. Already Written

**Cause**: Tool called twice (shouldn't happen but defensive)

**Diagnostic Shows**:
```
Already written: True
ERROR: llms.txt has already been written successfully
```

**Solution**: This is a bug in agent logic - shouldn't call twice

### 4. Disk Full / No Space

**Cause**: Cannot create file

**Diagnostic Shows**:
```
Exception Type: IOException
Message: There is not enough space on the disk
Output directory: /workspace
Directory exists: True
```

**Solution**: Free up disk space

## Debugging Tips

### Check the Error Log

```bash
cat /workspace/llms-txt-error.log
```

Contains the complete diagnostic dump.

### Check Content Length

Look for:
```
Content length: X characters
Content lines: Y
```

Should be 10,000+ characters minimum, ideally 50,000+.

### Check File Permissions

```bash
ls -la /workspace/llms.txt
# Should be writable
```

### Check Agent Progress

Look for:
```
Files read so far: X
```

Should be 15-30+ files for thorough analysis.

### Verify Output Path

```
Output path: /workspace/llms.txt
Output directory: /workspace
Directory exists: True
```

All should be valid.

## Recovery After Failure

### If Content Too Short

Run again with more iterations:
```bash
dotnet run -- --path ../../.. --max-iterations 50
```

### If Permission Issue

Change output path:
```bash
dotnet run -- --path ../../.. --output /tmp/llms.txt
```

### If Agent Confused

Add explicit guidance or check the error log for what was attempted.

## Summary

**The tool now CRASHES HARD on any WriteLlmsTxt failure.**

Benefits:
- ✅ No silent failures
- ✅ Complete diagnostic information
- ✅ Error log saved for review
- ✅ Clear error messages
- ✅ Application exits immediately
- ✅ Easy to debug and fix

You'll know EXACTLY what went wrong and how to fix it! 💪
