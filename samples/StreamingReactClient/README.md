# OpenRouter.NET Streaming React Client

A beautiful, lightweight React client for consuming OpenRouter.NET streaming endpoints with full SSE support.

## ✨ Features

- 🎨 **Beautiful Modern UI** - Dark theme with smooth animations
- 📡 **Real-time Streaming** - Server-Sent Events with live updates
- 🔧 **Tool Execution Indicators** - See when AI is using tools
- 📦 **Artifact Display** - View generated code and documents
- 💬 **Multi-turn Conversations** - Persistent conversation history
- 📋 **Copy to Clipboard** - One-click artifact copying
- 📱 **Responsive Design** - Works on desktop and mobile

## 🚀 Quick Start

### 1. Start the Backend

First, make sure the StreamingWebApiSample is running:

```bash
cd ../StreamingWebApiSample
dotnet run
```

The backend should be running at `http://localhost:5282`

### 2. Start the Frontend

```bash
npm install
npm run dev
```

Open your browser to `http://localhost:5173`

## 📦 The SSE Client Library

The core streaming client is in `src/lib/openrouter-sse-client.ts` and is designed to be extracted into its own npm package: `@openrouter-net/sse-client`

### Usage

```typescript
import { OpenRouterSseClient } from './lib/openrouter-sse-client';

const client = new OpenRouterSseClient('http://localhost:5282');

await client.stream(
  {
    message: 'What is 2 + 2?',
    conversationId: 'my-conversation'
  },
  {
    onText: (content) => console.log(content),
    onToolExecuting: (event) => console.log('Tool:', event.toolName),
    onToolCompleted: (event) => console.log('Result:', event.result),
    onArtifactCompleted: (artifact) => console.log('Code:', artifact.content),
    onComplete: () => console.log('Done!'),
  }
);
```

### React Hook

```typescript
import { useOpenRouterStream } from './lib/openrouter-sse-client';

function Chat() {
  const { stream, isStreaming, clearConversation } = useOpenRouterStream('/api');

  const handleSend = async (message: string) => {
    await stream({ message }, {
      onText: (text) => appendToChat(text),
      onToolCompleted: (event) => showToolResult(event),
      onArtifactCompleted: (artifact) => saveArtifact(artifact)
    });
  };

  return (
    <div>
      <button onClick={() => handleSend('Hello!')} disabled={isStreaming}>
        Send
      </button>
    </div>
  );
}
```

## 🎯 Event Types

The client handles all SSE event types:

### Text Events
```typescript
onText: (content: string) => void
```
Receives text content deltas as they stream in.

### Tool Events
```typescript
onToolExecuting: (event: ToolExecutingEvent) => void
onToolCompleted: (event: ToolCompletedEvent) => void
onToolError: (event: ToolErrorEvent) => void
```
Track tool execution lifecycle with name, arguments, and results.

### Artifact Events
```typescript
onArtifactStarted: (event: ArtifactStartedEvent) => void
onArtifactContent: (event: ArtifactContentEvent) => void
onArtifactCompleted: (event: ArtifactCompletedEvent) => void
```
Monitor artifact creation with incremental content updates.

### Completion Events
```typescript
onComplete: (event: CompletionEvent) => void
onError: (event: ErrorEvent) => void
```
Handle stream completion and errors.

## 🛠️ Try These Examples

### Simple Calculation
```
What is 25 + 17?
```
Watch the AI use the calculator tool!

### Multiple Operations
```
Calculate 123 * 456, then divide the result by 2
```
See multiple tool calls in sequence.

### Code Generation
```
Create a Python hello world function
```
View the generated code artifact.

### Complex Request
```
Create a React button component with hover effects, and calculate what 50 times 20 is
```
See both tool usage and artifact generation!

## 📁 Project Structure

```
StreamingReactClient/
├── src/
│   ├── lib/
│   │   └── openrouter-sse-client.ts    # ⭐ SSE client library (extractable)
│   ├── App.tsx                          # Main chat application
│   ├── App.css                          # Styles
│   └── main.tsx                         # Entry point
├── package.json
├── vite.config.ts                       # Proxy configuration
└── README.md
```

## 🎨 UI Components

### Chat Messages
- **User messages** - Right-aligned blue
- **Assistant messages** - Left-aligned with streaming indicator
- **Timestamps** - For each message

### Tool Indicators
- **⚙️ Executing** - Yellow pulsing indicator
- **✅ Completed** - Green with result display
- **❌ Error** - Red with error message

### Artifacts Panel
- **Real-time preview** - See artifacts as they're being created
- **Syntax highlighting** - Language badges for code
- **One-click copy** - Copy to clipboard button

## 🔧 Configuration

### Vite Proxy Setup

The project uses Vite's proxy feature to forward API requests:

```typescript
// vite.config.ts
export default defineConfig({
  server: {
    proxy: {
      '/api': {
        target: 'http://localhost:5282',
        changeOrigin: true,
      }
    }
  }
})
```

This means:
- Frontend runs on `http://localhost:5173`
- API requests to `/api/*` are proxied to `http://localhost:5282/api/*`
- No CORS issues during development
- Clean separation of concerns

### Change Backend URL

For production, update the client base URL in `App.tsx`:

```typescript
const clientRef = useRef(new OpenRouterSseClient('https://your-api.com/api'));
```

Or use environment variables:

```typescript
const clientRef = useRef(new OpenRouterSseClient(
  import.meta.env.VITE_API_BASE_URL || '/api'
));
```

Then create `.env.production`:
```
VITE_API_BASE_URL=https://your-api.com/api
```

## 📦 Extracting the SSE Client as NPM Package

The `openrouter-sse-client.ts` is ready to be published as `@openrouter-net/sse-client`:

### 1. Create package structure
```
@openrouter-net/sse-client/
├── src/
│   └── index.ts                 # Export client and types
├── package.json
├── tsconfig.json
└── README.md
```

### 2. Update package.json
```json
{
  "name": "@openrouter-net/sse-client",
  "version": "1.0.0",
  "main": "./dist/index.js",
  "types": "./dist/index.d.ts",
  "exports": {
    ".": {
      "import": "./dist/index.js",
      "types": "./dist/index.d.ts"
    }
  }
}
```

### 3. Users can then:
```bash
npm install @openrouter-net/sse-client
```

```typescript
import { OpenRouterSseClient, useOpenRouterStream } from '@openrouter-net/sse-client';
```

## 🌟 Design Goals

### Developer Experience
- ✅ **Minimal setup** - Just instantiate and stream
- ✅ **Type-safe** - Full TypeScript support
- ✅ **Callback-based** - Simple event handlers
- ✅ **Zero dependencies** - Pure fetch API
- ✅ **React hook included** - But framework agnostic

### User Experience  
- ✅ **Real-time feedback** - See everything as it happens
- ✅ **Clear indicators** - Know what the AI is doing
- ✅ **Beautiful UI** - Modern, polished interface
- ✅ **Responsive** - Works on all screen sizes

## 🤝 Integration with Backend

This client is designed to work seamlessly with the OpenRouter.NET SDK:

**Backend (C#)**
```csharp
app.MapPost("/stream", async (ChatRequest request, HttpContext context) =>
{
    var client = new OpenRouterClient(apiKey);
    client.RegisterTool(calculator, nameof(calculator.Add));
    
    var chatRequest = new ChatCompletionRequest
    {
        Model = request.Model,
        Messages = history
    };
    
    request.EnableArtifactSupport();
    
    await client.StreamAsSseAsync(chatRequest, context.Response);
});
```

**Frontend (TypeScript)**
```typescript
await client.stream({ message: 'Add 5 and 3' }, {
  onToolCompleted: (event) => showResult(event.result),
  onArtifactCompleted: (artifact) => displayCode(artifact.content)
});
```

## 📚 Learn More

- [OpenRouter.NET SDK](../../README.md)
- [Streaming Web API Sample](../StreamingWebApiSample/README.md)
- [SSE Implementation](../../src/Sse/README.md)

## 🐛 Troubleshooting

### Backend not connecting
- Ensure backend is running on `http://localhost:5282`
- Check CORS settings if using different ports
- Verify proxy configuration in `vite.config.ts`

### Events not appearing
- Check browser console for errors
- Ensure backend is sending proper SSE format
- Verify `Content-Type: text/event-stream` header

### TypeScript errors
- Run `npm install` to ensure all dependencies are installed
- Check that `tsconfig.json` includes `src/lib/`

## 🎉 Enjoy!

This is a complete, production-ready example of streaming with OpenRouter.NET. Feel free to use it as a starting point for your own applications!
