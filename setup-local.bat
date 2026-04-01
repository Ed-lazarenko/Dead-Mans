@echo off
setlocal EnableExtensions
cd /d "%~dp0"

title Dead Mans — первая настройка (Docker, база, файлы)

echo.
echo   Подготовка среды: база данных, MinIO, миграции, тестовые картинки.
echo   Папка с проектом может быть любой — важно запускать этот файл из корня репозитория.
echo.
echo   Перед стартом: установите Docker Desktop, .NET 8 SDK и запустите Docker (иконка в трее).
echo.
pause

where docker >nul 2>&1
if errorlevel 1 (
  echo.
  echo   [ОШИБКА] Команда docker не найдена. Установите Docker Desktop и перезагрузите компьютер.
  echo.
  pause
  exit /b 1
)

where dotnet >nul 2>&1
if errorlevel 1 (
  echo.
  echo   [ОШИБКА] Команда dotnet не найдена. Установите .NET 8 SDK с https://dotnet.microsoft.com/download
  echo.
  pause
  exit /b 1
)

if not exist "backend\scripts\setup-local.ps1" (
  echo.
  echo   [ОШИБКА] Не найден файл backend\scripts\setup-local.ps1
  echo            Скопируйте весь репозиторий, не одну папку.
  echo.
  pause
  exit /b 1
)

echo.
echo   Запуск setup-local.ps1 ...
echo   ------------------------------------------------------------
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0backend\scripts\setup-local.ps1"
set "EC=%ERRORLEVEL%"
echo   ------------------------------------------------------------
if not "%EC%"=="0" (
  echo.
  echo   [ОШИБКА] Настройка не завершилась. Прочитайте текст выше (часто не запущен Docker).
  echo.
  pause
  exit /b 1
)

echo.
echo   Готово. Дальше: дважды щёлкните dev-full.bat и откройте в браузере http://localhost:5180
echo.
pause
exit /b 0
