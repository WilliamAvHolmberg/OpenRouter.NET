/**
 * useOpenRouterChat
 *
 * Main hook for managing chat conversations with streaming,
 * artifacts, and tool calls - all in correct order
 */

import { useState, useRef, useCallback, useMemo } from 'react';
import { OpenRouterClient } from '../client';
import { convertToBackendMessages } from '../utils/messageConverter';
import type {
  ChatMessage,
  TextBlock,
  ArtifactBlock,
  ToolCallBlock,
  ChatState,
  ChatActions,
  UseChatReturn,
  ClientConfig,
  EndpointConfig,
  SseEvent,
  ArtifactStartedEvent,
  ArtifactCompletedEvent,
  ArtifactContentEvent,
  ToolExecutingEvent,
  ToolCompletedEvent,
  ErrorEvent,
  ToolErrorEvent,
  CompletionEvent,
  ToolClientEvent,
} from '../types';

interface UseChatOptions {
  endpoints: EndpointConfig;
  conversationId?: string;
  defaultModel?: string;
  config?: ClientConfig;
  onClientTool?: (event: ToolClientEvent) => void;
  onCompleted?: (event: CompletionEvent) => void;
  onError?: (event: ErrorEvent) => void;
  onArtifactCompleted?: (event: ArtifactCompletedEvent) => void;
  onToolCompleted?: (event: ToolCompletedEvent) => void;
  onToolError?: (event: ToolErrorEvent) => void;
}

export function useOpenRouterChat({
  endpoints,
  conversationId: initialConversationId,
  defaultModel = 'openai/gpt-4o',
  config,
  onClientTool,
  onCompleted,
  onError: onErrorCallback,
  onArtifactCompleted: onArtifactCompletedCallback,
  onToolCompleted: onToolCompletedCallback,
  onToolError: onToolErrorCallback,
}: UseChatOptions): UseChatReturn {
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [isStreaming, setIsStreaming] = useState(false);
  const [error, setError] = useState<any>(null);
  const [debugMode, setDebugMode] = useState(() => {
    // Check for DEBUG_AI environment variable
    if (typeof window !== 'undefined') {
      return localStorage.getItem('DEBUG_AI') === 'true';
    }
    return false;
  });
  const [debugData, setDebugData] = useState<{
    rawLines: string[];
    parsedEvents: any[];
  }>({ rawLines: [], parsedEvents: [] });

  const conversationIdRef = useRef(initialConversationId || `conv_${Date.now()}`);
  const orderCounterRef = useRef(0);
  const lastUserMessageRef = useRef<string>('');

  // Memoize client creation to prevent unnecessary recreations
  const client = useMemo(() => {
    return new OpenRouterClient(endpoints, {
      ...config,
      onRawLine: (line: string) => {
        setDebugData((prev) => ({
          ...prev,
          rawLines: [...prev.rawLines, line],
        }));
      },
      onParsedEvent: (event: SseEvent) => {
        setDebugData((prev) => ({
          ...prev,
          parsedEvents: [...prev.parsedEvents, event],
        }));
      },
    });
  }, [endpoints, config]);

  const clientRef = useRef(client);
  clientRef.current = client;

  // Log debug data to console when enabled
  if (debugMode) {
    console.log('🔍 DEBUG MODE ENABLED');
    console.log('📨 Messages:', messages);
    console.log('📝 Raw Lines:', debugData.rawLines);
    console.log('🎯 Parsed Events:', debugData.parsedEvents);
  }

  /**
   * Toggle debug mode
   */
  const toggleDebugMode = useCallback(() => {
    setDebugMode((prev) => {
      const newValue = !prev;
      if (typeof window !== 'undefined') {
        localStorage.setItem('DEBUG_AI', String(newValue));
      }
      return newValue;
    });
  }, []);

  /**
   * Clear debug data
   */
  const clearDebugData = useCallback(() => {
    setDebugData({ rawLines: [], parsedEvents: [] });
  }, []);

  /**
   * Create a new message with empty blocks
   */
  const createMessage = (role: 'user' | 'assistant' | 'system', content?: string): ChatMessage => {
    const message: ChatMessage = {
      id: `msg_${Date.now()}_${Math.random()}`,
      role,
      blocks: [],
      timestamp: new Date(),
      isStreaming: role === 'assistant',
    };

    if (content) {
      message.blocks.push({
        id: `block_${Date.now()}_${Math.random()}`,
        type: 'text',
        order: 0,
        timestamp: new Date(),
        content,
      });
    }

    return message;
  };

  /**
   * Append or update a block in the current streaming message
   */
  const updateStreamingMessage = useCallback((updater: (message: ChatMessage) => ChatMessage) => {
    setMessages((prev) => {
      const lastMessage = prev[prev.length - 1];
      if (!lastMessage || !lastMessage.isStreaming) {
        return prev;
      }

      const updated = updater(lastMessage);
      return [...prev.slice(0, -1), updated];
    });
  }, []);

  /**
   * Send a message and start streaming
   */
  const sendMessage = useCallback(
    async (
      message: string,
      options?: { model?: string; history?: ChatMessage[]; [key: string]: any }
    ) => {
      // Clear debug data for new message
      if (debugMode) {
        setDebugData({ rawLines: [], parsedEvents: [] });
      }

      // Add user message
      const userMessage = createMessage('user', message);
      setMessages((prev) => [...prev, userMessage]);
      setIsStreaming(true);
      setError(null);
      lastUserMessageRef.current = message;

      // Reset order counter for new assistant message
      orderCounterRef.current = 0;

      // Create empty assistant message that will be populated as we stream
      const assistantMessage = createMessage('assistant');
      setMessages((prev) => [...prev, assistantMessage]);

      try {
        if (!clientRef.current) {
          throw new Error('Client not initialized');
        }

        // Build request payload
        const { history, model, ...otherOptions } = options || {};
        const requestPayload: any = {
          ...otherOptions,
          message,
          model: model || defaultModel,
          conversationId: conversationIdRef.current,
        };

        // Handle conversation history - user must provide explicit history if needed
        if (history && Array.isArray(history)) {
          requestPayload.messages = convertToBackendMessages(history);
        }

        await clientRef.current.stream(
          requestPayload,
          {
            onToolClient: (event: ToolClientEvent) => {
              const toolBlock: ToolCallBlock = {
                id: `block_${Date.now()}_${Math.random()}`,
                type: 'tool_call',
                order: orderCounterRef.current++,
                timestamp: new Date(),
                toolId: event.toolId,
                toolName: event.toolName,
                arguments: event.arguments,
                status: 'executing',
              };

              updateStreamingMessage((msg) => ({
                ...msg,
                blocks: [...msg.blocks, toolBlock],
              }));

              onClientTool?.(event);
            },
            onText: (textDelta: string) => {
              updateStreamingMessage((msg) => {
                // Find the last text block (if any)
                const lastBlock = msg.blocks[msg.blocks.length - 1];
                const isLastBlockText = lastBlock?.type === 'text';

                if (!isLastBlockText) {
                  // Create new text block
                  const newTextBlock: TextBlock = {
                    id: `block_${Date.now()}_${Math.random()}`,
                    type: 'text',
                    order: orderCounterRef.current++,
                    timestamp: new Date(),
                    content: textDelta,
                  };
                  return {
                    ...msg,
                    blocks: [...msg.blocks, newTextBlock],
                  };
                } else {
                  // Append to existing text block
                  const updatedBlock: TextBlock = {
                    ...lastBlock,
                    content: lastBlock.content + textDelta,
                  };
                  return {
                    ...msg,
                    blocks: [...msg.blocks.slice(0, -1), updatedBlock],
                  };
                }
              });
            },

            onArtifactStarted: (event: ArtifactStartedEvent) => {
              // Artifacts interrupt text flow - new text blocks will be created after
              const artifactBlock: ArtifactBlock = {
                id: `block_${Date.now()}_${Math.random()}`,
                type: 'artifact',
                order: orderCounterRef.current++,
                timestamp: new Date(),
                artifactId: event.artifactId,
                artifactType: event.artifactType,
                title: event.title,
                language: event.language,
                content: '',
                isStreaming: true,
              };

              updateStreamingMessage((msg) => ({
                ...msg,
                blocks: [...msg.blocks, artifactBlock],
              }));
            },

            onArtifactContent: (event: ArtifactContentEvent) => {
              updateStreamingMessage((msg) => ({
                ...msg,
                blocks: msg.blocks.map((block) => {
                  if (block.type === 'artifact' && block.artifactId === event.artifactId) {
                    return {
                      ...block,
                      content: block.content + event.contentDelta,
                    };
                  }
                  return block;
                }),
              }));
            },

            onArtifactCompleted: (event: ArtifactCompletedEvent) => {
              updateStreamingMessage((msg) => ({
                ...msg,
                blocks: msg.blocks.map((block) => {
                  if (block.type === 'artifact' && block.artifactId === event.artifactId) {
                    return {
                      ...block,
                      content: event.content,
                      isStreaming: false,
                    };
                  }
                  return block;
                }),
              }));
              
              onArtifactCompletedCallback?.(event);
            },

            onToolExecuting: (event: ToolExecutingEvent) => {
              // Tools also interrupt text flow - new text blocks will be created after
              const toolBlock: ToolCallBlock = {
                id: `block_${Date.now()}_${Math.random()}`,
                type: 'tool_call',
                order: orderCounterRef.current++,
                timestamp: new Date(),
                toolId: event.toolId,
                toolName: event.toolName,
                arguments: event.arguments,
                status: 'executing',
              };

              updateStreamingMessage((msg) => ({
                ...msg,
                blocks: [...msg.blocks, toolBlock],
              }));
            },

            onToolCompleted: (event: ToolCompletedEvent) => {
              updateStreamingMessage((msg) => ({
                ...msg,
                blocks: msg.blocks.map((block) => {
                  if (block.type === 'tool_call' && block.toolId === event.toolId) {
                    return {
                      ...block,
                      result: event.result,
                      executionTimeMs: event.executionTimeMs,
                      status: 'completed' as const,
                    };
                  }
                  return block;
                }),
              }));
              
              onToolCompletedCallback?.(event);
            },

            onToolError: (event: ToolErrorEvent) => {
              updateStreamingMessage((msg) => ({
                ...msg,
                blocks: msg.blocks.map((block) => {
                  if (block.type === 'tool_call' && block.toolId === event.toolId) {
                    return {
                      ...block,
                      error: event.error,
                      status: 'error' as const,
                    };
                  }
                  return block;
                }),
              }));
              
              onToolErrorCallback?.(event);
            },

            onComplete: (event: CompletionEvent) => {
              updateStreamingMessage((msg) => ({
                ...msg,
                isStreaming: false,
                model: event.model,
                completion: {
                  finishReason: event.finishReason,
                  model: event.model,
                  id: event.id,
                },
              }));
              setIsStreaming(false);
              
              onCompleted?.(event);
            },

            onError: (event: ErrorEvent) => {
              setError(event);
              setIsStreaming(false);
              updateStreamingMessage((msg) => ({
                ...msg,
                isStreaming: false,
              }));
              
              onErrorCallback?.(event);
            },
          }
        );
      } catch (err) {
        setError(err);
        setIsStreaming(false);
      }
    },
    [defaultModel, updateStreamingMessage, debugMode]
  );

  /**
   * Clear conversation
   * - If clearConversation endpoint is configured: calls backend (server-side history)
   * - If no endpoint: just clears local state (client-side history)
   */
  const clearConversation = useCallback(async () => {
    try {
      // Only call backend if endpoint is configured (server-side history pattern)
      if (endpoints.clearConversation && clientRef.current) {
        await clientRef.current.clearConversation(conversationIdRef.current);
      }
      
      // Always clear local state (works for both patterns)
      setMessages([]);
      setError(null);
      // Generate new conversation ID
      conversationIdRef.current = `conv_${Date.now()}`;
    } catch (err) {
      console.error('Failed to clear conversation:', err);
    }
  }, [endpoints]);

  /**
   * Set messages directly (useful for loading history from external source)
   */
  const setMessagesAction = useCallback((newMessages: ChatMessage[]) => {
    setMessages(newMessages);
  }, []);

  /**
   * Cancel current stream (not implemented in client yet)
   */
  const cancelStream = useCallback(() => {
    // TODO: Implement abort controller in client
    setIsStreaming(false);
  }, []);

  /**
   * Retry last message
   */
  const retry = useCallback(async () => {
    if (!lastUserMessageRef.current) return;

    // Remove last assistant message if it exists and was streaming/errored
    setMessages((prev) => {
      const last = prev[prev.length - 1];
      if (last?.role === 'assistant') {
        return prev.slice(0, -1);
      }
      return prev;
    });

    await sendMessage(lastUserMessageRef.current);
  }, [sendMessage]);

  const state: ChatState = {
    messages,
    currentMessage: isStreaming ? messages[messages.length - 1] : null,
    isStreaming,
    error,
  };

  const actions: ChatActions = {
    sendMessage,
    clearConversation,
    setMessages: setMessagesAction,
    cancelStream,
    retry,
  };

  return {
    state,
    actions,
    debug: {
      enabled: debugMode,
      data: debugData,
      toggle: toggleDebugMode,
      clear: clearDebugData,
    },
  };
}
