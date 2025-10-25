/**
 * Markdown renderer (assistant only)
 * Compact, GFM-enabled, no raw HTML
 */

'use client';

import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';

interface MarkdownProps {
  children: string;
}

export function Markdown({ children }: MarkdownProps) {
  return (
    <div className="prose prose-sm max-w-none text-slate-800 prose-headings:text-slate-900 prose-headings:mt-3 prose-headings:mb-2 prose-p:my-2 prose-ul:my-2 prose-ol:my-2 prose-li:my-0.5 prose-a:text-indigo-600 hover:prose-a:underline prose-pre:rounded-lg prose-code:px-1.5 prose-code:py-0.5 prose-code:bg-slate-100 prose-code:text-slate-800">
      <ReactMarkdown
        remarkPlugins={[remarkGfm]}
        // DO NOT enable rehypeRaw; we don't want raw HTML
        components={{
          h1: (p) => <h3 {...p} className="text-lg font-semibold text-slate-900" />,
          h2: (p) => <h4 {...p} className="text-base font-semibold text-slate-900" />,
          h3: (p) => <h5 {...p} className="text-sm font-semibold text-slate-900" />,
          code: ({ inline, className, children, ...props }) =>
            inline ? (
              <code className="bg-slate-100 text-slate-800 rounded px-1.5 py-0.5" {...props}>{children}</code>
            ) : (
              <pre className="bg-slate-950 text-slate-100 p-3 rounded-lg overflow-auto">
                <code className={className} {...props}>{children}</code>
              </pre>
            ),
          table: (p) => <div className="overflow-x-auto"><table {...p} className="min-w-full text-sm" /></div>,
        }}
      >
        {children}
      </ReactMarkdown>
    </div>
  );
}


