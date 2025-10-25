# 🚀 Quick Start Guide

Get up and running with OpenRouter.NET streaming in 5 minutes!

## Step 1: Start the Backend (1 minute)

```bash
cd samples/StreamingWebApiSample
dotnet run
```

The API will start at `http://localhost:5282`

## Step 2: Start the Frontend (2 minutes)

```bash
cd samples/StreamingReactClient
npm install  # Only needed first time
npm run dev
```

Open your browser to `http://localhost:5173`

## Step 3: Try it out! (2 minutes)

### Test Tool Usage
Type in the chat:
```
What is 25 + 17?
```

Watch the AI:
1. ⚙️ **Executing** - Yellow indicator shows tool is running
2. ✅ **Completed** - Green shows result: `42`
3. 💬 **Response** - AI explains the answer

### Test Artifact Generation
Type:
```
Create a Python hello world function
```

Watch:
1. 📦 **Creating artifact** - Shows progress
2. ✅ **Artifact completed** - Appears in right panel
3. 📋 **Copy button** - One-click copy to clipboard

### Test Multiple Tools
Type:
```
Calculate 123 * 456, then divide the result by 2
```

Watch multiple tools execute in sequence!

### Test Conversation
Type:
```
My name is Alex
```

Then:
```
What's my name?
```

The AI remembers the conversation context!

## That's it! 🎉

You now have a fully functional streaming AI chat with:
- ✅ Real-time streaming responses
- ✅ Tool execution with live indicators
- ✅ Artifact generation and display
- ✅ Multi-turn conversations
- ✅ Beautiful modern UI

## Next Steps

### Customize the Backend
Edit `StreamingWebApiSample/Program.cs`:
- Add more tools
- Change system prompt
- Configure different models
- Add authentication

### Customize the Frontend
Edit `StreamingReactClient/src/App.tsx`:
- Change colors in `App.css`
- Add new features
- Integrate with your app
- Extract the SSE client to npm package

### Deploy
The SSE client is production-ready and can be:
- Extracted to `@openrouter-net/sse-client` npm package
- Used in any JavaScript framework (React, Vue, Svelte, etc.)
- Integrated into existing applications

## Troubleshooting

**Backend not connecting?**
- Check backend is running: `curl http://localhost:5282/stream`
- Verify port 5282 is not in use
- Check firewall settings

**Frontend not loading?**
- Run `npm install` in StreamingReactClient
- Check port 5173 is available
- Try `npm run dev -- --port 3000` for different port

**SSE events not showing?**
- Open browser DevTools → Network tab
- Look for `/stream` request
- Should show `text/event-stream` content type

## Learn More

- [Backend README](../StreamingWebApiSample/README.md)
- [Frontend README](./README.md)
- [SSE Client Documentation](./src/lib/README.md)
- [OpenRouter.NET SDK](../../README.md)

## Example Prompts

Try these to see different features:

**Math:**
- "What's 50 times 20?"
- "Calculate 1000 / 25"
- "Add 123, 456, and 789"

**Code:**
- "Create a React button component"
- "Write a Python function to sort a list"
- "Make a TypeScript interface for a user"

**Mixed:**
- "Create a calculator component and tell me what 15 + 30 is"
- "Write a sorting algorithm and calculate 100 * 50"

Have fun! 🚀

