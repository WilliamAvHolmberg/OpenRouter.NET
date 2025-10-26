/**
 * Empty State
 * Shown when there are no messages
 */

export function EmptyState() {
  return (
    <div className="flex items-center justify-center h-full p-6 relative z-10">
      <div className="text-center max-w-sm">
        <div className="text-slate-500 text-sm font-medium mb-6">
          Ask me to filter orders, for example:
        </div>
        <div className="space-y-3 text-left">
          <div className="bg-white/50 backdrop-blur-sm border border-slate-200/50 rounded-xl p-4 shadow-sm hover:shadow-md hover:scale-[1.02] transition-all cursor-pointer">
            <span className="text-sm text-slate-700">"Show undelivered orders over $100"</span>
          </div>
          <div className="bg-white/50 backdrop-blur-sm border border-slate-200/50 rounded-xl p-4 shadow-sm hover:shadow-md hover:scale-[1.02] transition-all cursor-pointer">
            <span className="text-sm text-slate-700">"Filter shipped orders from this week"</span>
          </div>
        </div>
      </div>
    </div>
  );
}
