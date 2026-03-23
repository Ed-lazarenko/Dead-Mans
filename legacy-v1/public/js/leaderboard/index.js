// Загружаем данные таблицы из localStorage или создаём пустой массив
window.leaderboardData = JSON.parse(localStorage.getItem('leaderboardData')) || [];

// ⛑ Добавляем поле penalty командам, у которых его ещё нет
window.leaderboardData.forEach(team => {
  if (typeof team.penalty !== 'number') {
    team.penalty = 0;
  }
});

document.addEventListener('DOMContentLoaded', () => {
  // Рендерим таблицу
  if (typeof createLeaderboard === 'function') {
    createLeaderboard();
  }

  // Настраиваем логику добавления и удаления команд
  if (typeof setupTeamManagement === 'function') {
    setupTeamManagement();
  }

  // Настраиваем модалку редактирования очков
  if (typeof setupScoreEditModal === 'function') {
    setupScoreEditModal();
  }

  if (typeof setupPenaltyEditModal === 'function') {
    setupPenaltyEditModal();
  } 
});
