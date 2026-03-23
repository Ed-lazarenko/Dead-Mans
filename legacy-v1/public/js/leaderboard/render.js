// js/leaderboard/render.js

/* ==========================================================
   Leaderboard · рендер + сортировка по лучшему результату
   ========================================================== */
window.createLeaderboard = function () {
  console.log('Создание таблицы…', window.leaderboardData);

  const area = document.getElementById('leaderboard-area');
  const table = document.createElement('table');
  table.className = 'leaderboard-table';

  /* ---------- вычисляем “лучший счёт” и сортируем ---------- */
  const dataSorted = [...window.leaderboardData].sort((a, b) => {
    const bestA = Math.max(0, ...a.scores.filter(n => typeof n === 'number')) - (a.penalty ?? 0);
    const bestB = Math.max(0, ...b.scores.filter(n => typeof n === 'number')) - (b.penalty ?? 0);
    return bestB - bestA;               // по убыванию
  });

  /* ---------- Шапка ---------- */
  const thead = document.createElement('thead');
  const headerRow = document.createElement('tr');

  headerRow.appendChild(createHeaderCell('🏅'));   // колонка‑иконка
  headerRow.appendChild(createHeaderCell('Команда'));

  const gameCount = Math.max(0, ...dataSorted.map(t => t.scores.length));
  for (let i = 0; i < gameCount; i++) {
    headerRow.appendChild(createHeaderCell(`Игра ${i + 1}`));
  }

  headerRow.appendChild(createHeaderCell('Штраф', 'penalty-header'));
  headerRow.appendChild(createHeaderCell('Итог', 'total-header'));
  
  thead.appendChild(headerRow);
  table.appendChild(thead);

  /* ---------- Тело таблицы ---------- */
  const tbody = document.createElement('tbody');
  const activeTeamJSON = localStorage.getItem('activeTeam');

  const rankEmoji = ['🥇', '🥈', '🥉'];
  const defaultEmoji = '🎖️';

  dataSorted.forEach((team, index) => {
    const row = document.createElement('tr');

    if (activeTeamJSON && activeTeamJSON === JSON.stringify(team.players)) {
      row.classList.add('active-team');
    
      // 💡 Неоновый цвет из сохранённого цвета команды
      if (team.color) {
        row.style.boxShadow = `0 0 12px 2px ${team.color}`;
      }
    }

    // 🥇 Значок места
    const placeCell = document.createElement('td');
    placeCell.textContent = rankEmoji[index] ?? defaultEmoji;
    row.appendChild(placeCell);

    // 👥 Команда
    const teamCell = document.createElement('td');
    teamCell.classList.add('team-cell');
    teamCell.style.whiteSpace = 'pre-line';
    teamCell.style.textAlign = 'center';

    if (team.color) {
      teamCell.style.borderLeft = `3px solid ${team.color}`;
      teamCell.style.boxShadow = `inset 0 0 10px ${team.color}`;
    }
    

    team.players.forEach((name, i) => {
      const span = document.createElement('span');
      span.className = `nick${(i % 3) + 1}`;
      span.textContent = name;
      span.style.display = 'block';
      teamCell.appendChild(span);
    });
    row.appendChild(teamCell);

    // 🎯 Очки по играм
    for (let i = 0; i < gameCount; i++) {
      const scoreCell = document.createElement('td');
      scoreCell.textContent = team.scores[i] ?? '';
      scoreCell.addEventListener('contextmenu', e => {
        e.preventDefault();
        window.openScoreEditModal?.(
          window.leaderboardData.indexOf(team),
          i,
          team.scores[i] ?? ''
        );
      });
      row.appendChild(scoreCell);
    }

    // 💣 Штраф
    const penaltyCell = document.createElement('td');
    penaltyCell.classList.add('penalty-cell');
    penaltyCell.textContent = team.penalty ?? 0;
    penaltyCell.addEventListener('contextmenu', e => {
      e.preventDefault();
      window.openPenaltyEditModal?.(
        window.leaderboardData.indexOf(team),
        team.penalty ?? 0
      );
    });
    row.appendChild(penaltyCell);


    // 📊 Итог = максимум - штраф
    const bestScore = Math.max(0, ...team.scores.filter(n => typeof n === 'number'));
    const penalty = team.penalty ?? 0;
    const totalCell = document.createElement('td');
    totalCell.classList.add('total-cell');
    totalCell.textContent = bestScore > 0 ? bestScore - penalty : '–';
    row.appendChild(totalCell);

    tbody.appendChild(row);
  });

  table.appendChild(tbody);
  area.innerHTML = '';
  area.appendChild(table);
};

// утилита для ячеек‑заголовков
function createHeaderCell(text, className) {
  const th = document.createElement('th');
  th.textContent = text;
  if (className) th.classList.add(className);
  return th;
}

