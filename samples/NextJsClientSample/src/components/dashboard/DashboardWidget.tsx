'use client';

import { ReactRunner } from '../chat/ReactRunner';

export type DashboardWidgetData = {
  id: string;
  title: string;
  code: string;
  size: 'small' | 'medium' | 'large';
  createdAt: number;
};

interface DashboardWidgetProps {
  widget: DashboardWidgetData;
  database: any | null;
  onRemove: (id: string) => void;
  animationDelay?: number;
}

export function DashboardWidget({ widget, database, onRemove, animationDelay = 0 }: DashboardWidgetProps) {
  return (
    <div
      className={`
        rounded-xl border border-slate-200 bg-white shadow-sm overflow-hidden
        animate-fade-in-up
        ${widget.size === 'large' ? 'md:col-span-2' : ''}
      `}
      style={{ animationDelay: `${animationDelay}ms` }}
    >
      <div className="px-4 py-3 border-b border-slate-200 bg-slate-50 flex items-center justify-between">
        <h3 className="text-sm font-semibold text-slate-900">{widget.title}</h3>
        <button
          onClick={() => onRemove(widget.id)}
          className="text-xs px-2 py-1 rounded text-slate-500 hover:text-red-600 hover:bg-red-50 transition-colors"
          aria-label="Remove widget"
        >
          <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
          </svg>
        </button>
      </div>
      
      <div className="p-4">
        <ReactRunner code={widget.code} database={database} />
      </div>
    </div>
  );
}
