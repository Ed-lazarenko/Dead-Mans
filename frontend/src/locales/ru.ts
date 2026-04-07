const ru = {
  translation: {
    appTitle: 'Dead Man’s Game',
    auth: {
      title: 'Dead Man’s Game',
      subtitle: 'Войдите через Twitch, чтобы открыть игровое поле.',
      description: 'После входа панель загружает текущее игровое поле из базы данных.',
      button: 'Войти через Twitch',
      processing: 'Завершаем вход через Twitch...',
      checkingSession: 'Проверяем вашу сессию...',
      callbackFailedTitle: 'Не удалось завершить вход',
      sessionRestoreFailed:
        'Twitch подтвердил вход, но сессию приложения восстановить не удалось. Попробуйте войти снова.',
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
    gameBoard: {
      title: 'Игровое поле',
      loading: 'Загрузка игрового поля...',
      errorLoading: 'Не удалось загрузить игровое поле.',
      empty: 'В базе пока нет активной или завершённой игры.',
      cellLabel: 'Ячейка',
      closedCellLabel: 'Скрыто',
      costLabel: '{{cost}} очк.',
      statusActive: 'Активна',
      statusFinished: 'Завершена',
      openConfirmTitle: 'Открыть карточку?',
      openConfirmDescription:
        'Вы уверены, что хотите открыть эту карточку (ряд {{row}}, колонка {{col}}, стоимость {{cost}})?',
      openCancel: 'Отмена',
      openConfirm: 'Открыть',
      openSuccess: 'Карточка открыта.',
      openForbidden: 'Открывать карточки может только администратор.',
      openNotFound: 'Выбранная карточка не найдена.',
      openFailed: 'Не удалось открыть карточку.',
    },
  },
}

export default ru
