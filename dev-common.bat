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
  echo   [ERROR] Not in PATH: %~1
  echo           Install it and open a new terminal window.
  exit /b 1
)
exit /b 0

:: %~1 = port, %~2 = label for messages
:EnsurePortFree
shift
set "EP_PORT=%~1"
set "EP_NAME=%~2"
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
 "$port=%EP_PORT%; $hint='%EP_NAME%'; $ErrorActionPreference='SilentlyContinue'; $listener = Get-NetTCPConnection -LocalPort $port -State Listen; if (-not $listener) { exit 0 }; $pids = @($listener | ForEach-Object { $_.OwningProcess } | Sort-Object -Unique); Write-Host ''; Write-Host ('  Port ' + $port + ' (' + $hint + ') is in use.'); foreach ($procId in $pids) { $p = Get-Process -Id $procId -ErrorAction SilentlyContinue; $name = if ($p) { $p.ProcessName } else { '?' }; Write-Host ('    PID ' + $procId + ' -- ' + $name) }; Write-Host ''; $ans = Read-Host '  Terminate these processes? [Y/n]'; if ($ans -eq '' -or $ans -match '^[yYdD]') { foreach ($procId in $pids) { Stop-Process -Id $procId -Force -ErrorAction SilentlyContinue }; Write-Host '  Port freed.'; Start-Sleep -Milliseconds 400; exit 0 }; exit 1"
if errorlevel 1 (
  echo.
  echo   [ERROR] Port %EP_PORT% is still in use -- aborting.
  exit /b 1
)
exit /b 0

:: %~1 = port, %~2 = label; kills listeners without prompt
:StopListenPort
shift
set "EP_PORT=%~1"
set "EP_NAME=%~2"
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
 "$port=%EP_PORT%; $hint='%EP_NAME%'; $ErrorActionPreference='SilentlyContinue'; $listener = Get-NetTCPConnection -LocalPort $port -State Listen; if (-not $listener) { Write-Host ('  Port ' + $port + ' (' + $hint + ') -- free.'); exit 0 }; $pids = @($listener | ForEach-Object { $_.OwningProcess } | Sort-Object -Unique); foreach ($procId in $pids) { $p = Get-Process -Id $procId -ErrorAction SilentlyContinue; $name = if ($p) { $p.ProcessName } else { '?' }; Write-Host ('  Stopped: PID ' + $procId + ' -- ' + $name + ' (port ' + $port + ')'); Stop-Process -Id $procId -Force -ErrorAction SilentlyContinue }; Start-Sleep -Milliseconds 200; exit 0"
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
  echo   [i] Installing root dependencies (concurrently)...
  pushd "%~dp0" || exit /b 1
  call npm install
  if errorlevel 1 ( popd & exit /b 1 )
  popd
)
exit /b 0
