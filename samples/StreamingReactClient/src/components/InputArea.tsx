import { useState } from 'react';
import { ModelSelector, type Model } from './ModelSelector';

interface InputAreaProps {
  onSend: (message: string) => void;
  disabled: boolean;
  models: Model[];
  selectedModel: string;
  onSelectModel: (model: string) => void;
  isLoadingModels: boolean;
}

export function InputArea({
  onSend,
  disabled,
  models,
  selectedModel,
  onSelectModel,
  isLoadingModels,
}: InputAreaProps) {
  const [input, setInput] = useState('');

  const handleSend = () => {
    if (!input.trim() || disabled) return;
    onSend(input.trim());
    setInput('');
  };

  const handleKeyPress = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  };

  return (
    <div className="input-container">
      <ModelSelector
        models={models}
        selectedModel={selectedModel}
        onSelectModel={onSelectModel}
        disabled={disabled || isLoadingModels}
      />
      <input
        type="text"
        value={input}
        onChange={(e) => setInput(e.target.value)}
        onKeyPress={handleKeyPress}
        placeholder="Ask me anything... Try: 'What's 25 + 17?' or 'Create a Python hello world'"
        disabled={disabled}
      />
      <button onClick={handleSend} disabled={disabled || !input.trim()}>
        {disabled ? '⏳' : '📤'} Send
      </button>
    </div>
  );
}
