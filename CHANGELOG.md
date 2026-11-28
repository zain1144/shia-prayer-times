# Changelog / سجل التحديثات

All notable changes to **Shia Prayer Times (أوقات الصلاة الشيعية - الجعفرية)** are documented in this file.

---

## [v1.2.1] - 2026-11-28

### 🐛 Fixed | الإصلاحات
- **Reliable Adhan Audio on Lock Screen & Sleep Resume (`WaveOutPlayer.cs`, `AudioNotifier.cs`):**
  - Replaced DirectShow MCI (`type mpegvideo`) as the primary playback engine with direct **Media Foundation (`MFCreateSourceReaderFromURL`) -> `winmm.dll` `waveOutOpen` / `waveOutWrite`** raw PCM hardware streaming (retaining MCI and WPF `MediaPlayer` only as fallbacks).
  - Eliminates DirectShow/COM window-station stalls when Windows is locked (`Winlogon` Lock Screen), screen is off, or USB audio devices re-enumerate right after Sleep resume (includes automatic `waveOutOpen` retry).
  - Starts audio playback before displaying the WPF Toast notification window so UI visual state can never delay or block Adhan audio.
  - Added lightweight rotating diagnostic log (`%APPDATA%\ShiaPrayerTimes\app.log`) recording wake timer firing, alarm triggers, and audio engine status.

---

## [v1.2.0] - 2026-11-27

### ✨ Added | الإضافات الجديدة
- **Built-In In-App Updater (`AppUpdater.cs`):**
  - Checks GitHub Releases (`releases/latest`) over TLS 1.2 against the running app version (`AssemblyVersion`), silently once per day on startup (4 seconds after launch, toggleable in Settings) and manually anytime via **"Check for Updates / التحقق من التحديثات"** in Settings and the System Tray menu.
  - Displays a clean release notes dialog showing the new version, GitHub changelog, and live download progress bar while downloading `ShiaPrayerTimes.exe` into `%TEMP%` with size verification.
  - **Automatic Protected-Folder & `C:\` UAC Elevation:** Automatically detects whether the `.exe` folder is writable; performs silent self-replacement in normal user folders, or seamlessly requests Windows UAC admin elevation (`runas`) when placed in `C:\` or `C:\Program Files\`, then relaunches unelevated (`explorer.exe`) while preserving all user data in `%APPDATA%\ShiaPrayerTimes`.
- **Update Settings Control (`AppSettings.cs`, `SettingsWindow.cs`, `TrayManager.cs`):**
  - Added `AutoCheckUpdates` (enabled by default) and `LastUpdateCheckUtc` persistence fields.
  - Added a **"🔄 Check for Updates"** button in the Settings bottom bar and **"التحقق من التحديثات..."** item in the tray context menu.

---

## [v1.1.1] - 2026-11-27

### 🐛 Fixed | الإصلاحات
- **Adhan Playback Stalling While Locked / Screen Off / Unattended Wake (`AudioNotifier.cs`, `SleepWakeManager.cs`):**
  - Replaced WPF `MediaPlayer` with native Windows Multimedia (`winmm.dll` `mciSendStringW`) as the primary playback engine (keeping `MediaPlayer` as fallback). This eliminates the DWM/rendering stall where Adhan audio queued silently during screen-off or Lock Screen wake and only started playing after unlocking Windows.
  - Added `ES_DISPLAY_REQUIRED` alongside `ES_SYSTEM_REQUIRED` during wake hold and active audio playback so the monitor and HDMI/DisplayPort audio endpoints (such as NVIDIA High Definition Audio) power up cleanly on wake and turn off normally once playback finishes.
  - Prevented duplicate waitable timer re-arming during the 3-second pre-wake lead-in window (`_lastArmedAlertLocal`), and widened post-wake trigger window to 60 seconds (`MainWindow.cs`).

---

## [v1.1.0] - 2026-11-26

### ✨ Added | الإضافات الجديدة
- **Wake PC from Sleep for Adhan (`SleepWakeManager.cs`):**
  - Automatically arms a Windows hardware wakeable timer (`SetWaitableTimer` with `fResume = true`) 3 seconds prior to the next enabled prayer alert or pre-prayer reminder (rolling over cleanly into the next day after Midnight/Isha).
  - Prevents Windows from falling back asleep during long Adhan audio playback using `SetThreadExecutionState` (`ES_CONTINUOUS | ES_SYSTEM_REQUIRED`), releasing the power lock once playback finishes or stops.
  - Resumes UI countdowns and immediately checks missed/due alerts on `PowerModes.Resume`.
  - Added Settings toggle under Audio & System: **"Wake computer from Sleep mode automatically for prayer & reminder alerts"** (*إيقاظ الجهاز تلقائياً من وضع السكون (Sleep) عند حلول وقت الأذان أو التذكير*).
- **Versioning & Changelog:**
  - Embedded standard Windows Assembly version metadata (`1.1.0.0`) inside `ShiaPrayerTimes.exe` (`App.cs`).
  - Added structured `CHANGELOG.md` and semantic version tags/releases on GitHub.

### 🐛 Fixed & Improved (Since v1.0.0) | التحسينات والإصلاحات
- **First-Time Minimize Notification (`56d09e5`):**
  - Show the *"Application minimized to system tray"* Windows balloon hint only once on first close (`HasShownTrayMinimizeHint`), plus an explicit checkbox in Settings to re-enable or silence it.
- **Overflow Tray Flyout Support (`c619254`):**
  - Support live hover flyout positioning and hit-testing when the app icon is placed inside the Windows hidden system tray overflow menu (`^`).
- **Toast Notification Visual Polish (`d6b98a1`):**
  - Removed clipped `DropShadowEffect` and converted the countdown progress bar into a clean inset rounded capsule to eliminate square-corner artifacts.
- **Windows 11 Tray Hover Accuracy (`e2e6d5b` .. `ec8f807`):**
  - Actively suppress Windows 11 native `Xaml_WindowedPopupClass` empty tooltip popup.
  - Improved icon-slot bounding box hit-testing, responsive hiding on neighboring icons, multi-monitor clamping, and High-DPI coordinate scaling.
- **Responsive Window Sizing (`010b043`):**
  - Dynamically adapt `MainWindow` and `SettingsWindow` dimensions to work area across 720p, 1080p, 2K, and 4K displays.

---

## [v1.0.0] - 2026-11-18

### 🎉 Initial Release | الإصدار الأول
- Offline Shia Ithna-Ashari (Jafari) astronomical prayer times calculator supporting Sayyid Hashim al-Tubalani, Leva Qom, Tehran, and custom twilight angles.
- 9 configurable daily events (*Imsak, Fajr, Sunrise, Dhuhr, Asr, Sunset, Maghrib, Isha, Midnight*) with per-prayer visibility, minute offsets, Adhan library assignment, and pre-prayer reminders.
- Smart offline/online city search, Google Maps coordinate paste, and 50+ Gulf/Middle East city presets.
- Live System Tray countdown flyout card, customizable custom toast notifications, Arabic RTL & English LTR UI, Dark/Light themes, and standalone single-EXE portable build with embedded audio.
