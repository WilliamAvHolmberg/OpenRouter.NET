/**
 * Chat Header
 * Title, description, and action buttons
 */

interface ChatHeaderProps {
  onClear: () => void;
  onToggleDebug: () => void;
  debugEnabled: boolean;
}

export function ChatHeader({ onClear, onToggleDebug, debugEnabled }: ChatHeaderProps) {
  return (
    <div className="p-6 bg-white/70 backdrop-blur-md border-b border-indigo-100 sticky top-0 z-30">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">Full Chat Experience</h1>
          <p className="text-sm text-slate-600 mt-1">
            Text, artifacts, and tool calls in perfect order
          </p>
        </div>
        <div className="flex items-center gap-2">
          <button
            onClick={onToggleDebug}
            className={`px-4 py-2 rounded-lg transition-all duration-200 font-medium ${
              debugEnabled
                ? 'bg-yellow-100 text-yellow-900 hover:bg-yellow-200'
                : 'text-slate-600 hover:text-slate-900 hover:bg-slate-100'
            }`}
          >
            {debugEnabled ? '🐛 Debug ON' : '🐛 Debug'}
          </button>
          <button
            onClick={onClear}
            className="px-4 py-2 text-slate-600 hover:text-slate-900 hover:bg-slate-100 rounded-lg transition-all duration-200 font-medium"
          >
            Clear Chat
          </button>
        </div>
      </div>
    </div>
  );
}
