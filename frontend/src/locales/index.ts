import appTranslations from '../app/i18n/app-translations.ts'
import authTranslations from '../features/auth/i18n/auth-translations.ts'
import adminToolsTranslations from '../features/admin-tools/i18n/admin-tools-translations.ts'
import navigationTranslations from '../layouts/i18n/navigation-translations.ts'
import commonTranslations from '../shared/i18n/common-translations.ts'
import languageSwitcherTranslations from '../shared/i18n/language-switcher-translations.ts'

export const supportedLanguages = ['en', 'ru', 'uk', 'pl'] as const
export type SupportedLanguage = (typeof supportedLanguages)[number]

function createBaseTranslation(language: SupportedLanguage) {
  return {
    ...appTranslations[language],
    auth: authTranslations[language],
    navigation: navigationTranslations[language],
    languageSwitcher: languageSwitcherTranslations[language],
    common: commonTranslations[language],
    adminTools: adminToolsTranslations[language],
  }
}

type TranslationOf<TModule extends { default: Record<SupportedLanguage, object> }> =
  TModule['default']['en']

type FeatureTranslations = {
  gameBoard: TranslationOf<typeof import('../features/game-board/i18n/game-board-translations.ts')>
  gameSetup: TranslationOf<typeof import('../features/game-setup/i18n/game-setup-translations.ts')>
  gameApplication: TranslationOf<
    typeof import('../features/game-application/i18n/game-application-translations.ts')
  >
  gameModifiers: TranslationOf<
    typeof import('../features/game-modifiers/i18n/game-modifiers-translations.ts')
  >
  gameCatalog: TranslationOf<
    typeof import('../features/game-catalog/i18n/game-catalog-translations.ts')
  >
  gameHistory: TranslationOf<
    typeof import('../features/game-history/i18n/game-history-translations.ts')
  >
  gameQuiz: TranslationOf<typeof import('../features/game-quiz/i18n/game-quiz-translations.ts')>
  gameRegistration: TranslationOf<
    typeof import('../features/game-registration/i18n/game-registration-translations.ts')
  >
  teamRegistrations: TranslationOf<
    typeof import('../features/team-registrations/i18n/team-registrations-translations.ts')
  >
  modifierHistory: TranslationOf<
    typeof import('../features/modifier-history/i18n/modifier-history-translations.ts')
  >
}

export type DefaultTranslation = ReturnType<typeof createBaseTranslation> & FeatureTranslations

export const localeResources = Object.fromEntries(
  supportedLanguages.map((language) => [
    language,
    { translation: createBaseTranslation(language) },
  ]),
)
