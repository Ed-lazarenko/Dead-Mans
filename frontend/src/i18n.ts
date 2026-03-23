import i18n from 'i18next'
import { initReactI18next } from 'react-i18next'
import LanguageDetector from 'i18next-browser-languagedetector'
import { localeResources } from './locales/index.ts'

void i18n
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    fallbackLng: 'ru',
    supportedLngs: ['en', 'ru', 'uk', 'pl'],
    interpolation: {
      escapeValue: false,
    },
    resources: localeResources,
  })

export default i18n


