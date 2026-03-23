/*  js/modifiers/index.js
 *  ────────────────────────────────────────────────
 *  Показывает две таблицы на вкладке «Модификаторы»:
 *    • список участников (scores)
 *    • активные модификаторы (mods)
 *  Данные приходят по WebSocket‑событиям от сервера.
 *  При переключении на вкладку мы просто рендерим то,
 *  что уже лежит в памяти — без лишних запросов.
 */

/* ========= 1. Подключаем socket.io ========= */
const socket = io();                 // соединение WebSocket

/* ========= 2. Локальное хранилище ========= */
const store = {
  scores:        {},                 // участники и баланс
  activeMods:    []                  // активные модификаторы
};

/* ========= 3. Рендер таблицы участников ========= */
function renderParticipants(scores) {
  const container = document.getElementById('mod-points-table');
  container.innerHTML = '';

  const table = document.createElement('table');
  table.className = 'modifiers-table';
  table.innerHTML =
    `<tr><th>Зритель</th><th>Баланс</th></tr>`;

  Object.entries(scores).forEach(([name, data]) => {
    table.insertAdjacentHTML(
      'beforeend',
      `<tr>
         <td><span class="nickname">${name}</span></td>
         <td>${data.current_balance}</td>
       </tr>`
    );
  });

  container.appendChild(table);
}

/* ========= 4. Рендер таблицы модификаторов ========= */
function renderActiveModifiers() {
  const { activeMods } = store;
  const container = document.getElementById('active-modifiers-table');
  container.innerHTML = '';

  if (!activeMods.length) {
    container.textContent = 'Нет активных модификаторов';
    return;
  }

  const table = document.createElement('table');
  table.className = 'modifiers-table';
  table.innerHTML =
    `<tr>
       <th>Название</th>
       <th>Описание</th>
       <th>Кто активировал</th>
     </tr>`;

  activeMods.forEach(mod => {
    table.insertAdjacentHTML(
      'beforeend',
      `<tr>
         <td>${mod.name.toUpperCase()}</td>
         <td>${mod.description}</td>
         <td><span class="nickname">${mod.activated_by}</span></td>
       </tr>`
    );
  });

  container.appendChild(table);
}

/* ========= 5. Socket‑подписки ========= */
/*  Сервер шлёт полный объект scores и полный массив mods.
 *  Сохраняем их в store и сразу перерисовываем таблицу. */
socket.on('scores:update', scores => {
  store.scores = scores;
  renderParticipants(scores);
});

socket.on('mods:update', mods => {
  store.activeMods = mods;
  renderActiveModifiers();
});

/* ========= 6. Экспорт функции для переключения вкладок ========= */
/*  Вызывается из main.js, когда пользователь открывает
 *  вкладку «Модификаторы». Просто рисуем то, что уже есть. */
window.renderModifiersTab = () => {
  renderParticipants(store.scores);
  renderActiveModifiers();
};
