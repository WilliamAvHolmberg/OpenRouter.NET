import { useState } from 'react';

interface HtmlPreviewProps {
  code: string;
  title: string;
}

export function HtmlPreview({ code, title }: HtmlPreviewProps) {
  const [isDrawerOpen, setIsDrawerOpen] = useState(false);
  const [showCode, setShowCode] = useState(false);

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
                <div className="drawer-preview-render">
                  <iframe
                    srcDoc={code}
                    title={title}
                    sandbox="allow-scripts"
                    style={{
                      width: '100%',
                      height: '100%',
                      border: 'none',
                      backgroundColor: 'white',
                    }}
                  />
                </div>
              )}
            </div>
          </div>
        </>
      )}
    </>
  );
}

