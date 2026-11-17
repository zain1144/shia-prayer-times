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

### 4. Notification Testing (اختبار التنبيهات والإشعارات)
You can test and preview all notification types in 3 convenient ways:
1. **From Settings -> Audio & System Tab (الأصوات والمكتبة):**
   - **👁️ Visual Toast Only (إشعار مرئي فقط):** Displays a Windows notification banner in the corner of your screen without playing any audio.
   - **🎵 Chime Alert (نغمة تنبيه):** Displays the notification banner and plays the subtle chime tone.
   - **📢 Full Adhan (أذان كامل):** Displays the notification banner and plays the full Adhan audio (includes an instant red `⏹ Stop Sound` button).
   - **⏳ Pre-Prayer Reminder (تذكير مسبق):** Displays the countdown reminder notification banner (e.g., *"10 minutes before Dhuhr"*) and plays the reminder tone.
2. **Per-Prayer Row Testing (في مصفوفة الصلوات):**
   - In the **Prayers Matrix Tab (مصفوفة الصلوات)**, click the **`🔔`** button on any prayer row to trigger the exact notification configured for that specific prayer (taking into account its active notification mode and custom Adhan audio).
3. **From the Windows System Tray:**
   - Right-click the app icon in the Windows taskbar system tray -> hover over **اختبار التنبيهات (Test Notifications)** -> select any notification type to trigger it immediately.

---

### 5. Settings File Location (موقع ومسار ملف الإعدادات)
All user configurations, custom coordinates, selected calculation methods, per-prayer minute offsets, notification preferences, and custom Adhan sound libraries are stored locally in a persistent JSON file:

- **Path:**
  ```text
  %APPDATA%\ShiaPrayerTimes\settings.json
  ```
- **Full Path on Windows:**
  ```text
  C:\Users\<Your-Username>\AppData\Roaming\ShiaPrayerTimes\settings.json
  ```

#### How to Access or Edit the Settings File:
1. **Directly from the App:**
   - Open **Settings** -> go to the **🔊 الأصوات والمكتبة (Audio & System)** tab.
   - At the bottom, click **`📁 فتح مجلد الإعدادات في متصفح الملفات`** to open the folder in Windows File Explorer, or click **`📄 فتح ملف settings.json`** to view/edit the JSON file directly.
2. **Via Windows Run Dialog:**
   - Press `Win + R` on your keyboard, paste `%APPDATA%\ShiaPrayerTimes`, and hit Enter.
3. **To Reset to Factory Defaults:**
   - Close `ShiaPrayerTimes.exe` and simply delete `settings.json`. The application will regenerate the default configuration upon its next launch.

---

## 🚀 How to Run

1. Close any running instance of `ShiaPrayerTimes` in your system tray or background.
2. Double-click `ShiaPrayerTimes.exe` in:
   [`C:\Users\Home\shia-prayer-times\ShiaPrayerTimes.exe`](file:///C:/Users/Home/shia-prayer-times/ShiaPrayerTimes.exe)
3. To rebuild at any time, run `build.bat`.
