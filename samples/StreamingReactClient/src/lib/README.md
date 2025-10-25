# @openrouter-net/sse-client

> **Note:** This is a template for extracting the SSE client into a standalone npm package. The working implementation is in `openrouter-sse-client.ts`.

Lightweight, type-safe SSE client for consuming OpenRouter.NET streaming endpoints.

## Installation

```bash
npm install @openrouter-net/sse-client
```

## Features

- ✅ **Zero dependencies** - Pure fetch API
- ✅ **TypeScript first** - Full type safety
- ✅ **Framework agnostic** - Works with any JavaScript framework
- ✅ **React hook included** - Optional React integration
- ✅ **Event-driven** - Simple callback API
- ✅ **Lightweight** - < 5kb minified + gzipped

## Quick Start

```typescript
import { OpenRouterSseClient } from '@openrouter-net/sse-client';

const client = new OpenRouterSseClient('http://localhost:5282');

await client.stream(
  {
    message: 'What is 2 + 2?',
    conversationId: 'my-conversation'
  },
  {
    onText: (content) => console.log(content),
    onToolCompleted: (event) => console.log('Result:', event.result),
    onArtifactCompleted: (artifact) => console.log('Code:', artifact.content),
  }
);
```

## React Hook

```typescript
import { useOpenRouterStream } from '@openrouter-net/sse-client';

function Chat() {
  const { stream, isStreaming, clearConversation } = useOpenRouterStream(
    'http://localhost:5282'
  );

  const handleSend = async (message: string) => {
    await stream({ message }, {
      onText: (text) => appendToChat(text),
      onToolCompleted: (event) => showToolResult(event),
    });
  };

  return (
    <button onClick={() => handleSend('Hello!')} disabled={isStreaming}>
      {isStreaming ? 'Streaming...' : 'Send'}
    </button>
  );
}
```

## API Reference

### `OpenRouterSseClient`

#### Constructor

```typescript
new OpenRouterSseClient(baseUrl: string)
```

#### Methods

##### `stream(request, options)`

Stream a chat completion with full event handling.

**Parameters:**
- `request: ChatRequest`
  - `message: string` - The user message
  - `model?: string` - Optional model name
  - `conversationId?: string` - Optional conversation ID
- `options: StreamOptions` - Event callbacks

**Returns:** `Promise<void>`

##### `clearConversation(conversationId)`

Clear a conversation from the server.

**Parameters:**
- `conversationId: string` - The conversation to clear

**Returns:** `Promise<void>`

### Event Callbacks

All callbacks are optional:

```typescript
interface StreamOptions {
  onEvent?: (event: SseEvent) => void;
  onText?: (content: string) => void;
  onToolExecuting?: (event: ToolExecutingEvent) => void;
  onToolCompleted?: (event: ToolCompletedEvent) => void;
  onToolError?: (event: ToolErrorEvent) => void;
  onArtifactStarted?: (event: ArtifactStartedEvent) => void;
  onArtifactContent?: (event: ArtifactContentEvent) => void;
  onArtifactCompleted?: (event: ArtifactCompletedEvent) => void;
  onComplete?: (event: CompletionEvent) => void;
  onError?: (event: ErrorEvent) => void;
}
```

### Event Types

#### `TextEvent`
```typescript
{
  type: 'text';
  content: string;
  chunkIndex: number;
  elapsedMs: number;
}
```

#### `ToolCompletedEvent`
```typescript
{
  type: 'tool_completed';
  toolName: string;
  toolId: string;
  arguments: string;
  result: string;
  executionTimeMs?: number;
  chunkIndex: number;
  elapsedMs: number;
}
```

#### `ArtifactCompletedEvent`
```typescript
{
  type: 'artifact_completed';
  artifactId: string;
  artifactType: string;
  title: string;
  language?: string;
  content: string;
  chunkIndex: number;
  elapsedMs: number;
}
```

## React Hook

### `useOpenRouterStream(baseUrl)`

React hook for managing streaming state.

**Returns:**
```typescript
{
  stream: (request: ChatRequest, options: StreamOptions) => Promise<void>;
  clearConversation: (conversationId: string) => Promise<void>;
  isStreaming: boolean;
}
```

## Examples

### Vanilla JavaScript

```javascript
const client = new OpenRouterSseClient('http://localhost:5282');

document.getElementById('send').addEventListener('click', async () => {
  const message = document.getElementById('input').value;
  const output = document.getElementById('output');
  
  await client.stream({ message }, {
    onText: (text) => {
      output.textContent += text;
    },
    onComplete: () => {
      console.log('Done!');
    }
  });
});
```

### Vue.js

```vue
<script setup>
import { ref } from 'vue';
import { OpenRouterSseClient } from '@openrouter-net/sse-client';

const client = new OpenRouterSseClient('http://localhost:5282');
const message = ref('');
const response = ref('');
const isStreaming = ref(false);

const send = async () => {
  isStreaming.value = true;
  response.value = '';
  
  await client.stream({ message: message.value }, {
    onText: (text) => {
      response.value += text;
    },
    onComplete: () => {
      isStreaming.value = false;
    }
  });
};
</script>

<template>
  <input v-model="message" />
  <button @click="send" :disabled="isStreaming">Send</button>
  <div>{{ response }}</div>
</template>
```

### Svelte

```svelte
<script>
  import { OpenRouterSseClient } from '@openrouter-net/sse-client';
  
  const client = new OpenRouterSseClient('http://localhost:5282');
  let message = '';
  let response = '';
  let isStreaming = false;
  
  async function send() {
    isStreaming = true;
    response = '';
    
    await client.stream({ message }, {
      onText: (text) => {
        response += text;
      },
      onComplete: () => {
        isStreaming = false;
      }
    });
  }
</script>

<input bind:value={message} />
<button on:click={send} disabled={isStreaming}>Send</button>
<div>{response}</div>
```

## Backend Integration

This client is designed to work with OpenRouter.NET streaming endpoints:

```csharp
// Backend (C#)
app.MapPost("/stream", async (ChatRequest request, HttpContext context) =>
{
    var client = new OpenRouterClient(apiKey);
    
    var chatRequest = new ChatCompletionRequest
    {
        Model = request.Model,
        Messages = history
    };
    
    await client.StreamAsSseAsync(chatRequest, context.Response);
});
```

## License

MIT

## Contributing

Contributions are welcome! Please open an issue or PR on GitHub.

## Links

- [OpenRouter.NET SDK](https://github.com/WilliamAvHolmberg/OpenRouter.NET)
- [Documentation](https://github.com/WilliamAvHolmberg/OpenRouter.NET#readme)
- [Examples](https://github.com/WilliamAvHolmberg/OpenRouter.NET/tree/main/samples)

