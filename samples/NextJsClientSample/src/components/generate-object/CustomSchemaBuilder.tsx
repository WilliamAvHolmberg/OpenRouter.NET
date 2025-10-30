'use client';

import { useState, useEffect } from 'react';
import { z } from 'zod';

interface SchemaField {
  id: string;
  name: string;
  type: 'string' | 'number' | 'boolean' | 'array' | 'object';
  description: string;
  required: boolean;
  arrayItemType?: 'string' | 'number' | 'boolean';
}

interface CustomSchemaBuilderProps {
  onSchemaChange: (schema: z.ZodType | null) => void;
}

export function CustomSchemaBuilder({ onSchemaChange }: CustomSchemaBuilderProps) {
  const [fields, setFields] = useState<SchemaField[]>([]);
  const [schemaName, setSchemaName] = useState('CustomObject');

  useEffect(() => {
    buildSchema();
  }, [fields]);

  const addField = () => {
    const newField: SchemaField = {
      id: Date.now().toString(),
      name: `field${fields.length + 1}`,
      type: 'string',
      description: '',
      required: true,
    };
    setFields([...fields, newField]);
  };

  const removeField = (id: string) => {
    setFields(fields.filter(f => f.id !== id));
  };

  const updateField = (id: string, updates: Partial<SchemaField>) => {
    setFields(fields.map(f => f.id === id ? { ...f, ...updates } : f));
  };

  const buildSchema = () => {
    if (fields.length === 0) {
      onSchemaChange(null);
      return;
    }

    try {
      const schemaObj: Record<string, z.ZodTypeAny> = {};

      fields.forEach(field => {
        let fieldSchema: z.ZodTypeAny;

        switch (field.type) {
          case 'string':
            fieldSchema = z.string();
            break;
          case 'number':
            fieldSchema = z.number();
            break;
          case 'boolean':
            fieldSchema = z.boolean();
            break;
          case 'array':
            const itemType = field.arrayItemType || 'string';
            const itemSchema = itemType === 'string' ? z.string() : 
                              itemType === 'number' ? z.number() : z.boolean();
            fieldSchema = z.array(itemSchema);
            break;
          case 'object':
            fieldSchema = z.record(z.unknown());
            break;
          default:
            fieldSchema = z.string();
        }

        if (field.description) {
          fieldSchema = fieldSchema.describe(field.description);
        }

        if (!field.required) {
          fieldSchema = fieldSchema.optional();
        }

        schemaObj[field.name] = fieldSchema;
      });

      const schema = z.object(schemaObj);
      onSchemaChange(schema);
    } catch (error) {
      console.error('Failed to build schema:', error);
      onSchemaChange(null);
    }
  };

  return (
    <div className="space-y-4">
      <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
        <div className="flex items-start gap-3">
          <svg className="w-5 h-5 text-blue-600 flex-shrink-0 mt-0.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
          <div className="text-xs text-blue-800">
            <p className="font-semibold mb-1">Build your custom Zod schema</p>
            <p>Add fields with types, descriptions, and optionality. The schema will be validated automatically.</p>
          </div>
        </div>
      </div>

      {/* Schema Name */}
      <div>
        <label className="block text-xs font-medium text-slate-700 mb-2">Schema Name</label>
        <input
          type="text"
          value={schemaName}
          onChange={(e) => setSchemaName(e.target.value)}
          className="w-full px-3 py-2 text-sm border border-slate-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
          placeholder="e.g., User, Product, Event"
        />
      </div>

      {/* Fields */}
      <div>
        <div className="flex items-center justify-between mb-3">
          <label className="block text-xs font-medium text-slate-700">Fields ({fields.length})</label>
          <button
            onClick={addField}
            className="text-xs px-3 py-1.5 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors flex items-center gap-1"
          >
            <svg className="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
            </svg>
            Add Field
          </button>
        </div>

        {fields.length === 0 ? (
          <div className="text-center py-8 bg-slate-50 rounded-lg border border-slate-200">
            <p className="text-sm text-slate-500">No fields yet. Click "Add Field" to start building your schema.</p>
          </div>
        ) : (
          <div className="space-y-3 max-h-[400px] overflow-y-auto">
            {fields.map((field) => (
              <div key={field.id} className="bg-slate-50 rounded-lg p-3 border border-slate-200">
                <div className="grid grid-cols-2 gap-2 mb-2">
                  <div>
                    <label className="block text-xs text-slate-600 mb-1">Name</label>
                    <input
                      type="text"
                      value={field.name}
                      onChange={(e) => updateField(field.id, { name: e.target.value })}
                      className="w-full px-2 py-1.5 text-xs border border-slate-300 rounded focus:ring-1 focus:ring-blue-500"
                      placeholder="fieldName"
                    />
                  </div>
                  <div>
                    <label className="block text-xs text-slate-600 mb-1">Type</label>
                    <select
                      value={field.type}
                      onChange={(e) => updateField(field.id, { type: e.target.value as any })}
                      className="w-full px-2 py-1.5 text-xs border border-slate-300 rounded focus:ring-1 focus:ring-blue-500"
                    >
                      <option value="string">String</option>
                      <option value="number">Number</option>
                      <option value="boolean">Boolean</option>
                      <option value="array">Array</option>
                      <option value="object">Object</option>
                    </select>
                  </div>
                </div>

                {field.type === 'array' && (
                  <div className="mb-2">
                    <label className="block text-xs text-slate-600 mb-1">Array Item Type</label>
                    <select
                      value={field.arrayItemType || 'string'}
                      onChange={(e) => updateField(field.id, { arrayItemType: e.target.value as any })}
                      className="w-full px-2 py-1.5 text-xs border border-slate-300 rounded focus:ring-1 focus:ring-blue-500"
                    >
                      <option value="string">String</option>
                      <option value="number">Number</option>
                      <option value="boolean">Boolean</option>
                    </select>
                  </div>
                )}

                <div className="mb-2">
                  <label className="block text-xs text-slate-600 mb-1">Description (optional)</label>
                  <input
                    type="text"
                    value={field.description}
                    onChange={(e) => updateField(field.id, { description: e.target.value })}
                    className="w-full px-2 py-1.5 text-xs border border-slate-300 rounded focus:ring-1 focus:ring-blue-500"
                    placeholder="Describe this field..."
                  />
                </div>

                <div className="flex items-center justify-between">
                  <label className="flex items-center gap-2 cursor-pointer">
                    <input
                      type="checkbox"
                      checked={field.required}
                      onChange={(e) => updateField(field.id, { required: e.target.checked })}
                      className="rounded border-slate-300 text-blue-600 focus:ring-blue-500"
                    />
                    <span className="text-xs text-slate-700">Required</span>
                  </label>
                  <button
                    onClick={() => removeField(field.id)}
                    className="text-xs text-red-600 hover:text-red-700 flex items-center gap-1"
                  >
                    <svg className="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                    </svg>
                    Remove
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

