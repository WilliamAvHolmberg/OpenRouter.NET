'use client';

import type { OrderFilters } from '../../lib/orderFilters';

export function FiltersBar({ filters, onClear }: { filters: OrderFilters; onClear: () => void }) {
  const chips: string[] = [];
  if (filters.status?.length) chips.push(`status: ${filters.status.join(', ')}`);
  if (typeof filters.delivered === 'boolean') chips.push(`delivered: ${filters.delivered}`);
  if (filters.customerIds?.length) chips.push(`customers: ${filters.customerIds.length}`);
  if (typeof filters.minAmount === 'number') chips.push(`min: $${filters.minAmount}`);
  if (typeof filters.maxAmount === 'number') chips.push(`max: $${filters.maxAmount}`);
  if (filters.createdFrom || filters.createdTo) chips.push(`created: ${filters.createdFrom ?? ''} → ${filters.createdTo ?? ''}`);
  if (filters.deliveredFrom || filters.deliveredTo) chips.push(`delivered: ${filters.deliveredFrom ?? ''} → ${filters.deliveredTo ?? ''}`);
  if (filters.text) chips.push(`text: "${filters.text}"`);
  if (filters.tags?.length) chips.push(`tags: ${filters.tags.join(', ')}`);

  return (
    <div className="flex items-center justify-between pb-4 border-b border-slate-200">
      <div className="flex gap-2 flex-wrap items-center">
        {chips.length === 0 ? (
          <span className="text-sm text-slate-400">No filters applied</span>
        ) : (
          <>
            <span className="text-xs font-medium text-slate-500">Filters:</span>
            {chips.map((c, i) => (
              <span
                key={i}
                className="inline-flex items-center rounded-md bg-blue-50 text-blue-700 px-3 py-1 text-xs font-medium border border-blue-100 animate-fade-in-up"
                style={{ animationDelay: `${i * 100}ms` }}
              >
                {c}
              </span>
            ))}
          </>
        )}
      </div>
      {chips.length > 0 && (
        <button
          onClick={onClear}
          className="px-3 py-1.5 text-sm font-medium text-slate-700 bg-slate-100 rounded-md hover:bg-slate-200 transition-colors"
        >
          Clear
        </button>
      )}
    </div>
  );
}


