@echo off
cls
echo ======================
echo    WINDOWS EXPORTER
echo ======================

set GODOT_PATH="C:\Program Files\Godot_v4.5.1-stable_mono_win64\Godot_v4.5.1-stable_mono_win64.exe"
set PROJECT_PATH=%~dp0..\src\SteamPanno
set EXPORT_PATH=%~dp0..\dist\windows-x64

echo Exporting...

:: Create app directory
mkdir "%EXPORT_PATH%" 2> NUL
if not exist "%EXPORT_PATH%" (
    echo ERROR: Failed to create directory: %EXPORT_PATH%
    exit /b 1
)

:: Run godot export
%GODOT_PATH% --verbose --headless --path "%PROJECT_PATH%" --export-release "Windows Desktop" "%EXPORT_PATH%\SteamPanno.exe"
if %errorlevel% neq 0 (
    echo ERROR: Failed to export project!
    exit /b 1
)

echo Copying files...

:: Create translations directory
mkdir "%EXPORT_PATH%\custom-assets\translations" 2> NUL
if not exist "%EXPORT_PATH%\custom-assets\translations" (
    echo ERROR: Failed to create directory: %EXPORT_PATH%\custom-assets\translations
    exit /b 1
)

:: Copy translations
echo Copying translation files...
copy "%PROJECT_PATH%\assets\translations\*.*" "%EXPORT_PATH%\custom-assets\translations\"
if %errorlevel% neq 0 (
    echo ERROR: Failed to copy translation files!
    exit /b 1
)

:: Copy steam dependencies
echo Copying steam dependencies...
copy "%PROJECT_PATH%\addons\steam\windows\steam_api64.dll" "%EXPORT_PATH%\steam_api64.dll"
if %errorlevel% neq 0 (
    echo ERROR: Failed to copy steam dependencies!
    exit /b 1
)

echo Export completed!
pause