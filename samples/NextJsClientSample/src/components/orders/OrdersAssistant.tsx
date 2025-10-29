'use client';

import { useState } from 'react';
import { useOpenRouterChat, useOpenRouterModels, ToolClientEvent } from '@openrouter-dotnet/react';
import type { OrderFilters } from '../../lib/orderFilters';
import { MessageList } from '../chat/MessageList';
import { ChatInput } from '../chat/ChatInput';

export function OrdersAssistant({
  onApplyFilters,
  selectedModel,
  onModelChange,
}: {
  onApplyFilters: (f: OrderFilters) => void;
  selectedModel: string;
  onModelChange: (modelId: string) => void;
}) {
  const [input, setInput] = useState('');

  const { state, actions } = useOpenRouterChat({
    endpoints: {
      stream: '/api/stream',
      clearConversation: '/api/conversation',
    },
    defaultModel: selectedModel,
    config: { debug: true },
    onClientTool: (event: ToolClientEvent) => {
      if (event.toolName === 'set_order_filters') {
        try {
          const raw = JSON.parse(event.arguments);
          const normalized = normalizeFiltersPayload(raw);
          onApplyFilters(normalized);
        } catch {}
      }
    },
  } as any);

  const { models } = useOpenRouterModels('/api/models');

  const handleSend = async () => {
    if (!input.trim() || state.isStreaming) return;
    await (actions.sendMessage as any)(input, { model: selectedModel });
    setInput('');
  };

  return (
    <div className="flex flex-col h-full">
      <div className="flex-shrink-0 px-6 py-4 border-b border-slate-200">
        <h3 className="text-sm font-semibold text-slate-900">AI Assistant</h3>
        <p className="text-xs text-slate-500 mt-0.5">Ask me to filter orders</p>
      </div>

      <div className="flex-1 overflow-hidden min-h-0">
        <MessageList messages={state.messages} error={state.error} lastMessageMinHeight="55vh" />
      </div>

      <div className="flex-shrink-0 p-4 border-t border-slate-200">
        <ChatInput
          value={input}
          onChange={setInput}
          onSend={handleSend}
          isStreaming={state.isStreaming}
          models={models}
          selectedModel={selectedModel}
          onModelChange={onModelChange}
          variant="inline"
        />
      </div>
    </div>
  );
}

function normalizeFiltersPayload(input: any): OrderFilters {
  const src = input && typeof input === 'object' && input.filters ? input.filters : input;
  const out: OrderFilters = {};
  if (src == null || typeof src !== 'object') return out;

  const coerceBool = (v: any) => (typeof v === 'boolean' ? v : typeof v === 'string' ? v.toLowerCase() === 'true' : undefined);
  const coerceNum = (v: any) => (typeof v === 'number' ? v : typeof v === 'string' ? (v.trim() === '' ? undefined : Number(v)) : undefined);
  const coerceArr = (v: any) => (Array.isArray(v) ? v : typeof v === 'string' && v.trim() ? [v] : undefined);

  out.status = coerceArr(src.status);
  const delivered = coerceBool(src.delivered);
  if (typeof delivered === 'boolean') out.delivered = delivered;
  out.customerIds = coerceArr(src.customerIds);
  const minAmount = coerceNum(src.minAmount);
  if (typeof minAmount === 'number' && !Number.isNaN(minAmount)) out.minAmount = minAmount;
  const maxAmount = coerceNum(src.maxAmount);
  if (typeof maxAmount === 'number' && !Number.isNaN(maxAmount)) out.maxAmount = maxAmount;
  if (typeof src.createdFrom === 'string') out.createdFrom = src.createdFrom;
  if (typeof src.createdTo === 'string') out.createdTo = src.createdTo;
  if (typeof src.deliveredFrom === 'string') out.deliveredFrom = src.deliveredFrom;
  if (typeof src.deliveredTo === 'string') out.deliveredTo = src.deliveredTo;
  if (typeof src.text === 'string') out.text = src.text;
  out.tags = coerceArr(src.tags);

  return out;
}


