document.addEventListener('DOMContentLoaded', () => {
  // переключатель вкладок
  document.querySelectorAll('.tab-button').forEach(button => {
    button.addEventListener('click', () => {
      document
        .querySelectorAll('.tab-button')
        .forEach(b => b.classList.remove('active'));
      document
        .querySelectorAll('.tab-content')
        .forEach(tab => tab.classList.remove('active'));

      button.classList.add('active');
      document.getElementById(button.dataset.tab).classList.add('active');

      // при открытии вкладки «Модификаторы» — загружаем участников,
      // а модификаторы пользователь может подтянуть отдельной кнопкой
      if (button.dataset.tab === 'modifiers') {
        window.renderModifiersTab?.();
      }
    });
  });
});
