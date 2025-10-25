import { useEffect, useState } from 'react';
import type { ConversationTurn } from '../types';

const STORAGE_KEY = 'openrouter-conversation';

export function useConversationPersistence(conversationId: string) {
  const [turns, setTurns] = useState<ConversationTurn[]>([]);

  // Load from localStorage on mount
  useEffect(() => {
    const stored = localStorage.getItem(`${STORAGE_KEY}-${conversationId}`);
    if (stored) {
      try {
        const parsed: ConversationTurn[] = JSON.parse(stored);
        // Convert timestamp strings back to Date objects
        const turnsWithDates = parsed.map(turn => ({
          ...turn,
          timestamp: new Date(turn.timestamp),
        }));
        setTurns(turnsWithDates);
      } catch (error) {
        console.error('Failed to parse stored conversation:', error);
      }
    }
  }, [conversationId]);

  // Save to localStorage whenever turns change
  useEffect(() => {
    if (turns.length > 0) {
      localStorage.setItem(`${STORAGE_KEY}-${conversationId}`, JSON.stringify(turns));
    }
  }, [turns, conversationId]);

  const clearConversation = () => {
    setTurns([]);
    localStorage.removeItem(`${STORAGE_KEY}-${conversationId}`);
  };

  return {
    turns,
    setTurns,
    clearConversation,
  };
}
