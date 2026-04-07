const pl = {
  translation: {
    appTitle: 'Dead Man’s Game',
    auth: {
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
        account_inactive: 'Twoje konto jest wyłączone. Skontaktuj się z administratorem, aby przywrócić dostęp.',
        missing_code: 'Twitch nie zwrócił kodu autoryzacyjnego.',
        missing_state: 'Twitch nie zwrócił parametru bezpieczeństwa state.',
        state_cookie_missing: 'Lokalna sesja logowania wygasła. Rozpocznij ponownie.',
        state_mismatch: 'Weryfikacja bezpieczeństwa logowania nie powiodła się. Spróbuj ponownie.',
        authentication_failed:
          'Serwer nie mógł dokończyć uwierzytelnienia. Spróbuj ponownie później.',
        unknown: 'Wystąpił nieznany błąd podczas logowania.',
      },
    },
    gameBoard: {
      title: 'Plansza gry',
      loading: 'Ładowanie planszy gry...',
      errorLoading: 'Nie udało się załadować planszy gry.',
      empty: 'W bazie nie ma jeszcze aktywnej ani zakończonej gry.',
      cellLabel: 'Pole',
      closedCellLabel: 'Ukryte',
      costLabel: '{{cost}} pkt',
      statusActive: 'Aktywna',
      statusFinished: 'Zakończona',
      openConfirmTitle: 'Otworzyć kartę?',
      openConfirmDescription:
        'Czy na pewno chcesz otworzyć tę kartę (wiersz {{row}}, kolumna {{col}}, koszt {{cost}})?',
      openCancel: 'Anuluj',
      openConfirm: 'Otwórz',
      openSuccess: 'Karta została otwarta.',
      openForbidden: 'Tylko administrator może otwierać karty.',
      openNotFound: 'Wybrana karta nie została znaleziona.',
      openFailed: 'Nie udało się otworzyć karty.',
    },
  },
}

export default pl
