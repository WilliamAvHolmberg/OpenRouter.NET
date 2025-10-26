/**
 * RightDrawer - inline rail that shifts layout when open
 */

'use client';

import { useEffect } from 'react';
import { useDrawer } from './DrawerContext';

interface RightDrawerProps {
  open: boolean;
  onClose: () => void;
  width?: number;
  children: React.ReactNode;
}

export function RightDrawer({ open, onClose, width = 520, children }: RightDrawerProps) {
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose();
    };
    document.addEventListener('keydown', onKey);
    return () => document.removeEventListener('keydown', onKey);
  }, [onClose]);

  return (
    <div className={`h-full bg-white/95 backdrop-blur-xl border-l border-slate-200/50 shadow-2xl overflow-hidden ${open ? 'w-[var(--drawer-width)]' : 'w-0'} transition-all duration-300`} style={{ ['--drawer-width' as any]: `${width}px` }}>
      {open && (
        <div className="h-full flex flex-col animate-in slide-in-from-right duration-300 relative">
          {/* Subtle gradient background */}
          <div className="absolute inset-0 bg-gradient-to-br from-blue-50/30 via-transparent to-indigo-50/30 pointer-events-none" />

          {/* Header */}
          <div className="flex-shrink-0 px-6 py-4 border-b border-slate-200/50 bg-white/50 backdrop-blur-sm relative z-10">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-3">
                <div className="w-8 h-8 rounded-lg bg-gradient-to-br from-blue-500 to-indigo-600 flex items-center justify-center shadow-lg shadow-blue-500/30">
                  <svg className="w-4 h-4 text-white" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                  </svg>
                </div>
                <div className="text-base font-semibold text-slate-900">Preview</div>
              </div>
              <button
                className="px-3 py-1.5 text-sm text-slate-600 hover:text-slate-900 rounded-lg hover:bg-white/60 transition-all"
                onClick={onClose}
              >
                Close
              </button>
            </div>
          </div>

          {/* Content */}
          <div className="flex-1 overflow-y-auto p-6 relative z-10">
            <div className="max-w-[1200px] mx-auto">{children}</div>
          </div>
        </div>
      )}
    </div>
  );
}


