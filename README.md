# Windows Shia (Jafari) Prayer Times Desktop Application
# تطبيق أوقات الصلاة الشيعية (الجعفرية) لنظام ويندوز

A lightweight, offline-capable, standalone Windows desktop application designed specifically for calculating and displaying Shia Ithna-Ashari (Jafari) Islamic prayer times with astronomical accuracy. Built to match the behavior, calculation standards, and per-prayer customization of the Android app **Pray Times** (`com.smustafa.praytimes`).

---

## 🌟 Key Features

### 1. Astronomical Calculation Engine (100% Offline)
- **Zero Internet Requirement:** Calculates all times locally using exact solar coordinates and spherical astronomy formulas.
- **Supported Shia (Jafari) Methods:**
  - **Leva Research Institute, Qom (معهد لواء للأبحاث، قم):**
    - Fajr: `16.0°`
    - Maghrib: `4.0°` (twilight depression angle below horizon)
    - Isha: `14.0°`
    - Midnight: Midpoint between Sunset and Fajr
    - Asr: Jafari jurisprudence standard (shadow ratio = 1)
  - **Institute of Geophysics, University of Tehran (معهد الجيوفيزياء بجامعة طهران):**
    - Fajr: `17.7°`
    - Maghrib: `4.5°`
    - Isha: `14.0°`
    - Midnight: Midpoint between Sunset and Fajr
  - **Custom Angles (زوايا فلكية مخصصة):** Full manual override for Fajr, Maghrib, and Isha twilight angles.
- **Supported Time Events (9 Events):**
  1. **Imsak (الإمساك):** Default 10 minutes before Fajr (configurable 5-30 min)
  2. **Fajr (الفجر / الصباح):** True astronomical dawn
  3. **Sunrise (الشروق):** Upper limb of the sun on horizon (with elevation refraction)
  4. **Dhuhr (الظهر):** Midday transit
  5. **Asr (العصر):** Jafari juristic shadow calculation
  6. **Sunset (الغروب):** Disappearance of the solar disk
  7. **Maghrib (المغرب):** Disappearance of eastern redness (حمرة مشرقية)
  8. **Isha (العشاء):** Disappearance of western twilight
  9. **Midnight (منتصف الليل الشرعي):** Midpoint between Sunset and Fajr (Jafari standard) or Sunset and Sunrise (selectable option).

---

### 2. Granular Per-Prayer Settings Matrix
Every single prayer event has an independent configuration row:
- **Display Toggle (Show/Hide):** Show or hide that prayer from the main schedule list and live countdown cycle (e.g. show Fajr, Sunrise, Dhuhr, Maghrib, Midnight while hiding Sunset, Asr, or Imsak).
- **Manual Time Offset:** Individual minute adjustment (`-15` to `+15` minutes) to match local mosque timetables.
- **Notification Mode:**
  - *Off / Silent* (No alert)
  - *Visual Toast Only* (Windows notification popup without sound)
  - *Short Chime / Beep* (Subtle alert chime)
  - *Full Adhan Audio* (Plays full Adhan)
- **Pre-Prayer Reminder:** Independent pre-alarm (`Off`, `5`, `10`, `15`, `20`, or `30` minutes before prayer).
- **Custom Audio Selection:** Assign a specific `.mp3` or `.wav` per prayer (e.g. distinct Fajr Adhan), with preview play/stop button.

---

### 3. Location & Calendar Management
- **Pre-configured City Presets:**
  - Qatif (القطيف - Saudi Arabia) [Default: 26.5577°N, 50.0081°E, UTC+3]
  - Najaf al-Ashraf (النجف الأشرف - Iraq)
  - Karbala al-Muqaddasa (كربلاء المقدسة - Iraq)
  - Qom al-Muqaddasa (قم المقدسة - Iran)
  - Tehran (طهران - Iran)
  - Manama (المنامة - Bahrain)
  - Al-Ahsa (الأحساء - Saudi Arabia)
  - Muscat (مسقط - Oman)
  - Beirut (بيروت - Lebanon)
  - London (لندن - UK)
  - Dearborn / Detroit (ديربورن / ديترويت - USA)
- **Custom Location Input:** Input custom Latitude, Longitude, Timezone, and Elevation with "Save as Preset" button.
- **Dual Calendar:**
  - Current Gregorian date (localized).
  - Current Umm Al-Qura / Tabular Hijri date with manual moon sighting offset adjustment (`-2` to `+2` days).
  - Past & Future Date Browser: Navigate with `< Prev Day`, `Today`, `Next Day >`, and Calendar Date Picker.

---

### 4. Modern UI & Windows Integration
- **Bilingual Interface:** Instant one-click toggle between Arabic RTL (`🇸🇦 العربية`) and English LTR (`🇬🇧 English`).
- **Dark & Light Themes:** Elegant Dark slate theme with emerald & gold Islamic accents, plus a clean Light pearl theme.
- **Live Countdown Display:** Large `HH:MM:SS` countdown timer to the next visible prayer with a dynamic elapsed progress bar.
- **System Tray Background Operation:**
  - Minimizes to system tray on window close.
  - Hover tooltip displays upcoming prayer and countdown (`Next: Dhuhr in 00:42:15`).
  - Right-click tray context menu: Open, Mute, Stop Audio, Next Prayer status, Settings, Exit.
  - Windows Startup: Option to launch automatically on Windows login (with `--tray` support to start silently in the background).
- **Audio Control:** Global volume slider, instant mute toggle, and prominent "Stop Audio" banner.

---

### 5. Standalone Packaging & Portability
- **Single Standalone Executable:** `ShiaPrayerTimes.exe` is ~93 KB, completely standalone, and runs directly on Windows 10 & Windows 11 without requiring any installers, runtime downloads, or external dependencies.
- **Persistent Storage:** Saves all configurations to `%APPDATA%\ShiaPrayerTimes\settings.json`.

---

## 🚀 How to Run

1. Simply double-click `ShiaPrayerTimes.exe`.
2. The application opens with Qatif, Saudi Arabia coordinates by default (UTC+3, Leva Qom method).
3. Click the **⚙️ Settings** icon to customize calculation methods, prayer visibility, offsets, alerts, or location.
4. To run minimized in system tray:
   ```cmd
   ShiaPrayerTimes.exe --tray
   ```

## 🛠️ How to Rebuild (Source Code)

Run `build.bat` in the project directory, or execute:
```powershell
& "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /target:winexe /win32icon:"app.ico" /out:"ShiaPrayerTimes.exe" /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF\PresentationCore.dll" /r:"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF\PresentationFramework.dll" /r:"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF\WindowsBase.dll" /r:"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.Xaml.dll" /r:"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.Web.Extensions.dll" PrayerCalculator.cs HijriCalendar.cs AppSettings.cs AudioNotifier.cs TrayManager.cs SettingsWindow.cs MainWindow.cs App.cs
```
