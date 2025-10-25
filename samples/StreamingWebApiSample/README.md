# Streaming Web API Sample

This sample demonstrates how to build a streaming ASP.NET Core Web API using OpenRouter.NET with:
- **Server-Sent Events (SSE)** streaming
- **Tool calling** (calculator functions)
- **Artifact support** (code generation)
- **Conversation management** (multi-turn conversations)

## Features

### 🔧 Tool Calling
The sample includes a `CalculatorTools` class with four operations:
- Add
- Subtract
- Multiply
- Divide

When users ask for calculations, the AI automatically invokes these tools and returns results via SSE.

### 📦 Artifact Support
The endpoint is configured to generate artifacts for:
- Code examples
- Scripts
- Deliverable content

Artifacts are automatically parsed and sent as SSE events (`artifact_started`, `artifact_content`, `artifact_completed`).

### 💬 Conversation Management
- Conversations are stored in-memory using `ConcurrentDictionary`
- Each conversation has a unique `conversationId`
- System prompt automatically added to new conversations
- Endpoint to clear conversations: `DELETE /conversation/{conversationId}`

## Endpoints

### POST /api/stream
Stream a chat completion with tools and artifacts.

**Request Body:**
```json
{
  "message": "Your message here",
  "model": "google/gemini-2.0-flash-exp",  // Optional, defaults to gemini-2.0-flash-exp
  "conversationId": "unique-id"             // Optional, auto-generated if not provided
}
```

**Response:** Server-Sent Events (SSE) stream with event types:
- `text` - Text content deltas
- `tool_executing` - Tool starting execution
- `tool_completed` - Tool finished with result
- `tool_error` - Tool execution error
- `artifact_started` - Artifact beginning
- `artifact_content` - Artifact content chunks
- `artifact_completed` - Complete artifact
- `completion` - Stream finished
- `error` - Error occurred

### DELETE /api/conversation/{conversationId}
Clear a conversation from the store.

**Response:**
```json
{
  "message": "Conversation cleared"
}
```

## Running the Sample

1. Set your OpenRouter API key in `Program.cs` line 24 (or better, use environment variables)
2. Run the application:
   ```bash
   dotnet run
   ```
3. Use the `.http` file to test endpoints or connect with a client

## Example Usage

### Simple Calculation
```bash
curl -X POST http://localhost:5282/api/stream \
  -H "Content-Type: application/json" \
  -d '{"message": "What is 25 + 17?"}'
```

### Code Artifact
```bash
curl -X POST http://localhost:5282/api/stream \
  -H "Content-Type: application/json" \
  -d '{"message": "Create a Python hello world function"}'
```

### Multi-turn Conversation
```bash
# First message
curl -X POST http://localhost:5282/api/stream \
  -H "Content-Type: application/json" \
  -d '{"message": "Hello!", "conversationId": "conv-1"}'

# Follow-up
curl -X POST http://localhost:5282/api/stream \
  -H "Content-Type: application/json" \
  -d '{"message": "Calculate 10 * 5", "conversationId": "conv-1"}'
```

## Client-Side Integration

### JavaScript/TypeScript
```javascript
const response = await fetch('/api/stream', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    message: 'What is 25 + 17?',
    conversationId: 'my-conversation'
  })
});

const reader = response.body.getReader();
const decoder = new TextDecoder();

while (true) {
  const { done, value } = await reader.read();
  if (done) break;

  const chunk = decoder.decode(value);
  const lines = chunk.split('\n').filter(line => line.startsWith('data: '));

  for (const line of lines) {
    const json = line.substring(6);
    const event = JSON.parse(json);
    
    switch (event.type) {
      case 'text':
        console.log('Text:', event.content);
        break;
      case 'tool_completed':
        console.log('Tool result:', event.result);
        break;
      case 'artifact_completed':
        console.log('Artifact:', event.title, event.content);
        break;
    }
  }
}
```

## SSE Event Structure

All events follow this base structure:
```json
{
  "type": "text|tool_executing|tool_completed|artifact_started|...",
  "chunkIndex": 0,
  "elapsedMs": 123
}
```

### Text Event
```json
{
  "type": "text",
  "chunkIndex": 5,
  "elapsedMs": 250,
  "content": "Hello"
}
```

### Tool Event
```json
{
  "type": "tool_completed",
  "chunkIndex": 10,
  "elapsedMs": 500,
  "toolName": "add",
  "toolId": "call_abc123",
  "arguments": "{\"a\":5,\"b\":3}",
  "result": "8"
}
```

### Artifact Event
```json
{
  "type": "artifact_completed",
  "chunkIndex": 20,
  "elapsedMs": 1000,
  "artifactId": "artifact_xyz",
  "artifactType": "code",
  "title": "hello.py",
  "language": "python",
  "content": "def hello():\n    print('Hello, World!')"
}
```

## Notes

- The `StreamAsSseAsync` extension handles all SSE formatting automatically
- Tools are registered per-request (consider using dependency injection for production)
- Conversation history is stored in-memory (use a database for production)
- The default model is `google/gemini-2.0-flash-exp` (fast and supports tools)

## See Also

- [OpenRouter.NET Documentation](../../README.md)
- [SSE Implementation](../../src/Sse/README.md)
- [Tool Calling Guide](../../llms.txt)

