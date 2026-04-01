@echo off
setlocal EnableExtensions
cd /d "%~dp0"

title Dead Mans — остановить сервер и сайт

call "%~dp0dev-common.bat" PrintBanner "Остановка" "Порт 5285 — сервер API" "Порт 5180 — сайт"
if errorlevel 1 exit /b 1

echo   [i] Освобождаем порты 5285 и 5180...
echo   ------------------------------------------------------------
call "%~dp0dev-common.bat" StopListenPort 5285 "сервер API"
call "%~dp0dev-common.bat" StopListenPort 5180 "сайт в браузере"
echo   ------------------------------------------------------------
echo   [OK] Готово.
echo.
pause
exit /b 0
