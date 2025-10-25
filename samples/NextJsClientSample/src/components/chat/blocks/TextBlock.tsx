/**
 * Text Block
 * Simple text content
 */

import type { ContentBlock } from '@openrouter-dotnet/react';
import { Markdown } from '../Markdown';

interface TextBlockProps {
  block: ContentBlock & { type: 'text' };
  isUserMessage: boolean;
}

export function TextBlock({ block, isUserMessage }: TextBlockProps) {
  return (
    isUserMessage ? (
      <div className="whitespace-pre-wrap leading-7 text-[15px] text-white">{block.content}</div>
    ) : (
      <Markdown>{block.content}</Markdown>
    )
  );
}
