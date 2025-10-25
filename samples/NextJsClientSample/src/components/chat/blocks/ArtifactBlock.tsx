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
  const [tab, setTab] = useState<'preview' | 'code'>(
    block.language === 'tsx.reactrunner' && block.isStreaming ? 'code' : 'preview'
  );
  const drawer = useDrawer();

  // Auto-scroll to bottom when content updates during streaming
  useEffect(() => {
    if (block.isStreaming && scrollRef.current) {
      scrollRef.current.scrollTop = scrollRef.current.scrollHeight;
    }
  }, [block.content, block.isStreaming]);

  // When generation completes for react runner artifacts, switch to preview automatically
  useEffect(() => {
    if (block.language === 'tsx.reactrunner' && !block.isStreaming) {
      setTab('preview');
    }
  }, [block.language, block.isStreaming]);

  return (
    <div className={`border border-slate-200 rounded-xl overflow-hidden bg-white shadow-sm ${block.isStreaming ? 'animate-pulse' : ''}`}>
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
          <button className="text-[11px] px-2 py-1 rounded-full border border-slate-200 text-slate-600 hover:bg-slate-50" onClick={() => navigator.clipboard.writeText(block.content || '')}>Copy</button>
          <a
            className="text-[11px] px-2 py-1 rounded-full border border-slate-200 text-slate-600 hover:bg-slate-50"
            download={block.title || 'artifact.txt'}
            href={`data:text/plain;charset=utf-8,${encodeURIComponent(block.content || '')}`}
          >
            Download
          </a>
          {block.language === 'tsx.reactrunner' && (
            <button
              className="text-[11px] px-2 py-1 rounded-full border border-slate-200 text-slate-600 hover:bg-slate-50"
              onClick={() => drawer.setContent(
                <div>
                  <div className="mb-3 text-sm font-medium text-slate-900">{block.title}</div>
                  <div className="border border-slate-200 rounded-lg p-4 bg-white">
                    <ReactRunner code={block.content || ''} />
                  </div>
                </div>,
                { width: 820 }
              )}
            >
              Expand
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

      {/* Content - tabs for reactrunner artifacts; else code */}
      {expanded && (
        <div className="px-3 pb-3">
          {block.language === 'tsx.reactrunner' ? (
            <div>
              <div className="mb-2 inline-flex items-center gap-1 rounded-full p-0.5 text-[12px] bg-white">
                <button
                  className={`px-3 py-1 rounded-full border transition-colors ${
                    tab === 'preview'
                      ? 'bg-indigo-600 border-indigo-600 text-white shadow'
                      : 'border-slate-300 text-slate-700 hover:bg-slate-100'
                  }`}
                  onClick={() => setTab('preview')}
                >
                  Preview
                </button>
                <button
                  className={`px-3 py-1 rounded-full border transition-colors ${
                    tab === 'code'
                      ? 'bg-indigo-600 border-indigo-600 text-white shadow'
                      : 'border-slate-300 text-slate-700 hover:bg-slate-100'
                  }`}
                  onClick={() => setTab('code')}
                >
                  Code
                </button>
              </div>
              {tab === 'preview' ? (
                <div className="border border-slate-200 rounded-lg p-3 bg-white h-[300px] overflow-auto">
                  <ReactRunner code={block.content || ''} />
                </div>
              ) : (
                <pre
                  ref={scrollRef}
                  className="artifact-code-scroll m-0 p-4 bg-slate-950 text-slate-100 overflow-auto max-h-[300px] text-sm font-mono leading-relaxed"
                >
                  <code>{block.content || <span className="text-slate-500">...</span>}</code>
                </pre>
              )}
            </div>
          ) : (
            <pre
              ref={scrollRef}
              className="artifact-code-scroll m-0 p-4 bg-slate-950 text-slate-100 overflow-auto max-h-[220px] text-sm font-mono leading-relaxed"
            >
              <code>{block.content || <span className="text-slate-500">...</span>}</code>
            </pre>
          )}
        </div>
      )}

      {/* Drawer content handled by context at layout level */}
    </div>
  );
}
