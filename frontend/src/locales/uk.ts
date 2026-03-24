const uk = {
  translation: {
    appTitle: 'Dead Man’s Game',
    layout: {
      openNavigation: 'Відкрити навігацію',
    },
    nav: {
      loadout: 'Лоадаути',
      leaderboard: 'Таблиця лідерів',
      modifiers: 'Модифікатори',
      controls: 'Керування',
    },
    pages: {
      loadout: 'Лоадаути',
      leaderboard: 'Таблиця лідерів',
      modifiers: 'Модифікатори',
      controls: 'Керування',
    },
    auth: {
      title: 'Dead Man’s Loadout',
      subtitle: 'Панель керування грою Dead Man’s Loadout та взаємодією з глядачами.',
      description: 'Увійдіть, щоб керувати командами, лоадаутами та модифікаторами від глядачів під час стріму.',
      button: 'Увійти через Twitch',
      processing: 'Завершуємо вхід через Twitch...',
      checkingSession: 'Перевіряємо вашу сесію...',
      callbackFailedTitle: 'Не вдалося завершити вхід',
      sessionRestoreFailed:
        'Вхід через Twitch завершився успішно, але сесію застосунку не вдалося відновити. Спробуйте ще раз.',
      backToLogin: 'Повернутися до входу',
      callbackReasons: {
        access_denied: 'Ви скасували вхід через Twitch.',
        account_inactive: 'Ваш обліковий запис деактивовано. Зверніться до адміністратора, щоб відновити доступ.',
        missing_code: 'Twitch не повернув код авторизації.',
        missing_state: 'Twitch не повернув параметр безпеки state.',
        state_cookie_missing: 'Локальна сесія входу застаріла. Почніть вхід заново.',
        state_mismatch: 'Перевірка безпеки під час входу не пройшла. Спробуйте ще раз.',
        authentication_failed: 'Сервер не зміг завершити авторизацію. Спробуйте пізніше.',
        unknown: 'Під час входу сталася невідома помилка.',
      },
    },
    loadout: {
      loading: 'Завантаження сітки лоадаутів...',
      errorLoading: 'Не вдалося завантажити лоадаут.',
      hint: 'Клік по клітинці спочатку відкриває її, повторний клік відкриває картинку на повний екран.',
      hiddenCellLabel: 'Приховано',
    },
    controls: {
      loading: 'Завантаження стану гри...',
      errorLoading: 'Не вдалося завантажити стан гри.',
      currentState: 'Поточний стан гри:',
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
        'Дії використовують поточний API-режим. Live-оновлення через SignalR будуть додані пізніше.',
    },
    modifiers: {
      loading: 'Завантаження модифікаторів...',
      errorLoading: 'Не вдалося завантажити модифікатори.',
      availableTitle: 'Доступні модифікатори',
      availableLabel: '{{name}} (вартість: {{cost}})',
      activateButton: 'Активувати (-{{cost}})',
      activeTitle: 'Активні модифікатори',
      emptyActive: 'Поки немає активних модифікаторів.',
      activeFrom: 'Від: {{user}} • {{time}}',
    },
    leaderboard: {
      loading: 'Завантаження таблиці лідерів...',
      errorLoading: 'Не вдалося завантажити таблицю лідерів.',
      updatedAt: 'Оновлено: {{time}}',
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
        'Ваша роль не має доступу до цієї сторінки. Увійдіть як адміністратор або модератор.',
    },
  },
}

export default uk
