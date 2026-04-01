@echo off
setlocal EnableExtensions
cd /d "%~dp0"

title Dead Mans — только сайт

call "%~dp0dev-common.bat" PrintBanner "Только сайт" "http://localhost:5180"
if errorlevel 1 exit /b 1

call "%~dp0dev-common.bat" RequireCmd node
if errorlevel 1 goto :Fail
call "%~dp0dev-common.bat" RequireCmd npm
if errorlevel 1 goto :Fail

call "%~dp0dev-common.bat" EnsurePortFree 5180 "сайт в браузере"
if errorlevel 1 goto :Fail

call "%~dp0dev-common.bat" EnsureDeps
if errorlevel 1 goto :Fail

echo   [^>^>] Запуск сайта...
echo   ------------------------------------------------------------
call npm run dev:frontend
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
