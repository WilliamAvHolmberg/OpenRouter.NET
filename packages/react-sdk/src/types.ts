/**
 * Type definitions for OpenRouter.NET React SDK
 *
 * Uses a content-block model where text, artifacts, and tool calls
 * are interleaved in the order they appear in the stream.
 */

// ============================================================================
// SSE Event Types (from server)
// ============================================================================

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

// ============================================================================
// Content Block Model (for interleaved content)
// ============================================================================

export type ContentBlockType = 'text' | 'artifact' | 'tool_call';

/** Base content block */
export interface BaseContentBlock {
  id: string;
  type: ContentBlockType;
  /** Position in the stream (lower = earlier) */
  order: number;
  timestamp: Date;
}

/** Text content block */
export interface TextBlock extends BaseContentBlock {
  type: 'text';
  content: string;
}

/** Artifact content block */
export interface ArtifactBlock extends BaseContentBlock {
  type: 'artifact';
  artifactId: string;
  artifactType: string;
  title: string;
  language?: string;
  content: string;
  /** Is this artifact still being generated? */
  isStreaming: boolean;
}

/** Tool call content block */
export interface ToolCallBlock extends BaseContentBlock {
  type: 'tool_call';
  toolId: string;
  toolName: string;
  arguments: string;
  result?: string;
  error?: string;
  executionTimeMs?: number;
  status: 'executing' | 'completed' | 'error';
}

/** Union of all content blocks */
export type ContentBlock = TextBlock | ArtifactBlock | ToolCallBlock;

// ============================================================================
// Message Model
// ============================================================================

export interface ChatMessage {
  id: string;
  role: 'user' | 'assistant' | 'system';
  /** Ordered content blocks (text, artifacts, tools interleaved) */
  blocks: ContentBlock[];
  timestamp: Date;
  /** Is this message still being streamed? */
  isStreaming: boolean;
  /** Model used (for assistant messages) */
  model?: string;
  /** Completion metadata (when done) */
  completion?: {
    finishReason?: string;
    model?: string;
    id?: string;
  };
}

// ============================================================================
// Request/Response Types
// ============================================================================

export interface ChatRequest {
  message: string;
  model?: string;
  conversationId?: string;
  /** Optional conversation history (for client-side history management) */
  messages?: any[]; // Backend Message format
  /** Optional bag for arbitrary backend-recognized extras (e.g., enabledArtifacts) */
  customArguments?: Record<string, any>;
}

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

// Artifact enabling config (sent to backend)
export interface EnabledArtifact {
  id?: string;
  enabled?: boolean;
  type?: string; // e.g., "code", "document"
  preferredTitle?: string;
  language?: string; // e.g., 'tsx.reactrunner'
  instruction?: string;
  outputFormat?: string;
  attributes?: Record<string, string>;
}

// ============================================================================
// Client Configuration
// ============================================================================

/** Endpoint configuration for API calls */
export interface EndpointConfig {
  /** Endpoint for streaming chat completions */
  stream: string;
  /** Endpoint for clearing conversation history (optional) */
  clearConversation?: string;
}

export interface ClientConfig {
  /** Enable debug logging */
  debug?: boolean;
  /** Custom logger function */
  logger?: (level: 'info' | 'warn' | 'error', message: string, data?: any) => void;
  /** Callback for raw stream lines (for debugging) */
  onRawLine?: (line: string) => void;
  /** Callback for parsed events (for debugging) */
  onParsedEvent?: (event: SseEvent) => void;
  /** Include credentials (cookies) in fetch requests - set to true for cookie-based auth */
  includeCredentials?: boolean;
}

// ============================================================================
// Stream Options (for low-level client)
// ============================================================================

export interface StreamOptions {
  onEvent?: (event: SseEvent) => void;
  onText?: (content: string) => void;
  onToolExecuting?: (event: ToolExecutingEvent) => void;
  onToolCompleted?: (event: ToolCompletedEvent) => void;
  onToolError?: (event: ToolErrorEvent) => void;
  onToolClient?: (event: ToolClientEvent) => void;
  onArtifactStarted?: (event: ArtifactStartedEvent) => void;
  onArtifactContent?: (event: ArtifactContentEvent) => void;
  onArtifactCompleted?: (event: ArtifactCompletedEvent) => void;
  onComplete?: (event: CompletionEvent) => void;
  onError?: (event: ErrorEvent) => void;
}

// ============================================================================
// Hook Return Types
// ============================================================================

/** State returned by useOpenRouterChat */
export interface ChatState {
  /** All messages in conversation (including currently streaming) */
  messages: ChatMessage[];
  /** Currently streaming message (reference to last message if streaming) */
  currentMessage: ChatMessage | null;
  /** Is currently streaming */
  isStreaming: boolean;
  /** Last error */
  error: ErrorEvent | null;
}

/** Actions returned by useOpenRouterChat */
export interface ChatActions {
  /**
   * Send a message and start streaming response
   *
   * @param message - The message to send
   * @param options.history - Optional conversation history (ChatMessage[]) for client-side history management.
   *   If not provided, the backend is expected to manage conversation history server-side.
   */
  sendMessage: (message: string, options?: { model?: string; enabledArtifacts?: EnabledArtifact[]; customArguments?: Record<string, any>; history?: ChatMessage[] }) => Promise<void>;
  /** Clear conversation history */
  clearConversation: () => Promise<void>;
  /** Set messages directly (useful for loading history from external source like localStorage) */
  setMessages: (messages: ChatMessage[]) => void;
  /** Cancel current stream */
  cancelStream: () => void;
  /** Retry last message */
  retry: () => Promise<void>;
}

/** Debug data and controls */
export interface DebugControls {
  /** Is debug mode enabled */
  enabled: boolean;
  /** Debug data (raw lines and parsed events) */
  data: {
    rawLines: string[];
    parsedEvents: any[];
  };
  /** Toggle debug mode */
  toggle: () => void;
  /** Clear debug data */
  clear: () => void;
}

/** Combined hook return type */
export interface UseChatReturn {
  /** Current state */
  state: ChatState;
  /** Actions */
  actions: ChatActions;
  /** Debug controls */
  debug: DebugControls;
}

// ============================================================================
// Simplified Hook Types
// ============================================================================

/** State for useStreamingText hook (simple text-only) */
export interface StreamingTextState {
  text: string;
  isStreaming: boolean;
  error: ErrorEvent | null;
  completion: CompletionEvent | null;
}

/** State for useArtifacts hook */
export interface ArtifactsState {
  /** All artifacts (completed + streaming) */
  artifacts: ArtifactBlock[];
  /** Currently streaming artifacts */
  streamingArtifacts: ArtifactBlock[];
  /** Completed artifacts */
  completedArtifacts: ArtifactBlock[];
}

/** State for useToolCalls hook */
export interface ToolCallsState {
  /** All tool calls */
  toolCalls: ToolCallBlock[];
  /** Currently executing */
  executingTools: ToolCallBlock[];
  /** Completed tools */
  completedTools: ToolCallBlock[];
}

// ============================================================================
// Generate Object Types
// ============================================================================

/** Request payload for generateObject API */
export interface GenerateObjectRequest {
  /** JSON Schema describing the desired output structure */
  schema: any;
  /** Prompt describing what to generate */
  prompt: string;
  /** Model to use (e.g., 'openai/gpt-4o-mini') */
  model?: string;
  /** Temperature for generation (0.0 - 2.0) */
  temperature?: number;
  /** Maximum tokens to generate */
  maxTokens?: number;
  /** Maximum retry attempts on failure */
  maxRetries?: number;
}

/** Response from generateObject API */
export interface GenerateObjectResponse {
  /** Generated object matching the schema */
  object: any;
  /** Token usage information */
  usage?: {
    promptTokens: number;
    completionTokens: number;
    totalTokens: number;
  };
  /** Reason for completion */
  finishReason?: string;
}
