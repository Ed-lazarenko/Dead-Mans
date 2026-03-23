const uk = {
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
      description: 'Увійдіть, щоб керувати командами, лоадаутами та модифікаторами від глядачів під час стріму.',
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
        'Тут поки що тільки mock-логіка. У майбутньому ці дії викликатимуть реальні endpoint’и backend та надсилатимуть оновлення через SignalR.',
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
}

export default uk
