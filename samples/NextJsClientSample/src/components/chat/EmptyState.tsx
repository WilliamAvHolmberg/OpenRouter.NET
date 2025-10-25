/**
 * Empty State
 * Shown when there are no messages
 */

export function EmptyState() {
  return (
    <div className="flex items-center justify-center h-full">
      <div className="text-center">
        <div className="text-6xl mb-4">💬</div>
        <h3 className="text-xl font-semibold text-gray-900 mb-2">Start a conversation</h3>
        <p className="text-gray-600">
          Ask me to generate code, explain concepts, or use tools
        </p>
      </div>
    </div>
  );
}
