// Ждём полной загрузки DOM, прежде чем выполнять действия
document.addEventListener('DOMContentLoaded', () => {

  // Создаём таблицу лодаутов, используя глобальные данные
  window.createLoadoutTable(window.loadoutData);

  // Назначаем обработчик закрытия модалки (полноэкранного изображения)
  document.getElementById('modal').addEventListener('click', window.closeModal);
});
