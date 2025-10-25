/**
 * Debug Panel
 * Shows raw SSE lines, parsed events, and processed messages
 */

import type { ChatMessage, DebugControls } from '@openrouter-dotnet/react';

interface DebugPanelProps {
  debug: DebugControls;
  messages: ChatMessage[];
  showPanel: boolean;
  onTogglePanel: () => void;
}

export function DebugPanel({ debug, messages, showPanel, onTogglePanel }: DebugPanelProps) {
  return (
    <div className="w-96 border-l border-gray-300 bg-white overflow-hidden flex flex-col">
      <div className="p-4 bg-gray-900 text-white border-b border-gray-700">
        <div className="flex items-center justify-between mb-2">
          <h3 className="font-bold">🐛 Debug Panel</h3>
          <button
            onClick={onTogglePanel}
            className="text-xs px-2 py-1 bg-gray-800 hover:bg-gray-700 rounded"
          >
            {showPanel ? 'Hide' : 'Show'}
          </button>
        </div>
        <div className="text-xs text-gray-400 space-y-1">
          <div>Raw Lines: {debug.data.rawLines.length}</div>
          <div>Parsed Events: {debug.data.parsedEvents.length}</div>
          <div>Messages: {messages.length}</div>
        </div>
        <button
          onClick={debug.clear}
          className="mt-2 text-xs px-3 py-1 bg-red-600 hover:bg-red-700 rounded w-full"
        >
          Clear Debug Data
        </button>
      </div>

      {showPanel && (
        <div className="flex-1 overflow-y-auto p-4 space-y-4">
          {/* Raw Lines */}
          <div>
            <h4 className="font-semibold text-sm mb-2 text-gray-900">📝 Raw SSE Lines</h4>
            <div className="bg-gray-950 text-gray-100 rounded-lg p-3 text-xs font-mono max-h-60 overflow-y-auto">
              {debug.data.rawLines.length === 0 ? (
                <div className="text-gray-500">No data yet...</div>
              ) : (
                debug.data.rawLines.map((line, i) => (
                  <div key={i} className="py-0.5 border-b border-gray-800">
                    <span className="text-gray-600">{i}:</span> {line || '(empty)'}
                  </div>
                ))
              )}
            </div>
          </div>

          {/* Parsed Events */}
          <div>
            <h4 className="font-semibold text-sm mb-2 text-gray-900">🎯 Parsed Events</h4>
            <div className="bg-gray-950 text-gray-100 rounded-lg p-3 text-xs font-mono max-h-60 overflow-y-auto">
              {debug.data.parsedEvents.length === 0 ? (
                <div className="text-gray-500">No events yet...</div>
              ) : (
                debug.data.parsedEvents.map((event, i) => (
                  <div key={i} className="py-2 border-b border-gray-800">
                    <div className="text-yellow-400">Event #{i}</div>
                    <pre className="text-xs mt-1 whitespace-pre-wrap">
                      {JSON.stringify(event, null, 2)}
                    </pre>
                  </div>
                ))
              )}
            </div>
          </div>

          {/* Processed Messages */}
          <div>
            <h4 className="font-semibold text-sm mb-2 text-gray-900">📨 Processed Messages</h4>
            <div className="bg-gray-950 text-gray-100 rounded-lg p-3 text-xs font-mono max-h-60 overflow-y-auto">
              {messages.length === 0 ? (
                <div className="text-gray-500">No messages yet...</div>
              ) : (
                messages.map((msg, i) => (
                  <div key={i} className="py-2 border-b border-gray-800">
                    <div className="text-blue-400">
                      Message #{i} ({msg.role})
                    </div>
                    <pre className="text-xs mt-1 whitespace-pre-wrap">
                      {JSON.stringify(msg, null, 2)}
                    </pre>
                  </div>
                ))
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
