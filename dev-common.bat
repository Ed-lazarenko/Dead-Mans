@echo off
setlocal EnableExtensions
chcp 65001 >nul 2>&1
if not "%~1"=="" goto %~1
exit /b 0

:: %~1 = title, %~2 and %~3 = optional subtitle lines
:PrintBanner
shift
echo.
echo   ============================================================
echo      Dead Mans  ^|  %~1
echo   ============================================================
if not "%~2"=="" echo      %~2
if not "%~3"=="" echo      %~3
echo.
exit /b 0

:: %~1 = command name (node, npm, dotnet)
:RequireCmd
shift
where %~1 >nul 2>&1
if errorlevel 1 (
  echo   [ERROR] Required command was not found: %~1
  echo           Install it ^(see README^) and open a new terminal.
  exit /b 1
)
exit /b 0

:: %~1 = port, %~2 = label for messages
:EnsurePortFree
shift
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0backend\scripts\manage-dev-port.ps1" -Action Ensure -Port %~1 -Label "%~2"
if errorlevel 1 (
  echo.
  echo   [ERROR] Port %~1 is still busy. Close the other process or run dev-stop.bat.
  exit /b 1
)
exit /b 0

:: %~1 = port, %~2 = label; kills listeners without prompt
:StopListenPort
shift
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0backend\scripts\manage-dev-port.ps1" -Action Stop -Port %~1 -Label "%~2"
exit /b 0

:: npm install in frontend and repo root if node_modules missing
:EnsureDeps
shift
if not exist "%~dp0frontend\node_modules" (
  echo   [i] Installing frontend dependencies...
  pushd "%~dp0frontend" || exit /b 1
  call npm install
  if errorlevel 1 ( popd & exit /b 1 )
  popd
)
if not exist "%~dp0node_modules" (
  echo   [i] Installing root dependencies...
  pushd "%~dp0" || exit /b 1
  call npm install
  if errorlevel 1 ( popd & exit /b 1 )
  popd
)
exit /b 0
