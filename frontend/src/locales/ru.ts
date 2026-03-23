const ru = {
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
        'Здесь пока только mock-логика. В будущем эти действия будут вызывать реальные эндпоинты backend и рассылать обновления через SignalR.',
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
}

export default ru
