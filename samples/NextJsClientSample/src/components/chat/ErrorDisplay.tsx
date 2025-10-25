/**
 * Error Display
 * Shows error messages
 */

interface ErrorDisplayProps {
  error: any;
}

export function ErrorDisplay({ error }: ErrorDisplayProps) {
  return (
    <div className="p-4 bg-red-50 border border-red-200 rounded-xl animate-in fade-in slide-in-from-bottom-2 duration-300">
      <div className="flex items-start gap-3">
        <div className="text-red-500 text-xl">⚠️</div>
        <div className="flex-1">
          <p className="font-semibold text-red-900">Error occurred</p>
          <p className="text-red-700 text-sm mt-1">{error.message}</p>
          {error.details && (
            <p className="text-red-600 text-xs mt-2">{error.details}</p>
          )}
        </div>
      </div>
    </div>
  );
}
