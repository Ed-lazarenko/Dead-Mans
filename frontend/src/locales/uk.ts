const uk = {
  translation: {
    appTitle: 'Dead Man’s Game',
    auth: {
      title: 'Dead Man’s Game',
      subtitle: 'Увійдіть через Twitch, щоб відкрити ігрове поле.',
      description: 'Після входу панель завантажує поточне ігрове поле з бази даних.',
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
    gameBoard: {
      title: 'Ігрове поле',
      loading: 'Завантаження ігрового поля...',
      errorLoading: 'Не вдалося завантажити ігрове поле.',
      empty: 'У базі ще немає активної або завершеної гри.',
      cellLabel: 'Клітинка',
      closedCellLabel: 'Приховано',
      costLabel: '{{cost}} оч.',
      statusActive: 'Активна',
      statusFinished: 'Завершена',
      openConfirmTitle: 'Відкрити картку?',
      openConfirmDescription:
        'Ви впевнені, що хочете відкрити цю картку (ряд {{row}}, колонка {{col}}, вартість {{cost}})?',
      openCancel: 'Скасувати',
      openConfirm: 'Відкрити',
      openSuccess: 'Картку відкрито.',
      openForbidden: 'Відкривати картки може лише адміністратор.',
      openNotFound: 'Вибрану картку не знайдено.',
      openFailed: 'Не вдалося відкрити картку.',
    },
  },
}

export default uk
