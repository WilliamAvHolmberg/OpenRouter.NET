import { z } from 'zod';

export const SUPPORTED_LANGUAGES = [
  { code: 'en', name: 'English', flag: '🇬🇧' },
  { code: 'es', name: 'Spanish', flag: '🇪🇸' },
  { code: 'fr', name: 'French', flag: '🇫🇷' },
  { code: 'de', name: 'German', flag: '🇩🇪' },
  { code: 'it', name: 'Italian', flag: '🇮🇹' },
  { code: 'pt', name: 'Portuguese', flag: '🇵🇹' },
  { code: 'nl', name: 'Dutch', flag: '🇳🇱' },
  { code: 'sv', name: 'Swedish', flag: '🇸🇪' },
  { code: 'ja', name: 'Japanese', flag: '🇯🇵' },
  { code: 'zh', name: 'Chinese', flag: '🇨🇳' },
] as const;

export type LanguageCode = typeof SUPPORTED_LANGUAGES[number]['code'];

export const translationSchema = z.object({
  fieldName: z.string().describe('The field name/label to be translated'),
  translations: z.object({
    en: z.string().describe('English translation'),
    es: z.string().describe('Spanish translation'),
    fr: z.string().describe('French translation'),
    de: z.string().describe('German translation'),
    it: z.string().describe('Italian translation'),
    pt: z.string().describe('Portuguese translation'),
    nl: z.string().describe('Dutch translation'),
    sv: z.string().describe('Swedish translation'),
    ja: z.string().describe('Japanese translation'),
    zh: z.string().describe('Chinese translation'),
  }).describe('Translations for each language'),
  context: z.string().optional().describe('Additional context or notes about the field'),
});

export type TranslationData = z.infer<typeof translationSchema>;

export interface TranslationSuggestion {
  language: LanguageCode;
  languageName: string;
  flag: string;
  suggested: string;
  current: string;
  status: 'pending' | 'accepted' | 'rejected';
}

