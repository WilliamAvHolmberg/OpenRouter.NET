/**
 * Message Component
 * Renders a single message with all its content blocks in order
 */

import type { ChatMessage, ContentBlock } from '@openrouter-dotnet/react';
import { TextBlock } from './blocks/TextBlock';
import { ArtifactBlock } from './blocks/ArtifactBlock';
import { ToolCallBlock } from './blocks/ToolCallBlock';

interface MessageProps {
  message: ChatMessage;
  isLastMessage: boolean;
}

export function Message({ message, isLastMessage }: MessageProps) {
  const isUser = message.role === 'user';

  return (
    <div
      className={`flex ${isUser ? 'justify-end' : 'justify-start'} animate-in fade-in slide-in-from-bottom-2 duration-300`}
      style={{ minHeight: isLastMessage ? '70vh' : '0px' }}
    >
      <div className={`max-w-[85%] ${isUser ? '' : 'w-full'}`}>
        {/* User bubble or assistant plain container */}
        <div className={`${isUser ? 'rounded-2xl p-4 shadow-sm bg-indigo-600 text-white' : ''}`}>
          {/* Render blocks in order */}
          <div className={`space-y-4 ${isUser ? '' : ''}`}>
            {message.blocks.map((block) => (
              <BlockView key={block.id} block={block} isUserMessage={isUser} />
            ))}
          </div>

          {/* Streaming indicator - assistant only, subtle three dots */}
          {!isUser && message.isStreaming && (
            <div className="mt-2 flex items-center gap-1 text-indigo-600">
              <span className="w-1.5 h-1.5 rounded-full bg-indigo-500 animate-bounce" style={{ animationDelay: '0ms' }} />
              <span className="w-1.5 h-1.5 rounded-full bg-indigo-500 animate-bounce" style={{ animationDelay: '150ms' }} />
              <span className="w-1.5 h-1.5 rounded-full bg-indigo-500 animate-bounce" style={{ animationDelay: '300ms' }} />
            </div>
          )}

          {/* Completion info */}
          {message.completion && !isUser && (
            <div className="mt-2 text-xs text-slate-500">
              {message.completion.model} • {message.completion.finishReason}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

/**
 * Renders a single content block based on its type
 */
function BlockView({ block, isUserMessage }: { block: ContentBlock; isUserMessage: boolean }) {
  switch (block.type) {
    case 'text':
      return <TextBlock block={block} isUserMessage={isUserMessage} />;
    case 'artifact':
      return <ArtifactBlock block={block} />;
    case 'tool_call':
      return <ToolCallBlock block={block} />;
    default:
      return null;
  }
}
