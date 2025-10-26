/**
 * Tool Call Block
 * Compact one-liner by default, expandable for details
 */

'use client';

import { useState } from 'react';
import type { ContentBlock } from '@openrouter-dotnet/react';

interface ToolCallBlockProps {
  block: ContentBlock & { type: 'tool_call' };
}

export function ToolCallBlock({ block }: ToolCallBlockProps) {
  const [expanded, setExpanded] = useState(false);
  const isRunning = block.status === 'executing';

  const truncate = (v: unknown, n = 15) => {
    const s = typeof v === 'string' ? v : v == null ? '' : String(v);
    return s.length > n ? s.slice(0, n) + '…' : s || '—';
  };

  const statusText = (s: 'executing' | 'completed' | 'error') =>
    s === 'executing' ? 'Running' : s === 'completed' ? 'Completed' : 'Error';

  const statusColor = block.status === 'executing'
    ? 'bg-gradient-to-r from-blue-50 to-indigo-50 text-blue-700 border-blue-300/50'
    : block.status === 'completed'
    ? 'bg-gradient-to-r from-emerald-50 to-green-50 text-emerald-700 border-emerald-300/50'
    : 'bg-gradient-to-r from-red-50 to-rose-50 text-red-700 border-red-300/50';

  return (
    <div className="border border-slate-200/50 rounded-xl bg-white/40 backdrop-blur-sm overflow-hidden shadow-sm">
      {/* One-line summary header (click to expand) */}
      <button
        type="button"
        onClick={() => setExpanded(!expanded)}
        className="w-full px-3 py-2.5 flex items-center justify-between hover:bg-slate-100 transition-colors text-left"
      >
        <div className="flex items-center gap-2 min-w-0">
          <div className="flex flex-col gap-1 min-w-0">
            <div className="flex items-center gap-2">
              <span className="text-sm font-medium text-slate-900 truncate">{block.toolName}</span>
              <span className={`text-xs rounded-full px-3 py-0.5 border font-medium ${statusColor}`}>
                {statusText(block.status)}
              </span>
            </div>
            {/* Inline previews */}
            <div className="flex items-center gap-2 text-xs text-slate-500">
              {block.arguments && (
                <span className="truncate">args: "{truncate(block.arguments, 30)}"</span>
              )}
            </div>
          </div>
          {isRunning && (
            <span className="flex items-center gap-1 ml-2">
              <span className="w-1.5 h-1.5 rounded-full bg-blue-600 animate-bounce" style={{ animationDelay: '0ms' }} />
              <span className="w-1.5 h-1.5 rounded-full bg-blue-600 animate-bounce" style={{ animationDelay: '150ms' }} />
              <span className="w-1.5 h-1.5 rounded-full bg-blue-600 animate-bounce" style={{ animationDelay: '300ms' }} />
            </span>
          )}
        </div>
        <svg
          className={`h-5 w-5 text-slate-400 transition-transform ${expanded ? 'rotate-180' : ''}`}
          viewBox="0 0 20 20"
          fill="currentColor"
          aria-hidden="true"
        >
          <path fillRule="evenodd" d="M5.23 7.21a.75.75 0 011.06.02L10 10.94l3.71-3.71a.75.75 0 111.06 1.06l-4.24 4.24a.75.75 0 01-1.06 0L5.21 8.29a.75.75 0 01.02-1.08z" clipRule="evenodd" />
        </svg>
      </button>

      {/* Details */}
      {expanded && (
        <div className="px-3 pb-3 space-y-2">
          {block.executionTimeMs && (
            <div className="text-[11px] text-slate-500">{block.executionTimeMs}ms</div>
          )}

          {block.arguments && (
            <div>
              <div className="text-xs text-slate-600 mb-1">Arguments</div>
              <code className="block text-xs bg-slate-50 text-slate-800 px-2 py-1 rounded-md font-mono">
                {block.arguments}
              </code>
            </div>
          )}

          {block.status === 'completed' && block.result && (
            <div>
              <div className="text-xs text-slate-600 mb-1">Result</div>
              <div className="text-sm text-slate-800">{block.result}</div>
            </div>
          )}

          {block.status === 'error' && block.error && (
            <div>
              <div className="text-xs text-red-700 mb-1">Error</div>
              <div className="text-sm text-red-700">{block.error}</div>
            </div>
          )}
        </div>
      )}
    </div>
  );
}

/**
 * Status badge for tool calls
 */
function InlineStatus({ status }: { status: 'executing' | 'completed' | 'error' }) {
  const map = { executing: 'Running', completed: 'Completed', error: 'Error' } as const;
  return <span>{map[status]}</span>;
}
