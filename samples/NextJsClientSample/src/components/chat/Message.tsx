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
  minHeight?: string;
}

export function Message({ message, isLastMessage, minHeight = '70vh' }: MessageProps) {
  const isUser = message.role === 'user';

  return (
    <div
      className={`flex ${isUser ? 'justify-end' : 'justify-start'} gap-3 animate-fade-in-up`}
      style={{ minHeight: isLastMessage ? minHeight : '0px' }}
    >
      {/* Avatar for AI only */}
      {!isUser && (
        <div className="flex-shrink-0">
          <div className="w-8 h-8 rounded-full bg-gradient-to-br from-blue-500 to-indigo-600 flex items-center justify-center text-white text-xs font-medium shadow-lg shadow-blue-500/30">
            AI
          </div>
        </div>
      )}

      <div className={`max-w-[85%] ${isUser ? '' : 'w-full'}`}>
        {/* User bubble or assistant plain container */}
        <div className={`${
          isUser
            ? 'rounded-2xl p-3 bg-gradient-to-br from-blue-600 to-indigo-600 text-white shadow-lg shadow-blue-500/20'
            : 'rounded-2xl p-3 bg-white/60 backdrop-blur-md border border-white/20 shadow-lg'
        }`}>
          {/* Render blocks in order */}
          <div className={`space-y-3`}>
            {message.blocks.map((block) => (
              <BlockView key={block.id} block={block} isUserMessage={isUser} />
            ))}
          </div>

          {/* Streaming indicator - assistant only */}
          {!isUser && message.isStreaming && (
            <div className="mt-2 flex items-center gap-1">
              <span className="w-1.5 h-1.5 rounded-full bg-blue-600 animate-bounce" style={{ animationDelay: '0ms' }} />
              <span className="w-1.5 h-1.5 rounded-full bg-blue-600 animate-bounce" style={{ animationDelay: '150ms' }} />
              <span className="w-1.5 h-1.5 rounded-full bg-blue-600 animate-bounce" style={{ animationDelay: '300ms' }} />
            </div>
          )}

          {/* Completion info */}
          {message.completion?.model && !isUser && (
            <div className="mt-2 pt-2 border-t border-slate-100 text-xs text-slate-400">
              {message.completion.model.split('/').pop()}
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
