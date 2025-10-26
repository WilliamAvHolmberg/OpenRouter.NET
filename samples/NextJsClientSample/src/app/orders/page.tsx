'use client';

import { useMemo, useState, useEffect } from 'react';
import { ORDERS } from '../../lib/orders.mock';
import { applyOrderFilters, type OrderFilters } from '../../lib/orderFilters';
import { OrdersTable } from '../../components/orders/OrdersTable';
import { FiltersBar } from '../../components/orders/FiltersBar';
import { OrdersAssistant } from '../../components/orders/OrdersAssistant';
import { ModelPicker } from '../../components/chat/ModelPicker';
import { useOpenRouterModels } from '@openrouter-dotnet/react';

export default function OrdersPage() {
  const [filters, setFilters] = useState<OrderFilters>({});
  const [selectedModel, setSelectedModel] = useState<string>('anthropic/claude-3.5-sonnet');
  const [filterAnimation, setFilterAnimation] = useState(false);
  const { models } = useOpenRouterModels('/api');
  const filtered = useMemo(() => applyOrderFilters(ORDERS, filters), [filters]);

  // Trigger animation when filters change
  useEffect(() => {
    if (Object.keys(filters).length > 0) {
      setFilterAnimation(true);
      const timer = setTimeout(() => setFilterAnimation(false), 1000);
      return () => clearTimeout(timer);
    }
  }, [filters]);

  const hasActiveFilters = Object.keys(filters).length > 0;

  return (
    <div className="h-[calc(100vh-4rem)] flex flex-col bg-slate-50 relative overflow-hidden">
      {/* Subtle background accent */}
      <div className="absolute top-0 right-0 w-[500px] h-[500px] bg-blue-500/5 rounded-full blur-3xl" />

      <div className="flex-1 p-6 max-w-7xl mx-auto w-full overflow-hidden relative z-10">

        <div className="grid grid-cols-1 lg:grid-cols-5 gap-6 h-full">
          {/* Orders Section */}
          <div className="lg:col-span-3 flex flex-col overflow-hidden">
            <div className="rounded-xl border border-slate-200 bg-white shadow-sm p-6 flex flex-col h-full overflow-hidden relative pt-10">
              {/* Active Filters Badge */}
              {hasActiveFilters && (
                <div className={`absolute top-2 left-1/2 transform -translate-x-1/2 transition-all duration-300 z-20 ${filterAnimation ? 'scale-105' : 'scale-100'}`}>
                  <div className="bg-blue-600 text-white px-4 py-1.5 rounded-lg shadow-sm flex items-center gap-2 text-sm font-medium">
                    <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                    </svg>
                    <span>{filtered.length} results</span>
                  </div>
                </div>
              )}

              <FiltersBar filters={filters} onClear={() => setFilters({})} />
              <div className="flex-1 overflow-auto mt-4">
                <OrdersTable orders={filtered} filterAnimation={filterAnimation} />
              </div>
            </div>
          </div>

          {/* Chat Section */}
          <div className="lg:col-span-2 flex flex-col overflow-hidden">
            <div className="rounded-xl border border-slate-200 bg-white shadow-sm flex-1 overflow-hidden flex flex-col">
              <OrdersAssistant
                onApplyFilters={setFilters}
                selectedModel={selectedModel}
                onModelChange={setSelectedModel}
              />
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}


