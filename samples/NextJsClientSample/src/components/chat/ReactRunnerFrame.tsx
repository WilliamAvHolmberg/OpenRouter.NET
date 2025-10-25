/**
 * ReactRunnerFrame - isolates preview styles in a shadow root to prevent bleed
 */

'use client';

import { useEffect, useRef } from 'react';

interface FrameProps {
  children: React.ReactNode;
}

export function ReactRunnerFrame({ children }: FrameProps) {
  const hostRef = useRef<HTMLDivElement>(null);
  const rootRef = useRef<ShadowRoot | null>(null);

  useEffect(() => {
    if (hostRef.current && !rootRef.current) {
      rootRef.current = hostRef.current.attachShadow({ mode: 'open' });
      const style = document.createElement('style');
      style.textContent = `:host{all:initial;display:block} *{box-sizing:border-box}`;
      rootRef.current.appendChild(style);
    }
  }, []);

  useEffect(() => {
    if (!rootRef.current) return;
    const mount = document.createElement('div');
    mount.className = 'p-0 m-0';
    rootRef.current.appendChild(mount);
    // Render children into mount via portal
    // Note: we avoid extra deps; simple assignment for now
    mount.appendChild(document.createElement('div'));
    mount.firstChild?.replaceWith((children as any));
    return () => {
      rootRef.current?.removeChild(mount);
    };
  }, [children]);

  return <div ref={hostRef} />;
}


