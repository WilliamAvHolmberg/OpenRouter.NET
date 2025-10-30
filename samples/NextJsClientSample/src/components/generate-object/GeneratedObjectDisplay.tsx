'use client';

import { z } from 'zod';
import { personSchema, translationSchema, recipeSchema, useGenerateObjectContext } from './GenerateObjectContext';

export function GeneratedObjectDisplay() {
  const { selectedExample, generatedObject, usage, timingMs } = useGenerateObjectContext();

  if (!generatedObject) {
    return (
      <div className="flex items-center justify-center h-full">
        <div className="text-center max-w-md">
          <div className="text-slate-300 mb-4">
            <svg className="w-20 h-20 mx-auto" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1} d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
            </svg>
          </div>
          <h3 className="text-lg font-semibold text-slate-900 mb-2">No object generated yet</h3>
          <p className="text-sm text-slate-500">
            Select an example type, enter a prompt, and click Send to generate structured data
          </p>
        </div>
      </div>
    );
  }

  return (
    <div className="h-full overflow-auto">
      <div className="flex items-start justify-between mb-6 gap-4">
        <div>
          <h2 className="text-xl font-bold text-slate-900">Generated Object</h2>
          <p className="text-sm text-slate-500 mt-1">{selectedExample.label}</p>
        </div>
        <div className="flex gap-2 flex-shrink-0">
          {timingMs !== null && (
            <div className="text-xs bg-purple-50 px-3 py-2 rounded-lg border border-purple-200">
              <div className="flex items-center gap-1.5">
                <svg className="w-3.5 h-3.5 text-purple-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                <span className="font-semibold text-purple-700">{(timingMs / 1000).toFixed(2)}s</span>
              </div>
              <div className="text-purple-600 mt-0.5">Request time</div>
            </div>
          )}
          {usage && (
            <div className="text-xs bg-slate-50 px-3 py-2 rounded-lg border border-slate-200">
              <div className="font-semibold text-slate-700">{usage.totalTokens} tokens</div>
              <div className="text-slate-500 mt-0.5">{usage.promptTokens} prompt + {usage.completionTokens} completion</div>
            </div>
          )}
        </div>
      </div>

      <div className="space-y-6">
        {selectedExample.type === 'person' && <PersonView person={generatedObject} />}
        {selectedExample.type === 'translation' && <TranslationView data={generatedObject} />}
        {selectedExample.type === 'recipe' && <RecipeView recipe={generatedObject} />}
        {selectedExample.type === 'custom' && <CustomView data={generatedObject} />}

        {/* Raw JSON */}
        <details className="mt-8">
          <summary className="cursor-pointer text-sm font-medium text-slate-700 hover:text-slate-900 flex items-center gap-2 px-4 py-3 bg-slate-50 rounded-lg border border-slate-200">
            <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M10 20l4-16m4 4l4 4-4 4M6 16l-4-4 4-4" />
            </svg>
            View Raw JSON
          </summary>
          <pre className="mt-3 bg-slate-900 text-slate-100 p-4 rounded-lg overflow-x-auto text-xs font-mono">
            {JSON.stringify(generatedObject, null, 2)}
          </pre>
        </details>
      </div>
    </div>
  );
}

function PersonView({ person }: { person: z.infer<typeof personSchema> }) {
  return (
    <div className="space-y-6">
      <div className="bg-gradient-to-br from-blue-50 to-indigo-50 rounded-xl p-6 border border-blue-100">
        <div className="text-sm font-medium text-blue-600 uppercase tracking-wide mb-2">Name</div>
        <h3 className="text-2xl font-bold text-slate-900">{person.name}</h3>
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div className="bg-slate-50 rounded-lg p-4 border border-slate-200">
          <div className="text-xs font-medium text-slate-500 uppercase tracking-wide mb-2">Age</div>
          <p className="text-2xl font-bold text-slate-900">{person.age}</p>
        </div>
        <div className="bg-slate-50 rounded-lg p-4 border border-slate-200">
          <div className="text-xs font-medium text-slate-500 uppercase tracking-wide mb-2">Occupation</div>
          <p className="text-lg font-semibold text-slate-900">{person.occupation}</p>
        </div>
      </div>

      <div>
        <div className="text-sm font-medium text-slate-700 mb-3 flex items-center gap-2">
          <svg className="w-5 h-5 text-blue-500" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M14 10l-2 1m0 0l-2-1m2 1v2.5M20 7l-2 1m2-1l-2-1m2 1v2.5M14 4l-2-1-2 1M4 7l2-1M4 7l2 1M4 7v2.5M12 21l-2-1m2 1l2-1m-2 1v-2.5M6 18l-2-1v-2.5M18 18l2-1v-2.5" />
          </svg>
          Hobbies
        </div>
        <div className="grid gap-2">
          {person.hobbies.map((hobby, idx) => (
            <div key={idx} className="flex items-center gap-3 bg-white p-3 rounded-lg border border-slate-200">
              <div className="w-2 h-2 rounded-full bg-blue-500 flex-shrink-0"></div>
              <span className="text-slate-900">{hobby}</span>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

function TranslationView({ data }: { data: z.infer<typeof translationSchema> }) {
  return (
    <div className="space-y-4">
      {data.translations.map((translation, idx) => (
        <div key={idx} className="bg-white border-l-4 border-blue-500 rounded-r-lg shadow-sm overflow-hidden">
          <div className="p-4">
            <div className="flex items-center gap-2 mb-3">
              <span className="font-bold text-slate-900 text-lg">{translation.language}</span>
              <span className="text-xs text-slate-500 bg-slate-100 px-2 py-1 rounded font-mono">
                {translation.languageCode}
              </span>
            </div>
            <p className="text-base text-slate-800 font-medium bg-slate-50 px-4 py-3 rounded-lg">
              "{translation.translatedText}"
            </p>
            {translation.context && (
              <p className="text-sm text-slate-600 italic mt-3 pl-4 border-l-2 border-slate-200">
                {translation.context}
              </p>
            )}
          </div>
        </div>
      ))}
    </div>
  );
}

function RecipeView({ recipe }: { recipe: z.infer<typeof recipeSchema> }) {
  return (
    <div className="space-y-6">
      <div className="bg-gradient-to-br from-orange-50 to-red-50 rounded-xl p-6 border border-orange-100">
        <h3 className="text-2xl font-bold text-slate-900 mb-2">{recipe.name}</h3>
        <p className="text-slate-700">{recipe.description}</p>
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div className="bg-slate-50 rounded-lg p-4 border border-slate-200">
          <div className="text-xs font-medium text-slate-500 uppercase tracking-wide mb-2">Servings</div>
          <p className="text-2xl font-bold text-slate-900">{recipe.servings}</p>
        </div>
        <div className="bg-slate-50 rounded-lg p-4 border border-slate-200">
          <div className="text-xs font-medium text-slate-500 uppercase tracking-wide mb-2">Prep Time</div>
          <p className="text-xl font-semibold text-slate-900">{recipe.prepTime}</p>
        </div>
      </div>

      <div>
        <h4 className="font-bold text-slate-900 mb-4 flex items-center gap-2 text-lg">
          <svg className="w-6 h-6 text-orange-500" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
          </svg>
          Ingredients
        </h4>
        <div className="bg-white rounded-lg border border-slate-200 divide-y divide-slate-200">
          {recipe.ingredients.map((ingredient, idx) => (
            <div key={idx} className="flex items-center gap-3 p-3">
              <div className="w-2 h-2 rounded-full bg-orange-500 flex-shrink-0"></div>
              <span className="text-slate-900 flex-1">
                <span className="font-semibold text-orange-700">{ingredient.amount}</span> {ingredient.item}
              </span>
            </div>
          ))}
        </div>
      </div>

      <div>
        <h4 className="font-bold text-slate-900 mb-4 flex items-center gap-2 text-lg">
          <svg className="w-6 h-6 text-orange-500" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-6 9l2 2 4-4" />
          </svg>
          Instructions
        </h4>
        <div className="space-y-3">
          {recipe.instructions.map((instruction, idx) => (
            <div key={idx} className="flex gap-4 bg-white p-4 rounded-lg border border-slate-200">
              <div className="flex-shrink-0 w-8 h-8 rounded-full bg-orange-600 text-white flex items-center justify-center font-bold text-sm">
                {idx + 1}
              </div>
              <p className="text-slate-900 pt-1">{instruction}</p>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

function CustomView({ data }: { data: any }) {
  const renderValue = (value: any, depth = 0): React.ReactNode => {
    if (value === null || value === undefined) {
      return <span className="text-slate-400 italic">null</span>;
    }

    if (typeof value === 'boolean') {
      return <span className={value ? 'text-green-600' : 'text-red-600'}>{String(value)}</span>;
    }

    if (typeof value === 'number') {
      return <span className="text-blue-600 font-mono">{value}</span>;
    }

    if (typeof value === 'string') {
      return <span className="text-slate-900">"{value}"</span>;
    }

    if (Array.isArray(value)) {
      if (value.length === 0) {
        return <span className="text-slate-400 italic">[]</span>;
      }
      return (
        <div className="space-y-2 mt-2">
          {value.map((item, idx) => (
            <div key={idx} className="flex gap-3 items-start">
              <span className="text-slate-400 font-mono text-xs">[{idx}]</span>
              <div className="flex-1">{renderValue(item, depth + 1)}</div>
            </div>
          ))}
        </div>
      );
    }

    if (typeof value === 'object') {
      return (
        <div className="space-y-3 mt-2">
          {Object.entries(value).map(([key, val]) => (
            <div key={key} className="bg-slate-50 rounded-lg p-3 border border-slate-200">
              <div className="text-xs font-medium text-slate-500 uppercase tracking-wide mb-2">{key}</div>
              <div>{renderValue(val, depth + 1)}</div>
            </div>
          ))}
        </div>
      );
    }

    return <span className="text-slate-600">{String(value)}</span>;
  };

  return (
    <div className="bg-white rounded-lg border border-slate-200 p-4">
      {renderValue(data)}
    </div>
  );
}

