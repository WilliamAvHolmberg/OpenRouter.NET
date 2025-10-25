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
    <div className={`h-full bg-white border-l border-slate-200 shadow-xl overflow-hidden ${open ? 'w-[var(--drawer-width)]' : 'w-0'} transition-all duration-300`} style={{ ['--drawer-width' as any]: `${width}px` }}>
      {open && (
        <div className="h-full overflow-y-auto p-4 animate-in slide-in-from-right duration-300">
          <div className="flex items-center justify-between mb-3">
            <div className="text-sm font-medium text-slate-900">Preview</div>
            <button className="text-slate-500 hover:text-slate-900 text-sm" onClick={onClose}>Close</button>
          </div>
          <div className="max-w-[1200px] mx-auto">{children}</div>
        </div>
      )}
    </div>
  );
}


