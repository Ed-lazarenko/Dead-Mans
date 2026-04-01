@echo off
setlocal EnableExtensions
cd /d "%~dp0"

title Dead Mans — сайт и сервер

call "%~dp0dev-common.bat" PrintBanner "Запуск всего" "Сайт    http://localhost:5180" "Сервер  http://localhost:5285"
if errorlevel 1 exit /b 1

call "%~dp0dev-common.bat" RequireCmd node
if errorlevel 1 goto :Fail
call "%~dp0dev-common.bat" RequireCmd npm
if errorlevel 1 goto :Fail
call "%~dp0dev-common.bat" RequireCmd dotnet
if errorlevel 1 goto :Fail

echo   [i] Проверка портов 5285 ^(сервер^) и 5180 ^(сайт^)...
call "%~dp0dev-common.bat" EnsurePortFree 5285 "сервер API"
if errorlevel 1 goto :Fail
call "%~dp0dev-common.bat" EnsurePortFree 5180 "сайт в браузере"
if errorlevel 1 goto :Fail

call "%~dp0dev-common.bat" EnsureDeps
if errorlevel 1 goto :Fail

if not exist "backend\backend.csproj" (
  echo   [ОШИБКА] Нет файла backend\backend.csproj — откройте папку целиком из репозитория.
  goto :Fail
)

echo.
echo   [^>^>] Запуск сервера и сайта...
echo   ------------------------------------------------------------
call npm run dev
set "EC=%ERRORLEVEL%"
if not "%EC%"=="0" (
  echo.
  echo   [ОШИБКА] Команда завершилась с кодом %EC%.
  goto :Fail
)
exit /b 0

:Fail
echo.
pause
exit /b 1
