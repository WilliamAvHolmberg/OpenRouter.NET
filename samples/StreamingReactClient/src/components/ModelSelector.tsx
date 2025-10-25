import { useState, useRef, useEffect } from 'react';

export interface Model {
  id: string;
  name: string;
  contextLength?: number;
  pricing?: {
    prompt: string;
    completion: string;
  };
}

interface ModelSelectorProps {
  models: Model[];
  selectedModel: string;
  onSelectModel: (modelId: string) => void;
  disabled?: boolean;
}

export function ModelSelector({ models, selectedModel, onSelectModel, disabled }: ModelSelectorProps) {
  const [isOpen, setIsOpen] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  const dropdownRef = useRef<HTMLDivElement>(null);
  const searchInputRef = useRef<HTMLInputElement>(null);

  const selectedModelData = models.find(m => m.id === selectedModel);

  const sortedModels = [...models].sort((a, b) => a.name.localeCompare(b.name));

  const filteredModels = sortedModels.filter(model => 
    model.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
    model.id.toLowerCase().includes(searchQuery.toLowerCase())
  );

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsOpen(false);
        setSearchQuery('');
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  useEffect(() => {
    if (isOpen && searchInputRef.current) {
      searchInputRef.current.focus();
    }
  }, [isOpen]);

  const handleSelectModel = (modelId: string) => {
    onSelectModel(modelId);
    setIsOpen(false);
    setSearchQuery('');
  };

  return (
    <div className="model-selector" ref={dropdownRef}>
      <button
        className="model-selector-trigger"
        onClick={() => setIsOpen(!isOpen)}
        disabled={disabled}
        type="button"
      >
        <span className="model-selector-icon">🤖</span>
        <span className="model-selector-text">
          {selectedModelData?.name || 'Select Model'}
        </span>
        <span className="model-selector-arrow">{isOpen ? '▲' : '▼'}</span>
      </button>

      {isOpen && (
        <div className="model-dropdown-inline">
          <div className="model-search-container">
            <input
              ref={searchInputRef}
              type="text"
              className="model-search-input"
              placeholder="Search models..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
            />
          </div>
          
          <div className="model-list-inline">
            {filteredModels.length === 0 ? (
              <div className="model-empty">No models found</div>
            ) : (
              filteredModels.map((model) => (
                <button
                  key={model.id}
                  className={`model-item-inline ${model.id === selectedModel ? 'selected' : ''}`}
                  onClick={() => handleSelectModel(model.id)}
                  type="button"
                >
                  <div className="model-item-main">
                    <span className="model-item-name">{model.name}</span>
                    {model.id === selectedModel && (
                      <span className="model-item-check">✓</span>
                    )}
                  </div>
                  <div className="model-item-meta">
                    {model.contextLength && (
                      <span className="model-item-context">
                        {(model.contextLength / 1000).toFixed(0)}K context
                      </span>
                    )}
                  </div>
                </button>
              ))
            )}
          </div>
        </div>
      )}
    </div>
  );
}

