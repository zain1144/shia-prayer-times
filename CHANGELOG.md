# Changelog / سجل التحديثات

All notable changes to **Shia Prayer Times (أوقات الصلاة الشيعية - الجعفرية)** are documented in this file.

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
