/**
 * Artifact Block
 * Code/document viewer with fixed height and inline scroll
 * Auto-scrolls to bottom when streaming to show latest code
 */

'use client';

import { useRef, useEffect, useState } from 'react';
import type { ContentBlock } from '@openrouter-dotnet/react';
import { ReactRunner } from '../ReactRunner';
import { useDrawer } from '../DrawerContext';

interface ArtifactBlockProps {
  block: ContentBlock & { type: 'artifact' };
}

export function ArtifactBlock({ block }: ArtifactBlockProps) {
  const scrollRef = useRef<HTMLPreElement>(null);
  const [expanded, setExpanded] = useState(true);
  const drawer = useDrawer();

  // Auto-scroll to bottom when content updates during streaming
  useEffect(() => {
    if (block.isStreaming && scrollRef.current) {
      scrollRef.current.scrollTop = scrollRef.current.scrollHeight;
    }
  }, [block.content, block.isStreaming]);

  // Auto-expand drawer when React runner artifact completes
  useEffect(() => {
    if (block.language === 'tsx.reactrunner' && !block.isStreaming && block.content) {
      drawer.setContent(
        <div className="space-y-4">
          <div className="flex items-center gap-3 pb-3 border-b border-slate-200/50">
            <div className="flex-1">
              <div className="text-lg font-semibold text-slate-900">{block.title}</div>
              <div className="text-xs text-slate-500 mt-0.5">React Component Preview</div>
            </div>
          </div>
          <div className="rounded-2xl border border-slate-200/50 bg-white/60 backdrop-blur-sm shadow-xl overflow-hidden">
            <ReactRunner code={block.content} />
          </div>
        </div>,
        { width: 820 }
      );
    }
  }, [block.language, block.isStreaming, block.content, block.title, drawer]);

  return (
    <div className={`border border-slate-200/50 rounded-2xl overflow-hidden bg-white/40 backdrop-blur-sm shadow-lg ${block.isStreaming ? 'animate-pulse' : ''}`}>
      {/* Compact header */}
      <div className="px-3 py-2 flex items-center justify-between">
        <div className="flex items-center gap-2">
          <svg width="14" height="14" viewBox="0 0 24 24" className="text-slate-500" aria-hidden="true">
            <path fill="currentColor" d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8l-6-6zM14 9V3.5L19.5 9H14z" />
          </svg>
          <div className="text-sm font-semibold text-slate-900">{block.title}</div>
          {block.language && (
            <div className="text-[11px] text-slate-500">{block.language}</div>
          )}
        </div>
        {/* Actions */}
        <div className="flex items-center gap-1">
          <button className="text-[11px] px-3 py-1 rounded-full border border-slate-200/50 text-slate-600 hover:bg-white/60 hover:shadow transition-all" onClick={() => navigator.clipboard.writeText(block.content || '')}>Copy</button>
          <a
            className="text-[11px] px-3 py-1 rounded-full border border-slate-200/50 text-slate-600 hover:bg-white/60 hover:shadow transition-all"
            download={block.title || 'artifact.txt'}
            href={`data:text/plain;charset=utf-8,${encodeURIComponent(block.content || '')}`}
          >
            Download
          </a>
          {block.language === 'tsx.reactrunner' && (
            <button
              className="text-[11px] px-3 py-1 rounded-full bg-gradient-to-r from-blue-600 to-indigo-600 text-white hover:from-blue-700 hover:to-indigo-700 shadow-sm hover:shadow-md transition-all"
              onClick={() => drawer.setContent(
                <div className="space-y-4">
                  <div className="flex items-center gap-3 pb-3 border-b border-slate-200/50">
                    <div className="flex-1">
                      <div className="text-lg font-semibold text-slate-900">{block.title}</div>
                      <div className="text-xs text-slate-500 mt-0.5">React Component Preview</div>
                    </div>
                  </div>
                  <div className="rounded-2xl border border-slate-200/50 bg-white/60 backdrop-blur-sm shadow-xl overflow-hidden">
                    <ReactRunner code={block.content || ''} />
                  </div>
                </div>,
                { width: 820 }
              )}
            >
              Preview
            </button>
          )}
          <button
            type="button"
            aria-label={expanded ? 'Collapse artifact' : 'Expand artifact'}
            onClick={() => setExpanded(!expanded)}
            className="ml-1 h-7 w-7 inline-flex items-center justify-center rounded-md hover:bg-slate-50 text-slate-500"
          >
            <svg
              className={`h-4 w-4 transition-transform ${expanded ? 'rotate-180' : ''}`}
              viewBox="0 0 20 20"
              fill="currentColor"
              aria-hidden="true"
            >
              <path fillRule="evenodd" d="M5.23 7.21a.75.75 0 011.06.02L10 10.94l3.71-3.71a.75.75 0 111.06 1.06l-4.24 4.24a.75.75 0 01-1.06 0L5.21 8.29a.75.75 0 01.02-1.08z" clipRule="evenodd" />
            </svg>
          </button>
        </div>
      </div>

      {/* Content - always show code */}
      {expanded && (
        <div className="px-3 pb-3">
          <pre
            ref={scrollRef}
            className="artifact-code-scroll m-0 p-4 bg-slate-950 text-slate-100 overflow-auto max-h-[300px] text-sm font-mono leading-relaxed"
          >
            <code>{block.content || <span className="text-slate-500">...</span>}</code>
          </pre>
        </div>
      )}

      {/* Drawer content handled by context at layout level */}
    </div>
  );
}
