document.addEventListener('DOMContentLoaded', () => {
    /* кнопка «Сбросить всё» */
    const resetBtn = document.getElementById('reset-storage-btn');
    if(!resetBtn) return;        // если кнопка отсутствует – ничего не делаем
  
    resetBtn.addEventListener('click', () => {
      const confirmClear = confirm('Очистить все сохранённые данные?\n' +
                                   'Это удалит команды, очки и отмеченные лодауты.');
      if(!confirmClear) return;
  
      /* --- 1. Чистим localStorage --- */
      localStorage.removeItem('leaderboardData');
      localStorage.removeItem('activeTeam');
  
      /* ключи «played‑row‑col» для лодаутов */
      Object.keys(localStorage)
        .filter(k => k.startsWith('played-'))
        .forEach(k => localStorage.removeItem(k));
  
      /* --- 2. Перерисовываем UI --- */
      window.leaderboardData = [];                 // пустой массив очков
      window.createLeaderboard?.();                // обновляем таблицу
      window.createLoadoutTable?.(window.loadoutData); // весь лодаут-поле заново
  
      /* --- 3. Закрываем возможные модалки --- */
      document
        .querySelectorAll('.leaderboard-modal:not(.leaderboard-hidden), ' +
                          '#loadout-score-modal:not(.loadout-score-hidden), ' +
                          '#modal[style*="display: flex"]')
        .forEach(el => el.classList.add(
          el.id === 'loadout-score-modal' ? 'loadout-score-hidden' : 'leaderboard-hidden'
        ));
      /* для модалки с картинкой – просто скрываем через style */
      const imgModal = document.getElementById('modal');
      if(imgModal) imgModal.style.display = 'none';
    });
  });
  