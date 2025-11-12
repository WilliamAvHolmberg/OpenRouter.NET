/**
 * Stateless Chat with Client-Side History
 *
 * Demonstrates localStorage-based history management for production-ready,
 * stateless chat applications with zero server memory usage.
 */

"use client";

import { useState, useEffect } from "react";
import {
  useOpenRouterChat,
  saveHistory,
  loadHistory,
  clearHistory,
  listConversations,
  type ChatMessage,
} from "@openrouter-dotnet/react";
import { MessageList } from "@/components/chat/MessageList";
import { ChatInput } from "@/components/chat/ChatInput";

const DEFAULT_MODEL = "anthropic/claude-3.5-sonnet";
const CURRENT_CONV_KEY = "stateless_chat_current_conversation";

export function StatelessChatInterface() {
  const [input, setInput] = useState("");
  const [conversationId, setConversationId] = useState<string>(() => {
    if (typeof window !== "undefined") {
      return localStorage.getItem(CURRENT_CONV_KEY) || `conv_${Date.now()}`;
    }
    return `conv_${Date.now()}`;
  });

  // Initialize hook WITHOUT server-side history
  const { state, actions } = useOpenRouterChat({
    endpoints: {
      stream: "/api/stream-stateless", // New stateless endpoint
    },
    defaultModel: DEFAULT_MODEL,
    onClientTool: (event) => {
      console.log("🔧 [CLIENT TOOL] Tool called:", event.toolName);
      console.log("🔧 [CLIENT TOOL] Arguments:", event.arguments);
      
      // Handle client-side tools
      if (event.toolName === "get_current_time") {
        const now = new Date();
        const timeString = now.toLocaleTimeString();
        console.log(`⏰ [CLIENT TOOL] Current time: ${timeString}`);
        // Note: The tool result will be automatically added to history
        // via the mocked tool_completed event from the backend
      }
    },
  });

  // Load history from localStorage on mount and when conversationId changes
  useEffect(() => {
    if (typeof window === "undefined") return;

    const savedHistory = loadHistory(conversationId);
    
    // Populate the hook's state with loaded history
    actions.setMessages(savedHistory);

    // Save current conversation ID
    localStorage.setItem(CURRENT_CONV_KEY, conversationId);
  }, [conversationId]);

  // Auto-save history whenever state.messages changes
  useEffect(() => {
    if (typeof window === "undefined") return;
    if (state.messages.length > 0) {
      saveHistory(conversationId, state.messages, {
        maxMessages: 100, // Limit to prevent quota issues
      });
    }
  }, [state.messages, conversationId]);

  const handleSend = async () => {
    if (!input.trim() || state.isStreaming) return;

    await actions.sendMessage(input, {
      model: DEFAULT_MODEL,
      history: state.messages,
    });

    setInput("");
  };

  const handleNewConversation = () => {
    const newId = `conv_${Date.now()}`;
    setConversationId(newId);
    // Clear the hook's internal state
    actions.clearConversation();
  };

  const handleClearHistory = () => {
    clearHistory(conversationId);
    actions.clearConversation();
  };

  const conversationList =
    typeof window !== "undefined" ? listConversations() : [];

  return (
    <div className="flex h-screen bg-gradient-to-br from-purple-50 via-slate-50 to-white">
      {/* Sidebar with conversation list */}
      <div className="w-64 bg-white border-r border-gray-200 p-4 overflow-y-auto">
        <div className="mb-4">
          <h2 className="text-lg font-semibold text-gray-800 mb-2">
            Stateless Chat
          </h2>
          <p className="text-xs text-gray-600 mb-4">
            Client-side history with localStorage
          </p>
          <button
            onClick={handleNewConversation}
            className="w-full px-4 py-2 bg-purple-600 text-white rounded-lg hover:bg-purple-700 transition-colors text-sm font-medium"
          >
            + New Conversation
          </button>
        </div>

        <div className="space-y-2">
          <h3 className="text-xs font-semibold text-gray-500 uppercase tracking-wider">
            Conversations ({conversationList.length})
          </h3>
          {conversationList.map((id) => (
            <button
              key={id}
              onClick={() => setConversationId(id)}
              className={`w-full text-left px-3 py-2 rounded-lg text-sm transition-colors ${
                id === conversationId
                  ? "bg-purple-100 text-purple-900 font-medium"
                  : "text-gray-700 hover:bg-gray-100"
              }`}
            >
              {id.replace("openrouter_chat_", "").slice(0, 20)}...
            </button>
          ))}
        </div>

        {state.messages.length > 0 && (
          <div className="mt-6 pt-4 border-t border-gray-200">
            <button
              onClick={handleClearHistory}
              className="w-full px-3 py-2 text-sm text-red-600 hover:bg-red-50 rounded-lg transition-colors"
            >
              Clear This Conversation
            </button>
          </div>
        )}

        <div className="mt-6 pt-4 border-t border-gray-200">
          <div className="text-xs text-gray-600 space-y-2">
            <div className="flex justify-between">
              <span>Messages:</span>
              <span className="font-medium">{state.messages.length}</span>
            </div>
            <div className="flex justify-between">
              <span>Storage:</span>
              <span className="font-medium text-green-600">localStorage</span>
            </div>
            <div className="flex justify-between">
              <span>Server Memory:</span>
              <span className="font-medium text-green-600">0 KB</span>
            </div>
          </div>
        </div>
      </div>

      {/* Main chat area */}
      <div className="flex-1 flex flex-col">
        {/* Header */}
        <div className="bg-white border-b border-gray-200 px-6 py-4">
          <h1 className="text-xl font-bold text-gray-900">
            Stateless Chat Demo
          </h1>
          <p className="text-sm text-gray-600 mt-1">
            Zero server memory • localStorage persistence • Horizontally
            scalable
          </p>
        </div>

        {/* Messages */}
        <div className="flex-1 overflow-y-auto">
          {state.messages.length === 0 ? (
            <div className="h-full flex items-center justify-center">
              <div className="text-center max-w-md px-4">
                <div className="w-16 h-16 bg-purple-100 rounded-full flex items-center justify-center mx-auto mb-4">
                  <svg
                    className="w-8 h-8 text-purple-600"
                    fill="none"
                    viewBox="0 0 24 24"
                    stroke="currentColor"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z"
                    />
                  </svg>
                </div>
                <h2 className="text-2xl font-bold text-gray-900 mb-2">
                  Start a Stateless Conversation
                </h2>
                <p className="text-gray-600 mb-4">
                  This demo uses <strong>client-side history</strong> stored in
                  localStorage. The server maintains zero state between
                  requests.
                </p>
                <div className="bg-purple-50 border border-purple-200 rounded-lg p-4 text-left">
                  <h3 className="font-semibold text-purple-900 mb-2">
                    Benefits:
                  </h3>
                  <ul className="text-sm text-purple-800 space-y-1">
                    <li>✓ No server memory usage</li>
                    <li>✓ Survives server restarts</li>
                    <li>✓ Horizontally scalable</li>
                    <li>✓ No session affinity needed</li>
                  </ul>
                </div>
              </div>
            </div>
          ) : (
            <MessageList
              messages={state.messages}
              error={state.error}
            />
          )}
        </div>

        {/* Input */}
        <div className="border-t border-gray-200 bg-white p-4">
          <ChatInput
            value={input}
            isStreaming={state.isStreaming}
            models={[]}
            selectedModel={DEFAULT_MODEL}
            onModelChange={() => {}}
            onChange={setInput}
            onSend={handleSend}
            placeholder="Type your message... (stored in localStorage)"
          />
        </div>
      </div>
    </div>
  );
}
