# OpenRouter.NET SDK - Developer Handover Guide

> **Last Updated:** October 28, 2025  
> **SDK Version:** 0.3.0  
> **Target Audience:** Developers implementing chat interfaces with artifacts and tool calls

---

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Backend Implementation (.NET)](#backend-implementation-net)
3. [Frontend Implementation (React)](#frontend-implementation-react)
4. [Message Flow & Content Blocks](#message-flow--content-blocks)
5. [Artifact Handling](#artifact-handling)
6. [Tool Usage & Display](#tool-usage--display)
7. [Real-World Examples](#real-world-examples)
8. [Common Patterns](#common-patterns)
9. [Troubleshooting](#troubleshooting)

---

## Architecture Overview

### The Big Picture

```
┌─────────────┐         ┌──────────────┐         ┌─────────────┐
│   React     │  HTTP   │   ASP.NET    │   API   │ OpenRouter  │
│  Frontend   │ ──────> │   Backend    │ ──────> │   Service   │
│             │ <────── │              │ <────── │             │
└─────────────┘   SSE   └──────────────┘  Stream └─────────────┘
```

### Key Concepts

1. **Content Blocks Model**: Messages contain ordered blocks (text, artifacts, tool calls) that are interleaved in the exact order they appear in the stream
2. **Server-Sent Events (SSE)**: Backend streams events to frontend in real-time
3. **Tool Modes**: Tools can be server-side (executed on backend) or client-side (signal to frontend)
4. **Artifacts**: Special code/document content that can be rendered, previewed, or executed

---

## Backend Implementation (.NET)

### 1. Setup & Configuration

```csharp
using OpenRouter.NET;
using OpenRouter.NET.Sse;
using OpenRouter.NET.Models;
using OpenRouter.NET.Tools;
using OpenRouter.NET.Artifacts;

var apiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
var client = new OpenRouterClient(apiKey);
```

### 2. Basic Streaming Endpoint

```csharp
app.MapPost("/api/stream", async (ChatRequest chatRequest, HttpContext context) =>
{
    var client = new OpenRouterClient(apiKey);
    
    // Get or create conversation history
    var conversationId = chatRequest.ConversationId ?? Guid.NewGuid().ToString();
    var history = conversationStore.GetOrAdd(conversationId, _ => new List<Message>());
    
    // Add system prompt on first message
    if (history.Count == 0)
    {
        history.Add(Message.FromSystem("You are a helpful assistant."));
    }
    
    // Add user message
    history.Add(Message.FromUser(chatRequest.Message));
    
    // Create request
    var request = new ChatCompletionRequest
    {
        Model = chatRequest.Model ?? "anthropic/claude-3.5-sonnet",
        Messages = history
    };
    
    // Stream response as SSE
    var newMessages = await client.StreamAsSseAsync(request, context.Response);
    
    // Save assistant messages to history
    history.AddRange(newMessages);
});
```

### 3. Registering Tools

**Server-Side Tools** (executed on backend):
```csharp
public class CalculatorTools
{
    [ToolMethod("Add two numbers together")]
    public int Add(
        [ToolParameter("First number")] int a,
        [ToolParameter("Second number")] int b)
    {
        return a + b;
    }
}

// Register
var calculator = new CalculatorTools();
client.RegisterTool(calculator, nameof(calculator.Add));
```

**Client-Side Tools** (signal frontend to take action):
```csharp
public class FilterClientTools
{
    [ToolMethod("Apply filters to the list in the client UI")]
    public string SetFilters(
        [ToolParameter("Filter configuration")] Dictionary<string, object> filters)
    {
        // Return value doesn't matter - frontend will receive tool_client event
        return "ok";
    }
}

// Register with ClientSide mode
client.RegisterTool(filterTools, nameof(filterTools.SetFilters), ToolMode.ClientSide);
```

### 4. Enabling Artifacts

**Simple Enable (Auto-detection)**:
```csharp
request.EnableArtifactSupport(
    customInstructions: "Create artifacts for code examples, scripts, or deliverable content."
);
```

**Explicit Artifact Types** (with custom config from frontend):
```csharp
if (chatRequest.EnabledArtifacts != null)
{
    var enabled = chatRequest.EnabledArtifacts
        .Where(a => a.Enabled == true)
        .Select(a =>
        {
            var def = Artifacts.Custom(a.Type, a.PreferredTitle);
            if (!string.IsNullOrWhiteSpace(a.Language)) 
                def.WithLanguage(a.Language);
            if (!string.IsNullOrWhiteSpace(a.Instruction)) 
                def.WithInstruction(a.Instruction);
            return (ArtifactDefinition)def;
        })
        .ToArray();
    
    request.EnableArtifacts(enabled);
}
```

### 5. SSE Event Types from Backend

The backend sends these SSE events:

```typescript
type SseEventType =
  | 'text'              // Text content delta
  | 'tool_executing'    // Server-side tool started
  | 'tool_completed'    // Server-side tool finished
  | 'tool_error'        // Server-side tool failed
  | 'tool_client'       // Client-side tool signal (frontend should handle)
  | 'artifact_started'  // New artifact begins
  | 'artifact_content'  // Artifact content delta
  | 'artifact_completed'// Artifact finished
  | 'completion'        // Stream completed
  | 'error'             // Error occurred
```

---

## Frontend Implementation (React)

### 1. Setup Hook

```typescript
import { useOpenRouterChat } from '@openrouter-dotnet/react';

const { state, actions, debug } = useOpenRouterChat({
  endpoints: {
    stream: '/api/stream',
    clearConversation: '/api/conversation',
  },
  defaultModel: 'anthropic/claude-3.5-sonnet',
  config: {
    debug: true, // Enable debug logging
  },
});
```

### 2. Sending Messages

**Basic Send**:
```typescript
await actions.sendMessage('Create a React component');
```

**With Model Override**:
```typescript
await actions.sendMessage('Hello', { 
  model: 'openai/gpt-4o' 
});
```

**With Enabled Artifacts** (custom config):
```typescript
const enabledArtifacts = [
  {
    id: 'reactRunner',
    enabled: true,
    type: 'code',
    preferredTitle: 'Widget.tsx',
    language: 'tsx.reactrunner',
    instruction: 'Return exactly ONE self-contained React component...',
  },
];

await actions.sendMessage(input, { 
  model: selectedModel, 
  enabledArtifacts 
});
```

### 3. Other Actions

```typescript
// Clear conversation history
await actions.clearConversation();

// Retry last message (if error)
await actions.retry();

// Cancel current stream (not fully implemented)
actions.cancelStream();
```

### 4. State Structure

```typescript
interface ChatState {
  messages: ChatMessage[];           // All messages
  currentMessage: ChatMessage | null; // Currently streaming (if any)
  isStreaming: boolean;               // Is streaming active
  error: ErrorEvent | null;           // Last error
}

interface ChatMessage {
  id: string;
  role: 'user' | 'assistant' | 'system';
  blocks: ContentBlock[];     // Ordered blocks (text, artifacts, tools)
  timestamp: Date;
  isStreaming: boolean;       // Is this message still streaming
  model?: string;
  completion?: {
    finishReason?: string;
    model?: string;
    id?: string;
  };
}
```

---

## Message Flow & Content Blocks

### Content Block Types

Every message contains **ordered content blocks** that appear in the exact sequence they were streamed:

```typescript
type ContentBlock = TextBlock | ArtifactBlock | ToolCallBlock;

interface TextBlock {
  id: string;
  type: 'text';
  order: number;        // Position in stream
  timestamp: Date;
  content: string;
}

interface ArtifactBlock {
  id: string;
  type: 'artifact';
  order: number;
  timestamp: Date;
  artifactId: string;
  artifactType: string;
  title: string;
  language?: string;
  content: string;
  isStreaming: boolean;
}

interface ToolCallBlock {
  id: string;
  type: 'tool_call';
  order: number;
  timestamp: Date;
  toolId: string;
  toolName: string;
  arguments: string;
  result?: string;
  error?: string;
  executionTimeMs?: number;
  status: 'executing' | 'completed' | 'error';
}
```

### Rendering Messages

**Main Message Component**:
```tsx
export function Message({ message }: { message: ChatMessage }) {
  const isUser = message.role === 'user';
  
  return (
    <div className={`message ${isUser ? 'user' : 'assistant'}`}>
      {/* Render all blocks in order */}
      {message.blocks.map((block) => (
        <BlockView key={block.id} block={block} isUserMessage={isUser} />
      ))}
      
      {/* Streaming indicator */}
      {!isUser && message.isStreaming && <StreamingIndicator />}
    </div>
  );
}
```

**Block Router**:
```tsx
function BlockView({ block, isUserMessage }: BlockViewProps) {
  switch (block.type) {
    case 'text':
      return <TextBlock block={block} isUserMessage={isUserMessage} />;
    case 'artifact':
      return <ArtifactBlock block={block} />;
    case 'tool_call':
      return <ToolCallBlock block={block} />;
    default:
      return null;
  }
}
```

### How Blocks Are Created

The hook manages blocks automatically:

1. **Text**: Accumulates deltas into the last text block, or creates new block if interrupted
2. **Artifacts**: Create block on `artifact_started`, update on `artifact_content`, finalize on `artifact_completed`
3. **Tool Calls**: Create block on `tool_executing`/`tool_client`, update on `tool_completed`/`tool_error`

**Example Stream Order**:
```
[text] "I'll create a component for you"
[artifact] <starts streaming code>
[artifact] <accumulates content>
[artifact] <completes>
[text] "I've also added a calculator"
[tool_call] <executes Add(5, 3)>
[tool_call] <completes with result: 8>
[text] "The result is 8"
```

---

## Artifact Handling

### What Are Artifacts?

Artifacts are special content blocks for deliverable code, documents, or other structured content that can be:
- **Displayed** inline in chat
- **Downloaded** by the user
- **Executed/Previewed** (e.g., React components)

### Backend Artifact Configuration

```csharp
// Enable with custom type
request.EnableArtifacts(
    Artifacts.Custom("tsx.reactrunner", "Widget.tsx")
        .WithLanguage("tsx")
        .WithInstruction("Create a self-contained React component...")
);
```

### Frontend Artifact Display

**Basic Artifact Block**:
```tsx
export function ArtifactBlock({ block }: { block: ArtifactBlock }) {
  const [expanded, setExpanded] = useState(true);
  
  return (
    <div className="artifact">
      {/* Header with title and actions */}
      <div className="artifact-header">
        <span>{block.title}</span>
        <button onClick={() => navigator.clipboard.writeText(block.content)}>
          Copy
        </button>
        <a 
          download={block.title} 
          href={`data:text/plain;charset=utf-8,${encodeURIComponent(block.content)}`}
        >
          Download
        </a>
      </div>
      
      {/* Code content */}
      {expanded && (
        <pre className="artifact-code">
          <code>{block.content}</code>
        </pre>
      )}
    </div>
  );
}
```

### Auto-Scrolling During Streaming

Keep artifact code visible as it streams:
```tsx
const scrollRef = useRef<HTMLPreElement>(null);

useEffect(() => {
  if (block.isStreaming && scrollRef.current) {
    scrollRef.current.scrollTop = scrollRef.current.scrollHeight;
  }
}, [block.content, block.isStreaming]);
```

### React Component Preview (Advanced)

For `tsx.reactrunner` artifacts, you can render them live:

```tsx
import { ReactRunner } from './ReactRunner'; // Uses react-runner library

if (block.language === 'tsx.reactrunner') {
  return (
    <div>
      {/* Code display */}
      <ArtifactCode block={block} />
      
      {/* Live preview */}
      <button onClick={() => showInDrawer(
        <ReactRunner code={block.content} />
      )}>
        Preview Component
      </button>
    </div>
  );
}
```

---

## Tool Usage & Display

### Server-Side Tools

**Backend Execution** → Frontend displays progress:

```tsx
export function ToolCallBlock({ block }: { block: ToolCallBlock }) {
  const [expanded, setExpanded] = useState(false);
  
  const statusColor = 
    block.status === 'executing' ? 'blue' :
    block.status === 'completed' ? 'green' : 'red';
  
  return (
    <div className="tool-call">
      <button onClick={() => setExpanded(!expanded)}>
        <span>{block.toolName}</span>
        <span className={statusColor}>
          {block.status === 'executing' ? 'Running...' : 
           block.status === 'completed' ? 'Completed' : 'Error'}
        </span>
      </button>
      
      {expanded && (
        <div className="tool-details">
          {/* Execution time */}
          {block.executionTimeMs && (
            <div>{block.executionTimeMs}ms</div>
          )}
          
          {/* Arguments */}
          <div>Arguments: {block.arguments}</div>
          
          {/* Result */}
          {block.status === 'completed' && (
            <div>Result: {block.result}</div>
          )}
          
          {/* Error */}
          {block.status === 'error' && (
            <div>Error: {block.error}</div>
          )}
        </div>
      )}
    </div>
  );
}
```

### Client-Side Tools

**Backend signals** → Frontend takes action:

```typescript
const { state, actions } = useOpenRouterChat({
  // ... config
  onClientTool: (event: ToolClientEvent) => {
    // Handle client-side tool execution
    if (event.toolName === 'setOrderFilters') {
      const filters = JSON.parse(event.arguments);
      applyFiltersToUI(filters);
    }
  },
});
```

The `tool_client` event creates a regular `ToolCallBlock` but frontend can also handle it:

```tsx
// Display the tool call like normal
<ToolCallBlock block={toolBlock} />

// But also react to it programmatically via onClientTool
```

---

## Real-World Examples

### Example 1: Basic Chat Interface

```tsx
function ChatInterface() {
  const [input, setInput] = useState('');
  
  const { state, actions } = useOpenRouterChat({
    endpoints: {
      stream: '/api/stream',
      clearConversation: '/api/conversation',
    },
    defaultModel: 'anthropic/claude-3.5-sonnet',
  });
  
  const handleSend = async () => {
    if (!input.trim() || state.isStreaming) return;
    await actions.sendMessage(input);
    setInput('');
  };
  
  return (
    <div className="chat-interface">
      {/* Message list */}
      <div className="messages">
        {state.messages.map((message) => (
          <Message key={message.id} message={message} />
        ))}
      </div>
      
      {/* Input */}
      <input 
        value={input} 
        onChange={(e) => setInput(e.target.value)}
        onKeyDown={(e) => e.key === 'Enter' && handleSend()}
      />
      <button onClick={handleSend} disabled={state.isStreaming}>
        Send
      </button>
    </div>
  );
}
```

### Example 2: With Artifacts (React Runner)

```tsx
const handleSend = async () => {
  const enabledArtifacts = [
    {
      id: 'reactRunner',
      enabled: true,
      type: 'code',
      preferredTitle: 'Widget.tsx',
      language: 'tsx.reactrunner',
      instruction: `Return exactly ONE self-contained React component.
        Use Tailwind classes only. No imports beyond React.
        Emit as <artifact language="tsx.reactrunner" title="Widget.tsx">`,
    },
  ];
  
  await actions.sendMessage(input, { 
    model: selectedModel, 
    enabledArtifacts 
  });
  setInput('');
};
```

### Example 3: Dashboard with Client-Side Tools

**Backend**:
```csharp
public class DashboardTools
{
    [ToolMethod("Add widget to dashboard")]
    public void AddWidgetToDashboard(
        [ToolParameter("Artifact ID")] string artifactId,
        [ToolParameter("Widget ID")] string widgetId,
        [ToolParameter("Title")] string title,
        [ToolParameter("Size")] string size = "medium")
    {
        // No-op - client handles it
    }
}

client.RegisterTool(dashboardTools, 
    nameof(dashboardTools.AddWidgetToDashboard), 
    ToolMode.ClientSide);
```

**Frontend**:
```tsx
const { state, actions } = useOpenRouterChat({
  endpoints: { stream: '/api/dashboard/stream' },
  onClientTool: (event) => {
    if (event.toolName === 'add_widget_to_dashboard') {
      const { artifactId, widgetId, title, size } = JSON.parse(event.arguments);
      
      // Find the artifact in current message
      const artifact = findArtifactById(state.currentMessage, artifactId);
      
      // Add to dashboard
      addWidget({ 
        id: widgetId, 
        title, 
        size, 
        component: artifact.content 
      });
    }
  },
});
```

---

## Common Patterns

### Pattern 1: Extracting Content

```typescript
import { getTextContent, getArtifactBlocks, getToolCallBlocks } from '@openrouter-dotnet/react';

// Get all text (ignoring artifacts/tools)
const text = getTextContent(message);

// Get all artifacts
const artifacts = getArtifactBlocks(message);

// Get all tool calls
const tools = getToolCallBlocks(message);
```

### Pattern 2: Smooth Scroll on New Message

```tsx
function MessageList({ messages }: { messages: ChatMessage[] }) {
  const containerRef = useRef<HTMLDivElement>(null);
  const prevMessagesLengthRef = useRef(0);
  
  useEffect(() => {
    const container = containerRef.current;
    if (!container) return;
    
    // Scroll when new message added
    if (messages.length > prevMessagesLengthRef.current) {
      setTimeout(() => {
        container.scrollTo({
          top: container.scrollHeight,
          behavior: 'smooth',
        });
      });
    }
    
    prevMessagesLengthRef.current = messages.length;
  }, [messages.length]);
  
  return (
    <div ref={containerRef} className="message-list">
      {messages.map((msg) => <Message key={msg.id} message={msg} />)}
    </div>
  );
}
```

### Pattern 3: Message Animation

**CSS**:
```css
@keyframes fade-in-up {
  0% {
    opacity: 0;
    transform: translateY(20px);
  }
  100% {
    opacity: 1;
    transform: translateY(0);
  }
}

.animate-fade-in-up {
  animation: fade-in-up 0.5s ease-out forwards;
}
```

**Component**:
```tsx
<div className="message animate-fade-in-up">
  {/* message content */}
</div>
```

### Pattern 4: Debug Mode

```tsx
const { state, actions, debug } = useOpenRouterChat({
  config: { debug: true },
});

// Toggle debug
<button onClick={() => debug.toggle()}>
  {debug.enabled ? 'Disable' : 'Enable'} Debug
</button>

// View debug data
{debug.enabled && (
  <div>
    <h3>Raw SSE Lines</h3>
    <pre>{JSON.stringify(debug.data.rawLines, null, 2)}</pre>
    
    <h3>Parsed Events</h3>
    <pre>{JSON.stringify(debug.data.parsedEvents, null, 2)}</pre>
  </div>
)}
```

### Pattern 5: Model Selection

```tsx
import { useOpenRouterModels } from '@openrouter-dotnet/react';

function ModelSelector() {
  const { models, loading } = useOpenRouterModels('/api/models');
  const [selected, setSelected] = useState('anthropic/claude-3.5-sonnet');
  
  return (
    <select value={selected} onChange={(e) => setSelected(e.target.value)}>
      {models.map((model) => (
        <option key={model.id} value={model.id}>
          {model.name}
        </option>
      ))}
    </select>
  );
}
```

---

## Troubleshooting

### Issue: Blocks Appearing Out of Order

**Cause**: Hook manages order automatically via `order` counter  
**Solution**: Blocks should already be sorted. If not, use:
```typescript
import { sortBlocks } from '@openrouter-dotnet/react';
const sorted = sortBlocks(message.blocks);
```

### Issue: Artifact Not Streaming

**Cause**: Not checking `isStreaming` flag  
**Solution**:
```tsx
{block.type === 'artifact' && block.isStreaming && (
  <span>Generating...</span>
)}
```

### Issue: Tool Result Not Showing

**Cause**: Status not 'completed'  
**Solution**:
```tsx
{block.type === 'tool_call' && block.status === 'completed' && block.result && (
  <div>Result: {block.result}</div>
)}
```

### Issue: Client Tool Not Firing

**Cause**: Not registered with `ToolMode.ClientSide`  
**Solution** (backend):
```csharp
client.RegisterTool(tools, nameof(tools.MyTool), ToolMode.ClientSide);
```

### Issue: Message Not Auto-Scrolling

**Cause**: Missing scroll effect on message count change  
**Solution**: See Pattern 2 above

### Issue: SSE Connection Drops

**Cause**: Backend timeout or network issue  
**Solution**: Check backend keeps connection alive, ensure proper SSE headers

---

## Key Takeaways

1. **Content blocks are ordered**: Always render `message.blocks` in sequence
2. **Artifacts stream like text**: Handle `isStreaming` flag and delta updates
3. **Tools have two modes**: Server-side (backend executes) vs Client-side (frontend handles)
4. **Use the hook**: `useOpenRouterChat` manages all complexity
5. **Debug mode exists**: Enable it when troubleshooting streams

---

## Quick Reference

### Backend Essentials
```csharp
// Setup
var client = new OpenRouterClient(apiKey);

// Stream
await client.StreamAsSseAsync(request, context.Response);

// Tools
client.RegisterTool(obj, methodName, ToolMode.ClientSide);

// Artifacts
request.EnableArtifacts(Artifacts.Custom("tsx.reactrunner", "Widget.tsx"));
```

### Frontend Essentials
```typescript
// Hook
const { state, actions, debug } = useOpenRouterChat({ endpoints, defaultModel });

// Send
await actions.sendMessage(text, { model, enabledArtifacts });

// Render
message.blocks.map(block => <BlockView block={block} />)

// Types
block.type === 'text' | 'artifact' | 'tool_call'
```

---

## Additional Resources

- **Sample Code**: `/samples/NextJsClientSample/` - Full working example
- **Backend Sample**: `/samples/StreamingWebApiSample/` - ASP.NET endpoints
- **Types Reference**: `/packages/react-sdk/src/types.ts` - All TypeScript types
- **Hook Source**: `/packages/react-sdk/src/hooks/useOpenRouterChat.ts` - Implementation details

---

**Questions?** Check the samples or refer to the source code. The architecture is straightforward - SSE events flow from backend, hook transforms them into content blocks, components render blocks in order. That's it!

