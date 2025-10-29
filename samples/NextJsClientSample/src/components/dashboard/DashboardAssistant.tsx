'use client';

import { useState, useRef, useEffect } from 'react';
import { useOpenRouterChat, useOpenRouterModels, ToolClientEvent, ArtifactBlock } from '@openrouter-dotnet/react';
import { MessageList } from '../chat/MessageList';
import { ChatInput } from '../chat/ChatInput';
import type { DashboardWidgetData } from './DashboardWidget';

interface DashboardAssistantProps {
  selectedModel: string;
  onModelChange: (modelId: string) => void;
  onAddWidget: (widget: Omit<DashboardWidgetData, 'createdAt'>) => void;
  onRemoveWidget: (id: string) => void;
  onUpdateWidget: (id: string, updates: Partial<DashboardWidgetData>) => void;
}

export function DashboardAssistant({
  selectedModel,
  onModelChange,
  onAddWidget,
  onRemoveWidget,
  onUpdateWidget
}: DashboardAssistantProps) {
  const [input, setInput] = useState('');
  // Track artifacts by ID in a Map for easy lookup
  const artifactsMapRef = useRef<Map<string, ArtifactBlock>>(new Map());

  const { state, actions } = useOpenRouterChat({
    endpoints: {
      stream: '/api/dashboard/stream',
      clearConversation: '/api/dashboard/conversation',
    },
    defaultModel: selectedModel,
    config: { 
      debug: true,
      temperature: 0.3
    },
    onClientTool: (event: ToolClientEvent) => {
      try {
        console.log('🔧 [DASHBOARD TOOL] Tool called:', event.toolName);
        console.log('🔧 [DASHBOARD TOOL] Arguments:', event.arguments);
        console.log('📦 [DASHBOARD TOOL] Available artifacts:', Array.from(artifactsMapRef.current.keys()));

        const args = JSON.parse(event.arguments);

        if (event.toolName === 'add_widget_to_dashboard') {
          const { artifactId, widgetId, title, size = 'medium' } = args;

          console.log('📊 [DASHBOARD TOOL] Adding widget:', { artifactId, widgetId, title, size });

          // Look up the artifact by ID
          const artifact = artifactsMapRef.current.get(artifactId);

          console.log('📦 [DASHBOARD TOOL] Found artifact:', artifact);

          if (!artifact) {
            console.error('❌ [DASHBOARD TOOL] Artifact not found with ID:', artifactId);
            return {
              success: false,
              message: `Artifact with ID "${artifactId}" not found. Available IDs: ${Array.from(artifactsMapRef.current.keys()).join(', ')}`
            };
          }

          if (artifact.isStreaming) {
            console.warn('⚠️ [DASHBOARD TOOL] Artifact is still streaming');
            return {
              success: false,
              message: `Artifact "${artifactId}" is still being generated. Please wait for it to complete.`
            };
          }

          console.log('✅ [DASHBOARD TOOL] Using artifact code, length:', artifact.content.length);

          onAddWidget({
            id: widgetId,
            title,
            code: artifact.content,
            size
          });

          console.log('✅ [DASHBOARD TOOL] Widget added successfully!');

          return {
            success: true,
            message: `Widget "${title}" added to dashboard using artifact "${artifactId}"`
          };
        }

        if (event.toolName === 'update_widget') {
          const { widgetId, title } = args;
          const updates: Partial<DashboardWidgetData> = {};
          if (title) updates.title = title;
          onUpdateWidget(widgetId, updates);
          return { 
            success: true, 
            message: `Widget updated` 
          };
        }

        if (event.toolName === 'remove_widget') {
          const { widgetId } = args;
          onRemoveWidget(widgetId);
          return { 
            success: true, 
            message: `Widget removed from dashboard` 
          };
        }

        return { success: false, message: 'Unknown tool' };
      } catch (err) {
        console.error('Client tool error:', err);
        return { success: false, message: String(err) };
      }
    },
  } as any);

  const { models } = useOpenRouterModels('/api/models');

  // Track artifacts from messages and update the Map
  useEffect(() => {
    console.log('🔄 [ARTIFACT TRACKER] Messages updated:', state.messages.length);
    console.log('🔄 [ARTIFACT TRACKER] Current message:', state.currentMessage);

    // Build a new Map of all tsx.reactrunner artifacts
    const newArtifactsMap = new Map<string, ArtifactBlock>();

    state.messages.forEach(msg => {
      if (msg.role === 'assistant') {
        const artifacts = msg.blocks.filter((block): block is ArtifactBlock =>
          block.type === 'artifact' &&
          block.language === 'tsx.reactrunner'
        );
        artifacts.forEach(artifact => {
          newArtifactsMap.set(artifact.artifactId, artifact);
        });
      }
    });

    // Also check current message (may include streaming artifacts)
    if (state.currentMessage) {
      const currentArtifacts = state.currentMessage.blocks.filter((block): block is ArtifactBlock =>
        block.type === 'artifact' &&
        block.language === 'tsx.reactrunner'
      );
      currentArtifacts.forEach(artifact => {
        newArtifactsMap.set(artifact.artifactId, artifact);
      });
    }

    console.log('📦 [ARTIFACT TRACKER] Found artifacts:', Array.from(newArtifactsMap.entries()).map(([id, a]) => ({
      id,
      title: a.title,
      isStreaming: a.isStreaming,
      contentLength: a.content.length
    })));

    artifactsMapRef.current = newArtifactsMap;
  }, [state.messages, state.currentMessage]);

  const handleSend = async () => {
    if (!input.trim() || state.isStreaming) return;
    await (actions.sendMessage as any)(input, { model: selectedModel });
    setInput('');
  };

  return (
    <div className="flex flex-col h-full">
      <div className="flex-shrink-0 px-6 py-4 border-b border-slate-200">
        <h3 className="text-sm font-semibold text-slate-900">Dashboard Builder AI</h3>
        <p className="text-xs text-slate-500 mt-0.5">Ask me to create data visualizations</p>
      </div>

      <div className="flex-1 overflow-hidden min-h-0">
        <MessageList messages={state.messages} error={state.error} lastMessageMinHeight="40vh" />
      </div>

      <div className="flex-shrink-0 p-4 border-t border-slate-200">
        <ChatInput
          value={input}
          onChange={setInput}
          onSend={handleSend}
          isStreaming={state.isStreaming}
          models={models}
          selectedModel={selectedModel}
          onModelChange={onModelChange}
          variant="inline"
        />
      </div>
    </div>
  );
}
