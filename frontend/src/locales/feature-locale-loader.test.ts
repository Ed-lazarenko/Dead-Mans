import { createInstance } from 'i18next'
import { describe, expect, it, vi } from 'vitest'
import { registerFeatureTranslations } from './feature-locale-loader.ts'

describe('registerFeatureTranslations', () => {
  it('loads a feature once and registers every supported language without replacing base keys', async () => {
    const instance = createInstance()
    await instance.init({
      fallbackLng: 'en',
      resources: {
        en: { translation: { appTitle: 'Dead Mans' } },
        ru: { translation: { appTitle: 'Dead Mans' } },
      },
    })
    const load = vi.fn(async () => ({
      default: {
        en: { title: 'Feature' },
        ru: { title: 'Раздел' },
        uk: { title: 'Розділ' },
        pl: { title: 'Sekcja' },
      },
    }))

    await registerFeatureTranslations(instance, [{ key: 'feature', load }])

    expect(load).toHaveBeenCalledOnce()
    expect(instance.getFixedT('en')('appTitle')).toBe('Dead Mans')
    expect(instance.getFixedT('en')('feature.title')).toBe('Feature')
    expect(instance.getFixedT('ru')('feature.title')).toBe('Раздел')
    expect(instance.getFixedT('uk')('feature.title')).toBe('Розділ')
    expect(instance.getFixedT('pl')('feature.title')).toBe('Sekcja')
  })
})
