using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;
using Microsoft.Win32;

namespace ShiaPrayerTimes
{
    public class LocationPreset
    {
        public string Name { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Timezone { get; set; }
        public double Elevation { get; set; }

        public LocationPreset() { }

        public LocationPreset(string name, double lat, double lon, double tz, double elv = 0)
        {
            Name = name;
            Latitude = lat;
            Longitude = lon;
            Timezone = tz;
            Elevation = elv;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class AdhanSound
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string FilePath { get; set; }
        public bool IsBuiltIn { get; set; }

        public AdhanSound() { }
        public AdhanSound(string id, string name, string path, bool isBuiltIn = false)
        {
            Id = id;
            Name = name;
            FilePath = path ?? "";
            IsBuiltIn = isBuiltIn;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class PrayerEventConfig
    {
        public bool Visible { get; set; }
        public int OffsetMinutes { get; set; }
        public string NotificationMode { get; set; } // "Off", "Toast", "Chime", "Adhan"
        public int ReminderMinutes { get; set; }     // 0 = Off, 5, 10, 15, 20, 30
        public string SoundId { get; set; }          // ID of sound in AdhanLibrary for Adhan
        public string CustomAudioPath { get; set; }  // Direct path if custom
        public string ReminderSoundId { get; set; }  // "chime", "beep", "silent", or ID from AdhanLibrary
        public string CustomReminderAudioPath { get; set; } // Direct path if custom reminder sound

        public PrayerEventConfig()
        {
            Visible = true;
            OffsetMinutes = 0;
            NotificationMode = "Adhan";
            ReminderMinutes = 0;
            SoundId = "default";
            CustomAudioPath = "";
            ReminderSoundId = "chime";
            CustomReminderAudioPath = "";
        }

        public PrayerEventConfig(bool visible, int offset, string notifMode, int reminder = 0, string soundId = "default", string customAudio = "", string reminderSoundId = "chime", string customReminderAudio = "")
        {
            Visible = visible;
            OffsetMinutes = offset;
            NotificationMode = notifMode;
            ReminderMinutes = reminder;
            SoundId = soundId ?? "default";
            CustomAudioPath = customAudio ?? "";
            ReminderSoundId = reminderSoundId ?? "chime";
            CustomReminderAudioPath = customReminderAudio ?? "";
        }
    }

    public class EmbeddedSoundEntry
    {
        public string FileName { get; set; }
        public string DisplayName { get; set; }
        public string DefaultPrayer { get; set; }

        public EmbeddedSoundEntry(string fileName, string displayName, string defaultPrayer)
        {
            FileName = fileName;
            DisplayName = displayName;
            DefaultPrayer = defaultPrayer;
        }
    }

    public class AppSettings
    {
        public string Language { get; set; }
        public string Theme { get; set; }
        public bool TimeFormat24H { get; set; }
        public int HijriOffset { get; set; }
        public bool AutoStartWithWindows { get; set; }
        public bool MinimizeToTrayOnClose { get; set; }

        public LocationPreset Location { get; set; }
        public List<LocationPreset> Presets { get; set; }

        public string CalculationMethodName { get; set; } // "Jafari", "Tubalani", "Tehran", "Custom"
        public double CustomFajrAngle { get; set; }
        public double CustomMaghribAngle { get; set; }
        public double CustomIshaAngle { get; set; }
        public string MidnightModeName { get; set; }       // "Jafari", "Standard"
        public int ImsakMinutesBeforeFajr { get; set; }
        public string HighLatsName { get; set; }          // "AngleBased", "NightMiddle", "OneSeventh", "None"

        public int Volume { get; set; }                   // 0 - 100
        public bool IsMuted { get; set; }
        public string DefaultAdhanPath { get; set; }
        public string DefaultFajrAdhanPath { get; set; }

        public int NotificationDurationSeconds { get; set; } // 0 = persistent, 5, 10, 15, 30, 60
        public string NotificationStyle { get; set; }        // "CustomToast", "WindowsNative"
        public string ToastPosition { get; set; }            // "BottomRight", "BottomLeft", "TopRight", "TopLeft", "TopCenter", "Center", "Custom"
        public string ToastSize { get; set; }                // "Small", "Medium", "Large", "Wide"
        public double CustomToastLeft { get; set; }          // Drag & Drop saved X position
        public double CustomToastTop { get; set; }           // Drag & Drop saved Y position

        public List<AdhanSound> AdhanLibrary { get; set; }
        public Dictionary<string, PrayerEventConfig> Prayers { get; set; }

        public AppSettings()
        {
            Language = "ar";
            Theme = "Dark";
            TimeFormat24H = false;
            HijriOffset = 0;
            AutoStartWithWindows = false;
            MinimizeToTrayOnClose = true;
            NotificationDurationSeconds = 10;
            NotificationStyle = "CustomToast";
            ToastPosition = "BottomRight";
            ToastSize = "Medium";
            CustomToastLeft = -1;
            CustomToastTop = -1;

            Location = new LocationPreset("القطيف - المملكة العربية السعودية (Qatif)", 26.5577, 50.0081, 3.0, 0);

            Presets = new List<LocationPreset>
            {
                new LocationPreset("القطيف (Qatif, Saudi Arabia)", 26.5577, 50.0081, 3.0),
                new LocationPreset("النجف الأشرف (Najaf, Iraq)", 31.9959, 44.3148, 3.0),
                new LocationPreset("كربلاء المقدسة (Karbala, Iraq)", 32.6160, 44.0249, 3.0),
                new LocationPreset("قم المقدسة (Qom, Iran)", 34.6416, 50.8746, 3.5),
                new LocationPreset("طهران (Tehran, Iran)", 35.6892, 51.3890, 3.5),
                new LocationPreset("المنامة (Manama, Bahrain)", 26.2285, 50.5860, 3.0),
                new LocationPreset("الأحساء (Al-Ahsa, Saudi Arabia)", 25.3833, 49.5833, 3.0),
                new LocationPreset("مسقط (Muscat, Oman)", 23.5859, 58.4059, 4.0),
                new LocationPreset("بيروت (Beirut, Lebanon)", 33.8938, 35.5018, 2.0),
                new LocationPreset("لندن (London, UK)", 51.5074, -0.1278, 0.0),
                new LocationPreset("ديربورن - ديترويت (Dearborn / Detroit, USA)", 42.3223, -83.1763, -5.0)
            };

            CalculationMethodName = "Tubalani"; // Default or Jafari
            CustomFajrAngle = 18.0;
            CustomMaghribAngle = 4.0;
            CustomIshaAngle = 14.0;
            MidnightModeName = "Jafari";
            ImsakMinutesBeforeFajr = 10;
            HighLatsName = "AngleBased";

            Volume = 85;
            IsMuted = false;
            DefaultAdhanPath = "";
            DefaultFajrAdhanPath = "";

            InitDefaultSounds();
            EnsureEmbeddedSoundsInLibrary();

            if (AdhanLibrary != null)
            {
                foreach (var s in AdhanLibrary)
                {
                    if (s.Id == "default" && s.Name.Contains("Default Adhan")) s.Name = "الأذان الافتراضي (Default)";
                    else if (s.Id == "tone" && s.Name.Contains("Spiritual")) s.Name = "نغمة الأذان (Tone)";
                    else if (s.Id == "chime" && s.Name.Contains("Gentle")) s.Name = "نغمة هادئة (Chime)";
                    else if (s.Id == "fajr" && s.Name.Contains("Fajr Distinct")) s.Name = "أذان الفجر المخصوص (Fajr)";
                }
            }

            Prayers = new Dictionary<string, PrayerEventConfig>(StringComparer.OrdinalIgnoreCase)
            {
                { "Imsak", new PrayerEventConfig(false, 0, "Toast", 0, "chime") },
                { "Fajr", new PrayerEventConfig(true, 0, "Adhan", 15, "default") },
                { "Sunrise", new PrayerEventConfig(true, 0, "Chime", 0, "chime") },
                { "Dhuhr", new PrayerEventConfig(true, 0, "Adhan", 10, "default") },
                { "Asr", new PrayerEventConfig(true, 0, "Chime", 0, "chime") },
                { "Sunset", new PrayerEventConfig(false, 0, "Off", 0, "chime") },
                { "Maghrib", new PrayerEventConfig(true, 0, "Adhan", 10, "default") },
                { "Isha", new PrayerEventConfig(true, 0, "Chime", 0, "chime") },
                { "Midnight", new PrayerEventConfig(true, 0, "Chime", 0, "chime") }
            };
        }

        public void InitDefaultSounds()
        {
            ExtractEmbeddedAudioDefaults();
            ExtractEmbeddedIcon();

            if (AdhanLibrary == null) AdhanLibrary = new List<AdhanSound>();
            if (AdhanLibrary.Count == 0)
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string tonePath = Path.Combine(baseDir, "Audio", "adhan_tone.wav");
                if (!File.Exists(tonePath)) tonePath = Path.Combine(GetConfigDirectory(), "Audio", "adhan_tone.wav");

                string chimePath = Path.Combine(baseDir, "Audio", "chime.wav");
                if (!File.Exists(chimePath)) chimePath = Path.Combine(GetConfigDirectory(), "Audio", "chime.wav");

                string almusawiPath = Path.Combine(baseDir, "Audio", "almusawi_adhan.mp3");
                if (!File.Exists(almusawiPath)) almusawiPath = Path.Combine(GetConfigDirectory(), "Audio", "almusawi_adhan.mp3");

                string defaultAdhanPath = File.Exists(almusawiPath) ? almusawiPath : tonePath;
                string defaultAdhanName = File.Exists(almusawiPath) ? "أذان السيد هاني الموسوي" : "نغمة الأذان الافتراضية";

                AdhanLibrary.Add(new AdhanSound("default", defaultAdhanName, defaultAdhanPath, true));
                AdhanLibrary.Add(new AdhanSound("tone", "نغمة الأذان (Tone)", tonePath, true));
                AdhanLibrary.Add(new AdhanSound("chime", "نغمة هادئة (Chime)", chimePath, true));
                if (File.Exists(almusawiPath))
                {
                    AdhanLibrary.Add(new AdhanSound("almusawi_adhan", "أذان السيد هاني الموسوي", almusawiPath, true));
                }
            }
        }

        public static string EnsureExtractedResource(string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(fileName)) return "";
                string targetDir = Path.Combine(GetConfigDirectory(), "Audio");
                if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

                string dest = Path.Combine(targetDir, fileName);
                if (File.Exists(dest) && new FileInfo(dest).Length > 0)
                {
                    return dest;
                }

                var asm = System.Reflection.Assembly.GetExecutingAssembly();
                string resName = "Audio." + fileName;
                using (var stream = asm.GetManifestResourceStream(resName))
                {
                    if (stream != null)
                    {
                        using (var fs = new FileStream(dest, FileMode.Create, FileAccess.Write))
                        {
                            byte[] buffer = new byte[8192];
                            int read;
                            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                fs.Write(buffer, 0, read);
                            }
                        }
                        return dest;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("EnsureExtractedResource error: " + ex.Message);
            }
            return "";
        }

        public static void ExtractEmbeddedIcon()
        {
            try
            {
                string targetDir = GetConfigDirectory();
                string dest = Path.Combine(targetDir, "app_new.ico");
                if (!File.Exists(dest) || new FileInfo(dest).Length == 0)
                {
                    var asm = System.Reflection.Assembly.GetExecutingAssembly();
                    string[] iconNames = { "app_new.ico", "app.ico" };
                    foreach (var name in iconNames)
                    {
                        using (var stream = asm.GetManifestResourceStream(name))
                        {
                            if (stream != null)
                            {
                                using (var fs = new FileStream(dest, FileMode.Create, FileAccess.Write))
                                {
                                    byte[] buffer = new byte[8192];
                                    int read;
                                    while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                                    {
                                        fs.Write(buffer, 0, read);
                                    }
                                }
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ExtractEmbeddedIcon error: " + ex.Message);
            }
        }

        public static Dictionary<string, EmbeddedSoundEntry> LoadEmbeddedSoundConfigs(System.Reflection.Assembly asm)
        {
            var dict = new Dictionary<string, EmbeddedSoundEntry>(StringComparer.OrdinalIgnoreCase);
            try
            {
                string text = null;
                string localTxt = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Audio", "sounds.txt");
                if (File.Exists(localTxt))
                {
                    text = File.ReadAllText(localTxt, System.Text.Encoding.UTF8);
                }
                else if (asm != null)
                {
                    using (var s = asm.GetManifestResourceStream("Audio.sounds.txt"))
                    {
                        if (s != null)
                        {
                            using (var reader = new StreamReader(s, System.Text.Encoding.UTF8))
                            {
                                text = reader.ReadToEnd();
                            }
                        }
                    }
                }

                if (!string.IsNullOrEmpty(text))
                {
                    using (var reader = new StringReader(text))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            line = line.Trim();
                            if (string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;

                            string[] parts = line.Split('|');
                            if (parts.Length >= 2)
                            {
                                string fn = parts[0].Trim();
                                string name = parts[1].Trim();
                                string prayer = parts.Length > 2 ? parts[2].Trim() : "";
                                if (!string.IsNullOrEmpty(fn) && !string.IsNullOrEmpty(name))
                                {
                                    dict[fn] = new EmbeddedSoundEntry(fn, name, prayer);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("LoadEmbeddedSoundConfigs error: " + ex.Message);
            }
            return dict;
        }

        public static void ExtractEmbeddedAudioDefaults()
        {
            try
            {
                string targetDir = Path.Combine(GetConfigDirectory(), "Audio");
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                var asm = System.Reflection.Assembly.GetExecutingAssembly();
                string[] resources = asm.GetManifestResourceNames();

                foreach (string res in resources)
                {
                    if (res.StartsWith("Audio.") && !res.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                    {
                        string fileName = res.Substring("Audio.".Length);
                        string dest = Path.Combine(targetDir, fileName);

                        if (!File.Exists(dest) || new FileInfo(dest).Length == 0)
                        {
                            using (var stream = asm.GetManifestResourceStream(res))
                            {
                                if (stream != null)
                                {
                                    using (var fs = new FileStream(dest, FileMode.Create, FileAccess.Write))
                                    {
                                        byte[] buffer = new byte[8192];
                                        int read;
                                        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                                        {
                                            fs.Write(buffer, 0, read);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ExtractEmbeddedAudioDefaults error: " + ex.Message);
            }
        }

        public void EnsureEmbeddedSoundsInLibrary()
        {
            try
            {
                if (AdhanLibrary == null) AdhanLibrary = new List<AdhanSound>();
                string targetDir = Path.Combine(GetConfigDirectory(), "Audio");
                string baseAudio = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Audio");

                var asm = System.Reflection.Assembly.GetExecutingAssembly();
                string[] resources = asm.GetManifestResourceNames();
                var configs = LoadEmbeddedSoundConfigs(asm);

                foreach (string res in resources)
                {
                    if (res.StartsWith("Audio.") && !res.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                    {
                        string fileName = res.Substring("Audio.".Length);
                        string rawId = Path.GetFileNameWithoutExtension(fileName).ToLowerInvariant().Replace(" ", "_");

                        string filePath = Path.Combine(targetDir, fileName);
                        if (!File.Exists(filePath))
                        {
                            string localPath = Path.Combine(baseAudio, fileName);
                            if (File.Exists(localPath)) filePath = localPath;
                            else filePath = EnsureExtractedResource(fileName);
                        }

                        string displayName = Path.GetFileNameWithoutExtension(fileName);
                        string defaultPrayer = "";

                        if (configs.ContainsKey(fileName))
                        {
                            displayName = configs[fileName].DisplayName;
                            defaultPrayer = configs[fileName].DefaultPrayer;
                        }
                        else if (rawId == "adhan_tone" || rawId == "tone")
                        {
                            displayName = "نغمة الأذان الافتراضية";
                        }
                        else if (rawId == "chime")
                        {
                            displayName = "نغمة هادئة (Chime)";
                        }

                        var existing = AdhanLibrary.Find(x =>
                            x.Id == rawId ||
                            (x.FilePath != null && x.FilePath.EndsWith(fileName, StringComparison.OrdinalIgnoreCase)));

                        if (existing == null)
                        {
                            AdhanLibrary.Add(new AdhanSound(rawId, displayName, filePath, true));
                        }
                        else
                        {
                            existing.FilePath = filePath;
                            if (!string.IsNullOrEmpty(displayName) && (string.IsNullOrEmpty(existing.Name) || existing.IsBuiltIn))
                            {
                                existing.Name = displayName;
                            }
                        }

                        if (!string.IsNullOrEmpty(defaultPrayer) && Prayers != null)
                        {
                            string[] prayers = defaultPrayer.Split(new char[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var p in prayers)
                            {
                                if (Prayers.ContainsKey(p))
                                {
                                    var prayerCfg = Prayers[p];
                                    if (prayerCfg.SoundId == "default" || prayerCfg.SoundId == "chime" || string.IsNullOrEmpty(prayerCfg.SoundId))
                                    {
                                        prayerCfg.SoundId = rawId;
                                        if (prayerCfg.NotificationMode != "Off") prayerCfg.NotificationMode = "Adhan";
                                    }
                                }
                                else if (p.Equals("Reminder", StringComparison.OrdinalIgnoreCase))
                                {
                                    foreach (var kvp in Prayers)
                                    {
                                        if (kvp.Value.ReminderSoundId == "chime" || string.IsNullOrEmpty(kvp.Value.ReminderSoundId))
                                        {
                                            kvp.Value.ReminderSoundId = rawId;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // Ensure default adhan has a valid real file path
                var defSound = AdhanLibrary.Find(x => x.Id == "default");
                if (defSound != null)
                {
                    if (string.IsNullOrEmpty(defSound.FilePath) || !File.Exists(defSound.FilePath))
                    {
                        string p = EnsureExtractedResource("almusawi_adhan.mp3");
                        if (string.IsNullOrEmpty(p) || !File.Exists(p)) p = EnsureExtractedResource("adhan_tone.wav");
                        defSound.FilePath = p;
                        if (!string.IsNullOrEmpty(p) && p.EndsWith("almusawi_adhan.mp3", StringComparison.OrdinalIgnoreCase))
                        {
                            defSound.Name = "أذان السيد هاني الموسوي";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("EnsureEmbeddedSoundsInLibrary error: " + ex.Message);
            }
        }

        public static string GetConfigDirectory()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string dir = Path.Combine(appData, "ShiaPrayerTimes");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return dir;
        }

        public static string GetConfigFilePath()
        {
            return Path.Combine(GetConfigDirectory(), "settings.json");
        }

        public static AppSettings Load()
        {
            ExtractEmbeddedAudioDefaults();
            ExtractEmbeddedIcon();

            try
            {
                string path = GetConfigFilePath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var serializer = new JavaScriptSerializer();
                    var settings = serializer.Deserialize<AppSettings>(json);
                    if (settings != null)
                    {
                        settings.EnsureDefaults();
                        settings.Save();
                        return settings;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading settings: " + ex.Message);
            }

            var defaultSettings = new AppSettings();
            defaultSettings.EnsureDefaults();
            defaultSettings.Save();
            return defaultSettings;
        }

        public static List<LocationPreset> GetDefaultLocationPresets()
        {
            return new List<LocationPreset>
            {
                // محافظة القطيف وتوابعها
                new LocationPreset("القطيف - المركز (Qatif)", 26.5577, 50.0081, 3.0),
                new LocationPreset("سيهات (Saihat)", 26.4750, 50.0416, 3.0),
                new LocationPreset("جزيرة تاروت (Tarout Island)", 26.5745, 50.0560, 3.0),
                new LocationPreset("صفوى (Safwa)", 26.6508, 49.9577, 3.0),
                new LocationPreset("العوامية (Al-Awamiyah)", 26.5912, 49.9983, 3.0),
                new LocationPreset("القديح (Al-Qudaih)", 26.5786, 50.0028, 3.0),
                new LocationPreset("الجارودية (Al-Jaroudiya)", 26.5297, 49.9911, 3.0),
                new LocationPreset("أم الحمام (Umm Al-Hammam)", 26.5161, 50.0069, 3.0),
                new LocationPreset("الجش (Al-Jish)", 26.5117, 49.9886, 3.0),
                new LocationPreset("عنك (Anak)", 26.5244, 50.0381, 3.0),
                new LocationPreset("سنابس - تاروت (Sanabis)", 26.5767, 50.0764, 3.0),
                new LocationPreset("الربيعية (Al-Rabi'iya)", 26.5683, 50.0631, 3.0),
                new LocationPreset("الخويلدية (Al-Khuwaildiyah)", 26.5417, 49.9972, 3.0),
                new LocationPreset("التوبي (Al-Tobi)", 26.5431, 50.0056, 3.0),
                new LocationPreset("حلة محيش (Hilla Muhaysh)", 26.5250, 50.0083, 3.0),
                new LocationPreset("الأوجام (Al-Awjam)", 26.5639, 49.9361, 3.0),

                // محافظة الأحساء
                new LocationPreset("الأحساء - الهفوف (Hofuf)", 25.3640, 49.5880, 3.0),
                new LocationPreset("الأحساء - المبرز (Mubarraz)", 25.4190, 49.5930, 3.0),
                new LocationPreset("الأحساء - العمران (Al-Omran)", 25.4389, 49.7183, 3.0),
                new LocationPreset("الأحساء - القارة (Al-Qarah)", 25.4167, 49.6667, 3.0),
                new LocationPreset("الأحساء - الرميلة (Al-Rumaila)", 25.4333, 49.6833, 3.0),
                new LocationPreset("الأحساء - الطرف (Al-Taraf)", 25.3500, 49.7167, 3.0),

                // المنطقة الشرقية وباقي السعودية
                new LocationPreset("الدمام (Dammam)", 26.4207, 50.0888, 3.0),
                new LocationPreset("الخبر (Khobar)", 26.2818, 50.2084, 3.0),
                new LocationPreset("الظهران (Dhahran)", 26.2886, 50.1140, 3.0),
                new LocationPreset("الجبيل (Jubail)", 27.0046, 49.6596, 3.0),
                new LocationPreset("حفر الباطن (Hafar Al-Batin)", 28.4328, 45.9708, 3.0),
                new LocationPreset("الرياض (Riyadh)", 24.7136, 46.6753, 3.0),
                new LocationPreset("المدينة المنورة (Madinah)", 24.5247, 39.5692, 3.0),
                new LocationPreset("مكة المكرمة (Makkah)", 21.4225, 39.8262, 3.0),
                new LocationPreset("جدة (Jeddah)", 21.4858, 39.1925, 3.0),
                new LocationPreset("نجران (Najran)", 17.4924, 44.1277, 3.0),

                // مملكة البحرين
                new LocationPreset("المنامة (Manama, Bahrain)", 26.2285, 50.5860, 3.0),
                new LocationPreset("المحرق (Muharraq, Bahrain)", 26.2572, 50.6119, 3.0),
                new LocationPreset("سترة (Sitra, Bahrain)", 26.1550, 50.6186, 3.0),
                new LocationPreset("سار (Saar, Bahrain)", 26.1969, 50.4853, 3.0),
                new LocationPreset("الدراز (Diraz, Bahrain)", 26.2197, 50.4686, 3.0),
                new LocationPreset("جدحفص (Jidhafs, Bahrain)", 26.2167, 50.5333, 3.0),
                new LocationPreset("عالي (A'ali, Bahrain)", 26.1539, 50.5283, 3.0),
                new LocationPreset("السنابس (Sanabis, Bahrain)", 26.2294, 50.5406, 3.0),
                new LocationPreset("كرزكان (Karzakan, Bahrain)", 26.1417, 50.4833, 3.0),
                new LocationPreset("بوري (Buri, Bahrain)", 26.1667, 50.5000, 3.0),

                // العراق
                new LocationPreset("النجف الأشرف (Najaf, Iraq)", 31.9959, 44.3148, 3.0),
                new LocationPreset("كربلاء المقدسة (Karbala, Iraq)", 32.6160, 44.0249, 3.0),
                new LocationPreset("الكاظمية (Kadhimiya, Iraq)", 33.3800, 44.3400, 3.0),
                new LocationPreset("سامراء (Samarra, Iraq)", 34.1983, 43.8742, 3.0),
                new LocationPreset("الكوفة (Kufa, Iraq)", 32.0294, 44.4022, 3.0),
                new LocationPreset("البصرة (Basra, Iraq)", 30.5081, 47.7835, 3.0),
                new LocationPreset("بغداد (Baghdad, Iraq)", 33.3152, 44.3661, 3.0),

                // الكويت & عُمان & الإمارات & لبنان
                new LocationPreset("الكويت العاصمة (Kuwait City)", 29.3759, 47.9774, 3.0),
                new LocationPreset("مسقط (Muscat, Oman)", 23.5880, 58.3829, 4.0),
                new LocationPreset("دبي (Dubai, UAE)", 25.2048, 55.2708, 4.0),
                new LocationPreset("بيروت (Beirut, Lebanon)", 33.8938, 35.5018, 2.0),

                // إيران
                new LocationPreset("قم المقدسة (Qom, Iran)", 34.6416, 50.8746, 3.5),
                new LocationPreset("مشهد المقدسة (Mashhad, Iran)", 36.2972, 59.6067, 3.5),
                new LocationPreset("طهران (Tehran, Iran)", 35.6892, 51.3890, 3.5),
                new LocationPreset("الأهواز (Ahvaz, Iran)", 31.3183, 48.6706, 3.5),

                // بلدان المهجر
                new LocationPreset("ديربورن / ديترويت (Dearborn / Detroit, USA)", 42.3223, -83.1763, -5.0),
                new LocationPreset("لندن (London, UK)", 51.5074, -0.1278, 0.0)
            };
        }

        public void EnsureDefaults()
        {
            ExtractEmbeddedAudioDefaults();

            if (Location == null) Location = new LocationPreset("القطيف - المملكة العربية السعودية (Qatif)", 26.5577, 50.0081, 3.0, 0);
            if (Presets == null || Presets.Count < 20)
            {
                Presets = GetDefaultLocationPresets();
            }
            InitDefaultSounds();
            EnsureEmbeddedSoundsInLibrary();

            if (AdhanLibrary != null)
            {
                foreach (var s in AdhanLibrary)
                {
                    if (s.Id == "default" && s.Name.Contains("Default Adhan")) s.Name = "الأذان الافتراضي (Default)";
                    else if (s.Id == "tone" && s.Name.Contains("Spiritual")) s.Name = "نغمة الأذان (Tone)";
                    else if (s.Id == "chime" && s.Name.Contains("Gentle")) s.Name = "نغمة هادئة (Chime)";
                    else if (s.Id == "fajr" && s.Name.Contains("Fajr Distinct")) s.Name = "أذان الفجر المخصوص (Fajr)";
                }
            }

            if (NotificationDurationSeconds < 0) NotificationDurationSeconds = 10;
            if (string.IsNullOrEmpty(NotificationStyle)) NotificationStyle = "CustomToast";
            if (string.IsNullOrEmpty(ToastPosition)) ToastPosition = "BottomRight";
            if (ToastPosition == "Custom" && (CustomToastLeft < 0 || CustomToastTop < 0)) ToastPosition = "BottomRight";
            if (string.IsNullOrEmpty(ToastSize)) ToastSize = "Medium";
            if (string.IsNullOrEmpty(CalculationMethodName)) CalculationMethodName = "Tubalani";

            if (Prayers == null) Prayers = new Dictionary<string, PrayerEventConfig>(StringComparer.OrdinalIgnoreCase);

            string[] names = new string[] { "Imsak", "Fajr", "Sunrise", "Dhuhr", "Asr", "Sunset", "Maghrib", "Isha", "Midnight" };
            foreach (var n in names)
            {
                if (!Prayers.ContainsKey(n))
                {
                    bool vis = (n != "Imsak" && n != "Sunset");
                    string mode = (n == "Fajr" || n == "Dhuhr" || n == "Maghrib") ? "Adhan" : "Chime";
                    Prayers[n] = new PrayerEventConfig(vis, 0, mode, 0, (mode == "Adhan" ? "default" : "chime"), "", "chime", "");
                }
                else
                {
                    var cfg = Prayers[n];
                    if (string.IsNullOrEmpty(cfg.ReminderSoundId)) cfg.ReminderSoundId = "chime";
                    if (cfg.CustomReminderAudioPath == null) cfg.CustomReminderAudioPath = "";

                    // Auto-migrate any prayer previously set to "Toast" (which silenced audio) to "Adhan" for obligatory prayers
                    if (string.Equals(cfg.NotificationMode, "Toast", StringComparison.OrdinalIgnoreCase) &&
                        (n == "Fajr" || n == "Dhuhr" || n == "Asr" || n == "Maghrib" || n == "Isha"))
                    {
                        cfg.NotificationMode = "Adhan";
                    }
                }
            }
        }

        public void Save()
        {
            try
            {
                string path = GetConfigFilePath();
                var serializer = new JavaScriptSerializer();
                string json = serializer.Serialize(this);
                File.WriteAllText(path, json);

                ApplyStartupSetting();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error saving settings: " + ex.Message);
            }
        }

        public void ApplyStartupSetting()
        {
            try
            {
                string runKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(runKey, true))
                {
                    if (key != null)
                    {
                        string appPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                        if (AutoStartWithWindows)
                        {
                            key.SetValue("ShiaPrayerTimes", "\"" + appPath + "\" --tray");
                        }
                        else
                        {
                            if (key.GetValue("ShiaPrayerTimes") != null)
                            {
                                key.DeleteValue("ShiaPrayerTimes", false);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Startup registry error: " + ex.Message);
            }
        }

        public CalculationParameters ToCalculationParameters()
        {
            CalculationMethod method = CalculationMethod.Tubalani;
            if (CalculationMethodName == "Jafari") method = CalculationMethod.Jafari;
            else if (CalculationMethodName == "Tehran") method = CalculationMethod.Tehran;
            else if (CalculationMethodName == "Custom") method = CalculationMethod.Custom;

            var p = CalculationParameters.GetPreset(method);
            if (method == CalculationMethod.Custom)
            {
                p.FajrAngle = CustomFajrAngle;
                p.MaghribAngle = CustomMaghribAngle;
                p.IshaAngle = CustomIshaAngle;
            }

            p.Midnight = (MidnightModeName == "Standard") ? MidnightMode.Standard : MidnightMode.Jafari;
            p.ImsakMinutesBeforeFajr = ImsakMinutesBeforeFajr;

            if (HighLatsName == "NightMiddle") p.HighLats = HighLatsMethod.NightMiddle;
            else if (HighLatsName == "OneSeventh") p.HighLats = HighLatsMethod.OneSeventh;
            else if (HighLatsName == "None") p.HighLats = HighLatsMethod.None;
            else p.HighLats = HighLatsMethod.AngleBased;

            return p;
        }

        public Dictionary<string, int> GetOffsets()
        {
            var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in Prayers)
            {
                dict[kvp.Key] = kvp.Value.OffsetMinutes;
            }
            return dict;
        }

        public string ResolveSoundPath(string prayerName, string soundId, string customAudioPath)
        {
            if (!string.IsNullOrEmpty(customAudioPath) && File.Exists(customAudioPath))
            {
                return customAudioPath;
            }

            string targetDir = Path.Combine(GetConfigDirectory(), "Audio");
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            // 1. Check AdhanLibrary for matching soundId
            if (!string.IsNullOrEmpty(soundId) && AdhanLibrary != null)
            {
                foreach (var s in AdhanLibrary)
                {
                    if (string.Equals(s.Id, soundId, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!string.IsNullOrEmpty(s.FilePath) && File.Exists(s.FilePath))
                        {
                            return s.FilePath;
                        }

                        // Try extracting by filename if path lost or machine changed
                        if (!string.IsNullOrEmpty(s.FilePath))
                        {
                            string fname = Path.GetFileName(s.FilePath);
                            string extracted = EnsureExtractedResource(fname);
                            if (!string.IsNullOrEmpty(extracted) && File.Exists(extracted)) return extracted;
                        }
                    }
                }
            }

            // 2. Fallbacks by soundId name
            if (string.Equals(soundId, "chime", StringComparison.OrdinalIgnoreCase))
            {
                string p = EnsureExtractedResource("chime.wav");
                if (File.Exists(p)) return p;
            }
            else if (string.Equals(soundId, "tone", StringComparison.OrdinalIgnoreCase) || string.Equals(soundId, "adhan_tone", StringComparison.OrdinalIgnoreCase))
            {
                string p = EnsureExtractedResource("adhan_tone.wav");
                if (File.Exists(p)) return p;
            }

            // 3. User configured default paths
            if (prayerName.Equals("Fajr", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(DefaultFajrAdhanPath) && File.Exists(DefaultFajrAdhanPath))
            {
                return DefaultFajrAdhanPath;
            }

            if (!string.IsNullOrEmpty(DefaultAdhanPath) && File.Exists(DefaultAdhanPath))
            {
                return DefaultAdhanPath;
            }

            // 4. Default Adhan: Try almusawi_adhan.mp3 first!
            string almusawi = EnsureExtractedResource("almusawi_adhan.mp3");
            if (!string.IsNullOrEmpty(almusawi) && File.Exists(almusawi)) return almusawi;

            // 5. Check if any sound in AdhanLibrary has a valid audio file
            if (AdhanLibrary != null)
            {
                foreach (var s in AdhanLibrary)
                {
                    if (s.Id.Equals("chime", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.IsNullOrEmpty(s.FilePath) && File.Exists(s.FilePath)) return s.FilePath;
                    if (!string.IsNullOrEmpty(s.FilePath))
                    {
                        string ext = EnsureExtractedResource(Path.GetFileName(s.FilePath));
                        if (!string.IsNullOrEmpty(ext) && File.Exists(ext)) return ext;
                    }
                }
            }

            // 6. Tone fallback
            string tone = EnsureExtractedResource("adhan_tone.wav");
            if (!string.IsNullOrEmpty(tone) && File.Exists(tone)) return tone;

            // 7. Last resort: chime.wav
            string chime = EnsureExtractedResource("chime.wav");
            if (!string.IsNullOrEmpty(chime) && File.Exists(chime)) return chime;

            return "";
        }
    }
}
