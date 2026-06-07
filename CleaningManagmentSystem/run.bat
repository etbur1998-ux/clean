@echo off
echo [run.bat] Stopping any existing server on port 5000...

REM Kill by port 5000
for /f "tokens=5" %%a in ('netstat -aon 2^>nul ^| findstr ":5000" ^| findstr "LISTENING"') do (
    echo [run.bat] Killing PID %%a on port 5000
    taskkill /F /PID %%a >nul 2>&1
)

REM Also kill any lingering dotnet processes running this project
for /f "tokens=1,2" %%a in ('tasklist /fi "imagename eq dotnet.exe" /fo table /nh 2^>nul') do (
    taskkill /F /PID %%b >nul 2>&1
)

REM Short wait for OS to release file locks
timeout /t 2 /nobreak >nul

echo [run.bat] Starting server...
dotnet run
