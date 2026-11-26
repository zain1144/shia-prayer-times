# Windows Shia (Jafari) Prayer Times Desktop Application
# تطبيق أوقات الصلاة الشيعية (الجعفرية) لنظام ويندوز

A lightweight, offline-capable, standalone Windows desktop application designed specifically for calculating and displaying Shia Ithna-Ashari (Jafari) Islamic prayer times with high astronomical accuracy. Built to match the behavior, calculation standards, and per-prayer customization of the Android app **Pray Times** (`com.smustafa.praytimes`).

---

## 🌟 Version 1.1.0 Highlights (Latest)

See the full version history in [CHANGELOG.md](CHANGELOG.md) and [GitHub Releases](https://github.com/zain1144/shia-prayer-times/releases).

1. **Wake PC from Windows Sleep Mode (إيقاظ الجهاز تلقائياً من السكون):**
   - Schedules Win32 wakeable timers (`SetWaitableTimer`) for upcoming enabled Adhan/reminder alerts so Windows wakes automatically from Sleep to play the Adhan, and keeps the PC awake until long Adhan playback finishes.
2. **System Tray & Notification Refinements:**
   - Works smoothly even inside the Windows hidden tray overflow menu (`^`), suppresses Windows 11 native empty tooltip artifacts, rounds toast progress bars cleanly, and shows the minimize-to-tray hint once on first close.
3. **Previous Core Features (v1.0.0+):**
   - **تقويم السيد هاشم التوبلاني (Sayyid Hashim al-Tubalani Calendar):** `Fajr: 18.0°, Maghrib: 4.0°, Isha: 14.0°, Midnight: Jafari`.
   - **Smart City Search & Google Maps Coordinate Paste:** Instant accurate coordinates without requiring Windows Location Services.
   - **Adhan Sounds Library & Per-Prayer Sound Assignment:** Embedded standalone audio with custom per-prayer assignment.
   - **Multi-Resolution & High-DPI Support:** Adapts automatically across 720p, 1080p, 2K, and 4K screens.
   - **Bilingual Dark/Light Theme UI:** Full Arabic (RTL) and English (LTR) support with integrated title bar.

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

### 3. Location & Calendar Management (Zero Location-Service Required)
Because telecom ISPs (such as STC, Mobily, Zain, 5G, or VPNs) route internet traffic through central corporate data centers (e.g. Riyadh, Jeddah, Dammam), relying on ISP IP geolocation often yields a server location far from your actual town. Astronomical prayer times require meter-accurate coordinates. The application provides multiple accurate methods that **do not require Windows Location Services or GPS**:
- **🔍 Smart City & Neighborhood Search (البحث الفوري بالاسم):**
  - Search by any city, town, or neighborhood name (e.g. *سيهات، تاروت، صفوى، الهفوف، سار، النجف، الكاظمية*).
  - Searches both the local offline Shia/Gulf database and OpenStreetMap text geocoding without accessing device GPS or location sensors.
- **📋 Smart Paste from Google Maps (اللصق الذكي من خرائط جوجل):**
  - Right-click your house or local mosque on Google Maps, copy the coordinates or link, and click **`📋 لصق من الحافظة`**. The app extracts the exact latitude and longitude with meter accuracy.
- **🏙️ 50+ Pre-loaded Gulf & Middle Eastern City Presets:**
  - Rich offline presets for Qatif and all its towns (Saihat, Tarout, Safwa, Awwamiyah, Qudaih, Jaroudiya, Umm Al-Hammam, Jish, Anak, Sanabis, Rabi'iya), Al-Ahsa (Hofuf, Mubarraz, Omran, Qarah, Rumaila, Taraf), Bahrain (Manama, Muharraq, Sitra, Saar, Diraz, Jidhafs, A'ali, Sanabis, Karzakan), Iraq (Najaf, Karbala, Kadhimiya, Samarra, Kufa, Basra, Baghdad), Kuwait, Oman, UAE, Lebanon, Iran, and diaspora centers.
- **✍️ Manual Coordinates:** Full manual override for Latitude, Longitude, Timezone, and Elevation.
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
