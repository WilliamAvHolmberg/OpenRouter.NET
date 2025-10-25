/**
 * Chat Input
 * Fixed bottom input area with model picker - minimal Cursor-like design
 */

'use client';

import { useRef, forwardRef, useImperativeHandle } from 'react';
import type { Model } from '@openrouter-dotnet/react';
import { ModelPicker } from './ModelPicker';

interface ChatInputProps {
  value: string;
  onChange: (value: string) => void;
  onSend: () => void;
  isStreaming: boolean;
  models: Model[];
  selectedModel: string;
  onModelChange: (modelId: string) => void;
  variant?: 'inline' | 'hero';
  placeholder?: string;
}

export interface ChatInputRef {
  focus: () => void;
}

export const ChatInput = forwardRef<ChatInputRef, ChatInputProps>(function ChatInput(
  {
    value,
    onChange,
    onSend,
    isStreaming,
    models,
    selectedModel,
    onModelChange,
    variant = 'inline',
    placeholder = 'Message...'
  },
  ref
) {
  const inputRef = useRef<HTMLInputElement>(null);

  // Expose focus method to parent
  useImperativeHandle(ref, () => ({
    focus: () => {
      inputRef.current?.focus();
    },
  }));

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      onSend();
    }
  };

  if (variant === 'hero') {
    return (
      <div className="w-full">
        <div className="max-w-2xl mx-auto px-4">
          <div className="w-full rounded-full border border-gray-200 bg-white/95 shadow-xl px-3 py-3 flex items-center gap-3">
            {/* Model Picker - inline pill */}
            <ModelPicker
              models={models}
              selectedModel={selectedModel}
              onSelect={onModelChange}
              disabled={isStreaming}
              variant="inline"
              size="md"
            />

            {/* Divider */}
            <div className="h-7 w-px bg-gray-200" />

            {/* Input */}
            <input
              autoFocus
              ref={inputRef}
              type="text"
              value={value}
              onChange={(e) => onChange(e.target.value)}
              onKeyDown={handleKeyDown}
              placeholder={placeholder}
              className="flex-1 px-4 py-3 text-base bg-transparent focus:outline-none text-gray-900 placeholder:text-gray-400"
              disabled={isStreaming}
            />

            {/* Send/Stop Button */}
            <button
              onClick={onSend}
              disabled={isStreaming || !value.trim()}
              className={`inline-flex items-center gap-1.5 px-5 py-3 text-sm rounded-full transition-all shadow-sm ${
                isStreaming || !value.trim()
                  ? 'bg-gray-200 text-gray-400 cursor-not-allowed'
                  : 'bg-indigo-600 hover:bg-indigo-700 text-white'
              }`}
            >
              {isStreaming ? (
                <span className="flex items-center gap-1.5">
                  <svg className="animate-spin h-3.5 w-3.5" viewBox="0 0 24 24">
                    <circle
                      className="opacity-25"
                      cx="12"
                      cy="12"
                      r="10"
                      stroke="currentColor"
                      strokeWidth="4"
                      fill="none"
                    />
                    <path
                      className="opacity-75"
                      fill="currentColor"
                      d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
                    />
                  </svg>
                  Stop
                </span>
              ) : (
                'Send'
              )}
            </button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="fixed bottom-0 left-0 right-0 z-40">
      <div className="bg-gradient-to-t from-white/90 via-white/60 to-transparent backdrop-blur-md border-t border-gray-200">
        <div className="max-w-3xl mx-auto px-4 py-4">
          <div className="w-full rounded-full border border-gray-200 bg-white/95 shadow-md px-2 py-1.5 flex items-center gap-2">
            {/* Model Picker - inline pill */}
            <ModelPicker
              models={models}
              selectedModel={selectedModel}
              onSelect={onModelChange}
              disabled={isStreaming}
              variant="inline"
            />

            {/* Divider */}
            <div className="h-6 w-px bg-gray-200" />

            {/* Input */}
            <input
              ref={inputRef}
              type="text"
              value={value}
              onChange={(e) => onChange(e.target.value)}
              onKeyDown={handleKeyDown}
              placeholder={placeholder}
              className="flex-1 px-3 py-2 text-sm bg-transparent focus:outline-none text-gray-900 placeholder:text-gray-400"
              disabled={isStreaming}
            />

            {/* Send/Stop Button */}
            <button
              onClick={onSend}
              disabled={isStreaming || !value.trim()}
              className={`inline-flex items-center gap-1.5 px-4 py-2 text-sm rounded-full transition-all shadow-sm ${
                isStreaming || !value.trim()
                  ? 'bg-gray-200 text-gray-400 cursor-not-allowed'
                  : 'bg-indigo-600 hover:bg-indigo-700 text-white'
              }`}
            >
              {isStreaming ? (
                <span className="flex items-center gap-1.5">
                  <svg className="animate-spin h-3.5 w-3.5" viewBox="0 0 24 24">
                    <circle
                      className="opacity-25"
                      cx="12"
                      cy="12"
                      r="10"
                      stroke="currentColor"
                      strokeWidth="4"
                      fill="none"
                    />
                    <path
                      className="opacity-75"
                      fill="currentColor"
                      d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
                    />
                  </svg>
                  Stop
                </span>
              ) : (
                'Send'
              )}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
});