# @openrouter-dotnet/react

React SDK for OpenRouter streaming chat with artifacts and tool calls.

## Features

- 🚀 **Streaming Chat**: Real-time streaming responses
- 🎨 **Artifacts**: Support for code generation and file artifacts
- 🔧 **Tool Calls**: Execute tools during conversations
- 📱 **React Hooks**: Easy-to-use React hooks
- 🔍 **Debug Mode**: Built-in debugging capabilities
- 📦 **TypeScript**: Full TypeScript support

## Installation

```bash
npm install @openrouter-dotnet/react
```

## Quick Start

### Basic Chat

```tsx
import { useOpenRouterChat } from '@openrouter-dotnet/react';

function ChatApp() {
  const { state, actions } = useOpenRouterChat({
    baseUrl: 'https://your-api.com',
    defaultModel: 'openai/gpt-4o'
  });

  return (
    <div>
      {state.messages.map((message) => (
        <div key={message.id}>
          <strong>{message.role}:</strong>
          {message.blocks.map((block) => (
            <div key={block.id}>
              {block.type === 'text' && <p>{block.content}</p>}
              {block.type === 'artifact' && (
                <pre>{block.content}</pre>
              )}
            </div>
          ))}
        </div>
      ))}
      
      <button onClick={() => actions.sendMessage('Hello!')}>
        Send Message
      </button>
    </div>
  );
}
```

### Simple Text Streaming

```tsx
import { useStreamingText } from '@openrouter-dotnet/react';

function SimpleChat() {
  const { text, isStreaming, stream } = useStreamingText({
    baseUrl: 'https://your-api.com'
  });

  return (
    <div>
      <div>{text}</div>
      {isStreaming && <div>Streaming...</div>}
      <button onClick={() => stream('Hello!')}>
        Send
      </button>
    </div>
  );
}
```

## API Reference

### Hooks

#### `useOpenRouterChat(options)`

Main hook for full chat functionality with artifacts and tools.

**Options:**
- `baseUrl: string` - Your API base URL
- `conversationId?: string` - Optional conversation ID
- `defaultModel?: string` - Default model to use
- `config?: ClientConfig` - Client configuration

**Returns:**
- `state: ChatState` - Current chat state
- `actions: ChatActions` - Chat actions
- `debug: DebugControls` - Debug controls

#### `useStreamingText(options)`

Simple hook for text-only streaming.

**Options:**
- `baseUrl: string` - Your API base URL
- `model?: string` - Model to use
- `config?: ClientConfig` - Client configuration

**Returns:**
- `text: string` - Current text content
- `isStreaming: boolean` - Is currently streaming
- `error: ErrorEvent | null` - Last error
- `completion: CompletionEvent | null` - Completion info
- `stream(message: string)` - Send a message
- `reset()` - Reset state

#### `useOpenRouterModels(baseUrl)`

Fetch available models from your API.

**Returns:**
- `models: Model[]` - Available models
- `loading: boolean` - Is loading
- `error: Error | null` - Error if any

### Core Client

#### `OpenRouterClient`

Low-level client for direct API interaction.

```tsx
import { OpenRouterClient } from '@openrouter-dotnet/react';

const client = new OpenRouterClient('https://your-api.com');

// Stream a message
await client.stream({
  message: 'Hello!',
  model: 'openai/gpt-4o'
}, {
  onText: (delta) => console.log(delta),
  onComplete: (event) => console.log('Done!', event)
});
```

## Content Blocks

The SDK uses a content block model where text, artifacts, and tool calls are interleaved in the order they appear in the stream.

### Text Blocks
```tsx
interface TextBlock {
  type: 'text';
  content: string;
}
```

### Artifact Blocks
```tsx
interface ArtifactBlock {
  type: 'artifact';
  artifactId: string;
  artifactType: string;
  title: string;
  language?: string;
  content: string;
  isStreaming: boolean;
}
```

### Tool Call Blocks
```tsx
interface ToolCallBlock {
  type: 'tool_call';
  toolId: string;
  toolName: string;
  arguments: string;
  result?: string;
  error?: string;
  status: 'executing' | 'completed' | 'error';
}
```

## Utilities

```tsx
import {
  getTextBlocks,
  getArtifactBlocks,
  getToolCallBlocks,
  getTextContent,
  hasArtifacts,
  hasToolCalls
} from '@openrouter-dotnet/react';

// Extract specific block types
const textBlocks = getTextBlocks(message);
const artifactBlocks = getArtifactBlocks(message);
const toolBlocks = getToolCallBlocks(message);

// Get all text content
const textContent = getTextContent(message);

// Check for specific content
const hasArtifacts = hasArtifacts(message);
const hasTools = hasToolCalls(message);
```

## Debug Mode

Enable debug mode to see raw stream data and parsed events:

```tsx
const { debug } = useOpenRouterChat({
  baseUrl: 'https://your-api.com'
});

// Toggle debug mode
debug.toggle();

// Clear debug data
debug.clear();

// Access debug data
console.log(debug.data.rawLines);
console.log(debug.data.parsedEvents);
```

## TypeScript

The package includes full TypeScript definitions:

```tsx
import type {
  ChatMessage,
  ContentBlock,
  TextBlock,
  ArtifactBlock,
  ToolCallBlock,
  ChatState,
  ChatActions,
  UseChatReturn
} from '@openrouter-dotnet/react';
```

## License

MIT
