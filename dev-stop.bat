@echo off
setlocal EnableExtensions
cd /d "%~dp0"

title Dead Mans - Stop frontend + backend

call "%~dp0dev-common.bat" PrintBanner "Stop" "Backend  :5285  (force kill)" "Frontend :5180  (force kill)"
if errorlevel 1 exit /b 1

echo   [i] Stopping processes bound to project ports...
echo   ------------------------------------------------------------
call "%~dp0dev-common.bat" StopListenPort 5285 "Kestrel (backend)"
call "%~dp0dev-common.bat" StopListenPort 5180 "Vite (frontend)"
echo   ------------------------------------------------------------
echo   [OK] Done.
echo.
pause
exit /b 0
