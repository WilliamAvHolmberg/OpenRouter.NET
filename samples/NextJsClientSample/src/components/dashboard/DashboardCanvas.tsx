'use client';

import { DashboardWidget, type DashboardWidgetData } from './DashboardWidget';

interface DashboardCanvasProps {
  widgets: DashboardWidgetData[];
  database: any | null;
  onRemoveWidget: (id: string) => void;
}

export function DashboardCanvas({ widgets, database, onRemoveWidget }: DashboardCanvasProps) {
  if (widgets.length === 0) {
    return (
      <div className="h-full flex items-center justify-center">
        <div className="text-center max-w-md">
          <div className="w-16 h-16 mx-auto mb-4 rounded-full bg-gradient-to-br from-blue-500 to-indigo-600 flex items-center justify-center">
            <svg className="w-8 h-8 text-white" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M7 12l3-3 3 3 4-4M8 21l4-4 4 4M3 4h18M4 4h16v12a1 1 0 01-1 1H5a1 1 0 01-1-1V4z" />
            </svg>
          </div>
          <h3 className="text-lg font-semibold text-slate-900 mb-2">Your Dashboard is Empty</h3>
          <p className="text-sm text-slate-500 mb-6">
            Ask the AI assistant to create widgets for you. Try these examples:
          </p>
          <div className="space-y-2 text-left">
            <div className="text-xs bg-blue-50 border border-blue-200 rounded-lg p-3 text-blue-700">
              💡 "Show me total revenue by month"
            </div>
            <div className="text-xs bg-purple-50 border border-purple-200 rounded-lg p-3 text-purple-700">
              💡 "Create a sales overview dashboard"
            </div>
            <div className="text-xs bg-emerald-50 border border-emerald-200 rounded-lg p-3 text-emerald-700">
              💡 "Display top 10 customers by revenue"
            </div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 auto-rows-auto">
      {widgets.map((widget, idx) => (
        <DashboardWidget
          key={widget.id}
          widget={widget}
          database={database}
          onRemove={onRemoveWidget}
          animationDelay={idx * 150}
        />
      ))}
    </div>
  );
}
