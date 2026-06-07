@echo off
echo ========================================
echo  CleanApp - Flutter Build and Run
echo ========================================
cd /d "C:\Users\2M\Pictures\CleaningManagmentSystem\cleanapp"

echo.
echo [1] Getting dependencies...
call flutter pub get

echo.
echo [2] Checking connected devices...
call flutter devices

echo.
echo [3] Building and running on Samsung...
call flutter run -d R38N701KYTA --release

pause
