// js/loadout/score.js

window.openScoreModal = function (td, rowIdx, colIdx, costLabel) {
  const modal = document.getElementById('loadout-score-modal');
  modal.classList.remove('loadout-score-hidden');

  const form = modal.querySelector('form');
  form.reset();

  form.onsubmit = (e) => {
    e.preventDefault();

    const kills = parseInt(form.kills.value) || 0;
    const reward = parseInt(form.reward.value) || 0;
    const penaltyRaw = parseInt(form.penalty.value) || 0;
    const multiplier = parseFloat(form.multiplier.value) || 1;
    const cost = parseInt(costLabel.split(' ')[0]) || 0;
    const result = (kills * cost + reward) * multiplier;
    const adjustedPenalty = Math.round(penaltyRaw * multiplier);

    const activeTeamJSON = localStorage.getItem('activeTeam');
    const team = activeTeamJSON ? JSON.parse(activeTeamJSON) : [];
    if (!team.length) return alert('Нет активной команды!');

    const entry = window.leaderboardData.find(t => JSON.stringify(t.players) === JSON.stringify(team));
    const teamColor = entry?.color || 'transparent';

    // Сохраняем результат, цвет и штраф
    const playedKey = `played-${rowIdx}-${colIdx}`;
    localStorage.setItem(playedKey, JSON.stringify({
      team,
      color: teamColor,
      result,
      penalty: adjustedPenalty
    }));

    td.classList.add('played');

    const wrapper = td.querySelector('.image-wrapper');
    if (wrapper) {
      wrapper.style.border = `3px solid ${teamColor}`;
      wrapper.style.borderRadius = '2px';
      wrapper.style.boxShadow = `0 0 10px 2px ${teamColor}`;
    }

    const overlayBlock = td.querySelector('.played-overlay');
    const scoreNote = document.createElement('div');
    scoreNote.className = 'played-score-note';
    if (adjustedPenalty > 0) {
      scoreNote.textContent = `−${adjustedPenalty}`;
      scoreNote.style.color = '#ff5555';
    } else {
      scoreNote.textContent = `+${result}`;
    }

    overlayBlock.innerHTML = '';
    overlayBlock.appendChild(scoreNote);
    overlayBlock.innerHTML += `
      <span class="played-text">Отыграно</span>
      <div class="played-team">
        ${team.map((name, i) =>
          `<span class="played-name played-name-${(i % 3) + 1}">${name}</span>`
        ).join('')}
      </div>
    `;

    if (entry) {
      const firstEmpty = entry.scores.findIndex(s => s === '' || s == null);
      if (firstEmpty !== -1) {
        entry.scores[firstEmpty] = result;
      } else {
        entry.scores.push(result);
      }

      entry.penalty = (entry.penalty || 0) + adjustedPenalty;

      localStorage.setItem('leaderboardData', JSON.stringify(window.leaderboardData));
      window.createLeaderboard?.();
    }

    modal.classList.add('loadout-score-hidden');
    window.updateLoadoutCounter();
  };

  document.getElementById('loadout-cancel-score').onclick = () => {
    modal.classList.add('loadout-score-hidden');
  };
};
