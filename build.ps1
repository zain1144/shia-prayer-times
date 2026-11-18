# Shia Prayer Times - Automated Build Script with Audio Embedding
$ErrorActionPreference = "Stop"

Write-Host "========================================================" -ForegroundColor Cyan
Write-Host " Building Windows Shia (Jafari) Prayer Times Application" -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan

$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$dotnetDir = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319"

if (-not (Test-Path $csc)) {
    Write-Host "Error: .NET Framework compiler csc.exe not found at $csc" -ForegroundColor Red
    exit 1
}

# Close any running instance if locked
$running = Get-Process -Name "ShiaPrayerTimes" -ErrorAction SilentlyContinue
if ($running) {
    Write-Host "Closing running instance of ShiaPrayerTimes..." -ForegroundColor Yellow
    $running | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 800
}

# Icon selection
$icon = "app_new.ico"
if (-not (Test-Path $icon)) { $icon = "app.ico" }

$resLines = [System.Collections.Generic.List[string]]::new()

if (Test-Path $icon) {
    $resLines.Add("/resource:`"$icon`",app_new.ico")
    Write-Host "Embedded icon resource: $icon -> app_new.ico" -ForegroundColor Green
}
if (Test-Path "app.ico") {
    $resLines.Add("/resource:`"app.ico`",app.ico")
}

# Collect resources from Audio directory
$audioDir = Join-Path $PSScriptRoot "Audio"

if (Test-Path $audioDir) {
    $soundsTxt = Join-Path $audioDir "sounds.txt"
    if (Test-Path $soundsTxt) {
        $resLines.Add("/resource:`"$soundsTxt`",Audio.sounds.txt")
        Write-Host "Embedded config found: Audio\sounds.txt" -ForegroundColor Green
    }

    $audioFiles = Get-ChildItem -Path $audioDir -File | Where-Object {
        $_.Extension -match '^\.(mp3|wav|ogg|wma|m4a|aac)$'
    }

    if ($audioFiles.Count -gt 0) {
        Write-Host "Found $($audioFiles.Count) audio file(s) to embed into EXE:" -ForegroundColor Green
        foreach ($f in $audioFiles) {
            $resId = "Audio." + $f.Name
            $resLines.Add("/resource:`"$($f.FullName)`",`"$resId`"")
            Write-Host "  [+] $($f.Name) -> $resId" -ForegroundColor Gray
        }
    } else {
        Write-Host "No custom audio files found in Audio\ directory." -ForegroundColor Yellow
    }
}

# Source files
$sources = @(
    "UIThemeHelper.cs",
    "PrayerCalculator.cs",
    "LocationDetector.cs",
    "HijriCalendar.cs",
    "AppSettings.cs",
    "ToastNotificationWindow.cs",
    "AudioNotifier.cs",
    "TrayManager.cs",
    "SettingsWindow.cs",
    "MainWindow.cs",
    "App.cs"
)

# Build response file
$rspLines = [System.Collections.Generic.List[string]]::new()
$rspLines.Add("/target:winexe")
$rspLines.Add("/win32icon:$icon")
$rspLines.Add("/out:ShiaPrayerTimes.exe")
$rspLines.Add("/r:System.dll")
$rspLines.Add("/r:System.Core.dll")
$rspLines.Add("/r:System.Drawing.dll")
$rspLines.Add("/r:System.Windows.Forms.dll")
$rspLines.Add("/r:$dotnetDir\WPF\PresentationCore.dll")
$rspLines.Add("/r:$dotnetDir\WPF\PresentationFramework.dll")
$rspLines.Add("/r:$dotnetDir\WPF\WindowsBase.dll")
$rspLines.Add("/r:$dotnetDir\System.Xaml.dll")
$rspLines.Add("/r:$dotnetDir\System.Web.Extensions.dll")
$rspLines.Add("/r:$dotnetDir\System.Device.dll")
$rspLines.Add("/r:$dotnetDir\Microsoft.VisualBasic.dll")

foreach ($r in $resLines) {
    $rspLines.Add($r)
}

foreach ($s in $sources) {
    $rspLines.Add($s)
}

$rspFile = Join-Path $PSScriptRoot "build_args.rsp"
[System.IO.File]::WriteAllLines($rspFile, $rspLines, [System.Text.Encoding]::UTF8)

Write-Host "`nCompiling application with csc.exe..." -ForegroundColor Cyan
& $csc "@$rspFile"
$exitCode = $LASTEXITCODE

if (Test-Path $rspFile) {
    Remove-Item $rspFile -Force -ErrorAction SilentlyContinue
}

if ($exitCode -eq 0) {
    $exeSize = (Get-Item "ShiaPrayerTimes.exe").Length / 1MB
    Write-Host "`n========================================================" -ForegroundColor Green
    Write-Host (" Build Successful: ShiaPrayerTimes.exe ({0:N2} MB)" -f $exeSize) -ForegroundColor Green
    Write-Host " All audio files are now embedded inside the executable!" -ForegroundColor Green
    Write-Host "========================================================`n" -ForegroundColor Green
} else {
    Write-Host "`nBuild failed with exit code $exitCode" -ForegroundColor Red
    exit $exitCode
}
