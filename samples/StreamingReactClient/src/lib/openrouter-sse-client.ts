/**
 * OpenRouter SSE Client
 * A lightweight, type-safe client for consuming OpenRouter.NET streaming endpoints
 * 
 * Designed to be extracted into: @openrouter-net/sse-client
 */

import { useState, useRef } from 'react';

export type SseEventType =
  | 'text'
  | 'tool_executing'
  | 'tool_completed'
  | 'tool_error'
  | 'tool_client'
  | 'artifact_started'
  | 'artifact_content'
  | 'artifact_completed'
  | 'completion'
  | 'error';

export interface BaseSseEvent {
  type: SseEventType;
  chunkIndex: number;
  elapsedMs: number;
}

export interface TextEvent extends BaseSseEvent {
  type: 'text';
  textDelta: string;
}

export interface ToolExecutingEvent extends BaseSseEvent {
  type: 'tool_executing';
  toolName: string;
  toolId: string;
  arguments: string;
}

export interface ToolCompletedEvent extends BaseSseEvent {
  type: 'tool_completed';
  toolName: string;
  toolId: string;
  arguments: string;
  result: string;
  executionTimeMs?: number;
}

export interface ToolErrorEvent extends BaseSseEvent {
  type: 'tool_error';
  toolName: string;
  toolId: string;
  error: string;
}

export interface ToolClientEvent extends BaseSseEvent {
  type: 'tool_client';
  toolName: string;
  toolId: string;
  arguments: string;
}

export interface ArtifactStartedEvent extends BaseSseEvent {
  type: 'artifact_started';
  artifactId: string;
  artifactType: string;
  title: string;
  language?: string;
}

export interface ArtifactContentEvent extends BaseSseEvent {
  type: 'artifact_content';
  artifactId: string;
  contentDelta: string;
}

export interface ArtifactCompletedEvent extends BaseSseEvent {
  type: 'artifact_completed';
  artifactId: string;
  artifactType: string;
  title: string;
  language?: string;
  content: string;
}

export interface CompletionEvent extends BaseSseEvent {
  type: 'completion';
  finishReason?: string;
  model?: string;
  id?: string;
}

export interface ErrorEvent extends BaseSseEvent {
  type: 'error';
  message: string;
  details?: string;
}

export type SseEvent =
  | TextEvent
  | ToolExecutingEvent
  | ToolCompletedEvent
  | ToolErrorEvent
  | ToolClientEvent
  | ArtifactStartedEvent
  | ArtifactContentEvent
  | ArtifactCompletedEvent
  | CompletionEvent
  | ErrorEvent;

export interface StreamOptions {
  onEvent?: (event: SseEvent) => void;
  onText?: (content: string) => void;
  onToolExecuting?: (event: ToolExecutingEvent) => void;
  onToolCompleted?: (event: ToolCompletedEvent) => void;
  onToolError?: (event: ToolErrorEvent) => void;
  onArtifactStarted?: (event: ArtifactStartedEvent) => void;
  onArtifactContent?: (event: ArtifactContentEvent) => void;
  onArtifactCompleted?: (event: ArtifactCompletedEvent) => void;
  onComplete?: (event: CompletionEvent) => void;
  onError?: (event: ErrorEvent) => void;
}

export interface ChatRequest {
  message: string;
  model?: string;
  conversationId?: string;
}

export interface Model {
  id: string;
  name: string;
  contextLength: number;
  pricing?: {
    prompt: string;
    completion: string;
  };
}

export class OpenRouterSseClient {
  baseUrl: string;

  constructor(baseUrl: string) {
    this.baseUrl = baseUrl;
  }

  /**
   * Get available models from OpenRouter
   */
  async getModels(): Promise<Model[]> {
    const response = await fetch(`${this.baseUrl}/models`);
    
    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    }
    
    return response.json();
  }

  /**
   * Stream a chat completion with full SSE event handling
   * 
   * @example
   * const client = new OpenRouterSseClient('http://localhost:5282');
   * 
   * await client.stream({
   *   message: 'What is 2 + 2?',
   *   conversationId: 'my-conversation'
   * }, {
   *   onText: (content) => console.log(content),
   *   onToolCompleted: (event) => console.log('Tool result:', event.result),
   *   onArtifactCompleted: (artifact) => console.log('Code:', artifact.content)
   * });
   */
  async stream(request: ChatRequest, options: StreamOptions = {}): Promise<void> {
    console.log('🚀 [SSE Client] Starting stream with request:', request);
    
    const response = await fetch(`${this.baseUrl}/stream`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });

    console.log('📡 [SSE Client] Response status:', response.status);
    console.log('📡 [SSE Client] Response headers:', Object.fromEntries(response.headers.entries()));

    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    }

    if (!response.body) {
      throw new Error('No response body');
    }

    const reader = response.body.getReader();
    const decoder = new TextDecoder();
    let buffer = '';
    let chunkCount = 0;

    try {
      while (true) {
        const { done, value } = await reader.read();
        if (done) {
          console.log('✅ [SSE Client] Stream ended after', chunkCount, 'chunks');
          break;
        }

        chunkCount++;
        const rawChunk = decoder.decode(value, { stream: true });
        console.log(`📦 [SSE Client] Raw chunk #${chunkCount}:`, rawChunk);
        
        buffer += rawChunk;
        const lines = buffer.split('\n');
        buffer = lines.pop() || '';

        console.log(`🔍 [SSE Client] Processing ${lines.length} lines from chunk #${chunkCount}`);

        for (const line of lines) {
          console.log('📝 [SSE Client] Line:', JSON.stringify(line));
          
          if (!line.trim()) {
            console.log('⏭️  [SSE Client] Skipping empty line');
            continue;
          }
          
          if (!line.startsWith('data: ')) {
            console.log('⏭️  [SSE Client] Skipping non-data line');
            continue;
          }

          const jsonStr = line.substring(6);
          console.log('🔍 [SSE Client] JSON string:', jsonStr);
          
          if (jsonStr === '[DONE]') {
            console.log('🏁 [SSE Client] Received [DONE] marker');
            continue;
          }

          try {
            const event = JSON.parse(jsonStr) as SseEvent;
            console.log('✨ [SSE Client] Parsed event:', event);
            
            options.onEvent?.(event);

            switch (event.type) {
              case 'text':
                console.log('💬 [SSE Client] Text event:', event.textDelta);
                options.onText?.(event.textDelta);
                break;
              case 'tool_executing':
                console.log('🔧 [SSE Client] Tool executing:', event.toolName);
                options.onToolExecuting?.(event);
                break;
              case 'tool_completed':
                console.log('✅ [SSE Client] Tool completed:', event.toolName, event.result);
                options.onToolCompleted?.(event);
                break;
              case 'tool_error':
                console.log('❌ [SSE Client] Tool error:', event.toolName, event.error);
                options.onToolError?.(event);
                break;
              case 'artifact_started':
                console.log('📦 [SSE Client] Artifact started:', event.title);
                options.onArtifactStarted?.(event);
                break;
              case 'artifact_content':
                console.log('📄 [SSE Client] Artifact content chunk');
                options.onArtifactContent?.(event);
                break;
              case 'artifact_completed':
                console.log('✅ [SSE Client] Artifact completed:', event.title);
                options.onArtifactCompleted?.(event);
                break;
              case 'completion':
                console.log('🏁 [SSE Client] Completion:', event.finishReason);
                // Only complete the turn on "stop", not "tool_calls"
                // tool_calls is just an intermediate state
                if (event.finishReason === 'stop') {
                  options.onComplete?.(event);
                } else {
                  console.log('⏸️  [SSE Client] Intermediate completion (tool_calls), not ending turn yet');
                }
                break;
              case 'error':
                console.log('❌ [SSE Client] Error event:', event.message);
                options.onError?.(event);
                break;
              default:
                console.warn('⚠️  [SSE Client] Unknown event type:', (event as any).type);
            }
          } catch (e) {
            console.error('❌ [SSE Client] Failed to parse SSE event:', jsonStr, e);
          }
        }
      }
    } finally {
      reader.releaseLock();
      console.log('🔒 [SSE Client] Reader lock released');
    }
  }

  /**
   * Clear a conversation from the server
   */
  async clearConversation(conversationId: string): Promise<void> {
    const response = await fetch(`${this.baseUrl}/conversation/${conversationId}`, {
      method: 'DELETE',
    });

    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    }
  }
}

/**
 * React hook for using the OpenRouter SSE client
 * 
 * @example
 * function Chat() {
 *   const { stream, isStreaming } = useOpenRouterStream('/api');
 *   
 *   const handleSend = async (message: string) => {
 *     await stream({ message }, {
 *       onText: (text) => appendToChat(text),
 *       onToolCompleted: (event) => showToolResult(event)
 *     });
 *   };
 * }
 */
export function useOpenRouterStream(baseUrl: string) {
  const [isStreaming, setIsStreaming] = useState(false);
  const clientRef = useRef(new OpenRouterSseClient(baseUrl));

  const stream = async (request: ChatRequest, options: StreamOptions = {}) => {
    setIsStreaming(true);
    try {
      await clientRef.current.stream(request, {
        ...options,
        onComplete: (event) => {
          options.onComplete?.(event);
          setIsStreaming(false);
        },
        onError: (event) => {
          options.onError?.(event);
          setIsStreaming(false);
        },
      });
    } catch (error) {
      setIsStreaming(false);
      throw error;
    }
  };

  const clearConversation = async (conversationId: string) => {
    await clientRef.current.clearConversation(conversationId);
  };

  return { stream, clearConversation, isStreaming };
}

