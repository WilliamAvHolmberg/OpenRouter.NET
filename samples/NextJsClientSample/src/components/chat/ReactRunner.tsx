/**
 * ReactRunner - minimal wrapper around react-runner
 * Scope: React only. Tailwind for styles.
 */

'use client';

import { useMemo } from 'react';
import { useRunner } from 'react-runner';
import * as React from 'react';

interface ReactRunnerProps {
  code: string;
}

function transformToModule(code: string): { program: string } {
  let transformed = code || '';

  // Strip filename lines like "Widget.tsx"
  transformed = transformed.replace(/^\s*[\w.-]+\.tsx\s*$/gim, '');

  // Remove import lines
  transformed = transformed.replace(/^\s*import[^;]*;?\s*$/gim, '');

  let defaultName: string | null = null;

  // export default function Name(...)
  transformed = transformed.replace(/export\s+default\s+function\s+([A-Za-z0-9_]+)\s*\(/, (_m, name) => {
    defaultName = String(name);
    return `function ${name}(`;
  });

  // export default class Name ...
  transformed = transformed.replace(/export\s+default\s+class\s+([A-Za-z0-9_]+)/, (_m, name) => {
    defaultName = String(name);
    return `class ${name}`;
  });

  // export default Identifier;
  const exportIdentMatch = transformed.match(/export\s+default\s+([A-Za-z0-9_]+)\s*;?/);
  if (exportIdentMatch) {
    defaultName = exportIdentMatch[1];
    transformed = transformed.replace(/export\s+default\s+[A-Za-z0-9_]+\s*;?/, '');
  }

  // export default ( ... ) or export default () => ...
  if (/export\s+default\s*\(?.*=>|export\s+default\s*\(/.test(transformed)) {
    transformed = transformed.replace(/export\s+default\s*/, 'const __DefaultExport__ = ');
    defaultName = '__DefaultExport__';
  }

  // Remove any remaining 'export default'
  transformed = transformed.replace(/export\s+default\s*/g, '');

  // Sanitize common full-screen / global positioning utilities to keep preview scoped
  const classFixes: Record<string, string> = {
    'min-h-screen': 'min-h-full',
    'h-screen': 'h-full',
    'w-screen': 'w-full',
    'fixed': 'absolute',
  };
  for (const [from, to] of Object.entries(classFixes)) {
    const re = new RegExp(`\\b${from}\\b`, 'g');
    transformed = transformed.replace(re, to);
  }

  // Ensure Component binding exists
  let footer = '';
  if (defaultName) {
    footer += `\nconst Component = ${defaultName};`;
  } else if (!/\bconst\s+Component\b|\bfunction\s+Component\b/.test(transformed)) {
    // Fallback: no default export; try to map common names
    const widgetMatch = /\bfunction\s+([A-Za-z0-9_]+)\s*\(/.exec(transformed) || /\bconst\s+([A-Za-z0-9_]+)\s*=\s*\(/.exec(transformed);
    if (widgetMatch) {
      footer += `\nconst Component = ${widgetMatch[1]};`;
    } else {
      footer += `\nconst Component = () => React.createElement('div', { className: 'text-xs text-slate-500' }, 'No default export found');`;
    }
  }

  const program = `${transformed}\n${footer}\nconst __root = React.createElement('div',{className:'relative w-full h-full'}, React.createElement(Component));\nrender(__root);`;
  return { program };
}

export function ReactRunner({ code }: ReactRunnerProps) {
  const { program } = useMemo(() => transformToModule(code), [code]);

  const scope = useMemo(
    () => ({
      React,
      // Common React globals for convenience
      useState: React.useState,
      useEffect: React.useEffect,
      useLayoutEffect: React.useLayoutEffect,
      useMemo: React.useMemo,
      useCallback: React.useCallback,
      useRef: React.useRef,
      useContext: React.useContext,
      useReducer: React.useReducer,
      useTransition: (React as any).useTransition,
      useDeferredValue: (React as any).useDeferredValue,
      Fragment: React.Fragment,
      createElement: React.createElement,
      Suspense: React.Suspense,
    }),
    []
  );

  const { element, error } = useRunner({ code: program, scope });

  if (error) {
    return (
      <div className="text-xs text-red-700 bg-red-50 border border-red-200 rounded-md p-2">
        {String(error)}
      </div>
    );
  }

  return (
    <div className="prose-sm max-w-none relative" style={{ transform: 'translateZ(0)' }}>
      {element}
    </div>
  );
}


