const translations = {
  en: {
    title: "Dead Man's Game",
    subtitle: 'Sign in with Twitch to open the game board.',
    description: 'After sign-in the panel loads the current game board from the database.',
    button: 'Sign in with Twitch',
    processing: 'Finishing Twitch sign-in...',
    checkingSession: 'Checking your session...',
    callbackFailedTitle: 'Unable to complete sign-in',
    sessionRestoreFailed:
      'Twitch sign-in succeeded, but the app session could not be restored. Please try again.',
    backToLogin: 'Back to sign-in',
    callbackReasons: {
      access_denied: 'You cancelled the Twitch sign-in flow.',
      account_inactive: 'Your account is disabled. Contact an administrator to restore access.',
      missing_code: 'Twitch did not return an authorization code.',
      missing_state: 'Twitch did not return the security state parameter.',
      state_cookie_missing: 'The local sign-in session expired. Please start again.',
      state_mismatch: 'Security validation failed during sign-in. Please try again.',
      authentication_failed:
        'The server could not complete authentication. Please try again later.',
      unknown: 'An unknown error occurred during sign-in.',
    },
  },
  ru: {
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
      account_inactive:
        'Ваша учетная запись деактивирована. Обратитесь к администратору для восстановления доступа.',
      missing_code: 'Twitch не вернул код авторизации.',
      missing_state: 'Twitch не вернул параметр безопасности state.',
      state_cookie_missing: 'Локальная сессия входа устарела. Начните вход заново.',
      state_mismatch: 'Проверка безопасности входа не прошла. Начните вход заново.',
      authentication_failed: 'Не удалось завершить авторизацию на сервере. Попробуйте позже.',
      unknown: 'Во время входа произошла неизвестная ошибка.',
    },
  },
  uk: {
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
      account_inactive:
        'Ваш обліковий запис деактивовано. Зверніться до адміністратора, щоб відновити доступ.',
      missing_code: 'Twitch не повернув код авторизації.',
      missing_state: 'Twitch не повернув параметр безпеки state.',
      state_cookie_missing: 'Локальна сесія входу застаріла. Почніть вхід заново.',
      state_mismatch: 'Перевірка безпеки під час входу не пройшла. Спробуйте ще раз.',
      authentication_failed: 'Сервер не зміг завершити авторизацію. Спробуйте пізніше.',
      unknown: 'Під час входу сталася невідома помилка.',
    },
  },
  pl: {
    title: 'Dead Man’s Game',
    subtitle: 'Zaloguj się przez Twitch, aby otworzyć planszę gry.',
    description: 'Po zalogowaniu panel ładuje aktualną planszę gry z bazy danych.',
    button: 'Zaloguj się przez Twitch',
    processing: 'Kończenie logowania przez Twitch...',
    checkingSession: 'Sprawdzanie sesji...',
    callbackFailedTitle: 'Nie udało się zakończyć logowania',
    sessionRestoreFailed:
      'Logowanie przez Twitch się powiodło, ale nie udało się przywrócić sesji aplikacji. Spróbuj ponownie.',
    backToLogin: 'Wróć do logowania',
    callbackReasons: {
      access_denied: 'Anulowano logowanie przez Twitch.',
      account_inactive:
        'Twoje konto jest wyłączone. Skontaktuj się z administratorem, aby przywrócić dostęp.',
      missing_code: 'Twitch nie zwrócił kodu autoryzacyjnego.',
      missing_state: 'Twitch nie zwrócił parametru bezpieczeństwa state.',
      state_cookie_missing: 'Lokalna sesja logowania wygasła. Rozpocznij ponownie.',
      state_mismatch: 'Weryfikacja bezpieczeństwa logowania nie powiodła się. Spróbuj ponownie.',
      authentication_failed:
        'Serwer nie mógł dokończyć uwierzytelnienia. Spróbuj ponownie później.',
      unknown: 'Wystąpił nieznany błąd podczas logowania.',
    },
  },
}

export default translations
