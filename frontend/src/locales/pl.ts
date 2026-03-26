const pl = {
  translation: {
    appTitle: 'Dead Man’s Game',
    layout: {
      openNavigation: 'Otwórz nawigację',
    },
    nav: {
      loadout: 'Loadouty',
      leaderboard: 'Tabela wyników',
      modifiers: 'Modyfikatory',
      controls: 'Sterowanie',
    },
    pages: {
      loadout: 'Loadouty',
      leaderboard: 'Tabela wyników',
      modifiers: 'Modyfikatory',
      controls: 'Sterowanie',
    },
    auth: {
      title: 'Dead Man’s Loadout',
      subtitle: 'Panel sterowania grą Dead Man’s Loadout i interakcją z widzami.',
      description: 'Zaloguj się, aby zarządzać drużynami, loadoutami i modyfikatorami widzów podczas streamu.',
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
    loadout: {
      loading: 'Ładowanie siatki loadoutów...',
      errorLoading: 'Nie udało się załadować loadoutu.',
      hint: 'Pierwsze kliknięcie odkrywa kartę, drugie otwiera obraz w trybie pełnoekranowym.',
      hiddenCellLabel: 'Ukryte',
    },
    controls: {
      loading: 'Ładowanie stanu gry...',
      errorLoading: 'Nie udało się załadować stanu gry.',
      currentState: 'Aktualny stan gry:',
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
    },
    modifiers: {
      loading: 'Ładowanie modyfikatorów...',
      errorLoading: 'Nie udało się załadować modyfikatorów.',
      availableTitle: 'Dostępne modyfikatory',
      availableLabel: '{{name}} (koszt: {{cost}})',
      activateButton: 'Aktywuj (-{{cost}})',
      activeTitle: 'Aktywne modyfikatory',
      emptyActive: 'Brak aktywnych modyfikatorów.',
      activeFrom: 'Od: {{user}} • {{time}}',
    },
    leaderboard: {
      loading: 'Ładowanie tabeli wyników...',
      errorLoading: 'Nie udało się załadować tabeli wyników.',
      updatedAt: 'Zaktualizowano: {{time}}',
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
}

export default pl
