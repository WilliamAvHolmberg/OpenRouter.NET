/**
 * Message converter - converts React ChatMessage format to backend Message format
 */

import type { ChatMessage, ToolCallBlock } from '../types';

/**
 * Backend Message format (matches OpenRouter.NET Message.cs)
 */
export interface BackendMessage {
  role: string;
  content?: string;
  name?: string;
  tool_call_id?: string;
  tool_calls?: BackendToolCall[];
}

export interface BackendToolCall {
  id: string;
  type: string;
  function: {
    name: string;
    arguments: string;
  };
}

/**
 * Converts ChatMessage[] (React format) to BackendMessage[] (API format)
 *
 * Flattens the block-based UI model into the OpenRouter API format:
 * - Text blocks become message content
 * - Tool call blocks become tool_calls array
 * - Artifacts are included as text (for now)
 */
export function convertToBackendMessages(chatMessages: ChatMessage[]): BackendMessage[] {
  return chatMessages.map((msg) => {
    const backendMsg: BackendMessage = {
      role: msg.role,
    };

    // Collect text content from all text blocks
    const textBlocks = msg.blocks.filter((b) => b.type === 'text');
    if (textBlocks.length > 0) {
      backendMsg.content = textBlocks
        .map((b) => (b.type === 'text' ? b.content : ''))
        .join('\n\n');
    }

    // Collect tool calls (only for assistant messages with tool blocks)
    const toolBlocks = msg.blocks.filter(
      (b) => b.type === 'tool_call'
    ) as ToolCallBlock[];

    if (toolBlocks.length > 0 && msg.role === 'assistant') {
      backendMsg.tool_calls = toolBlocks
        .filter((tb) => tb.status === 'completed' || tb.status === 'executing')
        .map((tb) => ({
          id: tb.toolId,
          type: 'function',
          function: {
            name: tb.toolName,
            arguments: tb.arguments,
          },
        }));
    }

    // Include artifacts as text (for conversation context)
    const artifactBlocks = msg.blocks.filter((b) => b.type === 'artifact');
    if (artifactBlocks.length > 0) {
      const artifactText = artifactBlocks
        .map((b) => {
          if (b.type === 'artifact') {
            return `[Artifact: ${b.title}]\n${b.content}`;
          }
          return '';
        })
        .join('\n\n');

      if (backendMsg.content) {
        backendMsg.content += '\n\n' + artifactText;
      } else {
        backendMsg.content = artifactText;
      }
    }

    return backendMsg;
  });
}
