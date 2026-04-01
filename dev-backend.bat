@echo off
setlocal EnableExtensions
cd /d "%~dp0"

title Dead Mans — только сервер

call "%~dp0dev-common.bat" PrintBanner "Только сервер API" "http://localhost:5285"
if errorlevel 1 exit /b 1

call "%~dp0dev-common.bat" RequireCmd dotnet
if errorlevel 1 goto :Fail

call "%~dp0dev-common.bat" EnsurePortFree 5285 "сервер API"
if errorlevel 1 goto :Fail

if not exist "backend\backend.csproj" (
  echo   [ОШИБКА] Нет файла backend\backend.csproj.
  goto :Fail
)

echo.
echo   [^>^>] Запуск сервера...
echo   ------------------------------------------------------------
call npm run dev:backend
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
