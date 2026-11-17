@echo off
chcp 65001 >nul
title Building Shia Prayer Times
echo Starting build...
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1"
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] Build failed! Check messages above.
)
pause
