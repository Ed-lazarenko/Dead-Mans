@echo off
setlocal EnableExtensions
cd /d "%~dp0"

title Dead Mans - Backend

call "%~dp0dev-common.bat" PrintBanner "Backend" "ASP.NET Core API  ^>^>  http://localhost:5285"
if errorlevel 1 exit /b 1

call "%~dp0dev-common.bat" RequireCmd dotnet
if errorlevel 1 goto :Fail

call "%~dp0dev-common.bat" EnsurePortFree 5285 "Kestrel (backend)"
if errorlevel 1 goto :Fail

if not exist "backend\backend.csproj" (
  echo   [ERROR] Missing: backend\backend.csproj
  goto :Fail
)

echo.
echo   [^>^>] npm run dev:backend
echo   ------------------------------------------------------------
call npm run dev:backend
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
