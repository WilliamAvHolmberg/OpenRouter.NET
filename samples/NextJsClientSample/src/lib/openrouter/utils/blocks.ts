/**
 * Utility functions for working with content blocks
 */

import type {
  ChatMessage,
  ContentBlock,
  TextBlock,
  ArtifactBlock,
  ToolCallBlock,
} from '../types';

/**
 * Extract all text blocks from a message
 */
export function getTextBlocks(message: ChatMessage): TextBlock[] {
  return message.blocks.filter((b): b is TextBlock => b.type === 'text');
}

/**
 * Extract all artifact blocks from a message
 */
export function getArtifactBlocks(message: ChatMessage): ArtifactBlock[] {
  return message.blocks.filter((b): b is ArtifactBlock => b.type === 'artifact');
}

/**
 * Extract all tool call blocks from a message
 */
export function getToolCallBlocks(message: ChatMessage): ToolCallBlock[] {
  return message.blocks.filter((b): b is ToolCallBlock => b.type === 'tool_call');
}

/**
 * Get all text content concatenated (ignoring artifacts and tools)
 */
export function getTextContent(message: ChatMessage): string {
  return getTextBlocks(message)
    .map((b) => b.content)
    .join('');
}

/**
 * Check if message has any artifacts
 */
export function hasArtifacts(message: ChatMessage): boolean {
  return message.blocks.some((b) => b.type === 'artifact');
}

/**
 * Check if message has any tool calls
 */
export function hasToolCalls(message: ChatMessage): boolean {
  return message.blocks.some((b) => b.type === 'tool_call');
}

/**
 * Get streaming artifacts (still being generated)
 */
export function getStreamingArtifacts(message: ChatMessage): ArtifactBlock[] {
  return getArtifactBlocks(message).filter((a) => a.isStreaming);
}

/**
 * Get completed artifacts
 */
export function getCompletedArtifacts(message: ChatMessage): ArtifactBlock[] {
  return getArtifactBlocks(message).filter((a) => !a.isStreaming);
}

/**
 * Get executing tools
 */
export function getExecutingTools(message: ChatMessage): ToolCallBlock[] {
  return getToolCallBlocks(message).filter((t) => t.status === 'executing');
}

/**
 * Get completed tools
 */
export function getCompletedTools(message: ChatMessage): ToolCallBlock[] {
  return getToolCallBlocks(message).filter((t) => t.status === 'completed');
}

/**
 * Get failed tools
 */
export function getFailedTools(message: ChatMessage): ToolCallBlock[] {
  return getToolCallBlocks(message).filter((t) => t.status === 'error');
}

/**
 * Sort blocks by order (they should already be sorted, but just in case)
 */
export function sortBlocks(blocks: ContentBlock[]): ContentBlock[] {
  return [...blocks].sort((a, b) => a.order - b.order);
}
