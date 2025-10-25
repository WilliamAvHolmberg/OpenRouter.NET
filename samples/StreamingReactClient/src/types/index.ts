export interface ConversationTurn {
  id: string;
  timestamp: Date;
  role: 'user' | 'assistant';
  items: TurnItem[];
}

export type TurnItem = TextItem | ToolItem | ArtifactItem;

export interface TextItem {
  type: 'text';
  content: string;
}

export interface ToolItem {
  type: 'tool';
  id: string;
  name: string;
  result?: string;
  error?: string;
}

export interface ArtifactItem {
  type: 'artifact';
  id: string;
  title: string;
  artifactType: string;
  language?: string;
  content: string;
}

// Legacy types for backward compatibility during migration
export interface Artifact {
  id: string;
  type: string;
  title: string;
  language?: string;
  content: string;
}

export interface ActiveTool {
  name: string;
  id: string;
  state: 'executing' | 'completed' | 'error';
  result?: string;
  error?: string;
}

export interface StreamingArtifact {
  id: string;
  title: string;
  type: string;
  language?: string;
  content: string;
}
