# Generate Object Feature

Generate structured, validated objects from LLMs with automatic schema conversion and retry logic.

## Overview

The `generateObject` feature provides a seamless way to get structured data from LLMs without manual JSON parsing or validation. Define your schema once in the frontend (using Zod or JSON Schema), and the backend automatically validates the LLM output and retries on failure.

## Key Benefits

✅ **Single Source of Truth** - Define schema once in frontend  
✅ **Full Type Safety** - TypeScript IntelliSense with Zod inference  
✅ **Automatic Validation** - Both frontend and backend validate output  
✅ **Smart Retries** - Backend retries with exponential backoff on validation failures  
✅ **No Streaming Complexity** - Simple request/response pattern  
✅ **Flexible Schemas** - Use Zod schemas or raw JSON Schema  

## Architecture

```
Frontend (Zod Schema) 
    ↓ 
Convert to JSON Schema
    ↓
POST /api/generate-object
    ↓
Backend (OpenRouter.NET)
    ↓
LLM with Function Calling
    ↓
Validate & Retry if needed
    ↓
Return Validated JSON
    ↓
Frontend validates with Zod
    ↓
Fully Typed Object
```

## Usage

### Frontend (React)

```typescript
import { z } from 'zod';
import { useGenerateObject } from '@openrouter-dotnet/react';

// Define your schema with Zod
const translationSchema = z.object({
  translations: z.array(
    z.object({
      language: z.string().describe('Target language name'),
      languageCode: z.string().describe('ISO 639-1 code'),
      translatedText: z.string().describe('Translated text'),
    })
  )
});

function TranslationComponent() {
  const { object, isLoading, error, generate } = useGenerateObject({
    schema: translationSchema,
    prompt: "Translate 'Hello World' to Spanish, French, German",
    endpoint: '/api/generate-object',
    model: 'openai/gpt-4o-mini'
  });

  return (
    <div>
      <button onClick={() => generate()} disabled={isLoading}>
        {isLoading ? 'Generating...' : 'Generate Translations'}
      </button>
      
      {object && (
        <div>
          {/* object.translations is fully typed! */}
          {object.translations.map(t => (
            <div key={t.languageCode}>
              <strong>{t.language}:</strong> {t.translatedText}
            </div>
          ))}
        </div>
      )}
      
      {error && <div>Error: {error.message}</div>}
    </div>
  );
}
```

### Using Raw JSON Schema (Without Zod)

```typescript
const jsonSchema = {
  type: "object",
  properties: {
    name: { type: "string" },
    age: { type: "number" }
  },
  required: ["name", "age"]
};

const { object } = useGenerateObject({
  schema: jsonSchema, // Raw JSON Schema works too
  prompt: "Generate a person named John who is 30",
  endpoint: '/api/generate-object'
});
```

### Backend (.NET)

```csharp
using OpenRouter.NET;
using OpenRouter.NET.Models;

[HttpPost("/api/generate-object")]
public async Task<IActionResult> GenerateObject(
    [FromBody] GenerateObjectApiRequest request)
{
    var client = new OpenRouterClient(_apiKey);
    
    var result = await client.GenerateObjectAsync(
        schema: request.Schema,
        prompt: request.Prompt,
        model: request.Model ?? "openai/gpt-4o-mini",
        options: new GenerateObjectOptions
        {
            Temperature = request.Temperature,
            MaxRetries = 3,
            SchemaWarningThresholdBytes = 2048
        }
    );
    
    return Ok(new
    {
        @object = result.Object,
        usage = result.Usage,
        finishReason = result.FinishReason
    });
}
```

## API Reference

### Frontend Hook: `useGenerateObject`

```typescript
function useGenerateObject<TSchema>(options: UseGenerateObjectOptions<TSchema>)
```

**Options:**
- `schema` - Zod schema or JSON Schema defining output structure
- `prompt` - Description of what to generate
- `endpoint` - Backend API endpoint (e.g., '/api/generate-object')
- `model?` - LLM model to use (default: 'openai/gpt-4o-mini')
- `temperature?` - Generation temperature 0.0-2.0 (default: 0.7)
- `maxTokens?` - Maximum tokens to generate
- `maxRetries?` - Retry attempts on failure (default: 3)

**Returns:**
- `object` - Generated object (fully typed if using Zod)
- `isLoading` - Whether generation is in progress
- `error` - Error if generation failed
- `usage` - Token usage statistics
- `generate(prompt?)` - Manually trigger generation
- `regenerate()` - Retry with same prompt
- `reset()` - Clear state

### Backend Method: `GenerateObjectAsync`

```csharp
Task<GenerateObjectResult> GenerateObjectAsync(
    JsonElement schema,
    string prompt,
    string model,
    GenerateObjectOptions? options = null,
    CancellationToken cancellationToken = default
)
```

**Parameters:**
- `schema` - JSON Schema as JsonElement
- `prompt` - Generation prompt
- `model` - Model identifier (e.g., "openai/gpt-4o-mini")
- `options` - Optional configuration
- `cancellationToken` - Cancellation token

**Options:**
- `Temperature` - Generation temperature
- `MaxTokens` - Token limit
- `MaxRetries` - Retry attempts (default: 3)
- `SchemaWarningThresholdBytes` - Warn if schema exceeds size (default: 2048)

**Returns:**
- `Object` - Generated JSON as JsonElement
- `Usage` - Token usage statistics
- `FinishReason` - Completion reason

## Examples

### Example 1: Person Profile

```typescript
const personSchema = z.object({
  name: z.string(),
  age: z.number(),
  occupation: z.string(),
  hobbies: z.array(z.string())
});

const { object } = useGenerateObject({
  schema: personSchema,
  prompt: "Generate a profile for a software engineer named Sarah who is 28",
  endpoint: '/api/generate-object'
});

// object is typed as:
// {
//   name: string;
//   age: number;
//   occupation: string;
//   hobbies: string[];
// }
```

### Example 2: Recipe Generator

```typescript
const recipeSchema = z.object({
  name: z.string(),
  servings: z.number(),
  ingredients: z.array(
    z.object({
      item: z.string(),
      amount: z.string()
    })
  ),
  instructions: z.array(z.string())
});

const { object } = useGenerateObject({
  schema: recipeSchema,
  prompt: "Generate a recipe for chocolate chip cookies",
  endpoint: '/api/generate-object'
});
```

### Example 3: Data Extraction

```typescript
const extractionSchema = z.object({
  entities: z.array(
    z.object({
      name: z.string(),
      type: z.enum(['person', 'organization', 'location']),
      mentions: z.number()
    })
  ),
  sentiment: z.enum(['positive', 'negative', 'neutral']),
  summary: z.string()
});

const { object } = useGenerateObject({
  schema: extractionSchema,
  prompt: `Extract entities and sentiment from: "${articleText}"`,
  endpoint: '/api/generate-object'
});
```

## How It Works

1. **Frontend**: Zod schema is converted to JSON Schema using `zod-to-json-schema`
2. **Transport**: JSON Schema + prompt sent to backend API
3. **Backend**: OpenRouter SDK converts JSON Schema to function calling format
4. **LLM**: Generates structured output matching the function schema
5. **Validation**: Backend validates output, retries if invalid
6. **Frontend**: Zod validates and parses the returned object
7. **Result**: Fully typed object with IntelliSense support

## Error Handling

### Automatic Retries

The backend automatically retries generation with exponential backoff:
- Attempt 1: Immediate
- Attempt 2: Wait 1 second
- Attempt 3: Wait 2 seconds

```csharp
options: new GenerateObjectOptions
{
    MaxRetries = 3 // Default
}
```

### Error Messages

Frontend hook provides detailed error messages:
- Schema conversion errors
- API/network errors
- Validation errors
- LLM generation failures

```typescript
if (error) {
  console.error(error.message);
  // Examples:
  // "Failed to convert Zod schema to JSON Schema: ..."
  // "Generated object failed Zod validation: ..."
  // "HTTP 400: Bad request"
}
```

## Schema Size Warning

Large schemas (>2KB) trigger a warning log:

```csharp
options: new GenerateObjectOptions
{
    SchemaWarningThresholdBytes = 2048 // Customize threshold
}
```

**Best practices:**
- Keep schemas focused and concise
- Break large schemas into smaller, composable parts
- Use descriptions effectively for better LLM understanding

## Model Compatibility

Tested models:
- ✅ `openai/gpt-4o-mini` (recommended for speed/cost)
- ✅ `openai/gpt-4o`
- ✅ `anthropic/claude-3.5-haiku`
- ✅ `anthropic/claude-3.5-sonnet`
- ✅ `google/gemini-2.5-flash`

All models support function calling, which is used internally for structured generation.

## Performance Considerations

**Typical response times:**
- Simple schema (2-3 fields): 1-3 seconds
- Medium schema (5-10 fields): 2-4 seconds
- Complex schema (arrays, nested): 3-6 seconds

**Optimization tips:**
- Use `gpt-4o-mini` or `claude-3.5-haiku` for speed
- Keep schemas under 2KB
- Use lower temperature (0.0-0.5) for consistency
- Set appropriate `maxTokens` to avoid waste

## Comparison with Alternatives

### vs. Manual JSON Parsing
❌ Manual: Write prompt, parse JSON, handle errors, validate manually  
✅ generateObject: Define schema, automatic parsing & validation

### vs. Hardcoded C# Models
❌ Hardcoded: Define C# class, maintain in backend, deploy for changes  
✅ generateObject: Define once in frontend, no backend changes

### vs. Function Calling Directly
❌ Direct: More boilerplate, manual tool handling, no type inference  
✅ generateObject: Simple hook, automatic retries, full type safety

### vs. JSON Mode
❌ JSON Mode: No schema enforcement, manual validation, more parsing code  
✅ generateObject: Schema-driven generation, built-in validation

## Testing

Run backend tests:
```bash
cd tests/OpenRouter.NET.Tests
dotnet test --filter "GenerateObjectTests"
```

Example test:
```csharp
[Fact]
public async Task GenerateObjectAsync_WithSimpleSchema_ReturnsValidObject()
{
    var schema = JsonSerializer.Deserialize<JsonElement>(@"{
        ""type"": ""object"",
        ""properties"": {
            ""name"": { ""type"": ""string"" },
            ""age"": { ""type"": ""number"" }
        },
        ""required"": [""name"", ""age""]
    }");
    
    var result = await client.GenerateObjectAsync(
        schema: schema,
        prompt: "Generate a person named John who is 30 years old",
        model: "openai/gpt-4o-mini"
    );
    
    Assert.NotNull(result.Object);
    // Validate structure...
}
```

## Try the Demo

See the live demo in the sample app:

```bash
# Start backend
cd samples/StreamingWebApiSample
dotnet run

# Start frontend (in another terminal)
cd samples/StreamingReactClient
npm install
npm run dev
```

Navigate to the "Generate Object" tab to try different examples.

## Troubleshooting

**Q: Schema conversion error?**  
A: Ensure Zod schema is serializable. Avoid circular references or functions.

**Q: Validation failures?**  
A: Check schema descriptions - LLM needs clear guidance. Try simpler schema.

**Q: Slow generation?**  
A: Switch to faster model (gpt-4o-mini), reduce schema complexity, or lower maxTokens.

**Q: Large schema warning?**  
A: Break schema into smaller parts or increase `SchemaWarningThresholdBytes`.

## Further Reading

- [Zod Documentation](https://zod.dev)
- [JSON Schema Specification](https://json-schema.org)
- [OpenRouter Models](https://openrouter.ai/models)
- [Function Calling Guide](https://platform.openai.com/docs/guides/function-calling)

## License

MIT
