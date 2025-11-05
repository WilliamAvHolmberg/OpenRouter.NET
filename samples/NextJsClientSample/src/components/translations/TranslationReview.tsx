'use client';

import { type TranslationSuggestion, type LanguageCode } from '@/lib/translationTypes';

interface TranslationReviewProps {
  suggestions: TranslationSuggestion[];
  showReview: boolean;
  onAccept: (lang: LanguageCode, value: string) => void;
  onReject: (lang: LanguageCode) => void;
  onAcceptAll: () => void;
  onClose: () => void;
}

export function TranslationReview({
  suggestions,
  showReview,
  onAccept,
  onReject,
  onAcceptAll,
  onClose,
}: TranslationReviewProps) {
  const pendingCount = suggestions.filter(s => s.status === 'pending').length;
  const acceptedCount = suggestions.filter(s => s.status === 'accepted').length;
  const rejectedCount = suggestions.filter(s => s.status === 'rejected').length;

  if (!showReview) {
    return (
      <div className="flex items-center justify-center h-full p-8">
        <div className="text-center max-w-md">
          <div className="text-slate-300 mb-4">
            <svg className="w-20 h-20 mx-auto" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
          </div>
          <h3 className="text-lg font-semibold text-slate-900 mb-2">No Suggestions Yet</h3>
          <p className="text-sm text-slate-500">
            Enter a field name and click "Get AI Suggestions" to generate translations
          </p>
        </div>
      </div>
    );
  }

  return (
    <div className="flex flex-col h-full">
      <div className="flex-shrink-0 px-6 py-4 border-b border-slate-200 bg-gradient-to-r from-blue-50 to-indigo-50">
        <div className="flex items-center justify-between mb-3">
          <div>
            <h3 className="text-sm font-semibold text-slate-900">Review Suggestions</h3>
            <p className="text-xs text-slate-500 mt-0.5">Review and approve AI-generated translations</p>
          </div>
          <button
            onClick={onClose}
            className="text-slate-400 hover:text-slate-600 transition-colors"
            title="Close review"
          >
            <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        {/* Stats */}
        <div className="flex gap-2 text-xs">
          {pendingCount > 0 && (
            <div className="bg-orange-100 text-orange-700 px-2 py-1 rounded">
              {pendingCount} pending
            </div>
          )}
          {acceptedCount > 0 && (
            <div className="bg-green-100 text-green-700 px-2 py-1 rounded">
              {acceptedCount} accepted
            </div>
          )}
          {rejectedCount > 0 && (
            <div className="bg-slate-100 text-slate-700 px-2 py-1 rounded">
              {rejectedCount} rejected
            </div>
          )}
        </div>
      </div>

      <div className="flex-1 overflow-auto p-6">
        <div className="space-y-3">
          {suggestions.map((suggestion) => (
            <div
              key={suggestion.language}
              className={`rounded-lg border-2 transition-all ${
                suggestion.status === 'accepted'
                  ? 'bg-green-50 border-green-500'
                  : suggestion.status === 'rejected'
                  ? 'bg-slate-50 border-slate-300 opacity-60'
                  : 'bg-white border-slate-200 hover:border-blue-300'
              }`}
            >
              <div className="p-4">
                <div className="flex items-start justify-between gap-3 mb-3">
                  <div className="flex items-center gap-2">
                    <span className="text-2xl">{suggestion.flag}</span>
                    <div>
                      <h4 className="font-semibold text-slate-900">{suggestion.languageName}</h4>
                      <p className="text-xs text-slate-500">{suggestion.language.toUpperCase()}</p>
                    </div>
                  </div>
                  
                  {suggestion.status === 'pending' && (
                    <div className="flex gap-2">
                      <button
                        onClick={() => onReject(suggestion.language)}
                        className="p-2 text-slate-600 hover:text-red-600 hover:bg-red-50 rounded-lg transition-colors"
                        title="Reject"
                      >
                        <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                        </svg>
                      </button>
                      <button
                        onClick={() => onAccept(suggestion.language, suggestion.suggested)}
                        className="p-2 text-white bg-green-600 hover:bg-green-700 rounded-lg transition-colors"
                        title="Accept"
                      >
                        <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                        </svg>
                      </button>
                    </div>
                  )}

                  {suggestion.status === 'accepted' && (
                    <div className="flex items-center gap-1 text-green-700 text-sm font-medium">
                      <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                      </svg>
                      Accepted
                    </div>
                  )}

                  {suggestion.status === 'rejected' && (
                    <div className="flex items-center gap-1 text-slate-500 text-sm font-medium">
                      <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                      </svg>
                      Rejected
                    </div>
                  )}
                </div>

                <div className="space-y-2">
                  {suggestion.current && (
                    <div className="bg-slate-100 rounded p-2">
                      <p className="text-xs text-slate-600 mb-1">Current:</p>
                      <p className="text-sm text-slate-900">{suggestion.current}</p>
                    </div>
                  )}
                  <div className={`rounded p-2 ${suggestion.status === 'accepted' ? 'bg-green-100' : 'bg-blue-50'}`}>
                    <p className="text-xs text-slate-600 mb-1">Suggested:</p>
                    <p className="text-sm font-medium text-slate-900">{suggestion.suggested}</p>
                  </div>
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* Accept All Button */}
      {pendingCount > 0 && (
        <div className="flex-shrink-0 p-4 border-t border-slate-200 bg-slate-50">
          <button
            onClick={onAcceptAll}
            className="w-full bg-gradient-to-r from-green-600 to-emerald-600 text-white px-6 py-3 rounded-lg font-medium hover:from-green-700 hover:to-emerald-700 transition-all shadow-lg shadow-green-500/25 flex items-center justify-center gap-2"
          >
            <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
            Accept All ({pendingCount})
          </button>
        </div>
      )}
    </div>
  );
}

