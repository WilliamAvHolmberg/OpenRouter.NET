/**
 * OpenRouter SSE Client
 * Core streaming client - no React dependencies
 */

import type {
  SseEvent,
  StreamOptions,
  ChatRequest,
  Model,
  ClientConfig,
} from './types';

export class OpenRouterClient {
  public baseUrl: string;
  private config: ClientConfig;

  constructor(baseUrl: string, config: ClientConfig = {}) {
    this.baseUrl = baseUrl;
    this.config = { debug: false, ...config };
  }

  private log(level: 'info' | 'warn' | 'error', message: string, data?: any) {
    if (!this.config.debug) return;

    if (this.config.logger) {
      this.config.logger(level, message, data);
    } else {
      const prefix = `[OpenRouter ${level.toUpperCase()}]`;
      if (data !== undefined) {
        console[level](prefix, message, data);
      } else {
        console[level](prefix, message);
      }
    }
  }

  /**
   * Get available models
   */
  async getModels(): Promise<Model[]> {
    const response = await fetch(`${this.baseUrl}/models`);

    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    }

    return response.json();
  }

  /**
   * Stream a chat completion with SSE
   */
  async stream(request: ChatRequest, options: StreamOptions = {}): Promise<void> {
    this.log('info', 'Starting stream', request);

    const response = await fetch(`${this.baseUrl}/stream`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      const error = `HTTP ${response.status}: ${response.statusText}`;
      this.log('error', 'Stream request failed', error);
      throw new Error(error);
    }

    if (!response.body) {
      throw new Error('No response body');
    }

    const reader = response.body.getReader();
    const decoder = new TextDecoder();
    let buffer = '';

    try {
      while (true) {
        const { done, value } = await reader.read();
        if (done) {
          this.log('info', 'Stream ended');
          break;
        }

        const chunk = decoder.decode(value, { stream: true });
        buffer += chunk;

        const lines = buffer.split('\n');
        buffer = lines.pop() || '';

        for (const line of lines) {
          // Debug: capture every raw line
          if (this.config.onRawLine) {
            this.config.onRawLine(line);
          }

          if (!line.trim() || !line.startsWith('data: ')) {
            continue;
          }

          const jsonStr = line.substring(6);

          if (jsonStr === '[DONE]') {
            this.log('info', 'Received [DONE] marker');
            continue;
          }

          try {
            const event = JSON.parse(jsonStr) as SseEvent;

            // Debug: capture parsed event
            if (this.config.onParsedEvent) {
              this.config.onParsedEvent(event);
            }

            // Call generic event handler
            options.onEvent?.(event);

            // Call specific event handlers
            switch (event.type) {
              case 'text':
                options.onText?.(event.textDelta);
                break;
              case 'tool_executing':
                options.onToolExecuting?.(event);
                break;
              case 'tool_completed':
                options.onToolCompleted?.(event);
                break;
              case 'tool_error':
                options.onToolError?.(event);
                break;
              case 'artifact_started':
                options.onArtifactStarted?.(event);
                break;
              case 'artifact_content':
                options.onArtifactContent?.(event);
                break;
              case 'artifact_completed':
                options.onArtifactCompleted?.(event);
                break;
              case 'completion':
                // Only fire onComplete for "stop", not intermediate "tool_calls"
                if (event.finishReason === 'stop') {
                  options.onComplete?.(event);
                }
                break;
              case 'error':
                this.log('error', 'Stream error event', event);
                options.onError?.(event);
                break;
            }
          } catch (e) {
            this.log('error', 'Failed to parse SSE event', { jsonStr, error: e });
          }
        }
      }
    } finally {
      reader.releaseLock();
    }
  }

  /**
   * Clear a conversation
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
