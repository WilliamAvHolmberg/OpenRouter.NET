# Complete SSE Streaming Example

This example shows a complete ASP.NET Core minimal API with SSE streaming support.

## Server-Side (ASP.NET Core)

### Program.cs

```csharp
using OpenRouter.NET;
using OpenRouter.NET.Sse;
using OpenRouter.NET.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);

// Configure CORS for browser access
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();
app.UseCors();

// SSE streaming endpoint
app.MapPost("/api/chat/stream", async (HttpContext context, ChatRequest body) =>
{
    var apiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
    if (string.IsNullOrEmpty(apiKey))
    {
        return Results.Problem("OpenRouter API key not configured", statusCode: 500);
    }

    var client = new OpenRouterClient(apiKey);
    
    var request = new ChatCompletionRequest
    {
        Model = body.Model ?? "anthropic/claude-3.5-sonnet",
        Messages = body.Messages
    };

    // ONE LINE - Handles everything!
    await client.StreamAsSseAsync(request, context.Response, context.RequestAborted);
    
    return Results.Empty;
});

// Non-streaming endpoint for comparison
app.MapPost("/api/chat", async (HttpContext context, ChatRequest body) =>
{
    var apiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
    if (string.IsNullOrEmpty(apiKey))
    {
        return Results.Problem("OpenRouter API key not configured", statusCode: 500);
    }

    var client = new OpenRouterClient(apiKey);
    
    var request = new ChatCompletionRequest
    {
        Model = body.Model ?? "anthropic/claude-3.5-sonnet",
        Messages = body.Messages
    };

    var response = await client.CreateChatCompletionAsync(request, context.RequestAborted);
    return Results.Ok(response);
});

app.Run();

// Request model
record ChatRequest(List<Message> Messages, string? Model = null);
```

## Client-Side Examples

### Vanilla JavaScript

```html
<!DOCTYPE html>
<html>
<head>
    <title>OpenRouter.NET SSE Demo</title>
</head>
<body>
    <div>
        <textarea id="input" placeholder="Type your message..."></textarea>
        <button onclick="sendMessage()">Send</button>
    </div>
    <div id="response"></div>
    <div id="status"></div>

    <script>
        let isStreaming = false;

        async function sendMessage() {
            if (isStreaming) return;
            
            const input = document.getElementById('input');
            const response = document.getElementById('response');
            const status = document.getElementById('status');
            
            const message = input.value;
            if (!message) return;
            
            isStreaming = true;
            status.textContent = 'Streaming...';
            response.textContent = '';

            try {
                const res = await fetch('http://localhost:5000/api/chat/stream', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        messages: [
                            { role: 'user', content: message }
                        ]
                    })
                });

                if (!res.ok) {
                    throw new Error(`HTTP error! status: ${res.status}`);
                }

                const reader = res.body.getReader();
                const decoder = new TextDecoder();
                let buffer = '';

                while (true) {
                    const { done, value } = await reader.read();
                    if (done) break;
                    
                    buffer += decoder.decode(value, { stream: true });
                    
                    const parts = buffer.split('\n\n');
                    buffer = parts.pop() || '';
                    
                    for (const part of parts) {
                        const lines = part.split('\n');
                        for (const line of lines) {
                            if (line.startsWith('data: ')) {
                                const event = JSON.parse(line.slice(6));
                                handleEvent(event, response, status);
                            }
                        }
                    }
                }
                
                status.textContent = 'Complete!';
            } catch (err) {
                status.textContent = 'Error: ' + err.message;
            } finally {
                isStreaming = false;
            }
        }

        function handleEvent(event, responseDiv, statusDiv) {
            switch (event.type) {
                case 'text':
                    responseDiv.textContent += event.textDelta;
                    break;
                case 'tool_executing':
                    statusDiv.textContent = `🔧 Executing tool: ${event.toolName}`;
                    break;
                case 'tool_completed':
                    statusDiv.textContent = `✅ Tool completed: ${event.toolName}`;
                    break;
                case 'artifact_completed':
                    const artifactDiv = document.createElement('div');
                    artifactDiv.innerHTML = `
                        <h3>${event.title}</h3>
                        <pre><code>${event.content}</code></pre>
                    `;
                    responseDiv.appendChild(artifactDiv);
                    break;
                case 'completion':
                    statusDiv.textContent = `Done! (${event.finishReason})`;
                    break;
                case 'error':
                    statusDiv.textContent = `❌ Error: ${event.message}`;
                    break;
            }
        }
    </script>
</body>
</html>
```

### React + TypeScript

```tsx
import { useState } from 'react';

interface Message {
    role: 'user' | 'assistant' | 'system';
    content: string;
}

interface SseEvent {
    type: string;
    chunkIndex: number;
    elapsedMs: number;
}

interface TextEvent extends SseEvent {
    type: 'text';
    textDelta: string;
}

function ChatComponent() {
    const [messages, setMessages] = useState<Message[]>([]);
    const [input, setInput] = useState('');
    const [currentResponse, setCurrentResponse] = useState('');
    const [isStreaming, setIsStreaming] = useState(false);
    const [status, setStatus] = useState('');

    const streamChat = async (userMessage: string) => {
        setIsStreaming(true);
        setCurrentResponse('');
        setStatus('Streaming...');

        const newMessages: Message[] = [
            ...messages,
            { role: 'user', content: userMessage }
        ];

        try {
            const response = await fetch('http://localhost:5000/api/chat/stream', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ messages: newMessages })
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const reader = response.body!.getReader();
            const decoder = new TextDecoder();
            let buffer = '';
            let fullResponse = '';

            while (true) {
                const { done, value } = await reader.read();
                if (done) break;
                
                buffer += decoder.decode(value, { stream: true });
                
                const parts = buffer.split('\n\n');
                buffer = parts.pop() || '';
                
                for (const part of parts) {
                    const lines = part.split('\n');
                    for (const line of lines) {
                        if (line.startsWith('data: ')) {
                            const event = JSON.parse(line.slice(6)) as SseEvent;
                            
                            if (event.type === 'text') {
                                const textEvent = event as TextEvent;
                                fullResponse += textEvent.textDelta;
                                setCurrentResponse(fullResponse);
                            } else if (event.type === 'completion') {
                                setStatus('Complete!');
                            }
                        }
                    }
                }
            }

            // Add assistant response to messages
            setMessages([
                ...newMessages,
                { role: 'assistant', content: fullResponse }
            ]);
        } catch (err) {
            setStatus(`Error: ${err instanceof Error ? err.message : 'Unknown error'}`);
        } finally {
            setIsStreaming(false);
            setCurrentResponse('');
        }
    };

    const handleSubmit = () => {
        if (!input.trim() || isStreaming) return;
        streamChat(input);
        setInput('');
    };

    return (
        <div style={{ padding: '20px', maxWidth: '800px', margin: '0 auto' }}>
            <h1>OpenRouter.NET Chat Demo</h1>
            
            <div style={{ marginBottom: '20px', minHeight: '400px', border: '1px solid #ccc', padding: '10px' }}>
                {messages.map((msg, i) => (
                    <div key={i} style={{ 
                        margin: '10px 0', 
                        padding: '10px',
                        backgroundColor: msg.role === 'user' ? '#e3f2fd' : '#f5f5f5',
                        borderRadius: '8px'
                    }}>
                        <strong>{msg.role}:</strong> {msg.content}
                    </div>
                ))}
                
                {currentResponse && (
                    <div style={{ 
                        margin: '10px 0', 
                        padding: '10px',
                        backgroundColor: '#f5f5f5',
                        borderRadius: '8px'
                    }}>
                        <strong>assistant:</strong> {currentResponse}
                    </div>
                )}
            </div>

            <div style={{ display: 'flex', gap: '10px' }}>
                <input
                    type="text"
                    value={input}
                    onChange={(e) => setInput(e.target.value)}
                    onKeyPress={(e) => e.key === 'Enter' && handleSubmit()}
                    placeholder="Type your message..."
                    style={{ flex: 1, padding: '10px', fontSize: '16px' }}
                    disabled={isStreaming}
                />
                <button 
                    onClick={handleSubmit}
                    disabled={isStreaming}
                    style={{ padding: '10px 20px', fontSize: '16px' }}
                >
                    {isStreaming ? 'Streaming...' : 'Send'}
                </button>
            </div>
            
            {status && <div style={{ marginTop: '10px', color: '#666' }}>{status}</div>}
        </div>
    );
}

export default ChatComponent;
```

## Environment Setup

```bash
# Set your API key
export OPENROUTER_API_KEY="your-key-here"

# Run the server
dotnet run

# Server will be available at http://localhost:5000
```

## Testing with curl

```bash
# Stream to console
curl -N -X POST http://localhost:5000/api/chat/stream \
  -H "Content-Type: application/json" \
  -d '{
    "messages": [
      {"role": "user", "content": "Count to 5"}
    ]
  }'
```

Expected output:
```
data: {"type":"text","chunkIndex":0,"elapsedMs":123.45,"textDelta":"1"}

data: {"type":"text","chunkIndex":1,"elapsedMs":145.67,"textDelta":", "}

data: {"type":"text","chunkIndex":2,"elapsedMs":167.89,"textDelta":"2"}

...

data: {"type":"completion","chunkIndex":10,"elapsedMs":890.12,"finishReason":"stop"}
```
