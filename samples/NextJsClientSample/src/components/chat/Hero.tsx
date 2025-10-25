/**
 * Hero
 * Landing view with large centered input and suggested prompts
 */

'use client';

import type { Model } from '@openrouter-dotnet/react';
import { ChatInput } from './ChatInput';

interface HeroProps {
  input: string;
  onChange: (value: string) => void;
  onSend: () => void;
  isStreaming: boolean;
  models: Model[];
  selectedModel: string;
  onModelChange: (modelId: string) => void;
  onUsePrompt: (text: string) => void;
}

export function Hero({
  input,
  onChange,
  onSend,
  isStreaming,
  models,
  selectedModel,
  onModelChange,
  onUsePrompt,
}: HeroProps) {
  const prompts = [
    'Generate a product PRD for a new AI feature',
    'Create a migration plan from OpenAI to OpenRouter',
    'Draft a streaming chat UI in React + Tailwind',
    'Design a tool call for fetching GitHub issues',
  ];

  return (
    <div className="flex-1 flex flex-col items-center justify-center px-4">
      <div className="text-center mb-6">
        <h1 className="text-3xl font-bold text-slate-900">What do you want to do?</h1>
        <p className="text-slate-600 mt-2">Ask anything. Pick a model. Hit Enter.</p>
      </div>

      <div className="w-full max-w-3xl">
        <ChatInput
          value={input}
          onChange={onChange}
          onSend={onSend}
          isStreaming={isStreaming}
          models={models}
          selectedModel={selectedModel}
          onModelChange={onModelChange}
          variant="hero"
          placeholder="What do you want to do?"
        />
      </div>

      <div className="mt-6 grid grid-cols-1 sm:grid-cols-2 gap-3 w-full max-w-3xl">
        {prompts.map((p) => (
          <button
            key={p}
            onClick={() => onUsePrompt(p)}
            className="text-left px-4 py-3 rounded-xl border border-slate-200 bg-white shadow-sm hover:shadow transition-all text-slate-700 hover:text-slate-900"
          >
            {p}
          </button>
        ))}
      </div>
    </div>
  );
}


