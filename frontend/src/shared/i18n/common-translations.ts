const translations = {
  en: {
    actions: {
      back: 'Back',
      cancel: 'Cancel',
      close: 'Close',
      open: 'Open',
      openCard: 'Open card',
      viewCard: 'View card',
      remove: 'Remove',
      next: 'Next',
      retry: 'Retry',
      save: 'Save',
    },
    teamWithSlot: 'Team #{{slot}}',
    scoreBreakdown: {
      title: 'How the final score was calculated',
      authoritative:
        'Confirmed server calculation. Each line shows its source and effect on the total.',
      final: 'Final round score',
      modifierTitle: '{{name}} ×{{count}}',
      runningTotal: 'Running total: {{value}}',
      kind: { kills: 'Kills', bounties: 'Bounties', emptyCardPenalty: 'Empty-card penalty' },
      formula: {
        units: '{{count}} × {{unit}} = {{result}}',
        emptyPenalty: 'No kills, bounties, or bonus kills: −{{cardValue}}.',
        growing:
          'Bonus to one kill: {{increment}} × {{kills}} kills × {{activations}} activations = {{bonusPerKill}}. New kill value: {{cardValue}} + {{bonusPerKill}} = {{adjustedKillValue}}. Kills total: {{adjustedKillValue}} × {{kills}} = {{adjustedKillsScore}}. Contribution above the base {{baseKillsScore}}: +{{result}}.',
        growingZero: 'No kills: −{{penalty}} × {{activations}} activations = {{result}}.',
        bonusKills: '{{bonusKills}} bonus kills × {{cardValue}} = {{result}}.',
        windowBonus: '{{count}} qualifying kills × {{cardValue}} × {{rate}}% = {{result}}.',
        delta: 'Modifier contribution: {{result}}.',
      },
    },
    entities: {
      categories: 'Categories',
      modifiers: 'Modifiers',
      player: 'Player',
      players: 'Players',
      team: 'Team',
      teams: 'Teams',
    },
    filters: {
      allCategories: 'All categories',
    },
    modifiers: {
      searchLabel: 'Search modifiers',
      emptySearch: 'No modifiers match your search.',
      categories: {
        preparation: 'Before the round',
        round: 'During the round',
        result: 'Affects the round result',
      },
    },
  },
  ru: {
    actions: {
      back: 'Назад',
      cancel: 'Отмена',
      close: 'Закрыть',
      open: 'Открыть',
      openCard: 'Открыть карточку',
      viewCard: 'Посмотреть карточку',
      remove: 'Удалить',
      next: 'Далее',
      retry: 'Повторить',
      save: 'Сохранить',
    },
    teamWithSlot: 'Команда #{{slot}}',
    scoreBreakdown: {
      title: 'Как рассчитан итог',
      authoritative:
        'Подтверждённый расчёт сервера. В каждой строке указаны источник очков и влияние на итог.',
      final: 'Финальный счёт раунда',
      modifierTitle: '{{name}} ×{{count}}',
      runningTotal: 'Промежуточный итог: {{value}}',
      kind: {
        kills: 'Убийства',
        bounties: 'Награды',
        emptyCardPenalty: 'Штраф за пустую карточку',
      },
      formula: {
        units: '{{count}} × {{unit}} = {{result}}',
        emptyPenalty: 'Нет убийств, наград и бонусных убийств: −{{cardValue}}.',
        growing:
          'Бонус к одному убийству: {{increment}} × {{kills}} убийств × {{activations}} активаций = {{bonusPerKill}}. Новая стоимость убийства: {{cardValue}} + {{bonusPerKill}} = {{adjustedKillValue}}. Очки за убийства: {{adjustedKillValue}} × {{kills}} = {{adjustedKillsScore}}. Вклад сверх базовых {{baseKillsScore}}: +{{result}}.',
        growingZero: 'Убийств нет: −{{penalty}} × {{activations}} активаций = {{result}}.',
        bonusKills: '{{bonusKills}} бонусных убийств × {{cardValue}} = {{result}}.',
        windowBonus: '{{count}} подходящих убийств × {{cardValue}} × {{rate}}% = {{result}}.',
        delta: 'Вклад модификатора: {{result}}.',
      },
    },
    entities: {
      categories: 'Категории',
      modifiers: 'Модификаторы',
      player: 'Игрок',
      players: 'Игроки',
      team: 'Команда',
      teams: 'Команды',
    },
    filters: {
      allCategories: 'Все категории',
    },
    modifiers: {
      searchLabel: 'Поиск модификаторов',
      emptySearch: 'По вашему запросу модификаторы не найдены.',
      categories: {
        preparation: 'Перед раундом',
        round: 'Во время раунда',
        result: 'На итог раунда',
      },
    },
  },
  uk: {
    actions: {
      back: 'Назад',
      cancel: 'Скасувати',
      close: 'Закрити',
      open: 'Відкрити',
      openCard: 'Відкрити картку',
      viewCard: 'Переглянути картку',
      remove: 'Видалити',
      next: 'Далі',
      retry: 'Повторити',
      save: 'Зберегти',
    },
    teamWithSlot: 'Команда #{{slot}}',
    scoreBreakdown: {
      title: 'Як розраховано підсумок',
      authoritative:
        'Підтверджений розрахунок сервера. У кожному рядку вказано джерело очок і вплив на підсумок.',
      final: 'Фінальний рахунок раунду',
      modifierTitle: '{{name}} ×{{count}}',
      runningTotal: 'Проміжний підсумок: {{value}}',
      kind: {
        kills: 'Вбивства',
        bounties: 'Нагороди',
        emptyCardPenalty: 'Штраф за порожню картку',
      },
      formula: {
        units: '{{count}} × {{unit}} = {{result}}',
        emptyPenalty: 'Немає вбивств, нагород і бонусних вбивств: −{{cardValue}}.',
        growing:
          'Бонус до одного вбивства: {{increment}} × {{kills}} вбивств × {{activations}} активацій = {{bonusPerKill}}. Нова вартість вбивства: {{cardValue}} + {{bonusPerKill}} = {{adjustedKillValue}}. Очки за вбивства: {{adjustedKillValue}} × {{kills}} = {{adjustedKillsScore}}. Внесок понад базові {{baseKillsScore}}: +{{result}}.',
        growingZero: 'Вбивств немає: −{{penalty}} × {{activations}} активацій = {{result}}.',
        bonusKills: '{{bonusKills}} бонусних вбивств × {{cardValue}} = {{result}}.',
        windowBonus: '{{count}} відповідних вбивств × {{cardValue}} × {{rate}}% = {{result}}.',
        delta: 'Внесок модифікатора: {{result}}.',
      },
    },
    entities: {
      categories: 'Категорії',
      modifiers: 'Модифікатори',
      player: 'Гравець',
      players: 'Гравці',
      team: 'Команда',
      teams: 'Команди',
    },
    filters: {
      allCategories: 'Усі категорії',
    },
    modifiers: {
      searchLabel: 'Пошук модифікаторів',
      emptySearch: 'За вашим запитом модифікаторів не знайдено.',
      categories: {
        preparation: 'Перед раундом',
        round: 'Під час раунду',
        result: 'На підсумок раунду',
      },
    },
  },
  pl: {
    actions: {
      back: 'Wstecz',
      cancel: 'Anuluj',
      close: 'Zamknij',
      open: 'Otwórz',
      openCard: 'Otwórz kartę',
      viewCard: 'Zobacz kartę',
      remove: 'Usuń',
      next: 'Dalej',
      retry: 'Ponów',
      save: 'Zapisz',
    },
    teamWithSlot: 'Drużyna #{{slot}}',
    scoreBreakdown: {
      title: 'Jak obliczono wynik',
      authoritative:
        'Potwierdzone obliczenie serwera. Każdy wiersz pokazuje źródło punktów i wpływ na wynik.',
      final: 'Końcowy wynik rundy',
      modifierTitle: '{{name}} ×{{count}}',
      runningTotal: 'Suma częściowa: {{value}}',
      kind: { kills: 'Zabójstwa', bounties: 'Nagrody', emptyCardPenalty: 'Kara za pustą kartę' },
      formula: {
        units: '{{count}} × {{unit}} = {{result}}',
        emptyPenalty: 'Brak zabójstw, nagród i zabójstw bonusowych: −{{cardValue}}.',
        growing:
          'Bonus do jednego zabójstwa: {{increment}} × {{kills}} zabójstw × {{activations}} aktywacji = {{bonusPerKill}}. Nowa wartość zabójstwa: {{cardValue}} + {{bonusPerKill}} = {{adjustedKillValue}}. Punkty za zabójstwa: {{adjustedKillValue}} × {{kills}} = {{adjustedKillsScore}}. Wkład ponad bazowe {{baseKillsScore}}: +{{result}}.',
        growingZero: 'Brak zabójstw: −{{penalty}} × {{activations}} aktywacji = {{result}}.',
        bonusKills: '{{bonusKills}} zabójstw bonusowych × {{cardValue}} = {{result}}.',
        windowBonus: '{{count}} pasujących zabójstw × {{cardValue}} × {{rate}}% = {{result}}.',
        delta: 'Wkład modyfikatora: {{result}}.',
      },
    },
    entities: {
      categories: 'Kategorie',
      modifiers: 'Modyfikatory',
      player: 'Gracz',
      players: 'Gracze',
      team: 'Drużyna',
      teams: 'Drużyny',
    },
    filters: {
      allCategories: 'Wszystkie kategorie',
    },
    modifiers: {
      searchLabel: 'Szukaj modyfikatorów',
      emptySearch: 'Brak modyfikatorów pasujących do wyszukiwania.',
      categories: {
        preparation: 'Przed rundą',
        round: 'W trakcie rundy',
        result: 'Na wynik rundy',
      },
    },
  },
}

export default translations
