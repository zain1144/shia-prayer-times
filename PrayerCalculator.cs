using System;
using System.Collections.Generic;

namespace ShiaPrayerTimes
{
    public enum CalculationMethod
    {
        Jafari,   // Leva Institute, Qom (Fajr: 16°, Maghrib: 4°, Isha: 14°)
        Tehran,   // University of Tehran (Fajr: 17.7°, Maghrib: 4.5°, Isha: 14°)
        Tubalani, // تقويم السيد هاشم التوبلاني (Fajr: 18°, Maghrib: 4°, Isha: 14°)
        Custom    // User-defined angles
    }

    public enum MidnightMode
    {
        Jafari,   // Midpoint between Sunset and Fajr
        Standard  // Midpoint between Sunset and Sunrise
    }

    public enum HighLatsMethod
    {
        None,
        NightMiddle,
        OneSeventh,
        AngleBased
    }

    public class CalculationParameters
    {
        public CalculationMethod Method = CalculationMethod.Tubalani;
        public double FajrAngle = 18.0;
        public double MaghribAngle = 4.0;
        public double IshaAngle = 14.0;
        public MidnightMode Midnight = MidnightMode.Jafari;
        public HighLatsMethod HighLats = HighLatsMethod.AngleBased;
        public double AsrFactor = 1.0; // Standard / Jafari jurisprudence = 1
        public int ImsakMinutesBeforeFajr = 10;
        public double DhuhrMinutesAfterNoon = 0.0;

        public static CalculationParameters GetPreset(CalculationMethod method)
        {
            var p = new CalculationParameters { Method = method };
            if (method == CalculationMethod.Jafari)
            {
                p.FajrAngle = 16.0;
                p.MaghribAngle = 4.0;
                p.IshaAngle = 14.0;
                p.Midnight = MidnightMode.Jafari;
            }
            else if (method == CalculationMethod.Tehran)
            {
                p.FajrAngle = 17.7;
                p.MaghribAngle = 4.5;
                p.IshaAngle = 14.0;
                p.Midnight = MidnightMode.Jafari;
            }
            else if (method == CalculationMethod.Tubalani)
            {
                p.FajrAngle = 18.0;
                p.MaghribAngle = 4.0;
                p.IshaAngle = 14.0;
                p.Midnight = MidnightMode.Jafari;
            }
            return p;
        }
    }

    public class PrayerTimesResult
    {
        public DateTime Date { get; set; }
        public DateTime Imsak { get; set; }
        public DateTime Fajr { get; set; }
        public DateTime Sunrise { get; set; }
        public DateTime Dhuhr { get; set; }
        public DateTime Asr { get; set; }
        public DateTime Sunset { get; set; }
        public DateTime Maghrib { get; set; }
        public DateTime Isha { get; set; }
        public DateTime Midnight { get; set; }

        public Dictionary<string, DateTime> ToDictionary()
        {
            return new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase)
            {
                { "Imsak", Imsak },
                { "Fajr", Fajr },
                { "Sunrise", Sunrise },
                { "Dhuhr", Dhuhr },
                { "Asr", Asr },
                { "Sunset", Sunset },
                { "Maghrib", Maghrib },
                { "Isha", Isha },
                { "Midnight", Midnight }
            };
        }
    }

    public static class PrayerCalculator
    {
        public static PrayerTimesResult Calculate(
            DateTime date,
            double latitude,
            double longitude,
            double timeZone,
            double elevationMeters,
            CalculationParameters parameters,
            Dictionary<string, int> prayerOffsets = null)
        {
            double jDate = Julian(date.Year, date.Month, date.Day) - longitude / 360.0;

            // Default times for dayPortions
            var times = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                { "imsak", 5.0 / 24.0 },
                { "fajr", 5.0 / 24.0 },
                { "sunrise", 6.0 / 24.0 },
                { "dhuhr", 12.0 / 24.0 },
                { "asr", 13.0 / 24.0 },
                { "sunset", 18.0 / 24.0 },
                { "maghrib", 18.0 / 24.0 },
                { "isha", 18.0 / 24.0 }
            };

            // 2 iterations refinement to ensure second-level astronomical accuracy across equinoxes
            for (int iter = 0; iter < 2; iter++)
            {
                times = ComputePrayerTimes(times, jDate, latitude, elevationMeters, parameters);
                if (iter == 0)
                {
                    var dayPortions = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                    foreach (var k in times.Keys)
                    {
                        dayPortions[k] = times[k] / 24.0;
                    }
                    times = dayPortions;
                }
            }

            // Adjust times with timezone and longitude
            var keys = new List<string>(times.Keys);
            foreach (var k in keys)
            {
                times[k] += timeZone - longitude / 15.0;
            }

            // High latitudes adjustment
            if (parameters.HighLats != HighLatsMethod.None)
            {
                times = AdjustHighLats(times, parameters);
            }

            // Imsak is minutes before Fajr
            times["imsak"] = times["fajr"] - (parameters.ImsakMinutesBeforeFajr / 60.0);

            // Dhuhr minute offset
            times["dhuhr"] += parameters.DhuhrMinutesAfterNoon / 60.0;

            // Compute midnight
            if (parameters.Midnight == MidnightMode.Jafari)
            {
                times["midnight"] = times["sunset"] + TimeDiff(times["sunset"], times["fajr"] + 24.0) / 2.0;
            }
            else
            {
                times["midnight"] = times["sunset"] + TimeDiff(times["sunset"], times["sunrise"] + 24.0) / 2.0;
            }

            // Apply fine-tuning offsets
            if (prayerOffsets != null)
            {
                foreach (var kvp in prayerOffsets)
                {
                    string k = kvp.Key.ToLowerInvariant();
                    if (times.ContainsKey(k))
                    {
                        times[k] += kvp.Value / 60.0;
                    }
                }
            }

            // Convert to DateTime objects
            var result = new PrayerTimesResult
            {
                Date = date.Date,
                Imsak = HoursToDateTime(date, times["imsak"]),
                Fajr = HoursToDateTime(date, times["fajr"]),
                Sunrise = HoursToDateTime(date, times["sunrise"]),
                Dhuhr = HoursToDateTime(date, times["dhuhr"]),
                Asr = HoursToDateTime(date, times["asr"]),
                Sunset = HoursToDateTime(date, times["sunset"]),
                Maghrib = HoursToDateTime(date, times["maghrib"]),
                Isha = HoursToDateTime(date, times["isha"]),
                Midnight = HoursToDateTime(date, times["midnight"])
            };

            return result;
        }

        private static Dictionary<string, double> ComputePrayerTimes(
            Dictionary<string, double> times,
            double jDate,
            double lat,
            double elevation,
            CalculationParameters p)
        {
            double riseSetAngle = RiseSetAngle(elevation);

            return new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                { "imsak", SunAngleTime(p.FajrAngle, times["imsak"], jDate, lat, true) },
                { "fajr", SunAngleTime(p.FajrAngle, times["fajr"], jDate, lat, true) },
                { "sunrise", SunAngleTime(riseSetAngle, times["sunrise"], jDate, lat, true) },
                { "dhuhr", MidDay(times["dhuhr"], jDate) },
                { "asr", AsrTime(p.AsrFactor, times["asr"], jDate, lat) },
                { "sunset", SunAngleTime(riseSetAngle, times["sunset"], jDate, lat, false) },
                { "maghrib", SunAngleTime(p.MaghribAngle, times["maghrib"], jDate, lat, false) },
                { "isha", SunAngleTime(p.IshaAngle, times["isha"], jDate, lat, false) }
            };
        }

        private static double MidDay(double timePortion, double jDate)
        {
            SunPosition pos = GetSunPosition(jDate + timePortion);
            return FixHour(12.0 - pos.Equation);
        }

        private static double SunAngleTime(double angle, double timePortion, double jDate, double lat, bool ccw)
        {
            SunPosition pos = GetSunPosition(jDate + timePortion);
            double noon = MidDay(timePortion, jDate);
            double cosH = (-DMathSin(angle) - DMathSin(pos.Declination) * DMathSin(lat)) /
                          (DMathCos(pos.Declination) * DMathCos(lat));

            if (cosH > 1.0) cosH = 1.0;
            if (cosH < -1.0) cosH = -1.0;

            double t = (1.0 / 15.0) * DMathArcCos(cosH);
            return noon + (ccw ? -t : t);
        }

        private static double AsrTime(double factor, double timePortion, double jDate, double lat)
        {
            SunPosition pos = GetSunPosition(jDate + timePortion);
            double angle = -DMathArcCot(factor + DMathTan(Math.Abs(lat - pos.Declination)));
            return SunAngleTime(angle, timePortion, jDate, lat, false);
        }

        private static double RiseSetAngle(double elevation)
        {
            return 0.833 + 0.0347 * Math.Sqrt(Math.Max(0, elevation));
        }

        private static Dictionary<string, double> AdjustHighLats(
            Dictionary<string, double> times,
            CalculationParameters p)
        {
            double nightTime = TimeDiff(times["sunset"], times["sunrise"]);
            times["imsak"] = AdjustHLTime(times["imsak"], times["sunrise"], p.FajrAngle, nightTime, true, p.HighLats);
            times["fajr"] = AdjustHLTime(times["fajr"], times["sunrise"], p.FajrAngle, nightTime, true, p.HighLats);
            times["isha"] = AdjustHLTime(times["isha"], times["sunset"], p.IshaAngle, nightTime, false, p.HighLats);
            times["maghrib"] = AdjustHLTime(times["maghrib"], times["sunset"], p.MaghribAngle, nightTime, false, p.HighLats);
            return times;
        }

        private static double AdjustHLTime(double time, double b, double angle, double night, bool ccw, HighLatsMethod method)
        {
            double portion = NightPortion(angle, night, method);
            double diff = ccw ? TimeDiff(time, b) : TimeDiff(b, time);
            if (double.IsNaN(time) || diff > portion)
            {
                time = b + (ccw ? -portion : portion);
            }
            return time;
        }

        private static double NightPortion(double angle, double night, HighLatsMethod method)
        {
            double portion = 0.5; // NightMiddle
            if (method == HighLatsMethod.AngleBased) portion = (1.0 / 60.0) * angle;
            else if (method == HighLatsMethod.OneSeventh) portion = 1.0 / 7.0;
            return portion * night;
        }

        public struct SunPosition
        {
            public double Declination;
            public double Equation;
        }

        public static SunPosition GetSunPosition(double jd)
        {
            double D = jd - 2451545.0;
            double g = FixAngle(357.529 + 0.98560028 * D);
            double q = FixAngle(280.459 + 0.98564736 * D);
            double L = FixAngle(q + 1.915 * DMathSin(g) + 0.020 * DMathSin(2.0 * g));
            double e = 23.439 - 0.00000036 * D;
            double RA = DMathArcTan2(DMathCos(e) * DMathSin(L), DMathCos(L)) / 15.0;
            RA = FixHour(RA);

            SunPosition sp;
            sp.Declination = DMathArcSin(DMathSin(e) * DMathSin(L));
            sp.Equation = q / 15.0 - RA;
            return sp;
        }

        public static double Julian(int year, int month, int day)
        {
            if (month <= 2)
            {
                year -= 1;
                month += 12;
            }
            double A = Math.Floor(year / 100.0);
            double B = 2.0 - A + Math.Floor(A / 4.0);
            return Math.Floor(365.25 * (year + 4716.0)) + Math.Floor(30.6001 * (month + 1.0)) + day + B - 1524.5;
        }

        public static DateTime HoursToDateTime(DateTime baseDate, double hourFloat)
        {
            // If hourFloat is near or past 24.0 (accounting for rounding to nearest minute),
            // advance baseDate by 1 day so that midnight occurring after 12:00 AM has the correct next-day date.
            if (hourFloat + 0.5 / 60.0 >= 24.0)
            {
                baseDate = baseDate.AddDays(1);
            }

            // add 0.5 / 60 minute for nearest minute rounding
            hourFloat = FixHour(hourFloat + 0.5 / 60.0);
            int hours = (int)Math.Floor(hourFloat);
            int minutes = (int)Math.Floor((hourFloat - hours) * 60.0);
            return new DateTime(baseDate.Year, baseDate.Month, baseDate.Day, hours % 24, minutes % 60, 0);
        }

        public static bool IsActualPrayer(string eventKey)
        {
            if (string.IsNullOrEmpty(eventKey)) return false;
            string k = eventKey.Trim().ToLowerInvariant();
            return k == "fajr" || k == "dhuhr" || k == "asr" || k == "maghrib" || k == "isha";
        }

        public static double TimeDiff(double time1, double time2)
        {
            return FixHour(time2 - time1);
        }

        public static double DMathDTR(double d) { return (d * Math.PI) / 180.0; }
        public static double DMathRTD(double r) { return (r * 180.0) / Math.PI; }
        public static double DMathSin(double d) { return Math.Sin(DMathDTR(d)); }
        public static double DMathCos(double d) { return Math.Cos(DMathDTR(d)); }
        public static double DMathTan(double d) { return Math.Tan(DMathDTR(d)); }
        public static double DMathArcSin(double d) { return DMathRTD(Math.Asin(d)); }
        public static double DMathArcCos(double d) { return DMathRTD(Math.Acos(d)); }
        public static double DMathArcTan(double d) { return DMathRTD(Math.Atan(d)); }
        public static double DMathArcCot(double x) { return DMathRTD(Math.Atan(1.0 / x)); }
        public static double DMathArcTan2(double y, double x) { return DMathRTD(Math.Atan2(y, x)); }
        public static double FixAngle(double a) { return Fix(a, 360.0); }
        public static double FixHour(double a) { return Fix(a, 24.0); }
        public static double Fix(double a, double b)
        {
            a = a - b * Math.Floor(a / b);
            return a < 0 ? a + b : a;
        }
    }
}
