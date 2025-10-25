/**
 * OpenRouter.NET React SDK
 *
 * @packageDocumentation
 */

// Core client
export { OpenRouterClient } from './client';

// Hooks
export { useOpenRouterChat } from './hooks/useOpenRouterChat';
export { useStreamingText } from './hooks/useStreamingText';
export { useOpenRouterModels } from './hooks/useOpenRouterModels';
export type { Model } from './hooks/useOpenRouterModels';

// Types
export type {
  // Events
  SseEvent,
  SseEventType,
  TextEvent,
  ToolExecutingEvent,
  ToolCompletedEvent,
  ToolErrorEvent,
  ToolClientEvent,
  ArtifactStartedEvent,
  ArtifactContentEvent,
  ArtifactCompletedEvent,
  CompletionEvent,
  ErrorEvent,

  // Content Blocks
  ContentBlock,
  ContentBlockType,
  TextBlock,
  ArtifactBlock,
  ToolCallBlock,

  // Messages
  ChatMessage,

  // Config
  ClientConfig,
  ChatRequest,

  // Hook Types
  ChatState,
  ChatActions,
  UseChatReturn,
  DebugControls,
  StreamingTextState,
} from './types';

// Utilities
export {
  getTextBlocks,
  getArtifactBlocks,
  getToolCallBlocks,
  getTextContent,
  hasArtifacts,
  hasToolCalls,
  getStreamingArtifacts,
  getCompletedArtifacts,
  getExecutingTools,
  getCompletedTools,
  getFailedTools,
  sortBlocks,
} from './utils/blocks';
