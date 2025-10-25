import type { ConversationTurn, ArtifactItem } from '../types';
import { ArtifactRenderer } from './ArtifactRenderer';

interface ArtifactsPanelProps {
  turns: ConversationTurn[];
  currentTurn: ConversationTurn | null;
  onCopy: (content: string) => void;
}

export function ArtifactsPanel({ turns, currentTurn, onCopy }: ArtifactsPanelProps) {
  // Extract all artifacts from all turns
  const artifacts: ArtifactItem[] = [];

  // Get artifacts from completed turns
  turns.forEach(turn => {
    turn.items.forEach(item => {
      if (item.type === 'artifact') {
        artifacts.push(item);
      }
    });
  });

  // Get artifacts from current streaming turn
  if (currentTurn) {
    currentTurn.items.forEach(item => {
      if (item.type === 'artifact') {
        artifacts.push(item);
      }
    });
  }

  if (artifacts.length === 0) return null;

  return (
    <div className="artifacts-panel">
      <h2>📦 Artifacts ({artifacts.length})</h2>
      <div className="artifacts-list">
        {artifacts.map((artifact) => (
          <ArtifactRenderer
            key={artifact.id}
            artifact={artifact}
            variant="panel"
            showCopyButton
            onCopy={onCopy}
          />
        ))}
      </div>
    </div>
  );
}
