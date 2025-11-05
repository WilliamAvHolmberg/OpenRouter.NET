'use client';

import { useState, useEffect } from 'react';
import { useGenerateObject } from '@openrouter-dotnet/react';
import { SUPPORTED_LANGUAGES, translationSchema, type LanguageCode, type TranslationSuggestion } from '@/lib/translationTypes';

interface TranslationFormProps {
  fieldName: string;
  translations: Record<LanguageCode, string>;
  selectedModel: string;
  models: { id: string; name: string }[];
  onFieldNameChange: (value: string) => void;
  onTranslationChange: (lang: LanguageCode, value: string) => void;
  onModelChange: (modelId: string) => void;
  onSuggestionsGenerated: (suggestions: TranslationSuggestion[]) => void;
}

export function TranslationForm({
  fieldName,
  translations,
  selectedModel,
  models,
  onFieldNameChange,
  onTranslationChange,
  onModelChange,
  onSuggestionsGenerated,
}: TranslationFormProps) {
  const [timingMs, setTimingMs] = useState<number | null>(null);

  const { object, isLoading, error, generate } = useGenerateObject({
    schema: translationSchema,
    prompt: `Generate translations for the field name "${fieldName}". Provide natural, contextually appropriate translations for UI labels.`,
    endpoint: '/api/generate-object',
    model: selectedModel,
    temperature: 0.3,
  });

  useEffect(() => {
    if (object && object.translations) {
      const suggestions: TranslationSuggestion[] = SUPPORTED_LANGUAGES.map(lang => ({
        language: lang.code,
        languageName: lang.name,
        flag: lang.flag,
        suggested: object.translations[lang.code] || '',
        current: translations[lang.code],
        status: 'pending' as const,
      }));
      onSuggestionsGenerated(suggestions);
    }
  }, [object, translations, onSuggestionsGenerated]);

  const handleGenerateSuggestions = async () => {
    if (!fieldName.trim()) return;

    const startTime = performance.now();
    await generate();
    const elapsed = performance.now() - startTime;
    setTimingMs(elapsed);
  };

  return (
    <div className="flex flex-col h-full">
      <div className="flex-shrink-0 px-6 py-4 border-b border-slate-200 bg-gradient-to-r from-green-50 to-emerald-50">
        <div className="flex items-center justify-between">
          <div>
            <h3 className="text-sm font-semibold text-slate-900">Translation Manager</h3>
            <p className="text-xs text-slate-500 mt-0.5">AI-powered translation suggestions</p>
          </div>
          {timingMs && (
            <div className="text-xs bg-white px-3 py-1.5 rounded-lg border border-green-200 shadow-sm">
              <span className="text-green-700 font-semibold">{(timingMs / 1000).toFixed(2)}s</span>
            </div>
          )}
        </div>
      </div>

      <div className="flex-1 overflow-auto p-6 space-y-6">
        {/* Field Name Input */}
        <div>
          <label className="block text-sm font-medium text-slate-700 mb-2">
            Field Name / Label
          </label>
          <input
            type="text"
            value={fieldName}
            onChange={(e) => onFieldNameChange(e.target.value)}
            placeholder="e.g., First Name, Email Address, Submit Button"
            className="w-full px-4 py-3 border border-slate-300 rounded-lg focus:ring-2 focus:ring-green-500 focus:border-transparent text-sm"
          />
          <p className="text-xs text-slate-500 mt-1.5">
            Enter the field name or label you want to translate
          </p>
        </div>

        {/* AI Suggestions Button */}
        <div>
          <button
            onClick={handleGenerateSuggestions}
            disabled={!fieldName.trim() || isLoading}
            className="w-full bg-gradient-to-r from-green-600 to-emerald-600 text-white px-6 py-3 rounded-lg font-medium hover:from-green-700 hover:to-emerald-700 disabled:from-slate-300 disabled:to-slate-300 disabled:cursor-not-allowed transition-all shadow-lg shadow-green-500/25 flex items-center justify-center gap-2"
          >
            {isLoading ? (
              <>
                <svg className="animate-spin h-4 w-4" viewBox="0 0 24 24">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" fill="none" />
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
                </svg>
                Generating AI Suggestions...
              </>
            ) : (
              <>
                <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 10V3L4 14h7v7l9-11h-7z" />
                </svg>
                Get AI Suggestions
              </>
            )}
          </button>
          
          {/* Model Selector */}
          <div className="mt-3 flex items-center justify-between">
            <label className="text-xs text-slate-600">Model:</label>
            <select
              value={selectedModel}
              onChange={(e) => onModelChange(e.target.value)}
              disabled={isLoading}
              className="text-xs px-3 py-1.5 border border-slate-300 rounded-lg focus:ring-2 focus:ring-green-500 bg-white disabled:opacity-50"
            >
              {models.map(model => (
                <option key={model.id} value={model.id}>{model.name}</option>
              ))}
            </select>
          </div>
        </div>

        {/* Error Display */}
        {error && (
          <div className="bg-red-50 border border-red-200 rounded-lg p-3">
            <div className="flex items-start gap-2">
              <svg className="w-5 h-5 text-red-600 flex-shrink-0 mt-0.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
              <div>
                <h4 className="text-red-800 font-semibold text-sm">Error</h4>
                <p className="text-red-600 text-xs mt-1">{error.message}</p>
              </div>
            </div>
          </div>
        )}

        {/* Current Translations */}
        <div>
          <h4 className="text-sm font-semibold text-slate-700 mb-3 flex items-center gap-2">
            <svg className="w-4 h-4 text-slate-500" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 5h12M9 3v2m1.048 9.5A18.022 18.022 0 016.412 9m6.088 9h7M11 21l5-10 5 10M12.751 5C11.783 10.77 8.07 15.61 3 18.129" />
            </svg>
            Current Translations
          </h4>
          <div className="space-y-2 max-h-[400px] overflow-y-auto pr-2">
            {SUPPORTED_LANGUAGES.map(lang => (
              <div key={lang.code} className="flex items-center gap-3 bg-slate-50 p-3 rounded-lg border border-slate-200">
                <span className="text-2xl">{lang.flag}</span>
                <div className="flex-1">
                  <label className="block text-xs font-medium text-slate-600 mb-1">
                    {lang.name}
                  </label>
                  <input
                    type="text"
                    value={translations[lang.code]}
                    onChange={(e) => onTranslationChange(lang.code, e.target.value)}
                    placeholder={`${lang.name} translation...`}
                    className="w-full px-3 py-1.5 text-sm border border-slate-300 rounded focus:ring-2 focus:ring-green-500 focus:border-transparent"
                  />
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}

