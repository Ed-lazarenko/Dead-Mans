import '@testing-library/jest-dom/vitest'
import i18n from '../i18n.ts'
import {
  allFeatureTranslationBundles,
  registerFeatureTranslations,
} from '../locales/feature-locale-loader.ts'

await registerFeatureTranslations(i18n, allFeatureTranslationBundles)
