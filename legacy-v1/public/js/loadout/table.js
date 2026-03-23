// js/loadout/table.js

window.createLoadoutTable = function (data) {
  const area = document.getElementById('loadout-area');
  const table = document.createElement('table');
  table.className = 'loadout-table';

  window.updateActiveTeamDisplay();

  const headerRow = document.createElement('tr');
  const cornerCell = document.createElement('th');
  cornerCell.className = 'corner-cell';
  cornerCell.textContent = '💀';
  headerRow.appendChild(cornerCell);

  data.columns.forEach(col => {
    const th = document.createElement('th');
    th.textContent = col;
    headerRow.appendChild(th);
  });
  table.appendChild(headerRow);

  data.rows.forEach((label, rowIdx) => {
    const row = document.createElement('tr');

    const th = document.createElement('th');
    th.textContent = label;
    row.appendChild(th);

    data.cells[rowIdx].forEach((cell, colIdx) => {
      const td = document.createElement('td');
      td.className = 'loadout-cell closed';

      const playedKey = `played-${rowIdx}-${colIdx}`;
      const raw = localStorage.getItem(playedKey);
      let storedTeam = [];
      let storedColor = null;
      let storedResult = null;
      let storedPenalty = 0;

      if (raw) {
        try {
          const parsed = JSON.parse(raw);
          if (parsed && Array.isArray(parsed.team)) {
            storedTeam = parsed.team;
            storedColor = parsed.color;
            storedResult = parsed.result;
            storedPenalty = parsed.penalty || 0;
            td.classList.remove('closed');
            td.classList.add('opened', 'played');
          }
        } catch {
          localStorage.removeItem(playedKey);
        }
      }

      const img = document.createElement('img');
      img.src = cell.image;
      img.alt = '';
      const wrapper = document.createElement('div');
      wrapper.className = 'image-wrapper';
      wrapper.appendChild(img);
      td.appendChild(wrapper);

      if (storedColor) {
        wrapper.style.border = `3px solid ${storedColor}`;
        wrapper.style.borderRadius = '2px';
        wrapper.style.boxShadow = `0 0 10px 2px ${storedColor}`;
      }

      const overlay = document.createElement('div');
      overlay.className = 'played-overlay';

      let scoreHTML = '';
      if (storedResult !== null) {
        const scoreNote = document.createElement('div');
        scoreNote.className = 'played-score-note';
        if (storedPenalty > 0) {
          scoreNote.textContent = `−${storedPenalty}`;
          scoreNote.style.color = '#ff5555';
        } else {
          scoreNote.textContent = `+${storedResult}`;
        }
        overlay.appendChild(scoreNote);
      }

      let teamHTML = storedTeam.length > 0
        ? `<div class="played-team">` +
          storedTeam.map((name, i) =>
            `<span class="played-name played-name-${(i % 3) + 1}">${name}</span>`
          ).join('') + `</div>`
        : '';

      overlay.innerHTML += `
        <span class="played-text">Отыграно</span>
        ${teamHTML}
      `;

      td.appendChild(overlay);

      td.addEventListener('click', e => {
        if (e.button !== 0) return;
        if (td.classList.contains('closed')) {
          td.classList.remove('closed');
          td.classList.add('opened');
        } else {
          window.openModal(img.src);
        }
      });

      td.addEventListener('contextmenu', e => {
        e.preventDefault();
        if (td.classList.contains('opened')) {
          window.openScoreModal(td, rowIdx, colIdx, label);
        }
      });

      row.appendChild(td);
    });

    table.appendChild(row);
  });

  area.innerHTML = '';
  area.appendChild(table);
  window.updateLoadoutCounter();
};

window.updateLoadoutCounter = function () {
  const count = document.querySelectorAll('.loadout-cell.played').length;
  const counter = document.getElementById('controls-counter');
  if (counter) {
    counter.textContent = `Отыграно: ${count}`;
  }
};

window.updateActiveTeamDisplay = function () {
  const container = document.getElementById('active-team-display');
  if (!container) return;

  const raw = localStorage.getItem('activeTeam');
  if (!raw) {
    container.innerHTML = '<div class="active-team-box">Нет активной команды</div>';
    return;
  }

  try {
    const team = JSON.parse(raw);
    const namesHTML = team.map((name, i) =>
      `<span class="active-player player-${(i % 3) + 1}">${name}</span>`
    ).join('<span class="pipe"> | </span>');

    container.innerHTML = `
      <div class="active-team-box">
        <span class="active-team-title">Активная команда:</span>
        <span class="active-team-names">${namesHTML}</span>
      </div>
    `;
  } catch {
    container.innerHTML = '<div class="active-team-box">Нет активной команды</div>';
  }
};
