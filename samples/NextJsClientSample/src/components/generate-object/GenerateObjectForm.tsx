'use client';

import { useState, useEffect } from 'react';
import { useGenerateObject } from '@openrouter-dotnet/react';
import { ChatInput } from '../chat/ChatInput';
import { CustomSchemaBuilder } from './CustomSchemaBuilder';
import { examples, useGenerateObjectContext } from './GenerateObjectContext';

interface GenerateObjectFormProps {
  selectedModel: string;
  onModelChange: (modelId: string) => void;
  models: { id: string; name: string }[];
}

export function GenerateObjectForm({ selectedModel, onModelChange, models }: GenerateObjectFormProps) {
  const { 
    selectedExample, 
    setSelectedExample, 
    setGeneratedObject, 
    setUsage,
    customSchema,
    setCustomSchema,
    setTimingMs
  } = useGenerateObjectContext();
  const [prompt, setPrompt] = useState(selectedExample.defaultPrompt);

  const currentSchema = selectedExample.type === 'custom' ? customSchema : selectedExample.schema;

  const { object, isLoading, error, usage, generate, reset } = useGenerateObject({
    schema: currentSchema || examples[0].schema!,
    prompt: prompt,
    endpoint: '/api/generate-object',
    model: selectedModel,
    temperature: 0.7,
  });

  useEffect(() => {
    if (object) {
      setGeneratedObject(object);
    }
  }, [object, setGeneratedObject]);

  useEffect(() => {
    if (usage) {
      setUsage(usage);
    }
  }, [usage, setUsage]);

  const handleExampleChange = (example: typeof examples[0]) => {
    setSelectedExample(example);
    setPrompt(example.defaultPrompt);
    reset();
    setGeneratedObject(null);
    setUsage(null);
    setTimingMs(null);
  };

  const handleGenerate = async () => {
    if (!prompt.trim() || isLoading) return;
    if (selectedExample.type === 'custom' && !customSchema) {
      return;
    }
    
    const startTime = performance.now();
    await generate(prompt);
    const elapsed = performance.now() - startTime;
    setTimingMs(elapsed);
  };

  return (
    <div className="flex flex-col h-full">
      <div className="flex-shrink-0 px-6 py-4 border-b border-slate-200">
        <h3 className="text-sm font-semibold text-slate-900">Generate Object</h3>
        <p className="text-xs text-slate-500 mt-0.5">Create structured data with validation</p>
      </div>

      <div className="flex-1 overflow-auto p-6 space-y-6">
        {/* Example Type Selection */}
        <div>
          <label className="block text-xs font-medium text-slate-700 mb-2 uppercase tracking-wide">
            Example Type
          </label>
          <div className="flex flex-col gap-2">
            {examples.map((example) => (
              <button
                key={example.type}
                onClick={() => handleExampleChange(example)}
                disabled={isLoading}
                className={`px-4 py-3 rounded-lg font-medium transition-all duration-200 text-left ${
                  selectedExample.type === example.type
                    ? 'bg-blue-600 text-white shadow-sm'
                    : 'bg-slate-50 text-slate-700 hover:bg-slate-100 disabled:opacity-50 disabled:cursor-not-allowed'
                }`}
              >
                <div className="font-semibold text-sm">{example.label}</div>
                <div className={`text-xs mt-0.5 ${selectedExample.type === example.type ? 'text-blue-100' : 'text-slate-500'}`}>
                  {example.description}
                </div>
              </button>
            ))}
          </div>
        </div>

        {/* Custom Schema Builder */}
        {selectedExample.type === 'custom' && (
          <div>
            <CustomSchemaBuilder onSchemaChange={setCustomSchema} />
            {!customSchema && (
              <p className="text-xs text-orange-600 mt-2 flex items-center gap-1">
                <svg className="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
                </svg>
                Add fields above to build your schema
              </p>
            )}
          </div>
        )}

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
      </div>

      {/* Bottom Input with Model Picker */}
      <div className="flex-shrink-0 p-4 border-t border-slate-200">
        <ChatInput
          value={prompt}
          onChange={setPrompt}
          onSend={handleGenerate}
          isStreaming={isLoading}
          models={models}
          selectedModel={selectedModel}
          onModelChange={onModelChange}
          variant="inline"
          placeholder="Describe what to generate..."
        />
      </div>
    </div>
  );
}

