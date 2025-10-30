'use client';

import { useState, useEffect } from 'react';
import { useOpenRouterModels } from '@openrouter-dotnet/react';
import { TranslationForm } from '@/components/translations/TranslationForm';
import { TranslationReview } from '@/components/translations/TranslationReview';
import { SUPPORTED_LANGUAGES, type TranslationSuggestion, type LanguageCode } from '@/lib/translationTypes';

const STORAGE_KEY_MODEL = 'openrouter_translation_model';
const STORAGE_KEY_FIELDS = 'translation_fields';
const DEFAULT_MODEL = 'openai/gpt-4o-mini';

export default function TranslationsPage() {
  const [selectedModel, setSelectedModel] = useState<string>(() => {
    if (typeof window !== 'undefined') {
      return localStorage.getItem(STORAGE_KEY_MODEL) || DEFAULT_MODEL;
    }
    return DEFAULT_MODEL;
  });

  const [fieldName, setFieldName] = useState('');
  const [translations, setTranslations] = useState<Record<LanguageCode, string>>(() => {
    const empty: Record<string, string> = {};
    SUPPORTED_LANGUAGES.forEach(lang => {
      empty[lang.code] = '';
    });
    return empty as Record<LanguageCode, string>;
  });

  const [suggestions, setSuggestions] = useState<TranslationSuggestion[]>([]);
  const [showReview, setShowReview] = useState(false);

  const { models } = useOpenRouterModels('/api/models');

  useEffect(() => {
    if (typeof window !== 'undefined') {
      localStorage.setItem(STORAGE_KEY_MODEL, selectedModel);
    }
  }, [selectedModel]);

  useEffect(() => {
    if (typeof window !== 'undefined' && fieldName) {
      const saved = {
        fieldName,
        translations,
      };
      localStorage.setItem(STORAGE_KEY_FIELDS, JSON.stringify(saved));
    }
  }, [fieldName, translations]);

  const handleAcceptSuggestion = (languageCode: LanguageCode, value: string) => {
    setTranslations(prev => ({
      ...prev,
      [languageCode]: value,
    }));
    setSuggestions(prev =>
      prev.map(s => s.language === languageCode ? { ...s, status: 'accepted' as const } : s)
    );
  };

  const handleRejectSuggestion = (languageCode: LanguageCode) => {
    setSuggestions(prev =>
      prev.map(s => s.language === languageCode ? { ...s, status: 'rejected' as const } : s)
    );
  };

  const handleAcceptAll = () => {
    const newTranslations = { ...translations };
    suggestions.forEach(suggestion => {
      if (suggestion.status === 'pending') {
        newTranslations[suggestion.language] = suggestion.suggested;
      }
    });
    setTranslations(newTranslations);
    setSuggestions(prev => prev.map(s => ({ ...s, status: 'accepted' as const })));
  };

  const handleCloseReview = () => {
    setShowReview(false);
    setSuggestions([]);
  };

  return (
    <div className="h-[calc(100vh-4rem)] flex flex-col bg-slate-50 relative overflow-hidden">
      <div className="absolute top-0 left-0 w-[500px] h-[500px] bg-green-500/5 rounded-full blur-3xl" />

      <div className="flex-1 p-6 max-w-7xl mx-auto w-full overflow-hidden relative z-10">
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 h-full">
          {/* Form Section */}
          <div className="flex flex-col overflow-hidden">
            <div className="rounded-xl border border-slate-200 bg-white shadow-sm flex-1 overflow-hidden flex flex-col">
              <TranslationForm
                fieldName={fieldName}
                translations={translations}
                selectedModel={selectedModel}
                models={models || []}
                onFieldNameChange={setFieldName}
                onTranslationChange={(lang, value) =>
                  setTranslations(prev => ({ ...prev, [lang]: value }))
                }
                onModelChange={setSelectedModel}
                onSuggestionsGenerated={(suggestions) => {
                  setSuggestions(suggestions);
                  setShowReview(true);
                }}
              />
            </div>
          </div>

          {/* Review Section */}
          <div className="flex flex-col overflow-hidden">
            <div className="rounded-xl border border-slate-200 bg-white shadow-sm flex-1 overflow-hidden flex flex-col">
              <TranslationReview
                suggestions={suggestions}
                showReview={showReview}
                onAccept={handleAcceptSuggestion}
                onReject={handleRejectSuggestion}
                onAcceptAll={handleAcceptAll}
                onClose={handleCloseReview}
              />
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

