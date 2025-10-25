import { useRef, useEffect } from 'react';
import type { ConversationTurn, ActiveTool, StreamingArtifact } from '../types';
import { ArtifactRenderer } from './ArtifactRenderer';
import { ThinkingIndicator } from './ThinkingIndicator';

interface ConversationViewProps {
  turns: ConversationTurn[];
  currentTurn: ConversationTurn | null;
  isStreaming: boolean;
  activeTool: ActiveTool | null;
  streamingArtifact: StreamingArtifact | null;
}

export function ConversationView({
  turns,
  currentTurn,
  isStreaming,
  activeTool,
  streamingArtifact,
}: ConversationViewProps) {
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const artifactPreviewRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [turns, currentTurn]);

  useEffect(() => {
    if (artifactPreviewRef.current) {
      artifactPreviewRef.current.scrollTop = artifactPreviewRef.current.scrollHeight;
    }
  }, [streamingArtifact?.content]);

  const renderTurn = (turn: ConversationTurn, isCurrentTurn: boolean = false) => {
    console.log(`🎨 [RENDER DEBUG] Rendering turn ${turn.id}:`, turn);
    console.log(`🎨 [RENDER DEBUG] Turn has ${turn.items.length} items:`, turn.items);

    return (
      <div key={turn.id} className={`message ${turn.role} ${isCurrentTurn ? 'streaming' : ''}`}>
        <div className="message-header">
          <span className="role">{turn.role === 'user' ? '👤 You' : '🤖 Assistant'}</span>
          {isCurrentTurn && <span className="streaming-indicator">●</span>}
          {!isCurrentTurn && <span className="timestamp">{turn.timestamp.toLocaleTimeString()}</span>}
        </div>

        {/* Render items in order */}
        {turn.items.map((item, idx) => {
          console.log(`🎨 [RENDER DEBUG] Rendering item ${idx}:`, item);
          if (item.type === 'text') {
            return (
              <div key={idx} className="message-content">
                {item.content}
              </div>
            );
          }

          if (item.type === 'tool') {
            return (
              <div key={idx} className="message-metadata">
                <div className="metadata-badge tool-badge">
                  🔧 {item.name}
                  {item.result && <span className="badge-detail">→ {item.result}</span>}
                  {item.error && <span className="badge-detail error">✗ {item.error}</span>}
                </div>
              </div>
            );
          }

          if (item.type === 'artifact') {
            return (
              <div key={idx} className="message-artifacts">
                <ArtifactRenderer artifact={item} variant="message" />
              </div>
            );
          }

          return null;
        })}
      </div>
    );
  };

  // Check if currentTurn is already in the turns array to avoid duplicate rendering
  const isCurrentTurnPersisted = currentTurn && turns.some(t => t.id === currentTurn.id);

  console.log('🖼️ [VIEW DEBUG] ConversationView rendering:');
  console.log('🖼️ [VIEW DEBUG] - Total turns:', turns.length);
  console.log('🖼️ [VIEW DEBUG] - Current turn:', currentTurn);
  console.log('🖼️ [VIEW DEBUG] - Is current turn persisted?', isCurrentTurnPersisted);
  console.log('🖼️ [VIEW DEBUG] - Active tool:', activeTool);
  console.log('🖼️ [VIEW DEBUG] - Streaming artifact:', streamingArtifact);

  return (
    <div className="messages">
      {/* Render completed turns */}
      {turns.map(turn => renderTurn(turn))}

      {/* Show thinking indicator if streaming but no turn yet */}
      {isStreaming && !currentTurn && !activeTool && (
        <ThinkingIndicator />
      )}

      {/* Render current streaming turn ONLY if not already in persisted turns */}
      {currentTurn && !isCurrentTurnPersisted && renderTurn(currentTurn, true)}

      {/* Show active tool indicator */}
      {activeTool && (
        <div className="tool-indicator">
          <div className={`tool-status ${activeTool.state}`}>
            {activeTool.state === 'executing' && '⚙️ Executing'}
            {activeTool.state === 'completed' && '✅ Completed'}
            {activeTool.state === 'error' && '❌ Error'}
          </div>
          <div className="tool-name">{activeTool.name}</div>
          {activeTool.result && (
            <div className="tool-result">Result: {activeTool.result}</div>
          )}
          {activeTool.error && (
            <div className="tool-error">Error: {activeTool.error}</div>
          )}
        </div>
      )}

      {/* Show streaming artifact preview */}
      {streamingArtifact && (
        <div className="artifact-progress artifact-generating">
          <div className="artifact-header">
            <span className="artifact-header-text">
              📦 Creating: {streamingArtifact.title}
              {streamingArtifact.language && ` (${streamingArtifact.language})`}
            </span>
            <span className="artifact-pulse">●</span>
          </div>
          <div className="artifact-preview-live" ref={artifactPreviewRef}>
            <div className="preview-lines">
              {streamingArtifact.content.split('\n').slice(-15).map((line, idx, arr) => (
                <div key={idx} className="preview-line">
                  <span className="line-number">{streamingArtifact.content.split('\n').length - arr.length + idx + 1}</span>
                  <span className="line-content">{line || ' '}</span>
                </div>
              ))}
            </div>
          </div>
          <div className="preview-stats">
            {streamingArtifact.content.split('\n').length} lines • {streamingArtifact.content.length} characters
          </div>
        </div>
      )}

      <div ref={messagesEndRef} />
    </div>
  );
}
