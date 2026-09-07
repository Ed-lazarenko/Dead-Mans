@echo off
setlocal EnableExtensions
cd /d "%~dp0"

title Dead Mans - site and API

call "%~dp0dev-common.bat" PrintBanner "Start all" "Site  http://localhost:5180" "API   http://localhost:5285"
if errorlevel 1 exit /b 1

call "%~dp0dev-common.bat" RequireCmd node
if errorlevel 1 goto :Fail
call "%~dp0dev-common.bat" RequireCmd npm
if errorlevel 1 goto :Fail
call "%~dp0dev-common.bat" RequireCmd dotnet
if errorlevel 1 goto :Fail
call "%~dp0dev-common.bat" RequireCmd docker
if errorlevel 1 goto :Fail

echo   [i] Checking Docker Engine and repairing stale transient runtime state...
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0backend\scripts\ensure-docker-desktop.ps1"
if errorlevel 1 goto :Fail

echo   [i] Checking ports 5285 and 5180...
call "%~dp0dev-common.bat" EnsurePortFree 5285 "API"
if errorlevel 1 goto :Fail
call "%~dp0dev-common.bat" EnsurePortFree 5180 "site"
if errorlevel 1 goto :Fail

call "%~dp0dev-common.bat" EnsureDeps
if errorlevel 1 goto :Fail

if not exist "backend\backend.csproj" (
  echo   [ERROR] backend\backend.csproj was not found.
  goto :Fail
)

echo.
echo   [^>^>] Starting API and site...
echo   ------------------------------------------------------------
call npm run dev
set "EC=%ERRORLEVEL%"
if not "%EC%"=="0" (
  echo.
  echo   [ERROR] Development services exited with code %EC%.
  goto :Fail
)
exit /b 0

:Fail
echo.
pause
exit /b 1
