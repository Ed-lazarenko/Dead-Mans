@echo off
setlocal EnableExtensions
cd /d "%~dp0"

title Dead Mans - Full stack (frontend + backend)

call "%~dp0dev-common.bat" PrintBanner "Full stack" "Frontend http://localhost:5180" "Backend  http://localhost:5285"
if errorlevel 1 exit /b 1

call "%~dp0dev-common.bat" RequireCmd node
if errorlevel 1 goto :Fail
call "%~dp0dev-common.bat" RequireCmd npm
if errorlevel 1 goto :Fail
call "%~dp0dev-common.bat" RequireCmd dotnet
if errorlevel 1 goto :Fail

echo   [i] Checking ports (API, then Vite)...
call "%~dp0dev-common.bat" EnsurePortFree 5285 "Kestrel (backend)"
if errorlevel 1 goto :Fail
call "%~dp0dev-common.bat" EnsurePortFree 5180 "Vite (frontend)"
if errorlevel 1 goto :Fail

call "%~dp0dev-common.bat" EnsureDeps
if errorlevel 1 goto :Fail

if not exist "backend\backend.csproj" (
  echo   [ERROR] Missing: backend\backend.csproj
  goto :Fail
)

echo.
echo   [^>^>] npm run dev  (concurrently: backend + frontend)
echo   ------------------------------------------------------------
call npm run dev
set "EC=%ERRORLEVEL%"
if not "%EC%"=="0" (
  echo.
  echo   [ERROR] Command exited with code %EC%.
  goto :Fail
)
exit /b 0

:Fail
echo.
pause
exit /b 1
