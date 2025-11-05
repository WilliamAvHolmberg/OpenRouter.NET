'use client';

import { useState, useEffect } from 'react';
import { useOpenRouterModels } from '@openrouter-dotnet/react';
import { GeneratedObjectDisplay } from '@/components/generate-object/GeneratedObjectDisplay';
import { GenerateObjectForm } from '@/components/generate-object/GenerateObjectForm';
import { GenerateObjectProvider } from '@/components/generate-object/GenerateObjectContext';

const STORAGE_KEY = 'openrouter_generate_object_model';
const DEFAULT_MODEL = 'openai/gpt-4o-mini';

function GenerateObjectPageContent() {
  const [selectedModel, setSelectedModel] = useState<string>(() => {
    if (typeof window !== 'undefined') {
      return localStorage.getItem(STORAGE_KEY) || DEFAULT_MODEL;
    }
    return DEFAULT_MODEL;
  });

  const { models } = useOpenRouterModels('/api/models');

  useEffect(() => {
    if (typeof window !== 'undefined') {
      localStorage.setItem(STORAGE_KEY, selectedModel);
    }
  }, [selectedModel]);

  return (
    <div className="h-[calc(100vh-4rem)] flex flex-col bg-slate-50 relative overflow-hidden">
      <div className="absolute top-0 right-0 w-[500px] h-[500px] bg-purple-500/5 rounded-full blur-3xl" />

      <div className="flex-1 p-6 max-w-7xl mx-auto w-full overflow-hidden relative z-10">
        <div className="grid grid-cols-1 lg:grid-cols-5 gap-6 h-full">
          {/* Results Section */}
          <div className="lg:col-span-3 flex flex-col overflow-hidden">
            <div className="rounded-xl border border-slate-200 bg-white shadow-sm p-6 flex flex-col h-full overflow-hidden">
              <GeneratedObjectDisplay />
            </div>
          </div>

          {/* Form Section */}
          <div className="lg:col-span-2 flex flex-col overflow-hidden">
            <div className="rounded-xl border border-slate-200 bg-white shadow-sm flex-1 overflow-hidden flex flex-col">
              <GenerateObjectForm
                selectedModel={selectedModel}
                onModelChange={setSelectedModel}
                models={models || []}
              />
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

export default function GenerateObjectPage() {
  return (
    <GenerateObjectProvider>
      <GenerateObjectPageContent />
    </GenerateObjectProvider>
  );
}

