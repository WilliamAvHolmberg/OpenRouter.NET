# OpenRouter.NET Development Proxy

## 💡 Concept

A hosted development proxy API that allows rapid prototyping without adding backends to every project. Client sends their OpenRouter API key directly, proxy logs everything for analysis and debugging.

## 🎯 Use Cases

- **Rapid prototyping** - Test OpenRouter in any frontend without backend setup
- **Debug streaming** - See exact SSE events and timing
- **Prompt engineering** - Analyze what works, get suggestions
- **Testing** - Try different models/prompts easily
- **Documentation** - Export conversations as examples

## 🏗️ Architecture

```
Client (React/Next.js/etc)
    ↓ POST { apiKey, model, messages }
Development Proxy API
    ↓ Use client's API key
OpenRouter API
    ↓ Stream response
Proxy → Database (log everything)
    ↓ SSE Stream
Client receives response
```

## 🔐 Security Model

**Development Mode Only:**
- Only enabled when `IsDevelopment()` is true
- Rate limiting per IP (e.g., 100 requests/hour)
- Never log full API keys (only last 8 chars)
- Auto-delete logs after 7 days
- Clear warning in responses

**Production:**
- Feature completely disabled
- Returns 404 if accessed

## 🌐 CORS Configuration

```csharp
// Allow common dev ports
policy.WithOrigins(
    "http://localhost:3000",   // Next.js
    "http://localhost:5173",   // Vite
    "http://localhost:5174"    // Vite alt
)
.AllowAnyMethod()
.AllowAnyHeader()
.AllowCredentials()
.WithExposedHeaders("*");  // For SSE
```

## 📁 Implementation Plan

### Phase 1: Basic Proxy
- `POST /proxy/stream` - Main proxy endpoint with client API key
- CORS configuration for localhost origins
- Simple in-memory logging

### Phase 2: Database Logging
- SQLite/PostgreSQL for conversation logs
- Request/response pairs
- SSE event details
- Token usage, timing, metadata

### Phase 3: Analysis Features
- `GET /proxy/conversations` - List all logged conversations
- `GET /proxy/conversation/{id}` - Get specific conversation
- `POST /proxy/replay/{id}` - Replay with different model/settings
- `GET /proxy/insights` - Pattern analysis, suggestions

### Phase 4: Dev Tools
- Export conversations as JSON/Markdown
- A/B test prompts
- Prompt suggestions based on patterns
- SSE event timeline visualization

## 🗄️ Database Schema

```sql
ConversationLogs:
- Id (Guid)
- Timestamp (DateTime)
- ClientApiKeyHint (string, last 8 chars only)
- Model (string)
- RequestJson (text)
- ResponseJson (text)
- SseEventsJson (text)
- DurationMs (int)
- TokensUsed (int)
- ClientIP (string)
- SessionId (string)
```

## 📋 Files to Create

```
samples/StreamingWebApiSample/
├── Program.cs                          [MODIFY] - Add CORS, logging
├── Controllers/
│   └── ProxyController.cs             [CREATE] - Proxy endpoints
├── Services/
│   ├── IConversationLogger.cs         [CREATE] - Logging interface
│   └── ConversationLogger.cs          [CREATE] - DB implementation
├── Models/
│   ├── ProxyRequest.cs                [CREATE] - Request model
│   └── ConversationLog.cs             [CREATE] - DB entity
└── appsettings.Development.json       [MODIFY] - Config
```

## 🚀 Hosting Ideas

- **Railway** - Simple deployment, free tier
- **Fly.io** - Global edge deployment
- **Azure App Service** - If already on Azure
- **Docker** - Self-host anywhere

## ⚠️ Important Notes

1. **Never log full API keys** - Security risk
2. **Rate limiting** - Prevent abuse
3. **Data retention** - Auto-cleanup old logs
4. **Disk space** - SSE events can be large
5. **Dev mode only** - Never enable in production

## 🎯 Benefits

✅ Lab quickly without backend in each project  
✅ Debug streaming issues with detailed logs  
✅ Learn prompt patterns from successful conversations  
✅ Share and replay conversations  
✅ Export examples for documentation  
✅ Test different models/settings easily  

---

**Status:** 💭 Idea/Planning Phase  
**Priority:** Nice to have for development workflow  
**Complexity:** Medium - ~2-3 days for full implementation


