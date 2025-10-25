import { useState, useMemo } from 'react';
import { useRunner } from 'react-runner';

interface LivePreviewProps {
  code: string;
  title: string;
}

export function LivePreview({ code, title }: LivePreviewProps) {
  const [isDrawerOpen, setIsDrawerOpen] = useState(false);
  const [showCode, setShowCode] = useState(false);

  const wrappedCode = useMemo(() => {
    if (!code) return '';
    
    let cleanedCode = code
      .replace(/import\s+.*?from\s+['"].*?['"];?\s*/g, '')
      .replace(/export\s+default\s+/g, '')
      .replace(/export\s+/g, '')
      .trim();
    
    let componentName = 'Component';
    
    const arrowFunctionMatch = cleanedCode.match(/(?:const|let|var)\s+(\w+)\s*[=:]/);
    const functionDeclarationMatch = cleanedCode.match(/function\s+(\w+)\s*\(/);
    
    if (arrowFunctionMatch) {
      componentName = arrowFunctionMatch[1];
    } else if (functionDeclarationMatch) {
      componentName = functionDeclarationMatch[1];
    }
    
    if (cleanedCode.includes('React.FC') || cleanedCode.includes(': FC')) {
      cleanedCode = cleanedCode
        .replace(/:\s*React\.FC\s*(<[^>]*>)?/g, '')
        .replace(/:\s*FC\s*(<[^>]*>)?/g, '');
    }
    
    return `
      ${cleanedCode}
      render(<${componentName} />);
    `;
  }, [code]);

  const scope = useMemo(() => ({
    useState,
  }), []);

  const { element, error: runnerError } = useRunner({
    code: wrappedCode,
    scope,
  });

  const copyCode = () => {
    navigator.clipboard.writeText(code);
  };

  return (
    <>
      <button 
        className="preview-trigger-button"
        onClick={() => setIsDrawerOpen(true)}
      >
        ▶️ Live Preview
      </button>

      {isDrawerOpen && (
        <>
          <div 
            className="drawer-overlay" 
            onClick={() => setIsDrawerOpen(false)}
          />
          <div className="preview-drawer">
            <div className="drawer-header">
              <h3>{title}</h3>
              <div className="drawer-controls">
                <button
                  className={`drawer-tab ${!showCode ? 'active' : ''}`}
                  onClick={() => setShowCode(false)}
                >
                  ▶️ Preview
                </button>
                <button
                  className={`drawer-tab ${showCode ? 'active' : ''}`}
                  onClick={() => setShowCode(true)}
                >
                  📝 Code
                </button>
                <button className="drawer-action-button" onClick={copyCode}>
                  📋 Copy
                </button>
                <button 
                  className="drawer-close-button"
                  onClick={() => setIsDrawerOpen(false)}
                >
                  ✕
                </button>
              </div>
            </div>

            <div className="drawer-content">
              {showCode ? (
                <pre className="drawer-code">
                  <code>{code}</code>
                </pre>
              ) : (
                <>
                  {runnerError ? (
                    <div className="preview-error">
                      <div className="error-title">❌ Error running component</div>
                      <pre className="error-message">{String(runnerError)}</pre>
                    </div>
                  ) : (
                    <div className="drawer-preview-render">{element}</div>
                  )}
                </>
              )}
            </div>
          </div>
        </>
      )}
    </>
  );
}

