/**
 * Model Picker
 * Searchable dropdown for selecting AI models
 */

'use client';

import { useState, useEffect, useRef } from 'react';

type Model = { id: string; name: string };

interface ModelPickerProps {
  models: Model[];
  selectedModel: string;
  onSelect: (modelId: string) => void;
  disabled?: boolean;
  variant?: 'default' | 'inline';
  size?: 'sm' | 'md';
}

export function ModelPicker({ models = [], selectedModel, onSelect, disabled, variant = 'default', size = 'sm' }: ModelPickerProps) {
  const [isOpen, setIsOpen] = useState(false);
  const [search, setSearch] = useState('');
  const dropdownRef = useRef<HTMLDivElement>(null);

  // Sort models alphabetically by name
  const sortedModels = [...models].sort((a, b) => a.name.localeCompare(b.name));

  // Filter models based on search
  const filteredModels = search
    ? sortedModels.filter((model) =>
        model.name.toLowerCase().includes(search.toLowerCase())
      )
    : sortedModels;

  // Find selected model info
  const selectedModelInfo = models.find((m) => m.id === selectedModel);

  // Close dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsOpen(false);
        setSearch('');
      }
    };

    if (isOpen) {
      document.addEventListener('mousedown', handleClickOutside);
    }

    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, [isOpen]);

  const handleSelect = (modelId: string) => {
    onSelect(modelId);
    setIsOpen(false);
    setSearch('');
  };

  const inlineSizeClass = size === 'md' ? 'px-3 py-2 text-sm' : 'px-2.5 py-1.5 text-xs';
  const triggerBase =
    variant === 'inline'
      ? `${inlineSizeClass} rounded-full bg-slate-100 text-slate-700 hover:bg-slate-200 border border-slate-200`
      : 'px-3 py-2 text-sm text-gray-600 hover:text-gray-900 hover:bg-gray-50 rounded-lg transition-all disabled:opacity-50 disabled:cursor-not-allowed border border-transparent hover:border-gray-200';

  return (
    <div className="relative" ref={dropdownRef}>
      {/* Trigger Button */}
      <button
        onClick={() => !disabled && setIsOpen(!isOpen)}
        disabled={disabled}
        className={triggerBase}
      >
        {selectedModelInfo?.name || 'Select Model'}
      </button>

      {/* Dropdown */}
      {isOpen && (
        <div className={`absolute ${variant === 'inline' ? 'bottom-full mb-2 left-0' : 'bottom-full mb-2 left-0'} w-96 bg-white border border-gray-200 rounded-lg shadow-lg overflow-hidden z-50`}>
          {/* Search Input */}
          <div className="p-2 border-b border-gray-200">
            <input
              type="text"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Search models..."
              className="w-full px-3 py-1.5 text-sm bg-white text-gray-900 border border-gray-200 rounded focus:outline-none focus:ring-1 focus:ring-blue-500 focus:border-blue-500"
              autoFocus
            />
          </div>

          {/* Model List */}
          <div className="max-h-80 overflow-y-auto">
            {filteredModels.length === 0 ? (
              <div className="p-4 text-sm text-gray-500 text-center">
                No models found
              </div>
            ) : (
              filteredModels.map((model) => (
                <button
                  key={model.id}
                  onClick={() => handleSelect(model.id)}
                  className={`w-full px-3 py-2 text-left text-sm hover:bg-gray-50 transition-colors ${
                    model.id === selectedModel
                      ? 'bg-blue-50 text-blue-900'
                      : 'text-gray-700'
                  }`}
                >
                  {model.name}
                </button>
              ))
            )}
          </div>
        </div>
      )}
    </div>
  );
}
