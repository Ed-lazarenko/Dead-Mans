// Глобально доступная функция открытия модального окна с изображением
window.openModal = function (src) {
    // Получаем DOM-элементы модалки и изображения внутри неё
    const modal = document.getElementById('modal');
    const modalImg = document.getElementById('modal-image');
  
    // Устанавливаем путь к изображению и показываем модалку
    modalImg.src = src;
    modal.style.display = 'flex';
  };
  
  // Глобально доступная функция закрытия модального окна
  window.closeModal = function () {
    // Скрываем модалку
    const modal = document.getElementById('modal');
    modal.style.display = 'none';
  
    // Очищаем src, чтобы избежать повторной загрузки при открытии
    document.getElementById('modal-image').src = '';
  };
  