import appTranslations from '../app/i18n/app-translations.ts'
import authTranslations from '../features/auth/i18n/auth-translations.ts'
import navigationTranslations from '../layouts/i18n/navigation-translations.ts'
import gameBoardTranslations from '../features/game-board/i18n/game-board-translations.ts'
import gameSetupTranslations from '../features/game-setup/i18n/game-setup-translations.ts'
import gameApplicationTranslations from '../features/game-application/i18n/game-application-translations.ts'
import gameModifiersTranslations from '../features/game-modifiers/i18n/game-modifiers-translations.ts'
import gameCatalogTranslations from '../features/game-catalog/i18n/game-catalog-translations.ts'
import gameHistoryTranslations from '../features/game-history/i18n/game-history-translations.ts'
import gameQuizTranslations from '../features/game-quiz/i18n/game-quiz-translations.ts'
import gameRegistrationTranslations from '../features/game-registration/i18n/game-registration-translations.ts'
import teamRegistrationsTranslations from '../features/team-registrations/i18n/team-registrations-translations.ts'
import languageSwitcherTranslations from '../shared/i18n/language-switcher-translations.ts'
import commonTranslations from '../shared/i18n/common-translations.ts'

export const supportedLanguages = ['en', 'ru', 'uk', 'pl'] as const
type SupportedLanguage = (typeof supportedLanguages)[number]

function createTranslation(language: SupportedLanguage) {
  return {
    ...appTranslations[language],
    auth: authTranslations[language],
    navigation: navigationTranslations[language],
    gameBoard: gameBoardTranslations[language],
    gameSetup: gameSetupTranslations[language],
    gameApplication: gameApplicationTranslations[language],
    gameModifiers: gameModifiersTranslations[language],
    gameCatalog: gameCatalogTranslations[language],
    gameHistory: gameHistoryTranslations[language],
    gameQuiz: gameQuizTranslations[language],
    gameRegistration: gameRegistrationTranslations[language],
    teamRegistrations: teamRegistrationsTranslations[language],
    languageSwitcher: languageSwitcherTranslations[language],
    common: commonTranslations[language],
  }
}

const defaultTranslation = {
  ...appTranslations.en,
  auth: authTranslations.en,
  navigation: navigationTranslations.en,
  gameBoard: gameBoardTranslations.en,
  gameSetup: gameSetupTranslations.en,
  gameApplication: gameApplicationTranslations.en,
  gameModifiers: gameModifiersTranslations.en,
  gameCatalog: gameCatalogTranslations.en,
  gameHistory: gameHistoryTranslations.en,
  gameQuiz: gameQuizTranslations.en,
  gameRegistration: gameRegistrationTranslations.en,
  teamRegistrations: teamRegistrationsTranslations.en,
  languageSwitcher: languageSwitcherTranslations.en,
  common: commonTranslations.en,
}

export type DefaultTranslation = typeof defaultTranslation

export const localeResources = {
  en: { translation: defaultTranslation },
  ru: { translation: createTranslation('ru') },
  uk: { translation: createTranslation('uk') },
  pl: { translation: createTranslation('pl') },
}
