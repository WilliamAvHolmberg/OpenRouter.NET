/**
 * Drawer context - inline right rail that pushes content
 */

'use client';

import { createContext, useContext, useState, useMemo, ReactNode } from 'react';

interface DrawerContextValue {
  open: boolean;
  width: number;
  content: ReactNode | null;
  setContent: (node: ReactNode, opts?: { width?: number }) => void;
  close: () => void;
  setWidth: (w: number) => void;
}

const DrawerContext = createContext<DrawerContextValue | null>(null);

export function DrawerProvider({ children }: { children: ReactNode }) {
  const [open, setOpen] = useState(false);
  const [width, setWidth] = useState(520);
  const [content, setContentState] = useState<ReactNode | null>(null);

  const api = useMemo<DrawerContextValue>(
    () => ({
      open,
      width,
      content,
      setContent: (node, opts) => {
        if (opts?.width) setWidth(opts.width);
        setContentState(node);
        setOpen(true);
      },
      close: () => {
        setOpen(false);
        setContentState(null);
      },
      setWidth,
    }),
    [open, width, content]
  );

  return <DrawerContext.Provider value={api}>{children}</DrawerContext.Provider>;
}

export function useDrawer() {
  const ctx = useContext(DrawerContext);
  return ctx;
}


