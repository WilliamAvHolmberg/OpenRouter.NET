/**
 * useOpenRouterModels
 *
 * Fetches available models from the API with abort/unmount safety
 */

import { useState, useEffect, useMemo } from 'react';
import { OpenRouterClient } from '../client';
import type { Model } from '../types';

interface UseModelsReturn {
  models: Model[];
  loading: boolean;
  error: Error | null;
}

export function useOpenRouterModels(modelsEndpoint: string): UseModelsReturn {
  const [models, setModels] = useState<Model[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<Error | null>(null);

  // Create a minimal client just for the getModels call
  const client = useMemo(() => {
    return new OpenRouterClient({
      stream: '', // Not used for models fetching
    });
  }, []);

  useEffect(() => {
    let isMounted = true;
    const abortController = new AbortController();

    const fetchModels = async () => {
      try {
        setLoading(true);
        setError(null);

        const data = await client.getModels(modelsEndpoint, abortController.signal);

        // Only update state if component is still mounted and not aborted
        if (isMounted && !abortController.signal.aborted) {
          setModels(data);
        }
      } catch (err) {
        // Only update state if component is still mounted and not aborted
        if (isMounted && !abortController.signal.aborted) {
          // Don't report AbortError as a real error
          if (err instanceof Error && err.name === 'AbortError') {
            return;
          }
          setError(err instanceof Error ? err : new Error('Failed to fetch models'));
        }
      } finally {
        // Only update loading state if component is still mounted and not aborted
        if (isMounted && !abortController.signal.aborted) {
          setLoading(false);
        }
      }
    };

    fetchModels();

    // Cleanup function to abort request and prevent state updates
    return () => {
      isMounted = false;
      abortController.abort();
    };
  }, [client, modelsEndpoint]);

  return { models, loading, error };
}
