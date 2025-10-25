/**
 * useStreamingText
 *
 * Simple hook for streaming text only (no artifacts, no tools)
 * Perfect for basic chat or completion UIs
 */

import { useState, useRef, useCallback, useMemo } from 'react';
import { OpenRouterClient } from '../client';
import type { StreamingTextState, ClientConfig, CompletionEvent, ErrorEvent } from '../types';

interface UseStreamingTextOptions {
  baseUrl: string;
  model?: string;
  config?: ClientConfig;
}

export function useStreamingText({
  baseUrl,
  model = 'openai/gpt-4o',
  config,
}: UseStreamingTextOptions) {
  const [text, setText] = useState('');
  const [isStreaming, setIsStreaming] = useState(false);
  const [error, setError] = useState<ErrorEvent | null>(null);
  const [completion, setCompletion] = useState<CompletionEvent | null>(null);

  // Memoize client creation to handle baseUrl/config changes properly
  const client = useMemo(() => {
    return new OpenRouterClient(baseUrl, config);
  }, [baseUrl, config]);

  const clientRef = useRef(client);
  clientRef.current = client;

  const stream = useCallback(
    async (message: string, options?: { model?: string; conversationId?: string }) => {
      setText('');
      setIsStreaming(true);
      setError(null);
      setCompletion(null);

      try {
        await clientRef.current.stream(
          {
            message,
            model: options?.model || model,
            conversationId: options?.conversationId,
          },
          {
            onText: (delta) => {
              setText((prev) => prev + delta);
            },
            onComplete: (event) => {
              setCompletion(event);
              setIsStreaming(false);
            },
            onError: (event) => {
              setError(event);
              setIsStreaming(false);
            },
          }
        );
      } catch (err: any) {
        setError({
          type: 'error',
          message: err.message || 'Unknown error',
          chunkIndex: 0,
          elapsedMs: 0,
        });
        setIsStreaming(false);
      }
    },
    [model]
  );

  const reset = useCallback(() => {
    setText('');
    setError(null);
    setCompletion(null);
  }, []);

  const state: StreamingTextState = {
    text,
    isStreaming,
    error,
    completion,
  };

  return {
    ...state,
    stream,
    reset,
  };
}
