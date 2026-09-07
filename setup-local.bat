@echo off
setlocal EnableExtensions
cd /d "%~dp0"

title Dead Mans - local setup

echo.
echo   Preparing PostgreSQL, MinIO, migrations, and local fixtures.
echo   Docker Desktop and the .NET 8 SDK are required.
echo.
pause

where docker >nul 2>&1
if errorlevel 1 (
  echo.
  echo   [ERROR] docker was not found. Install Docker Desktop and restart Windows.
  echo.
  pause
  exit /b 1
)

where dotnet >nul 2>&1
if errorlevel 1 (
  echo.
  echo   [ERROR] dotnet was not found. Install the .NET 8 SDK.
  echo.
  pause
  exit /b 1
)

if not exist "backend\scripts\setup-local.ps1" (
  echo.
  echo   [ERROR] backend\scripts\setup-local.ps1 was not found.
  echo.
  pause
  exit /b 1
)

echo.
echo   Running setup-local.ps1 ...
echo   ------------------------------------------------------------
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0backend\scripts\setup-local.ps1"
set "EC=%ERRORLEVEL%"
echo   ------------------------------------------------------------
if not "%EC%"=="0" (
  echo.
  echo   [ERROR] Local setup failed. Review the output above.
  echo.
  pause
  exit /b 1
)

echo.
echo   Ready. Run dev-full.bat and open http://localhost:5180
echo.
pause
exit /b 0
