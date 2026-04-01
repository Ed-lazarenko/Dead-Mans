@echo off
setlocal EnableExtensions
cd /d "%~dp0"

title Dead Mans — сброс локальных данных (БД и MinIO)

echo.
echo   ВНИМАНИЕ: будут удалены локальные данные PostgreSQL и MinIO в Docker для этого проекта.
echo   Папка проекта может быть любой — запускайте из корня репозитория.
echo   Docker Desktop должен быть запущен.
echo.
pause

where docker >nul 2>&1
if errorlevel 1 (
  echo.
  echo   [ОШИБКА] Команда docker не найдена. Установите Docker Desktop.
  echo.
  pause
  exit /b 1
)

where dotnet >nul 2>&1
if errorlevel 1 (
  echo.
  echo   [ОШИБКА] Команда dotnet не найдена. Установите .NET 8 SDK.
  echo.
  pause
  exit /b 1
)

if not exist "backend\scripts\reset-local.ps1" (
  echo.
  echo   [ОШИБКА] Не найден backend\scripts\reset-local.ps1
  echo.
  pause
  exit /b 1
)

echo.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0backend\scripts\reset-local.ps1"
set "EC=%ERRORLEVEL%"
if not "%EC%"=="0" (
  echo.
  echo   [ОШИБКА] Сброс не завершился. См. сообщения выше.
  echo.
  pause
  exit /b 1
)

echo.
pause
exit /b 0
