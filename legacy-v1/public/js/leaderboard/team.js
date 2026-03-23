/**
 * Настройка добавления/удаления команд и выбора активной команды
 */
window.setupTeamManagement = function () {
  // ===== Добавление команды =====

  // Открытие модалки добавления
  document.getElementById('add-team-button')
    .addEventListener('click', () => {
      document.getElementById('leaderboard-modal')
        .classList.remove('leaderboard-hidden');
    });

  // Сохранение новой команды
  document.getElementById('save-team')
    .addEventListener('click', () => {
      const p1 = document.getElementById('player1').value.trim();
      const p2 = document.getElementById('player2').value.trim();
      const p3 = document.getElementById('player3').value.trim();
      const players = [p1, p2, p3].filter(Boolean);
      if (players.length === 0) return;

      // Генерируем уникальный цвет
      const teamColor = getTeamColor(players);

      // Добавляем команду с цветом
      window.leaderboardData.push({ players, scores: [], penalty: 0, color: teamColor });
      localStorage.setItem('leaderboardData', JSON.stringify(window.leaderboardData));

      // Очистка и перерисовка
      ['player1', 'player2', 'player3'].forEach(id => document.getElementById(id).value = '');
      document.getElementById('leaderboard-modal').classList.add('leaderboard-hidden');
      window.createLeaderboard?.();
    });

  // Отмена добавления
  document.getElementById('cancel-team')
    .addEventListener('click', () => {
      document.getElementById('leaderboard-modal')
        .classList.add('leaderboard-hidden');
    });

  // ===== Удаление команды =====

  document.getElementById('remove-team-button')
    .addEventListener('click', () => {
      const select = document.getElementById('team-select');
      select.innerHTML = '';
      window.leaderboardData.forEach((team, index) => {
        const opt = document.createElement('option');
        opt.value = index;
        opt.textContent = team.players.join(' / ');
        select.appendChild(opt);
      });
      document.getElementById('remove-team-modal')
        .classList.remove('leaderboard-hidden');
    });

  document.getElementById('cancel-remove')
    .addEventListener('click', () => {
      document.getElementById('remove-team-modal')
        .classList.add('leaderboard-hidden');
    });

  document.getElementById('confirm-remove')
    .addEventListener('click', () => {
      const select = document.getElementById('team-select');
      const idx = parseInt(select.value, 10);
      if (!isNaN(idx)) {
        window.leaderboardData.splice(idx, 1);
        localStorage.setItem('leaderboardData', JSON.stringify(window.leaderboardData));
        document.getElementById('remove-team-modal')
          .classList.add('leaderboard-hidden');
        if (typeof window.createLeaderboard === 'function') {
          window.createLeaderboard();
        }
      }
    });

  // ===== Выбор активной команды через модалку =====
  document.getElementById('choose-active-team-button').addEventListener('click', () => {
    const modal = document.getElementById('choose-active-team-modal');
    const select = document.getElementById('choose-active-team-select');
    select.innerHTML = '';

    window.leaderboardData.forEach(team => {
      const opt = document.createElement('option');
      opt.value = JSON.stringify(team.players);
      opt.textContent = team.players.join(' / ');
      select.appendChild(opt);
    });

    modal.classList.remove('leaderboard-hidden');
  });

  document.getElementById('cancel-active-team').addEventListener('click', () => {
    document.getElementById('choose-active-team-modal').classList.add('leaderboard-hidden');
  });

  document.getElementById('confirm-active-team').addEventListener('click', () => {
    const select = document.getElementById('choose-active-team-select');
    const teamJSON = select.value;
    localStorage.setItem('activeTeam', teamJSON);
    document.getElementById('choose-active-team-modal').classList.add('leaderboard-hidden');

    if (typeof window.createLeaderboard === 'function') window.createLeaderboard();
    if (typeof window.updateActiveTeamDisplay === 'function') window.updateActiveTeamDisplay();
  });


  // ===== Добавление новой игры =====

  document.getElementById('add-game-button')
    .addEventListener('click', () => {
      window.leaderboardData.forEach(team => team.scores.push(''));
      localStorage.setItem('leaderboardData', JSON.stringify(window.leaderboardData));
      if (typeof window.createLeaderboard === 'function') {
        window.createLeaderboard();
      }
    });

  function getTeamColor(team) {
    const joined = team.slice().sort().join(',').toLowerCase();
    let hash = 0;
    for (let i = 0; i < joined.length; i++) {
      hash = joined.charCodeAt(i) + ((hash << 5) - hash);
    }
    const hue = Math.abs(hash) % 360;
    const saturation = 65;
    const lightness = 30;
    return `hsl(${hue}, ${saturation}%, ${lightness}%)`;
  }
};
