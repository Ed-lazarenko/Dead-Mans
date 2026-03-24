const ru = {
  translation: {
    appTitle: 'Dead Man’s Game',
    layout: {
      openNavigation: 'Открыть навигацию',
    },
    nav: {
      loadout: 'Лоадауты',
      leaderboard: 'Таблица',
      modifiers: 'Модификаторы',
      controls: 'Управление',
    },
    pages: {
      loadout: 'Лоадауты',
      leaderboard: 'Таблица',
      modifiers: 'Модификаторы',
      controls: 'Управление',
    },
    auth: {
      title: 'Dead Man’s Loadout',
      subtitle: 'Панель управления игрой Dead Man’s Loadout и взаимодействием со зрителями.',
      description: 'Войдите, чтобы управлять командами, лоадаутами и модификаторами зрителей во время стрима.',
      button: 'Войти через Twitch',
      processing: 'Завершаем вход через Twitch...',
      checkingSession: 'Проверяем вашу сессию...',
      callbackFailedTitle: 'Не удалось завершить вход',
      sessionRestoreFailed: 'Twitch подтвердил вход, но сессию приложения восстановить не удалось. Попробуйте войти снова.',
      backToLogin: 'Вернуться ко входу',
      callbackReasons: {
        access_denied: 'Вы отменили вход через Twitch.',
        account_inactive: 'Ваша учетная запись деактивирована. Обратитесь к администратору для восстановления доступа.',
        missing_code: 'Twitch не вернул код авторизации.',
        missing_state: 'Twitch не вернул параметр безопасности state.',
        state_cookie_missing: 'Локальная сессия входа устарела. Начните вход заново.',
        state_mismatch: 'Проверка безопасности входа не прошла. Начните вход заново.',
        authentication_failed: 'Не удалось завершить авторизацию на сервере. Попробуйте позже.',
        unknown: 'Во время входа произошла неизвестная ошибка.',
      },
    },
    loadout: {
      loading: 'Загрузка сетки лоадаутов...',
      errorLoading: 'Не удалось загрузить лоадаут.',
      hint: 'Клик по ячейке сначала раскрывает её, повторный клик открывает картинку на полный экран.',
      hiddenCellLabel: 'Скрыто',
    },
    controls: {
      loading: 'Загрузка состояния игры...',
      errorLoading: 'Не удалось загрузить состояние игры.',
      currentState: 'Текущее состояние игры:',
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
        'Действия используют текущий API-режим. Live-обновления через SignalR будут добавлены позже.',
    },
    modifiers: {
      loading: 'Загрузка модификаторов...',
      errorLoading: 'Не удалось загрузить модификаторы.',
      availableTitle: 'Доступные модификаторы',
      availableLabel: '{{name}} (стоимость: {{cost}})',
      activateButton: 'Активировать (-{{cost}})',
      activeTitle: 'Активные модификаторы',
      emptyActive: 'Пока нет активных модификаторов.',
      activeFrom: 'От: {{user}} • {{time}}',
    },
    leaderboard: {
      loading: 'Загрузка таблицы лидеров...',
      errorLoading: 'Не удалось загрузить таблицу лидеров.',
      updatedAt: 'Обновлено: {{time}}',
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
        'У вашей роли нет доступа к этой странице. Пожалуйста, войдите как администратор или модератор.',
    },
  },
}

export default ru
