# Windows Shia (Jafari) Prayer Times Desktop Application
# تطبيق أوقات الصلاة الشيعية (الجعفرية) لنظام ويندوز

A lightweight, offline-capable, standalone Windows desktop application designed specifically for calculating and displaying Shia Ithna-Ashari (Jafari) Islamic prayer times with high astronomical accuracy. Built to match the behavior, calculation standards, and per-prayer customization of the Android app **Pray Times** (`com.smustafa.praytimes`).

---

## 🌟 What's New in Version 2.1

1. **تقويم السيد هاشم التوبلاني (Sayyid Hashim al-Tubalani Calendar):**
   - Added as a primary calculation standard: `Fajr: 18.0°, Maghrib: 4.0°, Isha: 14.0°, Midnight: Jafari`.
2. **Auto-Detect My Location (تحديد الموقع تلقائياً):**
   - One-click automatic geolocation detection button (`📍`) in both the main window and settings to fetch coordinates, timezone, and city name instantly.
3. **Adhan Sounds Library & Per-Prayer Sound Assignment:**
   - Ability to add, name, and save multiple Adhan audio files in a persistent library.
   - Assign a distinct Adhan sound from your library to any prayer (e.g. distinct Fajr Adhan, Karbala Adhan for Maghrib, Najaf Adhan for Dhuhr).
4. **Enhanced High-Contrast Dark Theme UI:**
   - Completely resolved font contrast: ComboBoxes now feature a solid dark slate background (`#1E293B`) with pure white text (`#FFFFFF`) and golden indicators.
   - Integrated title bar with seamless window dragging, minimize, maximize/restore, and close buttons.
5. **Brand-New Multi-Resolution Icon:**
   - Ultra-sharp 256x256, 128x128, 64x64, 48x48, 32x32, 16x16 icon featuring a royal emerald/navy shield, 24K gold border, glowing crescent, and an 8-pointed star (*Rub El Hizb*).
6. **Git Version Control:**
   - Full Git repository initialized with clean history and `.gitignore`.

---

## 🚀 Key Features

### 1. Astronomical Calculation Engine (100% Offline)
- **Zero Internet Requirement:** Calculates all times locally using exact solar coordinates and spherical astronomy formulas.
- **Supported Shia (Jafari) Methods:**
  - **تقويم السيد هاشم التوبلاني (Sayyid Hashim al-Tubalani):** Fajr = 18.0°, Maghrib = 4.0°, Isha = 14.0°, Midnight = Sunset to Fajr.
  - **Leva Research Institute, Qom (معهد لواء للأبحاث، قم):** Fajr = 16.0°, Maghrib = 4.0°, Isha = 14.0°, Midnight = Sunset to Fajr.
  - **Institute of Geophysics, University of Tehran (جامعة طهران):** Fajr = 17.7°, Maghrib = 4.5°, Isha = 14.0°, Midnight = Sunset to Fajr.
  - **Custom Angles (زوايا فلكية مخصصة):** Manual override for Fajr, Maghrib, and Isha twilight angles.
- **Supported 9 Time Events:**
  1. **Imsak (الإمساك):** Default 10 minutes before Fajr (configurable 5-30 min)
  2. **Fajr (الفجر / الصباح):** True astronomical dawn
  3. **Sunrise (الشروق):** True solar rise with elevation adjustments
  4. **Dhuhr (الظهر):** Astronomical midday solar transit
  5. **Asr (العصر):** Jafari juristic shadow calculation (ratio = 1.0)
  6. **Sunset (الغروب):** Disappearance of the solar disk
  7. **Maghrib (المغرب):** Disappearance of the eastern redness (*al-Humrah al-Mashriqiyyah*)
  8. **Isha (العشاء):** Disappearance of western twilight
  9. **Midnight (منتصف الليل الشرعي):** Midpoint between Sunset and Fajr (Jafari standard) or Sunset and Sunrise (selectable option).

---

### 2. Granular Per-Prayer Settings Matrix
Every single prayer event has an independent configuration row:
- **Display Toggle (Show/Hide):** Show or hide that prayer from the main schedule list and live countdown cycle.
- **Manual Time Offset:** Individual minute adjustment (`-15` to `+15` minutes) to match local mosque timetables.
- **Notification Mode:** Off / Silent, Visual Toast Only, Short Chime / Beep, Full Adhan Audio.
- **Pre-Prayer Reminder:** Independent pre-alarm (Off, 5, 10, 15, 20, or 30 minutes before prayer).
- **Sound Choice:** Assign any sound from your Adhan Library to each prayer, with preview play/stop buttons.

---

### 3. Location & Calendar Management
- **One-Click Auto-Detect Location (`📍`):** Automatically detects coordinates, timezone, and city name via network or Windows sensors.
- **Pre-configured City Presets:** Qatif, Najaf, Karbala, Qom, Tehran, Manama, Al-Ahsa, Muscat, Beirut, London, Detroit.
- **Dual Calendar:** Localized Gregorian and Hijri dates with moon sighting manual offset (`-2` to `+2` days).
- **Past & Future Date Browser:** Navigate with `< Prev Day`, `Today`, `Next Day >`, and Calendar Date Picker.

---

## 🚀 How to Run

1. Close any running instance of `ShiaPrayerTimes` in your system tray or background.
2. Double-click `ShiaPrayerTimes.exe` in:
   [`C:\Users\Home\shia-prayer-times\ShiaPrayerTimes.exe`](file:///C:/Users/Home/shia-prayer-times/ShiaPrayerTimes.exe)
3. To rebuild at any time, run `build.bat`.
