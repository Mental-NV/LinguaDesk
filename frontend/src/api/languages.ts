import type { components } from './generated/linguadesk-api.d'

type Language = components['schemas']['LanguageCapability']
export function languageCapabilities(languages: readonly Language[]) {
  const ordered = [...languages].sort((a, b) => a.name.localeCompare(b.name, 'en') || a.id.localeCompare(b.id, 'en'))
  return {
    languages: Object.fromEntries(ordered.map(language => [language.id, language.id === 'zh' ? `${language.name} (Simplified output)` : ['pa', 'mn'].includes(language.id) ? `${language.name} (${language.script})` : language.name])),
    languageDirections: Object.fromEntries(ordered.map(language => [language.id, language.direction])),
    languageValues: ordered.map(language => language.id),
  }
}
