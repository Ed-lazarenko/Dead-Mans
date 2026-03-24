@echo off
setlocal EnableExtensions
cd /d "%~dp0"

title Dead Mans - Frontend

call "%~dp0dev-common.bat" PrintBanner "Frontend" "Vite + React  ^>^>  http://localhost:5180"
if errorlevel 1 exit /b 1

call "%~dp0dev-common.bat" RequireCmd node
if errorlevel 1 goto :Fail
call "%~dp0dev-common.bat" RequireCmd npm
if errorlevel 1 goto :Fail

call "%~dp0dev-common.bat" EnsurePortFree 5180 "Vite (frontend)"
if errorlevel 1 goto :Fail

if not exist "frontend\node_modules\" (
  echo   [i] First run: npm install in frontend...
  pushd frontend
  call npm install
  if errorlevel 1 popd & goto :Fail
  popd
  echo.
)

echo   [^>^>] npm run dev:frontend
echo   ------------------------------------------------------------
call npm run dev:frontend
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
