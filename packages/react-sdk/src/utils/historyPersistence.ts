/**
 * Helper utilities for persisting conversation history to localStorage
 */

import type { ChatMessage } from '../types';

export interface HistoryPersistenceOptions {
  /** Storage key prefix (default: 'openrouter_chat_') */
  keyPrefix?: string;
  /** Maximum messages to store per conversation (default: 100) */
  maxMessages?: number;
  /** Auto-save on message updates (default: true) */
  autoSave?: boolean;
}

/**
 * Save conversation history to localStorage
 *
 * @param conversationId - Unique conversation identifier
 * @param messages - Messages to save
 * @param options - Persistence options
 */
export function saveHistory(
  conversationId: string,
  messages: ChatMessage[],
  options: HistoryPersistenceOptions = {}
): void {
  const { keyPrefix = 'openrouter_chat_', maxMessages = 100 } = options;

  try {
    // Limit message count to prevent localStorage quota issues
    const messagesToSave = messages.slice(-maxMessages);

    const key = `${keyPrefix}${conversationId}`;
    localStorage.setItem(key, JSON.stringify(messagesToSave));
  } catch (error) {
    console.error('Failed to save conversation history:', error);
  }
}

/**
 * Load conversation history from localStorage
 *
 * @param conversationId - Unique conversation identifier
 * @param options - Persistence options
 * @returns Loaded messages or empty array if not found
 */
export function loadHistory(
  conversationId: string,
  options: HistoryPersistenceOptions = {}
): ChatMessage[] {
  const { keyPrefix = 'openrouter_chat_' } = options;

  try {
    const key = `${keyPrefix}${conversationId}`;
    const stored = localStorage.getItem(key);

    if (!stored) {
      return [];
    }

    const parsed = JSON.parse(stored);

    // Revive Date objects
    return parsed.map((msg: any) => ({
      ...msg,
      timestamp: new Date(msg.timestamp),
      blocks: msg.blocks.map((block: any) => ({
        ...block,
        timestamp: new Date(block.timestamp),
      })),
    }));
  } catch (error) {
    console.error('Failed to load conversation history:', error);
    return [];
  }
}

/**
 * Clear conversation history from localStorage
 *
 * @param conversationId - Unique conversation identifier
 * @param options - Persistence options
 */
export function clearHistory(
  conversationId: string,
  options: HistoryPersistenceOptions = {}
): void {
  const { keyPrefix = 'openrouter_chat_' } = options;

  try {
    const key = `${keyPrefix}${conversationId}`;
    localStorage.removeItem(key);
  } catch (error) {
    console.error('Failed to clear conversation history:', error);
  }
}

/**
 * List all conversation IDs stored in localStorage
 *
 * @param options - Persistence options
 * @returns Array of conversation IDs
 */
export function listConversations(
  options: HistoryPersistenceOptions = {}
): string[] {
  const { keyPrefix = 'openrouter_chat_' } = options;

  try {
    const conversations: string[] = [];

    for (let i = 0; i < localStorage.length; i++) {
      const key = localStorage.key(i);
      if (key?.startsWith(keyPrefix)) {
        conversations.push(key.slice(keyPrefix.length));
      }
    }

    return conversations;
  } catch (error) {
    console.error('Failed to list conversations:', error);
    return [];
  }
}

/**
 * Get storage size estimate for a conversation
 *
 * @param conversationId - Unique conversation identifier
 * @param options - Persistence options
 * @returns Size in bytes, or 0 if not found
 */
export function getStorageSize(
  conversationId: string,
  options: HistoryPersistenceOptions = {}
): number {
  const { keyPrefix = 'openrouter_chat_' } = options;

  try {
    const key = `${keyPrefix}${conversationId}`;
    const stored = localStorage.getItem(key);
    return stored ? new Blob([stored]).size : 0;
  } catch (error) {
    return 0;
  }
}
