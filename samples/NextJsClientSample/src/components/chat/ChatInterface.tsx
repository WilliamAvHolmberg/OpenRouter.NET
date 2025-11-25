/**
 * Main Chat Interface
 * Orchestrates the chat experience with all sub-components
 */

'use client';

import { useState, useEffect } from 'react';
import { useOpenRouterChat, useOpenRouterModels } from '@openrouter-dotnet/react';
import { MessageList } from './MessageList';
import { ChatInput } from './ChatInput';
import { DebugPanel } from './DebugPanel';
import { Hero } from './Hero';
import { DrawerProvider } from './DrawerContext';
import { DrawerHost } from './DrawerHost';

const STORAGE_KEY = 'openrouter_selected_model';
const DEFAULT_MODEL = 'anthropic/claude-3.5-sonnet';

export function ChatInterface() {
  const [input, setInput] = useState('');
  const [showDebugPanel, setShowDebugPanel] = useState(false);
  const [selectedModel, setSelectedModel] = useState<string>(() => {
    // Load from localStorage on mount
    if (typeof window !== 'undefined') {
      return localStorage.getItem(STORAGE_KEY) || DEFAULT_MODEL;
    }
    return DEFAULT_MODEL;
  });

  const { state, actions, debug } = useOpenRouterChat({
    endpoints: {
      stream: '/api/stream',
      clearConversation: '/api/conversation',
    },
    defaultModel: selectedModel,
    config: {
      debug: true,
    },
    onCompleted: (event) => {
      console.log('🎉 [CALLBACK] onCompleted called:', event);
      console.log('  - finishReason:', event.finishReason);
      console.log('  - model:', event.model);
      console.log('  - id:', event.id);
    },
    onError: (event) => {
      console.error('❌ [CALLBACK] onError called:', event);
      console.error('  - message:', event.message);
      console.error('  - details:', event.details);
    },
    onArtifactCompleted: (event) => {
      console.log('📦 [CALLBACK] onArtifactCompleted called:', event);
      console.log('  - artifactId:', event.artifactId);
      console.log('  - title:', event.title);
      console.log('  - artifactType:', event.artifactType);
      console.log('  - language:', event.language);
      console.log('  - content length:', event.content.length);
    },
    onToolCompleted: (event) => {
      console.log('🔧 [CALLBACK] onToolCompleted called:', event);
      console.log('  - toolName:', event.toolName);
      console.log('  - toolId:', event.toolId);
      console.log('  - result:', event.result);
      console.log('  - executionTimeMs:', event.executionTimeMs);
    },
    onToolError: (event) => {
      console.error('⚠️ [CALLBACK] onToolError called:', event);
      console.error('  - toolName:', event.toolName);
      console.error('  - toolId:', event.toolId);
      console.error('  - error:', event.error);
    },
  });

  const { models, loading: modelsLoading } = useOpenRouterModels('/api/models');

  // Save selected model to localStorage
  useEffect(() => {
    if (typeof window !== 'undefined') {
      localStorage.setItem(STORAGE_KEY, selectedModel);
    }
  }, [selectedModel]);

  const handleSend = async () => {
    if (!input.trim() || state.isStreaming) return;

    const enabledArtifacts = [
      {
        id: 'reactRunner',
        enabled: true,
        type: 'code',
        preferredTitle: 'Widget.tsx',
        language: 'tsx.reactrunner',
        instruction:
          'Return exactly ONE self-contained default-exported React component. Use Tailwind classes only. No imports beyond React. No side effects or network. Emit as <artifact> with language="tsx.reactrunner" and title="Widget.tsx".',
        attributes: {},
      },
    ];

    await (actions.sendMessage as any)(input, { model: selectedModel, enabledArtifacts });
    setInput('');
  };

  const isHero = state.messages.length === 0 && !state.isStreaming;

  return (
    <div className="flex h-screen bg-gradient-to-br from-indigo-50 via-slate-50 to-white">
      {isHero ? (
        <div className="flex-1 flex flex-col max-w-6xl mx-auto w-full">
          <Hero
            input={input}
            onChange={setInput}
            onSend={handleSend}
            isStreaming={state.isStreaming}
            models={models}
            selectedModel={selectedModel}
            onModelChange={setSelectedModel}
            onUsePrompt={(text) => setInput(text)}
          />
        </div>
      ) : (
        <DrawerProvider>
          <div className="flex h-full w-full">
            {/* Main Chat Column */}
            <div className="flex-1 flex flex-col max-w-5xl mx-auto w-full animate-in fade-in duration-300">
              <div className="flex-1 overflow-hidden">
                <MessageList messages={state.messages} error={state.error} />
              </div>

              {/* Fixed Bottom Input */}
              <ChatInput
                value={input}
                onChange={setInput}
                onSend={handleSend}
                isStreaming={state.isStreaming}
                models={models}
                selectedModel={selectedModel}
                onModelChange={setSelectedModel}
                variant="inline"
              />
            </div>

            {/* Right Drawer Column host; listens to context */}
            <DrawerHost />
          </div>

          {/* Debug Panel */}
          {debug.enabled && (
            <DebugPanel
              debug={debug}
              messages={state.messages}
              showPanel={showDebugPanel}
              onTogglePanel={() => setShowDebugPanel(!showDebugPanel)}
            />
          )}
        </DrawerProvider>
      )}

      {/* Minimal debug toggle (bottom-left) */}
      <button
        onClick={() => debug.toggle()}
        className="fixed left-4 bottom-20 z-50 h-9 w-9 rounded-full border border-slate-200 bg-white/90 text-slate-600 hover:text-slate-900 hover:bg-white shadow-sm flex items-center justify-center"
        title={debug.enabled ? 'Disable debug' : 'Enable debug'}
        aria-label="Toggle debug"
      >
        🐛
      </button>
    </div>
  );
}
