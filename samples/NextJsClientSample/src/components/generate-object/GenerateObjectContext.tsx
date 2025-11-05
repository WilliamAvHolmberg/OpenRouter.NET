'use client';

import { createContext, useContext, useState, ReactNode } from 'react';
import { z } from 'zod';

export const personSchema = z.object({
  name: z.string().describe('Full name of the person'),
  age: z.number().describe('Age in years'),
  occupation: z.string().describe('Current occupation'),
  hobbies: z.array(z.string()).describe('List of hobbies'),
});

export const translationSchema = z.object({
  translations: z.array(
    z.object({
      language: z.string().describe('Target language name'),
      languageCode: z.string().describe('ISO 639-1 language code (e.g., "en", "es")'),
      translatedText: z.string().describe('The translated text'),
      context: z.string().optional().describe('Optional translation notes'),
    })
  ),
});

export const recipeSchema = z.object({
  name: z.string().describe('Recipe name'),
  description: z.string().describe('Brief description'),
  servings: z.number().describe('Number of servings'),
  prepTime: z.string().describe('Preparation time (e.g., "30 minutes")'),
  ingredients: z.array(
    z.object({
      item: z.string().describe('Ingredient name'),
      amount: z.string().describe('Amount needed (e.g., "2 cups")'),
    })
  ),
  instructions: z.array(z.string()).describe('Step-by-step instructions'),
});

export type ExampleType = 'person' | 'translation' | 'recipe' | 'custom';

export interface Example {
  type: ExampleType;
  label: string;
  schema: z.ZodType | null;
  defaultPrompt: string;
  description: string;
}

export const examples: Example[] = [
  {
    type: 'person',
    label: 'Person Profile',
    schema: personSchema,
    defaultPrompt: 'Generate a profile for a software engineer named Sarah who is 28 years old',
    description: 'Create structured person profiles',
  },
  {
    type: 'translation',
    label: 'Translations',
    schema: translationSchema,
    defaultPrompt: 'Translate "Hello World" into Spanish, French, German, and Japanese',
    description: 'Get multiple translations with metadata',
  },
  {
    type: 'recipe',
    label: 'Recipe',
    schema: recipeSchema,
    defaultPrompt: 'Generate a simple recipe for chocolate chip cookies',
    description: 'Extract structured recipe data',
  },
  {
    type: 'custom',
    label: 'Custom Schema',
    schema: null,
    defaultPrompt: 'Generate data based on your custom schema',
    description: 'Build your own schema dynamically',
  },
];

interface GenerateObjectContextType {
  selectedExample: Example;
  setSelectedExample: (example: Example) => void;
  generatedObject: any;
  setGeneratedObject: (obj: any) => void;
  usage: { promptTokens: number; completionTokens: number; totalTokens: number } | null;
  setUsage: (usage: any) => void;
  customSchema: z.ZodType | null;
  setCustomSchema: (schema: z.ZodType | null) => void;
  timingMs: number | null;
  setTimingMs: (timing: number | null) => void;
}

const GenerateObjectContext = createContext<GenerateObjectContextType | undefined>(undefined);

export function GenerateObjectProvider({ children }: { children: ReactNode }) {
  const [selectedExample, setSelectedExample] = useState<Example>(examples[0]);
  const [generatedObject, setGeneratedObject] = useState<any>(null);
  const [usage, setUsage] = useState<any>(null);
  const [customSchema, setCustomSchema] = useState<z.ZodType | null>(null);
  const [timingMs, setTimingMs] = useState<number | null>(null);

  return (
    <GenerateObjectContext.Provider
      value={{
        selectedExample,
        setSelectedExample,
        generatedObject,
        setGeneratedObject,
        usage,
        setUsage,
        customSchema,
        setCustomSchema,
        timingMs,
        setTimingMs,
      }}
    >
      {children}
    </GenerateObjectContext.Provider>
  );
}

export function useGenerateObjectContext() {
  const context = useContext(GenerateObjectContext);
  if (!context) {
    throw new Error('useGenerateObjectContext must be used within GenerateObjectProvider');
  }
  return context;
}

