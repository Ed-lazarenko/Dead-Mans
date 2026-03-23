import i18n from 'i18next'
import { initReactI18next } from 'react-i18next'
import LanguageDetector from 'i18next-browser-languagedetector'

void i18n
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    fallbackLng: 'ru',
    supportedLngs: ['en', 'ru', 'uk', 'pl'],
    interpolation: {
      escapeValue: false,
    },
    resources: {
      en: {
        translation: {
          appTitle: "Dead Man's Game",
          nav: {
            loadout: 'Loadouts',
            leaderboard: 'Leaderboard',
            modifiers: 'Modifiers',
            controls: 'Controls',
          },
          pages: {
            loadout: 'Loadout page placeholder',
            leaderboard: 'Leaderboard page placeholder',
            modifiers: 'Modifiers page placeholder',
            controls: 'Controls page placeholder',
          },
          auth: {
            title: "Dead Man's Loadout",
            subtitle: "Control panel for Dead Man's Loadout game sessions and viewer interaction.",
            description: 'Sign in to manage teams, loadouts and viewer modifiers during your stream.',
            button: 'Sign in with Twitch',
          },
          loadout: {
            loading: 'Loading loadout grid (mock)...',
            hint: 'Mock data. First click reveals the card, second click opens it fullscreen.',
          },
          controls: {
            loading: 'Loading game state (mock)...',
            currentState: 'Current game state (mock):',
            phase: 'Phase: {{phase}}',
            round: 'Round: {{current}}/{{total}}',
            lastAction: 'Last action: {{time}}',
            quickActions: 'Quick actions:',
            start: 'Start game',
            pause: 'Pause',
            resume: 'Resume',
            nextRound: 'Next round',
            resetAll: 'Reset all',
            closeAllLoadoutCards: 'Close all loadout cards',
            mockNotice:
              'This is mock logic for now. Later these actions will call real backend endpoints and broadcast updates via SignalR.',
          },
          modifiers: {
            loading: 'Loading modifiers (mock)...',
            availableTitle: 'Available modifiers',
            activateButton: 'Activate (-{{cost}})',
            activeTitle: 'Active modifiers',
            emptyActive: 'No active modifiers yet.',
            activeFrom: 'From: {{user}} • {{time}}',
          },
          leaderboard: {
            errorLoading: 'Failed to load leaderboard (mock).',
            mockUpdatedAt: 'Mock data. Updated at: {{time}}',
            columns: {
              position: '#',
              team: 'Team',
              score: 'Score',
              penalty: 'Penalty',
              total: 'Total',
            },
          },
          authErrors: {
            noPermissionTitle: 'Insufficient permissions',
            noPermissionDescription:
              'Your role does not have access to this page. Please sign in as a streamer or moderator.',
          },
        },
      },
      ru: {
        translation: {
          appTitle: 'Dead Man’s Game',
          nav: {
            loadout: 'Лоадауты',
            leaderboard: 'Таблица',
            modifiers: 'Модификаторы',
            controls: 'Управление',
          },
          pages: {
            loadout: 'Заглушка страницы лоадаутов',
            leaderboard: 'Заглушка страницы таблицы',
            modifiers: 'Заглушка страницы модификаторов',
            controls: 'Заглушка страницы управления',
          },
          auth: {
            title: 'Dead Man’s Loadout',
            subtitle: 'Панель управления игрой Dead Man’s Loadout и взаимодействием со зрителями.',
            description: 'Войдите, чтобы управлять командами, лоадаутами и модификаторами зрителей во время стрима.',
            button: 'Войти через Twitch',
          },
          loadout: {
            loading: 'Загрузка сетки лоадаутов (mock)...',
            hint: 'Mock-данные. Клик по ячейке сначала раскрывает её, повторный клик открывает картинку на полный экран.',
          },
          controls: {
            loading: 'Загрузка состояния игры (mock)...',
            currentState: 'Текущее состояние игры (mock):',
            phase: 'Фаза: {{phase}}',
            round: 'Раунд: {{current}}/{{total}}',
            lastAction: 'Последнее действие: {{time}}',
            quickActions: 'Быстрые действия:',
            start: 'Старт игры',
            pause: 'Пауза',
            resume: 'Продолжить',
            nextRound: 'Следующий раунд',
            resetAll: 'Сбросить всё',
            closeAllLoadoutCards: 'Закрыть все карточки лоадаута',
            mockNotice:
              'Здесь пока только mock‑логика. В будущем эти действия будут вызывать реальные эндпоинты backend и рассылать обновления через SignalR.',
          },
          modifiers: {
            loading: 'Загрузка модификаторов (mock)...',
            availableTitle: 'Доступные модификаторы',
            activateButton: 'Активировать (-{{cost}})',
            activeTitle: 'Активные модификаторы',
            emptyActive: 'Пока нет активных модификаторов.',
            activeFrom: 'От: {{user}} • {{time}}',
          },
          leaderboard: {
            errorLoading: 'Не удалось загрузить таблицу лидеров (mock).',
            mockUpdatedAt: 'Mock-данные. Обновлено: {{time}}',
            columns: {
              position: '#',
              team: 'Команда',
              score: 'Очки',
              penalty: 'Штраф',
              total: 'Итого',
            },
          },
          authErrors: {
            noPermissionTitle: 'Недостаточно прав',
            noPermissionDescription:
              'У вашей роли нет доступа к этой странице. Пожалуйста, войдите как стример или модератор.',
          },
        },
      },
      uk: {
        translation: {
          appTitle: 'Dead Man’s Game',
          nav: {
            loadout: 'Лоадаути',
            leaderboard: 'Таблиця лідерів',
            modifiers: 'Модифікатори',
            controls: 'Керування',
          },
          pages: {
            loadout: 'Заглушка сторінки лоадаутів',
            leaderboard: 'Заглушка сторінки таблиці лідерів',
            modifiers: 'Заглушка сторінки модифікаторів',
            controls: 'Заглушка сторінки керування',
          },
          auth: {
            title: 'Dead Man’s Loadout',
            subtitle: 'Панель керування грою Dead Man’s Loadout та взаємодією з глядачами.',
            description:
              'Увійдіть, щоб керувати командами, лоадаутами та модифікаторами від глядачів під час стріму.',
            button: 'Увійти через Twitch',
          },
          loadout: {
            loading: 'Завантаження сітки лоадаутів (mock)...',
            hint: 'Mock-дані. Клік по клітинці спочатку відкриває її, повторний клік відкриває картинку на повний екран.',
          },
          controls: {
            loading: 'Завантаження стану гри (mock)...',
            currentState: 'Поточний стан гри (mock):',
            phase: 'Фаза: {{phase}}',
            round: 'Раунд: {{current}}/{{total}}',
            lastAction: 'Остання дія: {{time}}',
            quickActions: 'Швидкі дії:',
            start: 'Старт гри',
            pause: 'Пауза',
            resume: 'Продовжити',
            nextRound: 'Наступний раунд',
            resetAll: 'Скинути все',
            closeAllLoadoutCards: 'Закрити всі картки лоадауту',
            mockNotice:
              'Тут поки що тільки mock‑логіка. У майбутньому ці дії викликатимуть реальні endpoint’и backend та надсилатимуть оновлення через SignalR.',
          },
          modifiers: {
            loading: 'Завантаження модифікаторів (mock)...',
            availableTitle: 'Доступні модифікатори',
            activateButton: 'Активувати (-{{cost}})',
            activeTitle: 'Активні модифікатори',
            emptyActive: 'Поки немає активних модифікаторів.',
            activeFrom: 'Від: {{user}} • {{time}}',
          },
          leaderboard: {
            errorLoading: 'Не вдалося завантажити таблицю лідерів (mock).',
            mockUpdatedAt: 'Mock-дані. Оновлено: {{time}}',
            columns: {
              position: '#',
              team: 'Команда',
              score: 'Очки',
              penalty: 'Штраф',
              total: 'Разом',
            },
          },
          authErrors: {
            noPermissionTitle: 'Недостатньо прав',
            noPermissionDescription:
              'Ваша роль не має доступу до цієї сторінки. Увійдіть як стрімер або модератор.',
          },
        },
      },
      pl: {
        translation: {
          appTitle: 'Dead Man’s Game',
          nav: {
            loadout: 'Loadouty',
            leaderboard: 'Tabela wyników',
            modifiers: 'Modyfikatory',
            controls: 'Sterowanie',
          },
          pages: {
            loadout: 'Placeholder strony loadoutów',
            leaderboard: 'Placeholder strony tabeli wyników',
            modifiers: 'Placeholder strony modyfikatorów',
            controls: 'Placeholder strony panelu sterowania',
          },
          auth: {
            title: 'Dead Man’s Loadout',
            subtitle: 'Panel sterowania grą Dead Man’s Loadout i interakcją z widzami.',
            description:
              'Zaloguj się, aby zarządzać drużynami, loadoutami i modyfikatorami widzów podczas streamu.',
            button: 'Zaloguj się przez Twitch',
          },
          loadout: {
            loading: 'Ładowanie siatki loadoutów (mock)...',
            hint: 'Dane mock. Pierwsze kliknięcie odkrywa kartę, drugie otwiera obraz w trybie pełnoekranowym.',
          },
          controls: {
            loading: 'Ładowanie stanu gry (mock)...',
            currentState: 'Aktualny stan gry (mock):',
            phase: 'Faza: {{phase}}',
            round: 'Runda: {{current}}/{{total}}',
            lastAction: 'Ostatnia akcja: {{time}}',
            quickActions: 'Szybkie akcje:',
            start: 'Start gry',
            pause: 'Pauza',
            resume: 'Wznów',
            nextRound: 'Następna runda',
            resetAll: 'Zresetuj wszystko',
            closeAllLoadoutCards: 'Zamknij wszystkie karty loadoutu',
            mockNotice:
              'To na razie tylko logika mock. Później te akcje będą wywoływać prawdziwe endpointy backendu i wysyłać aktualizacje przez SignalR.',
          },
          modifiers: {
            loading: 'Ładowanie modyfikatorów (mock)...',
            availableTitle: 'Dostępne modyfikatory',
            activateButton: 'Aktywuj (-{{cost}})',
            activeTitle: 'Aktywne modyfikatory',
            emptyActive: 'Brak aktywnych modyfikatorów.',
            activeFrom: 'Od: {{user}} • {{time}}',
          },
          leaderboard: {
            errorLoading: 'Nie udało się załadować tabeli wyników (mock).',
            mockUpdatedAt: 'Dane mock. Zaktualizowano: {{time}}',
            columns: {
              position: '#',
              team: 'Drużyna',
              score: 'Punkty',
              penalty: 'Kara',
              total: 'Razem',
            },
          },
          authErrors: {
            noPermissionTitle: 'Brak uprawnień',
            noPermissionDescription:
              'Twoja rola nie ma dostępu do tej strony. Zaloguj się jako streamer lub moderator.',
          },
        },
      },
    },
  })

export default i18n


