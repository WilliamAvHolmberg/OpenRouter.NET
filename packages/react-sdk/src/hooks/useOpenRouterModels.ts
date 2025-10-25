/**
 * useOpenRouterModels
 *
 * Fetches available models from the API
 */

import { useState, useEffect } from 'react';

export interface Model {
  id: string;
  name: string;
  contextLength: number;
  pricing: {
    prompt: string;
    completion: string;
    image: string;
    request: string;
  };
}

interface UseModelsReturn {
  models: Model[];
  loading: boolean;
  error: Error | null;
}

export function useOpenRouterModels(baseUrl: string): UseModelsReturn {
  const [models, setModels] = useState<Model[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<Error | null>(null);

  useEffect(() => {
    const fetchModels = async () => {
      try {
        setLoading(true);
        setError(null);

        const response = await fetch(`${baseUrl}/models`);

        if (!response.ok) {
          throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }

        const data = await response.json();
        setModels(data);
      } catch (err) {
        setError(err instanceof Error ? err : new Error('Failed to fetch models'));
      } finally {
        setLoading(false);
      }
    };

    fetchModels();
  }, [baseUrl]);

  return { models, loading, error };
}
