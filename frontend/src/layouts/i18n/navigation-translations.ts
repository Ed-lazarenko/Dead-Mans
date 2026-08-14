const translations = {
  en: {
    primary: 'Primary navigation',
    adminNavigation: 'Administration navigation',
    menus: {
      currentGame: 'Current game',
      history: 'History',
      gameSetup: 'Game setup',
      globalSettings: 'Global settings',
    },
    profile: 'Profile',
    notifications: 'Notifications',
    openNotifications: 'Open notifications',
    notificationCount: '{{count}} important notification(s)',
    notificationsEmpty: 'No important notifications right now.',
    invitations: 'Invitations',
    openInvitations: 'Open game invitations',
    openApplicationPage: 'Go to game application page',
    openInvitationsPage: 'Go to game application page',
    invitationCount: '{{count}} pending',
    invitationsEmpty: 'No new invitations right now.',
    invitationItemTitle: '{{player}} invited you to a team',
    invitationItemDescription: 'Open the application page to respond. Slot {{slot}}.',
    disbandRequestItemTitle: '{{player}} requested team disband',
    disbandRequestItemDescription: 'Open team management to resolve it. Queue position {{slot}}.',
    modifierCancelledItemTitle: 'Modifier "{{modifier}}" was cancelled',
    modifierCancelledItemDescription:
      '{{player}} refunded {{points}} quiz points. Open modifiers to review the change.',
    genericNotificationTitle: 'New notification',
    genericNotificationDescription: 'Open the relevant page for details.',
    modifierFallback: 'Unknown modifier',
    someone: 'Someone',
    administration: 'Administration',
    language: 'Interface language',
    logout: 'Log out',
    roles: {
      admin: 'Administrator',
      moderator: 'Moderator',
      viewer: 'Participant',
    },
    items: {
      gameHistory: {
        label: 'Game history',
      },
      gameBoard: {
        label: 'Game',
      },
      gameLeaderboard: {
        label: 'Leaderboard',
      },
      gameApplication: {
        label: 'Apply',
      },
      gameQuiz: {
        label: 'Quiz',
      },
      gameSetup: {
        label: 'Board setup',
      },
      adminModifiers: {
        label: 'Modifier setup',
      },
      adminQuestions: {
        label: 'Question setup',
      },
      catalogModifiers: {
        label: 'Modifier setup',
      },
      catalogQuestions: {
        label: 'Question setup',
      },
    },
  },
  ru: {
    primary: 'Основная навигация',
    adminNavigation: 'Навигация администратора',
    menus: {
      currentGame: 'Текущая игра',
      history: 'История',
      gameSetup: 'Настройка игры',
      globalSettings: 'Глобальные настройки',
    },
    profile: 'Профиль',
    notifications: 'Уведомления',
    openNotifications: 'Открыть уведомления',
    notificationCount: 'Важных уведомлений: {{count}}',
    notificationsEmpty: 'Сейчас важных уведомлений нет.',
    invitations: 'Приглашения',
    openInvitations: 'Открыть приглашения в игру',
    openApplicationPage: 'Перейти на страницу заявок',
    openInvitationsPage: 'Перейти на страницу заявок',
    invitationCount: 'В ожидании: {{count}}',
    invitationsEmpty: 'Сейчас новых приглашений нет.',
    invitationItemTitle: '{{player}} пригласил вас в команду',
    invitationItemDescription: 'Откройте страницу заявок, чтобы ответить. Слот {{slot}}.',
    disbandRequestItemTitle: '{{player}} просит распустить команду',
    disbandRequestItemDescription:
      'Откройте управление командами, чтобы решить запрос. Очередь {{slot}}.',
    modifierCancelledItemTitle: 'Модификатор «{{modifier}}» отменён',
    modifierCancelledItemDescription:
      '{{player}} вернул вам {{points}} очк. Откройте модификаторы, чтобы проверить изменения.',
    genericNotificationTitle: 'Новое уведомление',
    genericNotificationDescription: 'Откройте нужную страницу, чтобы посмотреть детали.',
    modifierFallback: 'Неизвестный модификатор',
    someone: 'Кто-то',
    administration: 'Администрирование',
    language: 'Язык интерфейса',
    logout: 'Выйти',
    roles: {
      admin: 'Администратор',
      moderator: 'Модератор',
      viewer: 'Участник',
    },
    items: {
      gameHistory: {
        label: 'История игр',
      },
      gameBoard: {
        label: 'Игра',
      },
      gameLeaderboard: {
        label: 'Лидерборд',
      },
      gameApplication: {
        label: 'Подать заявку',
      },
      gameQuiz: {
        label: 'Викторина',
      },
      gameSetup: {
        label: 'Настройка доски',
      },
      adminModifiers: {
        label: 'Настройка модификаторов',
      },
      adminQuestions: {
        label: 'Настройка вопросов',
      },
      catalogModifiers: {
        label: 'Настройка модификаторов',
      },
      catalogQuestions: {
        label: 'Настройка вопросов',
      },
    },
  },
  uk: {
    primary: 'Основна навігація',
    adminNavigation: 'Навігація адміністратора',
    menus: {
      currentGame: 'Поточна гра',
      history: 'Історія',
      gameSetup: 'Налаштування гри',
      globalSettings: 'Глобальні налаштування',
    },
    profile: 'Профіль',
    notifications: 'Сповіщення',
    openNotifications: 'Відкрити сповіщення',
    notificationCount: 'Важливих сповіщень: {{count}}',
    notificationsEmpty: 'Зараз важливих сповіщень немає.',
    invitations: 'Запрошення',
    openInvitations: 'Відкрити запрошення до гри',
    openApplicationPage: 'Перейти на сторінку заявок',
    openInvitationsPage: 'Перейти на сторінку заявок',
    invitationCount: 'Очікує: {{count}}',
    invitationsEmpty: 'Нових запрошень зараз немає.',
    invitationItemTitle: '{{player}} запросив вас до команди',
    invitationItemDescription: 'Відкрийте сторінку заявок, щоб відповісти. Слот {{slot}}.',
    disbandRequestItemTitle: '{{player}} просить розформувати команду',
    disbandRequestItemDescription:
      'Відкрийте керування командами, щоб вирішити запит. Черга {{slot}}.',
    modifierCancelledItemTitle: 'Модифікатор «{{modifier}}» скасовано',
    modifierCancelledItemDescription:
      '{{player}} повернув вам {{points}} очк. Відкрийте модифікатори, щоб перевірити зміни.',
    genericNotificationTitle: 'Нове сповіщення',
    genericNotificationDescription: 'Відкрийте потрібну сторінку, щоб побачити деталі.',
    modifierFallback: 'Невідомий модифікатор',
    someone: 'Хтось',
    administration: 'Адміністрування',
    language: 'Мова інтерфейсу',
    logout: 'Вийти',
    roles: {
      admin: 'Адміністратор',
      moderator: 'Модератор',
      viewer: 'Учасник',
    },
    items: {
      gameHistory: {
        label: 'Історія ігор',
      },
      gameBoard: {
        label: 'Гра',
      },
      gameLeaderboard: {
        label: 'Лідерборд',
      },
      gameApplication: {
        label: 'Подати заявку',
      },
      gameQuiz: {
        label: 'Вікторина',
      },
      gameSetup: {
        label: 'Налаштування дошки',
      },
      adminModifiers: {
        label: 'Налаштування модифікаторів',
      },
      adminQuestions: {
        label: 'Налаштування питань',
      },
      catalogModifiers: {
        label: 'Налаштування модифікаторів',
      },
      catalogQuestions: {
        label: 'Налаштування питань',
      },
    },
  },
  pl: {
    primary: 'Główna nawigacja',
    adminNavigation: 'Nawigacja administratora',
    menus: {
      currentGame: 'Bieżąca gra',
      history: 'Historia',
      gameSetup: 'Konfiguracja gry',
      globalSettings: 'Ustawienia globalne',
    },
    profile: 'Profil',
    notifications: 'Powiadomienia',
    openNotifications: 'Otwórz powiadomienia',
    notificationCount: 'Ważne powiadomienia: {{count}}',
    notificationsEmpty: 'Brak ważnych powiadomień.',
    invitations: 'Zaproszenia',
    openInvitations: 'Otwórz zaproszenia do gry',
    openApplicationPage: 'Przejdź do strony zgłoszeń',
    openInvitationsPage: 'Przejdź do strony zgłoszeń',
    invitationCount: 'Oczekujące: {{count}}',
    invitationsEmpty: 'Brak nowych zaproszeń.',
    invitationItemTitle: '{{player}} zaprosił Cię do drużyny',
    invitationItemDescription: 'Otwórz stronę zgłoszeń, aby odpowiedzieć. Slot {{slot}}.',
    disbandRequestItemTitle: '{{player}} prosi o rozwiązanie drużyny',
    disbandRequestItemDescription:
      'Otwórz zarządzanie drużynami, aby rozwiązać zgłoszenie. Kolejka {{slot}}.',
    modifierCancelledItemTitle: 'Anulowano modyfikator „{{modifier}}”',
    modifierCancelledItemDescription:
      '{{player}} zwrócił {{points}} pkt quizowych. Otwórz modyfikatory, aby sprawdzić zmianę.',
    genericNotificationTitle: 'Nowe powiadomienie',
    genericNotificationDescription: 'Otwórz odpowiednią stronę, aby zobaczyć szczegóły.',
    modifierFallback: 'Nieznany modyfikator',
    someone: 'Ktoś',
    administration: 'Administracja',
    language: 'Język interfejsu',
    logout: 'Wyloguj się',
    roles: {
      admin: 'Administrator',
      moderator: 'Moderator',
      viewer: 'Uczestnik',
    },
    items: {
      gameHistory: {
        label: 'Historia gier',
      },
      gameBoard: {
        label: 'Gra',
      },
      gameLeaderboard: {
        label: 'Ranking',
      },
      gameApplication: {
        label: 'Zgłoś się',
      },
      gameQuiz: {
        label: 'Quiz',
      },
      gameSetup: {
        label: 'Konfiguracja planszy',
      },
      adminModifiers: {
        label: 'Konfiguracja modyfikatorów',
      },
      adminQuestions: {
        label: 'Konfiguracja pytań',
      },
      catalogModifiers: {
        label: 'Konfiguracja modyfikatorów',
      },
      catalogQuestions: {
        label: 'Konfiguracja pytań',
      },
    },
  },
}

export default translations
