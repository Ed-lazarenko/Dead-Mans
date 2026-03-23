const pl = {
  translation: {
    appTitle: 'Dead Man’s Game',
    nav: {
      loadout: 'Loadouty',
      leaderboard: 'Tabela wyników',
      modifiers: 'Modyfikatory',
      controls: 'Sterowanie',
    },
    pages: {
      loadout: 'Placeholder strony loadoutów',
      leaderboard: 'Placeholder strony tabeli wyników',
      modifiers: 'Placeholder strony modyfikatorów',
      controls: 'Placeholder strony panelu sterowania',
    },
    auth: {
      title: 'Dead Man’s Loadout',
      subtitle: 'Panel sterowania grą Dead Man’s Loadout i interakcją z widzami.',
      description: 'Zaloguj się, aby zarządzać drużynami, loadoutami i modyfikatorami widzów podczas streamu.',
      button: 'Zaloguj się przez Twitch',
    },
    loadout: {
      loading: 'Ładowanie siatki loadoutów (mock)...',
      hint: 'Dane mock. Pierwsze kliknięcie odkrywa kartę, drugie otwiera obraz w trybie pełnoekranowym.',
    },
    controls: {
      loading: 'Ładowanie stanu gry (mock)...',
      currentState: 'Aktualny stan gry (mock):',
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
      mockNotice:
        'To na razie tylko logika mock. Później te akcje będą wywoływać prawdziwe endpointy backendu i wysyłać aktualizacje przez SignalR.',
    },
    modifiers: {
      loading: 'Ładowanie modyfikatorów (mock)...',
      availableTitle: 'Dostępne modyfikatory',
      activateButton: 'Aktywuj (-{{cost}})',
      activeTitle: 'Aktywne modyfikatory',
      emptyActive: 'Brak aktywnych modyfikatorów.',
      activeFrom: 'Od: {{user}} • {{time}}',
    },
    leaderboard: {
      errorLoading: 'Nie udało się załadować tabeli wyników (mock).',
      mockUpdatedAt: 'Dane mock. Zaktualizowano: {{time}}',
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
