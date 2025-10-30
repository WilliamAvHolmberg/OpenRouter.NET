/**
 * GenerateObjectDemo
 * 
 * Demonstration of the useGenerateObject hook with Zod schema validation
 * Shows how to generate structured data from LLMs with full type safety
 */

import { useState } from 'react';
import { z } from 'zod';
import { useGenerateObject } from '@openrouter-dotnet/react';

// Define Zod schemas for different examples
const personSchema = z.object({
  name: z.string().describe('Full name of the person'),
  age: z.number().describe('Age in years'),
  occupation: z.string().describe('Current occupation'),
  hobbies: z.array(z.string()).describe('List of hobbies'),
});

const translationSchema = z.object({
  translations: z.array(
    z.object({
      language: z.string().describe('Target language name'),
      languageCode: z.string().describe('ISO 639-1 language code (e.g., "en", "es")'),
      translatedText: z.string().describe('The translated text'),
      context: z.string().optional().describe('Optional translation notes'),
    })
  ),
});

const recipeSchema = z.object({
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

type ExampleType = 'person' | 'translation' | 'recipe';

interface Example {
  type: ExampleType;
  label: string;
  schema: z.ZodType;
  defaultPrompt: string;
}

const examples: Example[] = [
  {
    type: 'person',
    label: 'Person Profile',
    schema: personSchema,
    defaultPrompt: 'Generate a profile for a software engineer named Sarah who is 28 years old',
  },
  {
    type: 'translation',
    label: 'Translations',
    schema: translationSchema,
    defaultPrompt: 'Translate "Hello World" into Spanish, French, German, and Japanese',
  },
  {
    type: 'recipe',
    label: 'Recipe',
    schema: recipeSchema,
    defaultPrompt: 'Generate a simple recipe for chocolate chip cookies',
  },
];

export function GenerateObjectDemo() {
  const [selectedExample, setSelectedExample] = useState<Example>(examples[0]);
  const [prompt, setPrompt] = useState(examples[0].defaultPrompt);
  const [model, setModel] = useState('openai/gpt-4o-mini');

  const { object, isLoading, error, usage, generate, reset } = useGenerateObject({
    schema: selectedExample.schema,
    prompt: prompt,
    endpoint: '/api/generate-object',
    model: model,
    temperature: 0.7,
  });

  const handleExampleChange = (example: Example) => {
    setSelectedExample(example);
    setPrompt(example.defaultPrompt);
    reset();
  };

  const handleGenerate = () => {
    generate(prompt);
  };

  return (
    <div className="max-w-6xl mx-auto p-6 space-y-6">
      <div className="bg-white rounded-lg shadow-lg p-6">
        <h1 className="text-3xl font-bold text-gray-900 mb-2">
          Generate Object Demo
        </h1>
        <p className="text-gray-600 mb-6">
          Generate structured data from LLMs with automatic Zod schema validation and full TypeScript type safety.
        </p>

        {/* Example Selection */}
        <div className="mb-6">
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Example Type
          </label>
          <div className="flex gap-2">
            {examples.map((example) => (
              <button
                key={example.type}
                onClick={() => handleExampleChange(example)}
                className={`px-4 py-2 rounded-md font-medium transition-colors ${
                  selectedExample.type === example.type
                    ? 'bg-blue-600 text-white'
                    : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
                }`}
              >
                {example.label}
              </button>
            ))}
          </div>
        </div>

        {/* Model Selection */}
        <div className="mb-6">
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Model
          </label>
          <select
            value={model}
            onChange={(e) => setModel(e.target.value)}
            className="w-full px-4 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-transparent"
          >
            <option value="openai/gpt-4o-mini">GPT-4o Mini</option>
            <option value="openai/gpt-4o">GPT-4o</option>
            <option value="anthropic/claude-3.5-haiku">Claude 3.5 Haiku</option>
            <option value="anthropic/claude-3.5-sonnet">Claude 3.5 Sonnet</option>
            <option value="google/gemini-2.5-flash">Gemini 2.5 Flash</option>
          </select>
        </div>

        {/* Prompt Input */}
        <div className="mb-6">
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Prompt
          </label>
          <textarea
            value={prompt}
            onChange={(e) => setPrompt(e.target.value)}
            rows={3}
            className="w-full px-4 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            placeholder="Describe what you want to generate..."
          />
        </div>

        {/* Generate Button */}
        <button
          onClick={handleGenerate}
          disabled={isLoading || !prompt.trim()}
          className="w-full bg-blue-600 text-white px-6 py-3 rounded-md font-medium hover:bg-blue-700 disabled:bg-gray-300 disabled:cursor-not-allowed transition-colors"
        >
          {isLoading ? 'Generating...' : 'Generate Object'}
        </button>
      </div>

      {/* Results */}
      {error && (
        <div className="bg-red-50 border border-red-200 rounded-lg p-4">
          <h3 className="text-red-800 font-semibold mb-2">Error</h3>
          <p className="text-red-600">{error.message}</p>
        </div>
      )}

      {object && (
        <div className="bg-white rounded-lg shadow-lg p-6">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-2xl font-bold text-gray-900">Generated Object</h2>
            {usage && (
              <div className="text-sm text-gray-600">
                Tokens: {usage.totalTokens} ({usage.promptTokens} prompt + {usage.completionTokens} completion)
              </div>
            )}
          </div>

          {/* Render based on schema type */}
          {selectedExample.type === 'person' && (
            <PersonView person={object} />
          )}
          {selectedExample.type === 'translation' && (
            <TranslationView data={object} />
          )}
          {selectedExample.type === 'recipe' && (
            <RecipeView recipe={object} />
          )}

          {/* Raw JSON */}
          <details className="mt-6">
            <summary className="cursor-pointer text-sm font-medium text-gray-700 hover:text-gray-900">
              View Raw JSON
            </summary>
            <pre className="mt-2 bg-gray-50 p-4 rounded-md overflow-x-auto text-sm">
              {JSON.stringify(object, null, 2)}
            </pre>
          </details>
        </div>
      )}
    </div>
  );
}

// Component views for different schema types
function PersonView({ person }: { person: z.infer<typeof personSchema> }) {
  return (
    <div className="space-y-4">
      <div>
        <span className="text-sm font-medium text-gray-500">Name:</span>
        <p className="text-lg font-semibold text-gray-900">{person.name}</p>
      </div>
      <div className="grid grid-cols-2 gap-4">
        <div>
          <span className="text-sm font-medium text-gray-500">Age:</span>
          <p className="text-lg text-gray-900">{person.age}</p>
        </div>
        <div>
          <span className="text-sm font-medium text-gray-500">Occupation:</span>
          <p className="text-lg text-gray-900">{person.occupation}</p>
        </div>
      </div>
      <div>
        <span className="text-sm font-medium text-gray-500">Hobbies:</span>
        <ul className="mt-2 space-y-1">
          {person.hobbies.map((hobby, idx) => (
            <li key={idx} className="text-gray-900 flex items-center">
              <span className="mr-2">•</span>
              {hobby}
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
}

function TranslationView({ data }: { data: z.infer<typeof translationSchema> }) {
  return (
    <div className="space-y-4">
      {data.translations.map((translation, idx) => (
        <div key={idx} className="border-l-4 border-blue-500 pl-4 py-2">
          <div className="flex items-center gap-2 mb-1">
            <span className="font-semibold text-gray-900">{translation.language}</span>
            <span className="text-sm text-gray-500">({translation.languageCode})</span>
          </div>
          <p className="text-lg text-gray-800">{translation.translatedText}</p>
          {translation.context && (
            <p className="text-sm text-gray-600 italic mt-1">{translation.context}</p>
          )}
        </div>
      ))}
    </div>
  );
}

function RecipeView({ recipe }: { recipe: z.infer<typeof recipeSchema> }) {
  return (
    <div className="space-y-6">
      <div>
        <h3 className="text-2xl font-bold text-gray-900">{recipe.name}</h3>
        <p className="text-gray-600 mt-1">{recipe.description}</p>
      </div>
      
      <div className="grid grid-cols-2 gap-4 text-sm">
        <div>
          <span className="font-medium text-gray-500">Servings:</span>
          <span className="ml-2 text-gray-900">{recipe.servings}</span>
        </div>
        <div>
          <span className="font-medium text-gray-500">Prep Time:</span>
          <span className="ml-2 text-gray-900">{recipe.prepTime}</span>
        </div>
      </div>

      <div>
        <h4 className="font-semibold text-gray-900 mb-3">Ingredients</h4>
        <ul className="space-y-2">
          {recipe.ingredients.map((ingredient, idx) => (
            <li key={idx} className="flex items-start">
              <span className="mr-2 text-gray-400">•</span>
              <span className="text-gray-900">
                <span className="font-medium">{ingredient.amount}</span> {ingredient.item}
              </span>
            </li>
          ))}
        </ul>
      </div>

      <div>
        <h4 className="font-semibold text-gray-900 mb-3">Instructions</h4>
        <ol className="space-y-3">
          {recipe.instructions.map((instruction, idx) => (
            <li key={idx} className="flex items-start">
              <span className="mr-3 font-semibold text-blue-600">{idx + 1}.</span>
              <span className="text-gray-900">{instruction}</span>
            </li>
          ))}
        </ol>
      </div>
    </div>
  );
}
