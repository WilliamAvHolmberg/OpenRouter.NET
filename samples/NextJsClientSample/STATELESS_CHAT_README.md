# Stateless Chat Demo

This demo showcases a **production-ready, stateless chat architecture** using client-side history management with localStorage.

## 🎯 What This Demonstrates

**Traditional (Server-Side) Pattern:**
```
Client → "Hello" → Server (stores in memory) → OpenRouter API
Client → "Continue" → Server (retrieves from memory + adds new msg) → OpenRouter API
```
❌ **Problem:** Server memory grows unbounded, can't scale horizontally, loses state on restart

**Stateless (Client-Side) Pattern:**
```
Client (localStorage) → sends full history + "Hello" → Server (stateless) → OpenRouter API
Client (localStorage) → sends full history + "Continue" → Server (stateless) → OpenRouter API
```
✅ **Solution:** Zero server memory, infinite scalability, survives restarts

## 🚀 How to Use

### 1. Navigate to the Demo

Visit `/stateless-chat` in the Next.js application

### 2. Start a Conversation

Type a message - it will be stored in your browser's localStorage automatically.

### 3. Check Server Logs

You'll see console output showing:
```
[STATELESS] Received 2 messages from client
[STATELESS] Streamed 1 response messages. Server memory: 0 bytes
```

### 4. Test Persistence

- Refresh the page → conversation persists ✅
- Restart the backend server → conversation still there ✅
- Open DevTools → Application → Local Storage → see your messages

## 📂 Files

### Frontend

**Page:**
- `/src/app/stateless-chat/page.tsx` - Next.js page route

**Component:**
- `/src/components/chat/StatelessChatInterface.tsx` - Main chat UI with:
  - localStorage integration
  - Conversation list sidebar
  - Auto-save on message updates
  - Multi-conversation support

### Backend

**Endpoint:**
- `/api/stream-stateless` in `StreamingWebApiSample/Program.cs` (lines 179-266)

**Key Features:**
- ❌ NO `conversationStore` usage
- ✅ Accepts `chatRequest.Messages` from client
- ✅ Console logging to show stateless behavior

## 🔍 Code Walkthrough

### Frontend: Sending Messages with History

```typescript
const handleSend = async () => {
  // Load current history from localStorage
  const currentHistory = loadHistory(conversationId);

  // Send with client-side history!
  await actions.sendMessage(input, {
    model: DEFAULT_MODEL,
    history: currentHistory, // ← Custom history array
  });
};
```

### Backend: Stateless Processing

```csharp
// ⚠️ CRITICAL: NO conversationStore usage
List<Message> messagesToSend;

if (chatRequest.Messages != null && chatRequest.Messages.Count > 0)
{
    // Client provided history - use it!
    messagesToSend = new List<Message>(chatRequest.Messages);
    messagesToSend.Add(Message.FromUser(chatRequest.Message));
}
else
{
    // No history - start fresh
    messagesToSend = new List<Message>
    {
        Message.FromSystem("You are a helpful assistant."),
        Message.FromUser(chatRequest.Message)
    };
}

// Stream response - NO server-side persistence!
var newMessages = await client.StreamAsSseAsync(request, context.Response);
// ⚠️ We DON'T save newMessages anywhere
```

### SDK: Flexible History Support

```typescript
// Option 1: Client-side with custom history (what we use here!)
const customHistory = loadHistory('conv-123');
await sendMessage("Hi", { history: customHistory });

// Option 2: Server-side (traditional)
await sendMessage("Hi"); // No history parameter - backend manages state
```

## 📊 Comparison

| Feature | Server-Side | Client-Side (This Demo) |
|---------|-------------|-------------------------|
| **Server Memory** | Grows unbounded | 0 KB |
| **Scalability** | Requires sticky sessions | Infinite horizontal |
| **Persistence** | Lost on restart | Survives restarts |
| **Session Affinity** | Required | Not needed |
| **Complexity** | Simple | Slightly more complex |
| **Best For** | Prototypes | Production |

## 🧪 Testing the Demo

### Test 1: Multi-Conversation
1. Start conversation "conv_1"
2. Send 3 messages
3. Click "New Conversation"
4. Send 2 messages in "conv_2"
5. Switch back to "conv_1" → see original 3 messages ✅

### Test 2: Persistence
1. Send several messages
2. Refresh browser → messages still there ✅
3. Restart backend server → messages still there ✅
4. Close browser tab, reopen → messages still there ✅

### Test 3: Storage Inspection
1. Open DevTools → Application → Local Storage
2. Find keys like `openrouter_chat_conv_1234567890`
3. See JSON with full message history
4. Check size in sidebar stats

## 🏗️ Architecture Benefits

### Production-Ready Features

**Zero Server Memory:**
- No memory leaks possible
- No need for cleanup jobs
- Predictable resource usage

**Horizontal Scalability:**
- Any server can handle any request
- Load balancer distributes freely
- Add/remove servers without state concerns

**Resilience:**
- Server crashes lose nothing
- Deploy new versions seamlessly
- No distributed state management needed

**Natural Limits:**
- Browser localStorage quota prevents abuse
- Network payload size limits conversation length
- Users see the cost (bandwidth) of long conversations

## 🔧 Advanced Usage

### Custom Pruning

```typescript
// Keep only last 50 messages to reduce payload
const fullHistory = loadHistory(conversationId);
const pruned = fullHistory.slice(-50);
await sendMessage("Continue", { history: pruned });
```

### Pre-Processing

```typescript
// Filter sensitive content before sending
const history = loadHistory(conversationId);
const sanitized = history.filter(msg =>
  !msg.blocks.some(b => b.type === 'sensitive')
);
await sendMessage("Task", { history: sanitized });
```

### Context Injection

```typescript
// Add system prompt to existing history
const baseHistory = loadHistory(conversationId);
const withContext = [
  createMessage('system', 'Custom instructions...'),
  ...baseHistory
];
await sendMessage("Task", { history: withContext });
```

## 📝 localStorage Utilities

The SDK provides helpers for history management:

```typescript
import {
  saveHistory,
  loadHistory,
  clearHistory,
  listConversations,
  getStorageSize,
} from '@openrouter-dotnet/react';

// Auto-save with pruning
saveHistory(conversationId, messages, { maxMessages: 100 });

// Load conversation
const history = loadHistory(conversationId);

// Clear conversation
clearHistory(conversationId);

// List all conversations
const ids = listConversations(); // ['conv_1', 'conv_2', ...]

// Check storage size
const bytes = getStorageSize(conversationId);
```

## 🎓 Learning Points

1. **Client-side history is production-ready** - Not just a toy pattern
2. **The SDK is flexible** - Doesn't force you into one approach
3. **Zero server memory is achievable** - No trade-offs in functionality
4. **localStorage is powerful** - Great for many use cases
5. **The conversation is the data** - Store it where it makes sense

## 🚦 When to Use

**Use Stateless (Client-Side):**
- ✅ Production web applications
- ✅ High-traffic services
- ✅ Horizontally scaled deployments
- ✅ When you need resilience

**Use Stateful (Server-Side):**
- ✅ Prototypes and demos
- ✅ Single-user CLI tools
- ✅ When server has strong security requirements
- ✅ When client can't be trusted with history

## 📚 Related Files

- `/packages/react-sdk/src/utils/messageConverter.ts` - ChatMessage → Message conversion
- `/packages/react-sdk/src/utils/historyPersistence.ts` - localStorage helpers
- `/packages/react-sdk/src/hooks/useOpenRouterChat.ts` - Hook implementation
- `/samples/StreamingWebApiSample/Program.cs` - Backend endpoints

## 🎉 Try It Out!

Run the Next.js app and backend, then:

1. Navigate to `/stateless-chat`
2. Send a few messages
3. Check browser DevTools → Local Storage
4. Restart the backend server
5. Refresh the page → conversation persists!

**You've just experienced zero-server-memory chat!** 🚀
