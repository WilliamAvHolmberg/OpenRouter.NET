# Generate Object - Quick Start Guide

## What is Generate Object?

Generate Object is a feature that allows you to get **structured, typed data from LLMs** without manual JSON parsing or validation. Define your schema once in TypeScript (using Zod), and get back a fully validated, typed object.

## Quick Example

```typescript
import { z } from 'zod';
import { useGenerateObject } from '@openrouter-dotnet/react';

// Define what you want
const schema = z.object({
  name: z.string(),
  age: z.number(),
  hobbies: z.array(z.string())
});

// Get it from an LLM
const { object } = useGenerateObject({
  schema,
  prompt: "Generate a profile for Sarah, a 28-year-old developer",
  endpoint: '/api/generate-object'
});

// Use it with full type safety
console.log(object.name); // TypeScript knows this is a string!
```

## Installation

### Frontend (React)
```bash
npm install @openrouter-dotnet/react zod zod-to-json-schema
```

### Backend (.NET)
```bash
dotnet add package OpenRouter.NET
```

## 30-Second Setup

### 1. Add Backend Endpoint

```csharp
// In your ASP.NET Core Program.cs
app.MapPost("/api/generate-object", async (GenerateObjectRequest request) =>
{
    var client = new OpenRouterClient(Environment.GetEnvironmentVariable("OPENROUTER_API_KEY"));
    
    var result = await client.GenerateObjectAsync(
        schema: request.Schema,
        prompt: request.Prompt,
        model: request.Model ?? "openai/gpt-4o-mini"
    );
    
    return Results.Ok(new { @object = result.Object, usage = result.Usage });
});

public record GenerateObjectRequest
{
    public JsonElement Schema { get; init; }
    public string Prompt { get; init; } = string.Empty;
    public string? Model { get; init; }
}
```

### 2. Use in React Component

```typescript
import { z } from 'zod';
import { useGenerateObject } from '@openrouter-dotnet/react';

const translationSchema = z.object({
  translations: z.array(z.object({
    language: z.string(),
    text: z.string()
  }))
});

function TranslateButton() {
  const { object, isLoading, generate } = useGenerateObject({
    schema: translationSchema,
    prompt: "Translate 'Hello' to Spanish, French, German",
    endpoint: '/api/generate-object'
  });

  return (
    <div>
      <button onClick={() => generate()} disabled={isLoading}>
        Translate
      </button>
      {object && object.translations.map(t => (
        <div>{t.language}: {t.text}</div>
      ))}
    </div>
  );
}
```

## Why Use Generate Object?

### ❌ Without Generate Object (The Old Way)

```typescript
// Define prompt manually
const prompt = `Generate JSON with this structure:
{
  "name": "string",
  "age": "number"
}`;

// Call LLM
const response = await fetch('/api/chat', { 
  method: 'POST',
  body: JSON.stringify({ prompt })
});

// Parse and hope it works
const text = await response.text();
let parsed;
try {
  parsed = JSON.parse(text);
} catch (e) {
  // Handle parse error...
}

// Validate manually
if (!parsed.name || typeof parsed.name !== 'string') {
  // Handle validation error...
}
if (!parsed.age || typeof parsed.age !== 'number') {
  // Handle validation error...
}

// No type safety
console.log(parsed.name); // TypeScript doesn't know what this is
```

### ✅ With Generate Object (The New Way)

```typescript
const schema = z.object({
  name: z.string(),
  age: z.number()
});

const { object } = useGenerateObject({
  schema,
  prompt: "Generate a person named John who is 30",
  endpoint: '/api/generate-object'
});

// Fully typed, validated, done!
console.log(object.name); // TypeScript knows this is a string
```

## Real-World Use Cases

### 1. Data Extraction
```typescript
const extractSchema = z.object({
  entities: z.array(z.object({
    name: z.string(),
    type: z.enum(['person', 'company', 'location'])
  })),
  sentiment: z.enum(['positive', 'negative', 'neutral']),
  summary: z.string()
});

useGenerateObject({
  schema: extractSchema,
  prompt: `Extract entities and sentiment from: "${userText}"`
});
```

### 2. Form Generation
```typescript
const formSchema = z.object({
  fields: z.array(z.object({
    name: z.string(),
    type: z.enum(['text', 'email', 'number']),
    label: z.string(),
    required: z.boolean()
  }))
});

useGenerateObject({
  schema: formSchema,
  prompt: "Create a contact form with name, email, and message"
});
```

### 3. Content Transformation
```typescript
const blogSchema = z.object({
  title: z.string(),
  excerpt: z.string(),
  sections: z.array(z.object({
    heading: z.string(),
    content: z.string()
  })),
  tags: z.array(z.string())
});

useGenerateObject({
  schema: blogSchema,
  prompt: "Convert these notes into a blog post: ..."
});
```

## Key Features

✅ **Full Type Safety** - TypeScript IntelliSense on generated objects  
✅ **Automatic Validation** - Zod validates LLM output  
✅ **Smart Retries** - Backend retries failed generations automatically  
✅ **No Streaming Complexity** - Simple request/response  
✅ **Flexible Schemas** - Use Zod or raw JSON Schema  
✅ **Schema Size Warnings** - Alerts for overly complex schemas  

## Hook API Reference

```typescript
const {
  object,        // Generated object (null until complete)
  isLoading,     // Is generation in progress?
  error,         // Error if generation failed
  usage,         // Token usage stats
  generate,      // Manually trigger with optional new prompt
  regenerate,    // Retry with same prompt
  reset          // Clear state
} = useGenerateObject({
  schema,        // Zod schema or JSON Schema
  prompt,        // What to generate
  endpoint,      // Your backend endpoint
  model,         // LLM model (optional)
  temperature,   // Generation temperature (optional)
  maxRetries     // Retry attempts (optional, default: 3)
});
```

## Backend API Reference

```csharp
var result = await client.GenerateObjectAsync(
    schema: jsonElement,     // JSON Schema
    prompt: "...",           // Generation prompt
    model: "openai/gpt-4o-mini",
    options: new GenerateObjectOptions
    {
        Temperature = 0.7,
        MaxRetries = 3,
        MaxTokens = 1000,
        SchemaWarningThresholdBytes = 2048
    }
);

// result.Object - JsonElement with generated data
// result.Usage - Token usage statistics
// result.FinishReason - Completion reason
```

## Supported Models

All function-calling capable models work:
- ✅ `openai/gpt-4o-mini` (recommended for speed/cost)
- ✅ `openai/gpt-4o`
- ✅ `anthropic/claude-3.5-haiku`
- ✅ `anthropic/claude-3.5-sonnet`
- ✅ `google/gemini-2.5-flash`

## Retry Logic

The backend automatically retries with exponential backoff:
- **Attempt 1**: Immediate
- **Attempt 2**: Wait 1 second
- **Attempt 3**: Wait 2 seconds

This handles transient LLM issues and validation failures.

## Performance

Typical response times:
- **Simple schema** (2-3 fields): 1-3 seconds
- **Medium schema** (5-10 fields): 2-4 seconds
- **Complex schema** (arrays, nested): 3-6 seconds

Tips for faster generation:
- Use `gpt-4o-mini` or `claude-3.5-haiku`
- Keep schemas focused and concise
- Use lower temperature (0.0-0.5)
- Set appropriate `maxTokens`

## Error Handling

```typescript
const { error } = useGenerateObject({ ... });

if (error) {
  // Error types:
  // - Schema conversion errors
  // - API/network errors
  // - Validation errors
  // - Generation failures
  
  console.error(error.message);
}
```

## Complete Working Example

See the full demo in `samples/StreamingReactClient/src/components/GenerateObjectDemo.tsx`

Run the demo:
```bash
# Terminal 1: Start backend
cd samples/StreamingWebApiSample
dotnet run

# Terminal 2: Start frontend
cd samples/StreamingReactClient
npm install
npm run dev
```

## Documentation

- 📘 [Full Documentation](./packages/dotnet-sdk/src/GenerateObject.md)
- 🧪 [Test Examples](./tests/OpenRouter.NET.Tests/GenerateObjectTests.cs)
- 💻 [Sample Component](./samples/StreamingReactClient/src/components/GenerateObjectDemo.tsx)
- 🌐 [Sample Backend](./samples/StreamingWebApiSample/Program.cs)

## FAQ

**Q: Can I use JSON Schema instead of Zod?**  
A: Yes! Pass any valid JSON Schema object as the `schema` parameter.

**Q: Does this work with streaming?**  
A: No, Generate Object uses simple request/response for reliability. Streaming support may come in v2.

**Q: What if the LLM returns invalid data?**  
A: The backend automatically retries up to 3 times with exponential backoff.

**Q: Can I customize retry behavior?**  
A: Yes, use `GenerateObjectOptions.MaxRetries` in the backend.

**Q: What about schema size limits?**  
A: Schemas over 2KB trigger a warning. Keep schemas focused for best results.

## Contributing

Found a bug or have a suggestion? Open an issue or PR!

## License

MIT
