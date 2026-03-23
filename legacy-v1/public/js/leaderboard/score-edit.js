// Глобально доступная функция открытия модалки редактирования очков
window.openScoreEditModal = function (teamIndex, scoreIndex, currentValue) {
  const modal = document.getElementById('edit-score-modal');
  const input = document.getElementById('score-input');
  input.value = currentValue;
  modal.classList.remove('leaderboard-hidden');

  // Сохраняем, какая ячейка сейчас редактируется
  window.editingScoreContext = { teamIndex, scoreIndex };
};

// Глобально доступная функция для инициализации модалки редактирования
window.setupScoreEditModal = function () {
  const modal = document.getElementById('edit-score-modal');
  const input = document.getElementById('score-input');

  // Подтверждение редактирования
  document.getElementById('save-edit-score').onclick = () => {
    const newVal = parseInt(input.value);
    if (!isNaN(newVal)) {
      const { teamIndex, scoreIndex } = window.editingScoreContext;
      window.leaderboardData[teamIndex].scores[scoreIndex] = newVal;
      localStorage.setItem('leaderboardData', JSON.stringify(window.leaderboardData));

      // Обновляем таблицу, если функция уже загружена
      if (typeof window.createLeaderboard === 'function') {
        window.createLeaderboard();
      }
    }
    modal.classList.add('leaderboard-hidden');
  };

  // Отмена редактирования
  document.getElementById('cancel-edit-score').onclick = () => {
    modal.classList.add('leaderboard-hidden');
  };
};
