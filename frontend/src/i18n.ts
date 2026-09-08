import i18n from 'i18next'
import { initReactI18next } from 'react-i18next'
import LanguageDetector from 'i18next-browser-languagedetector'
import { localeResources, supportedLanguages } from './locales/index.ts'

const fallbackLanguage = 'ru'

function synchronizeDocumentLanguage(language: string | undefined) {
  if (typeof document === 'undefined') {
    return
  }

  const normalizedLanguage = supportedLanguages.find(
    (supportedLanguage) =>
      language === supportedLanguage || language?.startsWith(`${supportedLanguage}-`),
  )
  document.documentElement.lang = normalizedLanguage ?? fallbackLanguage
}

i18n.on('languageChanged', synchronizeDocumentLanguage)

void i18n
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    fallbackLng: fallbackLanguage,
    supportedLngs: supportedLanguages,
    interpolation: {
      escapeValue: false,
    },
    resources: localeResources,
  })
  .then(() => synchronizeDocumentLanguage(i18n.resolvedLanguage))

export default i18n
