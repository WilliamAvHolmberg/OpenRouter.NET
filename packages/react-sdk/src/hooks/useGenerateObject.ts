/**
 * useGenerateObject
 * 
 * React hook for generating structured objects from LLMs with Zod schema validation.
 * Supports both Zod schemas (automatically converted to JSON Schema) and raw JSON Schema.
 */

import { useState, useCallback, useRef } from 'react';
import type { z } from 'zod';
import { zodToJsonSchema } from 'zod-to-json-schema';
import type { GenerateObjectRequest, GenerateObjectResponse } from '../types';

interface UseGenerateObjectOptions<TSchema> {
  /** Zod schema or raw JSON Schema defining the output structure */
  schema: TSchema;
  /** Prompt describing what to generate */
  prompt: string;
  /** API endpoint to call (backend /api/generate-object) */
  endpoint: string;
  /** Model to use (default: 'openai/gpt-4o-mini') */
  model?: string;
  /** Temperature for generation (default: 0.7) */
  temperature?: number;
  /** Maximum tokens to generate */
  maxTokens?: number;
  /** Maximum retry attempts on failure (default: 3) */
  maxRetries?: number;
  /** Whether to automatically generate on mount */
  autoGenerate?: boolean;
}

interface UseGenerateObjectResult<T> {
  /** Generated object (typed according to Zod schema inference) */
  object: T | null;
  /** Whether generation is in progress */
  isLoading: boolean;
  /** Error if generation failed */
  error: Error | null;
  /** Token usage information from last successful generation */
  usage: GenerateObjectResponse['usage'] | null;
  /** Trigger generation manually */
  generate: (overridePrompt?: string) => Promise<void>;
  /** Regenerate with the same prompt */
  regenerate: () => Promise<void>;
  /** Reset state */
  reset: () => void;
}

/**
 * Check if a value is a Zod schema by checking for common Zod methods
 */
function isZodSchema(value: any): value is z.ZodType {
  return (
    value &&
    typeof value === 'object' &&
    typeof value.parse === 'function' &&
    typeof value.safeParse === 'function'
  );
}

/**
 * Hook for generating structured objects with automatic schema validation
 * 
 * @example
 * ```typescript
 * const { object, isLoading, error, regenerate } = useGenerateObject({
 *   schema: z.object({
 *     name: z.string(),
 *     age: z.number()
 *   }),
 *   prompt: "Generate a person named John who is 30 years old",
 *   endpoint: "/api/generate-object"
 * });
 * ```
 */
export function useGenerateObject<TSchema extends z.ZodType | any>(
  options: UseGenerateObjectOptions<TSchema>
): UseGenerateObjectResult<TSchema extends z.ZodType ? z.infer<TSchema> : any> {
  type InferredType = TSchema extends z.ZodType ? z.infer<TSchema> : any;

  const [object, setObject] = useState<InferredType | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<Error | null>(null);
  const [usage, setUsage] = useState<GenerateObjectResponse['usage'] | null>(null);

  const optionsRef = useRef(options);
  optionsRef.current = options;

  const generate = useCallback(async (overridePrompt?: string) => {
    const {
      schema,
      prompt,
      endpoint,
      model = 'openai/gpt-4o-mini',
      temperature = 0.7,
      maxTokens,
      maxRetries = 3,
    } = optionsRef.current;

    setIsLoading(true);
    setError(null);

    try {
      let jsonSchema: any;

      if (isZodSchema(schema)) {
        try {
          jsonSchema = zodToJsonSchema(schema, { 
            $refStrategy: 'none',
            target: 'openApi3'
          });
        } catch (conversionError: any) {
          throw new Error(`Failed to convert Zod schema to JSON Schema: ${conversionError.message}`);
        }
      } else {
        jsonSchema = schema;
      }

      const requestPayload: GenerateObjectRequest = {
        schema: jsonSchema,
        prompt: overridePrompt || prompt,
        model,
        temperature,
        maxTokens,
        maxRetries,
      };

      const response = await fetch(endpoint, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(requestPayload),
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        throw new Error(
          errorData.error || 
          errorData.detail || 
          `HTTP ${response.status}: ${response.statusText}`
        );
      }

      const result: GenerateObjectResponse = await response.json();

      if (isZodSchema(schema)) {
        try {
          const parsed = schema.parse(result.object);
          setObject(parsed);
        } catch (zodError: any) {
          throw new Error(`Generated object failed Zod validation: ${zodError.message}`);
        }
      } else {
        setObject(result.object);
      }

      setUsage(result.usage || null);
      setError(null);
    } catch (err: any) {
      const error = err instanceof Error ? err : new Error(String(err));
      setError(error);
      setObject(null);
      setUsage(null);
    } finally {
      setIsLoading(false);
    }
  }, []);

  const regenerate = useCallback(() => {
    return generate();
  }, [generate]);

  const reset = useCallback(() => {
    setObject(null);
    setError(null);
    setUsage(null);
    setIsLoading(false);
  }, []);

  return {
    object,
    isLoading,
    error,
    usage,
    generate,
    regenerate,
    reset,
  };
}
