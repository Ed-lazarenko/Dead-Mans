import { afterAll, describe, expect, it } from 'vitest'
import i18n from './i18n.ts'

afterAll(async () => {
  await i18n.changeLanguage('ru')
})

describe('document language', () => {
  it('follows the active supported locale', async () => {
    await i18n.changeLanguage('pl')

    expect(document.documentElement.lang).toBe('pl')
  })

  it('uses the configured fallback for an unsupported locale', async () => {
    await i18n.changeLanguage('de')

    expect(document.documentElement.lang).toBe('ru')
  })
})
