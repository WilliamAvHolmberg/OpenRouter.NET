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

  return (
    <div className={`border border-slate-200 rounded-xl bg-white shadow-sm ${isRunning && !expanded ? 'animate-pulse' : ''}`}>
      {/* One-line summary header (click to expand) */}
      <button
        type="button"
        onClick={() => setExpanded(!expanded)}
        className="w-full px-3 py-2 flex items-center justify-between hover:bg-slate-50 rounded-xl text-left"
      >
        <div className="flex items-center gap-2 min-w-0 text-slate-800">
          {/* Minimal tool glyph */}
          <svg width="14" height="14" viewBox="0 0 24 24" className="text-slate-500" aria-hidden="true">
            <path fill="currentColor" d="M21 7.5l-6.5 6.5-4-4L17 3.5a5 5 0 104 4zM3 21l6.9-1.4L6.9 16 3 21z" />
          </svg>
          <span className="text-sm font-medium truncate">{block.toolName}</span>
          <span className="text-[11px] rounded-full px-2 py-0.5 border border-slate-200 text-slate-600">
            {statusText(block.status)}
          </span>
          {/* Inline previews */}
          {block.arguments && (
            <span className="text-[11px] text-slate-500 truncate">• args: “{truncate(block.arguments)}”</span>
          )}
          {(block.result || block.error) && (
            <span className="text-[11px] text-slate-500 truncate">• result: “{truncate(block.result || block.error)}”</span>
          )}
          {isRunning && (
            <span className="flex items-center gap-1 ml-1">
              <span className="w-1.5 h-1.5 rounded-full bg-indigo-500 animate-bounce" style={{ animationDelay: '0ms' }} />
              <span className="w-1.5 h-1.5 rounded-full bg-indigo-500 animate-bounce" style={{ animationDelay: '150ms' }} />
              <span className="w-1.5 h-1.5 rounded-full bg-indigo-500 animate-bounce" style={{ animationDelay: '300ms' }} />
            </span>
          )}
        </div>
        <svg
          className={`h-4 w-4 text-slate-400 transition-transform ${expanded ? 'rotate-180' : ''}`}
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
