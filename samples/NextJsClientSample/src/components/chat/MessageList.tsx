/**
 * Message List
 * Scrollable container for all messages with smooth scroll after user sends
 */

'use client';

import { useRef, useEffect } from 'react';
import type { ChatMessage } from '@openrouter-dotnet/react';
import { Message } from './Message';
import { EmptyState } from './EmptyState';
import { ErrorDisplay } from './ErrorDisplay';

interface MessageListProps {
  messages: ChatMessage[];
  error: any;
}

export function MessageList({ messages, error }: MessageListProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const prevMessagesLengthRef = useRef(0);

  // Smooth scroll to bottom when user sends a message (makes room for AI response)
  useEffect(() => {
    const container = containerRef.current;
    if (!container) return;

    // Only scroll when a new message is added (user just sent)
    if (messages.length > prevMessagesLengthRef.current) {
      setTimeout(() => {
      container.scrollTo({
          top: container.scrollHeight,
          behavior: 'smooth',
        });
      });
    }

    prevMessagesLengthRef.current = messages.length;
  }, [messages.length]);

  return (
    <div ref={containerRef} className="h-full overflow-y-auto px-6 py-6 pb-24 space-y-6">
      {messages.length === 0 && <EmptyState />}

      {messages.map((message, index) => (
        <Message
          key={message.id}
          message={message}
          isLastMessage={index === messages.length - 1}
        />
      ))}

      {error && <ErrorDisplay error={error} />}
    </div>
  );
}
