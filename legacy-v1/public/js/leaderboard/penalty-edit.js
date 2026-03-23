// Модалка редактирования штрафа
window.setupPenaltyEditModal = function () {
    const modal = document.getElementById('edit-penalty-modal');
    const input = document.getElementById('penalty-input');
    const saveBtn = document.getElementById('save-edit-penalty');
    const cancelBtn = document.getElementById('cancel-edit-penalty');
  
    let editingIndex = null;
  
    window.openPenaltyEditModal = function (teamIndex, currentValue) {
      editingIndex = teamIndex;
      input.value = currentValue ?? 0;
      modal.classList.remove('leaderboard-hidden');
      input.focus();
    };
  
    saveBtn.onclick = function () {
      const newValue = parseInt(input.value, 10) || 0;
      if (editingIndex !== null) {
        window.leaderboardData[editingIndex].penalty = newValue;
        localStorage.setItem('leaderboardData', JSON.stringify(window.leaderboardData));
        window.createLeaderboard?.();
      }
      modal.classList.add('leaderboard-hidden');
    };
  
    cancelBtn.onclick = function () {
      modal.classList.add('leaderboard-hidden');
    };
  };
  