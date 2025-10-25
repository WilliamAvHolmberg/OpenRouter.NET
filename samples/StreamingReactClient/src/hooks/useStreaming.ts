import { useState, useRef } from 'react';
import { OpenRouterSseClient } from '../lib/openrouter-sse-client';
import type { ConversationTurn, ActiveTool, StreamingArtifact } from '../types';

interface UseStreamingProps {
  conversationId: string;
  onUpdateTurn: (turn: ConversationTurn) => void;
  onCompleteTurn: (turn: ConversationTurn) => void;
}

export function useStreaming({ conversationId, onUpdateTurn, onCompleteTurn }: UseStreamingProps) {
  const [isStreaming, setIsStreaming] = useState(false);
  const [currentTurn, setCurrentTurn] = useState<ConversationTurn | null>(null);
  const [activeTool, setActiveTool] = useState<ActiveTool | null>(null);
  const [streamingArtifact, setStreamingArtifact] = useState<StreamingArtifact | null>(null);
  const [statusMessage, setStatusMessage] = useState('');

  const clientRef = useRef(new OpenRouterSseClient('/api'));
  const currentTurnRef = useRef<ConversationTurn | null>(null);

  const sendMessage = async (content: string, model: string) => {
    setIsStreaming(true);
    setActiveTool(null);
    setStreamingArtifact(null);

    // Create initial assistant turn
    const assistantTurn: ConversationTurn = {
      id: `turn-${Date.now()}`,
      timestamp: new Date(),
      role: 'assistant',
      items: [],
    };

    currentTurnRef.current = assistantTurn;
    setCurrentTurn(assistantTurn);

    try {
      await clientRef.current.stream(
        {
          message: content,
          conversationId,
          model,
        },
        {
          onText: (textContent) => {
            if (!currentTurnRef.current) return;

            // Create new items array without mutation
            const items = [...currentTurnRef.current.items];
            const lastItem = items[items.length - 1];

            if (lastItem && lastItem.type === 'text') {
              // Update the last text item with new content
              items[items.length - 1] = {
                ...lastItem,
                content: lastItem.content + textContent,
              };
            } else {
              // Add new text item
              items.push({
                type: 'text',
                content: textContent,
              });
            }

            const updatedTurn = { ...currentTurnRef.current, items };
            currentTurnRef.current = updatedTurn;
            setCurrentTurn(updatedTurn);
            onUpdateTurn(updatedTurn);
            setStatusMessage('');
          },

          onToolExecuting: (event) => {
            console.log('🔧 [TOOL DEBUG] Tool executing:', event);
            setActiveTool({
              name: event.toolName,
              id: event.toolId,
              state: 'executing',
            });
            setStatusMessage(`🔧 Executing tool: ${event.toolName}`);
          },

          onToolCompleted: (event) => {
            console.log('🔧 [TOOL DEBUG] Tool completed:', event);
            console.log('🔧 [TOOL DEBUG] Current turn before adding tool:', currentTurnRef.current);

            if (!currentTurnRef.current) {
              console.error('🔧 [TOOL DEBUG] No current turn ref! Tool will not be added.');
              return;
            }

            // Create new items array and add tool item
            const items = [
              ...currentTurnRef.current.items,
              {
                type: 'tool' as const,
                id: event.toolId,
                name: event.toolName,
                result: event.result,
              }
            ];

            console.log('🔧 [TOOL DEBUG] New items array:', items);

            const updatedTurn = { ...currentTurnRef.current, items };
            currentTurnRef.current = updatedTurn;
            setCurrentTurn(updatedTurn);
            onUpdateTurn(updatedTurn);

            console.log('🔧 [TOOL DEBUG] Updated turn:', updatedTurn);

            setActiveTool({
              name: event.toolName,
              id: event.toolId,
              state: 'completed',
              result: event.result,
            });

            setStatusMessage(`✅ Tool completed: ${event.toolName}`);
            setTimeout(() => setActiveTool(null), 2000);
          },

          onToolError: (event) => {
            console.log('🔧 [TOOL DEBUG] Tool error:', event);
            console.log('🔧 [TOOL DEBUG] Current turn before adding error:', currentTurnRef.current);

            if (!currentTurnRef.current) {
              console.error('🔧 [TOOL DEBUG] No current turn ref! Tool error will not be added.');
              return;
            }

            // Create new items array and add tool error item
            const items = [
              ...currentTurnRef.current.items,
              {
                type: 'tool' as const,
                id: event.toolId,
                name: event.toolName,
                error: event.error,
              }
            ];

            console.log('🔧 [TOOL DEBUG] New items array with error:', items);

            const updatedTurn = { ...currentTurnRef.current, items };
            currentTurnRef.current = updatedTurn;
            setCurrentTurn(updatedTurn);
            onUpdateTurn(updatedTurn);

            console.log('🔧 [TOOL DEBUG] Updated turn with error:', updatedTurn);

            setActiveTool({
              name: event.toolName,
              id: event.toolId,
              state: 'error',
              error: event.error,
            });

            setStatusMessage(`❌ Tool error: ${event.toolName}`);
          },

          onArtifactStarted: (event) => {
            setStreamingArtifact({
              id: event.artifactId,
              title: event.title,
              type: event.artifactType,
              language: event.language,
              content: '',
            });
            setStatusMessage(`📦 Creating artifact: ${event.title}`);
          },

          onArtifactContent: (event) => {
            setStreamingArtifact(prev => prev ? {
              ...prev,
              content: prev.content + event.contentDelta,
            } : null);
          },

          onArtifactCompleted: (event) => {
            if (!currentTurnRef.current) return;

            // Create new items array and add artifact item
            const items = [
              ...currentTurnRef.current.items,
              {
                type: 'artifact' as const,
                id: event.artifactId,
                title: event.title,
                artifactType: event.artifactType,
                language: event.language,
                content: event.content,
              }
            ];

            const updatedTurn = { ...currentTurnRef.current, items };
            currentTurnRef.current = updatedTurn;
            setCurrentTurn(updatedTurn);
            onUpdateTurn(updatedTurn);

            // Clear streaming artifact
            setStreamingArtifact(null);
            setStatusMessage(`✅ Artifact completed: ${event.title}`);
          },

          onComplete: () => {
            // Save the completed turn before clearing it
            const completedTurn = currentTurnRef.current;

            console.log('✅ [COMPLETE DEBUG] Stream completed!');
            console.log('✅ [COMPLETE DEBUG] Completed turn:', completedTurn);
            console.log('✅ [COMPLETE DEBUG] Turn items:', completedTurn?.items);
            console.log('✅ [COMPLETE DEBUG] Number of items:', completedTurn?.items.length);

            setIsStreaming(false);
            setActiveTool(null);
            setStreamingArtifact(null);
            setStatusMessage('');
            setCurrentTurn(null);
            currentTurnRef.current = null;

            // Pass the completed turn to the callback
            if (completedTurn) {
              console.log('✅ [COMPLETE DEBUG] Calling onCompleteTurn with:', completedTurn);
              onCompleteTurn(completedTurn);
            } else {
              console.error('✅ [COMPLETE DEBUG] No completed turn to save!');
            }
          },

          onError: (event) => {
            console.error('Stream error:', event);
            setIsStreaming(false);
            setStreamingArtifact(null);
            setStatusMessage(`❌ Error: ${event.message}`);
            setCurrentTurn(null);
            currentTurnRef.current = null;
          },
        }
      );
    } catch (error) {
      console.error('Failed to stream:', error);
      setIsStreaming(false);
      setStreamingArtifact(null);
      setStatusMessage(`❌ Connection error: ${error}`);
      setCurrentTurn(null);
      currentTurnRef.current = null;
    }
  };

  return {
    isStreaming,
    currentTurn,
    activeTool,
    streamingArtifact,
    statusMessage,
    sendMessage,
  };
}
