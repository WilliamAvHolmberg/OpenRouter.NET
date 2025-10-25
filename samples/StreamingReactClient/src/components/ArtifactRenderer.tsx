import { LivePreview } from './LivePreview';
import { HtmlPreview } from './HtmlPreview';
import type { Artifact, ArtifactItem } from '../types';

interface ArtifactRendererProps {
  artifact: Artifact | ArtifactItem;
  showCopyButton?: boolean;
  onCopy?: (content: string) => void;
  variant?: 'message' | 'panel';
}

export function ArtifactRenderer({ artifact, showCopyButton = false, onCopy, variant = 'message' }: ArtifactRendererProps) {
  const isReactComponent = (): boolean => {
    if (!artifact.language) return false;

    const reactLanguages = ['jsx', 'tsx', 'typescript', 'javascript', 'react'];
    const isReactLang = reactLanguages.some(lang =>
      artifact.language?.toLowerCase().includes(lang)
    );

    const hasReactCode = artifact.content.includes('React') ||
                        artifact.content.includes('useState') ||
                        artifact.content.includes('useEffect') ||
                        artifact.content.includes('export default') ||
                        /function\s+\w+\s*\([^)]*\)\s*\{/.test(artifact.content);

    return isReactLang || hasReactCode;
  };

  const isHtmlContent = (): boolean => {
    if (artifact.language?.toLowerCase() === 'html') return true;

    const artifactType = 'artifactType' in artifact ? artifact.artifactType : artifact.type;
    if (artifactType?.toLowerCase().includes('html')) return true;

    const trimmedContent = artifact.content.trim();
    const hasHtmlTags = /^<!DOCTYPE html>/i.test(trimmedContent) ||
                        /^<html/i.test(trimmedContent) ||
                        /<html[\s>]/i.test(trimmedContent);

    return hasHtmlTags;
  };

  if (variant === 'panel') {
    return (
      <div className="artifact-card">
        <div className="artifact-card-header">
          <span className="artifact-title">{artifact.title}</span>
          {artifact.language && (
            <span className="artifact-language">{artifact.language}</span>
          )}
        </div>
        {isReactComponent() ? (
          <LivePreview code={artifact.content} title={artifact.title} />
        ) : isHtmlContent() ? (
          <HtmlPreview code={artifact.content} title={artifact.title} />
        ) : (
          <pre className="artifact-content">
            <code>{artifact.content}</code>
          </pre>
        )}
        {showCopyButton && onCopy && (
          <button className="copy-button" onClick={() => onCopy(artifact.content)}>
            📋 Copy
          </button>
        )}
      </div>
    );
  }

  return (
    <div className="message-artifact-card">
      <div className="message-artifact-header">
        <span className="artifact-badge-inline">
          📦 {artifact.title}
          {artifact.language && <span className="badge-detail">{artifact.language}</span>}
        </span>
      </div>
      {isReactComponent() ? (
        <LivePreview code={artifact.content} title={artifact.title} />
      ) : isHtmlContent() ? (
        <HtmlPreview code={artifact.content} title={artifact.title} />
      ) : (
        <pre className="message-artifact-code">
          <code>{artifact.content}</code>
        </pre>
      )}
    </div>
  );
}
