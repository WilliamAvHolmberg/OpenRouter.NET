'use client';

import { useState } from 'react';
import { useOpenRouterChat, ToolClientEvent } from '@openrouter-dotnet/react';
import { MessageList } from '../chat/MessageList';
import { ChatInput } from '../chat/ChatInput';
import type { DashboardWidgetData } from './DashboardWidget';

interface DashboardAssistantProps {
  selectedModel: string;
  onModelChange: (modelId: string) => void;
  onAddWidget: (widget: Omit<DashboardWidgetData, 'createdAt'>) => void;
  onRemoveWidget: (id: string) => void;
  onUpdateWidget: (id: string, updates: Partial<DashboardWidgetData>) => void;
}

const SYSTEM_PROMPT = `You are a dashboard builder assistant. You help users create data visualizations from a SQLite database containing e-commerce data.

**Database Schema:**
- orders: id, customer_id, product_id, amount, quantity, status ('completed'|'pending'|'cancelled'), created_at, delivered_at
- customers: id, name, segment ('enterprise'|'smb'|'individual'), country, lifetime_value
- products: id, name, category, price, cost

**How to create widgets:**

1. Generate an artifact with language "tsx.reactrunner" containing BOTH the SQL query and React component:
   - Define SQL as a const at the top: \`const SQL_QUERY = \\\`SELECT ...\\\`;\`
   - Create component that uses \`useDatabase()\` hook or \`db\` directly
   - Component executes SQL in useEffect and sets state
   - Component renders visualization with the data

2. Then use add_widget_to_dashboard tool to add it to the canvas

**Available in component scope:**
- Database: \`useDatabase()\` hook returns db, or use \`db\` directly
- Execute queries: \`db.exec(SQL_QUERY)\` returns array of results
- Parse results: \`result[0]?.values\` gives you rows as arrays, map to objects
- All Recharts components: BarChart, LineChart, PieChart, AreaChart, RadarChart, etc.
- ResponsiveContainer: ALWAYS wrap charts in this for responsive sizing
- COLORS array: Pre-defined color palette for consistent charts
- All React hooks: useState, useEffect, useMemo, etc.
- Tailwind CSS for styling

**Widget patterns:**

Metric Card:
\`\`\`
const SQL = \\\`SELECT COUNT(*) as total FROM orders\\\`;
function Widget() {
  const db = useDatabase();
  const [value, setValue] = useState(0);
  useEffect(() => {
    if (!db) return;
    const result = db.exec(SQL);
    setValue(result[0]?.values[0]?.[0] || 0);
  }, []);
  return (
    <div className="text-center p-8">
      <div className="text-4xl font-bold text-blue-600">{value}</div>
      <div className="text-sm text-slate-500 mt-2">Total Orders</div>
    </div>
  );
}
\`\`\`

Chart Widget:
\`\`\`
const SQL = \\\`SELECT category, COUNT(*) as count FROM products GROUP BY category\\\`;
function Widget() {
  const db = useDatabase();
  const [data, setData] = useState([]);
  useEffect(() => {
    if (!db) return;
    const result = db.exec(SQL);
    const rows = result[0]?.values.map(([category, count]) => ({ category, count })) || [];
    setData(rows);
  }, []);
  return (
    <ResponsiveContainer width="100%" height={300}>
      <BarChart data={data}>
        <CartesianGrid strokeDasharray="3 3" />
        <XAxis dataKey="category" />
        <YAxis />
        <Tooltip />
        <Bar dataKey="count" fill={COLORS[0]} />
      </BarChart>
    </ResponsiveContainer>
  );
}
\`\`\`

**Widget sizes:**
- "small" - Use for metric cards
- "medium" - Default, use for standard charts
- "large" - Use for tables or complex visualizations (spans 2 columns)

**Important:**
- Always handle null/undefined database gracefully
- Always handle empty data results
- Keep components simple and focused
- Use COLORS array for consistent styling`;

export function DashboardAssistant({
  selectedModel,
  onModelChange,
  onAddWidget,
  onRemoveWidget,
  onUpdateWidget
}: DashboardAssistantProps) {
  const [input, setInput] = useState('');

  const { state, actions } = useOpenRouterChat({
    baseUrl: '/api',
    defaultModel: selectedModel,
    systemPrompt: SYSTEM_PROMPT,
    config: { 
      debug: true,
      temperature: 0.7
    },
    onClientTool: (event: ToolClientEvent) => {
      try {
        const args = JSON.parse(event.arguments);

        if (event.toolName === 'add_widget_to_dashboard') {
          const { widgetId, title, code, size = 'medium' } = args;
          onAddWidget({
            id: widgetId,
            title,
            code,
            size
          });
          return { 
            success: true, 
            message: `Widget "${title}" added to dashboard` 
          };
        }

        if (event.toolName === 'update_widget') {
          const { widgetId, title, code } = args;
          const updates: Partial<DashboardWidgetData> = {};
          if (title) updates.title = title;
          if (code) updates.code = code;
          onUpdateWidget(widgetId, updates);
          return { 
            success: true, 
            message: `Widget updated` 
          };
        }

        if (event.toolName === 'remove_widget') {
          const { widgetId } = args;
          onRemoveWidget(widgetId);
          return { 
            success: true, 
            message: `Widget removed from dashboard` 
          };
        }

        return { success: false, message: 'Unknown tool' };
      } catch (err) {
        console.error('Client tool error:', err);
        return { success: false, message: String(err) };
      }
    },
  } as any);

  const handleSend = async () => {
    if (!input.trim() || state.isStreaming) return;
    await (actions.sendMessage as any)(input, { model: selectedModel });
    setInput('');
  };

  return (
    <div className="flex flex-col h-full">
      <div className="flex-shrink-0 px-6 py-4 border-b border-slate-200">
        <h3 className="text-sm font-semibold text-slate-900">Dashboard Builder AI</h3>
        <p className="text-xs text-slate-500 mt-0.5">Ask me to create data visualizations</p>
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
          models={[]}
          selectedModel={selectedModel}
          onModelChange={onModelChange}
          variant="inline"
        />
      </div>
    </div>
  );
}
