/**
 * OpenRouter.NET React SDK
 *
 * @packageDocumentation
 */

// Core client
export { OpenRouterClient } from './client.ts';

// Hooks
export { useOpenRouterChat } from './hooks/useOpenRouterChat.ts';
export { useStreamingText } from './hooks/useStreamingText.ts';
export { useOpenRouterModels } from './hooks/useOpenRouterModels.ts';
export { useGenerateObject } from './hooks/useGenerateObject.ts';

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
  EndpointConfig,
  ClientConfig,
  ChatRequest,
  EnabledArtifact,

  // Hook Types
  ChatState,
  ChatActions,
  UseChatReturn,
  DebugControls,
  StreamingTextState,
  
  // Generate Object Types
  GenerateObjectRequest,
  GenerateObjectResponse,
} from './types.ts';

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

export { convertToBackendMessages } from './utils/messageConverter';
export type { BackendMessage, BackendToolCall } from './utils/messageConverter';

export {
  saveHistory,
  loadHistory,
  clearHistory,
  listConversations,
  getStorageSize,
} from './utils/historyPersistence';
export type { HistoryPersistenceOptions } from './utils/historyPersistence';
