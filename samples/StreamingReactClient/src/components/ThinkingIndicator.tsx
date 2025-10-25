export function ThinkingIndicator() {
  return (
    <div className="message assistant streaming">
      <div className="message-header">
        <span className="role">🤖 Assistant</span>
        <span className="streaming-indicator">●</span>
      </div>
      <div className="message-content loading-indicator">
        <div className="thinking-animation">
          <span className="thinking-dot"></span>
          <span className="thinking-dot"></span>
          <span className="thinking-dot"></span>
        </div>
        <span className="thinking-text">Thinking...</span>
      </div>
    </div>
  );
}
