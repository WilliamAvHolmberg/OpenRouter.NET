import { useState, useEffect, useRef } from 'react';
import { OpenRouterSseClient } from './lib/openrouter-sse-client';
import { ConversationView } from './components/ConversationView';
import { InputArea } from './components/InputArea';
import { ArtifactsPanel } from './components/ArtifactsPanel';
import { useConversationPersistence } from './hooks/useConversationPersistence';
import { useStreaming } from './hooks/useStreaming';
import type { ConversationTurn } from './types';
import { type Model } from './components/ModelSelector';
import './App.css';

function App() {
  const [conversationId] = useState(() => `conv-${Date.now()}`);
  const [models, setModels] = useState<Model[]>([]);
  const [selectedModel, setSelectedModel] = useState('google/gemini-2.5-flash');
  const [isLoadingModels, setIsLoadingModels] = useState(true);

  const clientRef = useRef(new OpenRouterSseClient('/api'));

  // Use persistence hook
  const { turns, setTurns, clearConversation: clearPersistedConversation } = useConversationPersistence(conversationId);

  // Use streaming hook
  const { isStreaming, currentTurn, activeTool, streamingArtifact, statusMessage, sendMessage } = useStreaming({
    conversationId,
    onUpdateTurn: () => {
      // Don't modify persisted turns while streaming - just let currentTurn display it
      // This prevents conflicts with localStorage persistence
    },
    onCompleteTurn: (completedTurn: ConversationTurn) => {
      console.log('💾 [APP DEBUG] onCompleteTurn called with:', completedTurn);
      console.log('💾 [APP DEBUG] Turn items:', completedTurn.items);
      console.log('💾 [APP DEBUG] Current turns before adding:', turns);

      // Add the completed turn to persisted turns
      setTurns(prev => {
        const newTurns = [...prev, completedTurn];
        console.log('💾 [APP DEBUG] New turns array:', newTurns);
        return newTurns;
      });
    },
  });

  // Load models on mount
  useEffect(() => {
    loadModels();
  }, []);

  const loadModels = async () => {
    try {
      const fetchedModels = await clientRef.current.getModels();
      setModels(fetchedModels);
    } catch (error) {
      console.error('Failed to load models:', error);
    } finally {
      setIsLoadingModels(false);
    }
  };

  const handleSend = async (content: string) => {
    // Add user turn
    const userTurn: ConversationTurn = {
      id: `turn-${Date.now()}`,
      timestamp: new Date(),
      role: 'user',
      items: [{ type: 'text', content }],
    };
    setTurns(prev => [...prev, userTurn]);

    // Send message and stream assistant response
    await sendMessage(content, selectedModel);
  };

  const handleClearConversation = async () => {
    try {
      await clientRef.current.clearConversation(conversationId);
      clearPersistedConversation();
    } catch (error) {
      console.error('Failed to clear conversation:', error);
    }
  };

  const copyArtifact = (content: string) => {
    navigator.clipboard.writeText(content);
  };

  return (
    <div className="app">
      <div className="header">
        <h1>🚀 OpenRouter.NET Streaming Chat</h1>
        <div className="header-controls">
          <button
            className="clear-button"
            onClick={handleClearConversation}
            disabled={isStreaming}
          >
            🗑️ Clear
          </button>
        </div>
      </div>

      <div className="main-content">
        <div className="chat-container">
          <ConversationView
            turns={turns}
            currentTurn={currentTurn}
            isStreaming={isStreaming}
            activeTool={activeTool}
            streamingArtifact={streamingArtifact}
          />

          {statusMessage && (
            <div className="status-bar">{statusMessage}</div>
          )}

          <InputArea
            onSend={handleSend}
            disabled={isStreaming}
            models={models}
            selectedModel={selectedModel}
            onSelectModel={setSelectedModel}
            isLoadingModels={isLoadingModels}
          />
        </div>

        <ArtifactsPanel turns={turns} currentTurn={currentTurn} onCopy={copyArtifact} />
      </div>
    </div>
  );
}

export default App;
