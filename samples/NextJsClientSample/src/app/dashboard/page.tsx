'use client';

import { useState, useEffect } from 'react';
import { SQLiteProvider, useDatabase, useDatabaseReady } from '../../lib/dashboard/SQLiteContext';
import { DashboardCanvas } from '../../components/dashboard/DashboardCanvas';
import { DashboardAssistant } from '../../components/dashboard/DashboardAssistant';
import type { DashboardWidgetData } from '../../components/dashboard/DashboardWidget';

function DashboardPageContent() {
  const [widgets, setWidgets] = useState<DashboardWidgetData[]>([]);
  const [selectedModel, setSelectedModel] = useState<string>('anthropic/claude-3.5-sonnet');
  const db = useDatabase();
  const { isReady, error: dbError } = useDatabaseReady();

  useEffect(() => {
    const saved = localStorage.getItem('dashboard_widgets');
    if (saved) {
      try {
        setWidgets(JSON.parse(saved));
      } catch (e) {
        console.error('Failed to load widgets from localStorage:', e);
      }
    }
  }, []);

  useEffect(() => {
    if (widgets.length > 0) {
      localStorage.setItem('dashboard_widgets', JSON.stringify(widgets));
    }
  }, [widgets]);

  const handleAddWidget = (widget: Omit<DashboardWidgetData, 'createdAt'>) => {
    const newWidget = {
      ...widget,
      createdAt: Date.now()
    };
    setWidgets(prev => [...prev, newWidget]);
  };

  const handleRemoveWidget = (id: string) => {
    setWidgets(prev => prev.filter(w => w.id !== id));
    if (widgets.length === 1) {
      localStorage.removeItem('dashboard_widgets');
    }
  };

  const handleUpdateWidget = (id: string, updates: Partial<DashboardWidgetData>) => {
    setWidgets(prev => prev.map(w => w.id === id ? { ...w, ...updates } : w));
  };

  const handleClearDashboard = () => {
    if (confirm('Are you sure you want to clear all widgets?')) {
      setWidgets([]);
      localStorage.removeItem('dashboard_widgets');
    }
  };

  if (dbError) {
    return (
      <div className="h-[calc(100vh-4rem)] flex items-center justify-center bg-slate-50">
        <div className="text-center max-w-md">
          <div className="text-red-600 mb-4">
            <svg className="w-16 h-16 mx-auto" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
            </svg>
          </div>
          <h3 className="text-lg font-semibold text-slate-900 mb-2">Database Error</h3>
          <p className="text-sm text-slate-500">{dbError}</p>
        </div>
      </div>
    );
  }

  if (!isReady) {
    return (
      <div className="h-[calc(100vh-4rem)] flex items-center justify-center bg-slate-50">
        <div className="text-center">
          <div className="w-16 h-16 mx-auto mb-4 rounded-full bg-gradient-to-br from-blue-500 to-indigo-600 flex items-center justify-center animate-pulse">
            <svg className="w-8 h-8 text-white" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 7v10c0 2.21 3.582 4 8 4s8-1.79 8-4V7M4 7c0 2.21 3.582 4 8 4s8-1.79 8-4M4 7c0-2.21 3.582-4 8-4s8 1.79 8 4" />
            </svg>
          </div>
          <h3 className="text-lg font-semibold text-slate-900 mb-2">Loading Database...</h3>
          <p className="text-sm text-slate-500">Setting up SQLite and mock data</p>
        </div>
      </div>
    );
  }

  return (
    <div className="h-[calc(100vh-4rem)] flex flex-col bg-slate-50 relative overflow-hidden">
      <div className="absolute top-0 right-0 w-[500px] h-[500px] bg-indigo-500/5 rounded-full blur-3xl" />

      <div className="flex-shrink-0 px-6 py-4 border-b border-slate-200 bg-white/80 backdrop-blur-sm z-10 relative">
        <div className="max-w-7xl mx-auto flex items-center justify-between">
          <div>
            <h1 className="text-xl font-bold text-slate-900">Dashboard Builder</h1>
            <p className="text-sm text-slate-500">Create dynamic data visualizations with AI</p>
          </div>
          {widgets.length > 0 && (
            <button
              onClick={handleClearDashboard}
              className="px-4 py-2 text-sm font-medium text-red-600 bg-red-50 border border-red-200 rounded-lg hover:bg-red-100 transition-colors"
            >
              Clear Dashboard
            </button>
          )}
        </div>
      </div>

      <div className="flex-1 p-6 max-w-7xl mx-auto w-full overflow-hidden relative z-10">
        <div className="grid grid-cols-1 lg:grid-cols-5 gap-6 h-full">
          <div className="lg:col-span-3 flex flex-col overflow-hidden">
            <div className="rounded-xl border border-slate-200 bg-white shadow-sm p-6 flex-1 overflow-auto">
              <DashboardCanvas
                widgets={widgets}
                database={db}
                onRemoveWidget={handleRemoveWidget}
              />
            </div>
          </div>

          <div className="lg:col-span-2 flex flex-col overflow-hidden">
            <div className="rounded-xl border border-slate-200 bg-white shadow-sm flex-1 overflow-hidden flex flex-col">
              <DashboardAssistant
                selectedModel={selectedModel}
                onModelChange={setSelectedModel}
                onAddWidget={handleAddWidget}
                onRemoveWidget={handleRemoveWidget}
                onUpdateWidget={handleUpdateWidget}
              />
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

export default function DashboardPage() {
  return (
    <SQLiteProvider>
      <DashboardPageContent />
    </SQLiteProvider>
  );
}
