@echo off
echo ========================================================
echo Building Windows Shia (Jafari) Prayer Times Application
echo ========================================================

set CSC="C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
set DOTNET_DIR="C:\Windows\Microsoft.NET\Framework64\v4.0.30319"

if not exist %CSC% (
    echo Error: .NET Framework C# compiler csc.exe not found!
    pause
    exit /b 1
)

set ICON="app_new.ico"
if not exist %ICON% (
    set ICON="app.ico"
)

%CSC% /target:winexe /win32icon:%ICON% /out:"ShiaPrayerTimes.exe" /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:%DOTNET_DIR%\WPF\PresentationCore.dll /r:%DOTNET_DIR%\WPF\PresentationFramework.dll /r:%DOTNET_DIR%\WPF\WindowsBase.dll /r:%DOTNET_DIR%\System.Xaml.dll /r:%DOTNET_DIR%\System.Web.Extensions.dll /r:%DOTNET_DIR%\System.Device.dll /r:%DOTNET_DIR%\Microsoft.VisualBasic.dll UIThemeHelper.cs PrayerCalculator.cs LocationDetector.cs HijriCalendar.cs AppSettings.cs ToastNotificationWindow.cs AudioNotifier.cs TrayManager.cs SettingsWindow.cs MainWindow.cs App.cs

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ========================================================
    echo Build Successful: ShiaPrayerTimes.exe generated!
    echo ========================================================
) else (
    echo.
    echo Build failed with error code %ERRORLEVEL%
)
pause
