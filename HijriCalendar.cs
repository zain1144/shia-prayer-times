using System;
using System.Globalization;

namespace ShiaPrayerTimes
{
    public class HijriDate
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int Day { get; set; }
        public string MonthNameEn { get; set; }
        public string MonthNameAr { get; set; }
        public string DayNameEn { get; set; }
        public string DayNameAr { get; set; }

        public string Format(string language)
        {
            if (language == "ar")
            {
                // Example: ٨ جمادى الآخرة ١٤٤٨ هـ
                return string.Format("{0} {1} {2} هـ", ToArabicNumbers(Day), MonthNameAr, ToArabicNumbers(Year));
            }
            // Example: 8 Jumada al-Akhirah 1448 AH
            return string.Format("{0} {1} {2} AH", Day, MonthNameEn, Year);
        }

        private static string ToArabicNumbers(int number)
        {
            string s = number.ToString();
            char[] arabicDigits = new char[] { '٠', '١', '٢', '٣', '٤', '٥', '٦', '٧', '٨', '٩' };
            var sb = new System.Text.StringBuilder();
            foreach (char c in s)
            {
                if (c >= '0' && c <= '9')
                    sb.Append(arabicDigits[c - '0']);
                else
                    sb.Append(c);
            }
            return sb.ToString();
        }
    }

    public static class HijriCalendarHelper
    {
        private static readonly UmAlQuraCalendar UaqCal = new UmAlQuraCalendar();

        private static readonly string[] MonthsAr = new string[]
        {
            "المحرّم", "صفر", "ربيع الأوّل", "ربيع الثاني",
            "جمادى الأولى", "جمادى الآخرة", "رجب", "شعبان",
            "شهر رمضان", "شوّال", "ذو القعدة", "ذو الحجّة"
        };

        private static readonly string[] MonthsEn = new string[]
        {
            "Muharram", "Safar", "Rabi' al-Awwal", "Rabi' al-Thani",
            "Jumada al-Ula", "Jumada al-Akhirah", "Rajab", "Sha'ban",
            "Ramadan", "Shawwal", "Dhu al-Qi'dah", "Dhu al-Hijjah"
        };

        private static readonly string[] DaysAr = new string[]
        {
            "الأحد", "الإثنين", "الثلاثاء", "الأربعاء", "الخميس", "الجمعة", "السبت"
        };

        private static readonly string[] DaysEn = new string[]
        {
            "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"
        };

        public static HijriDate GetHijriDate(DateTime gregorianDate, int offsetDays)
        {
            DateTime targetDate = gregorianDate.AddDays(offsetDays);
            int year = UaqCal.GetYear(targetDate);
            int month = UaqCal.GetMonth(targetDate);
            int day = UaqCal.GetDayOfMonth(targetDate);
            int dayOfWeek = (int)gregorianDate.DayOfWeek;

            return new HijriDate
            {
                Year = year,
                Month = month,
                Day = day,
                MonthNameAr = MonthsAr[Math.Max(0, Math.Min(11, month - 1))],
                MonthNameEn = MonthsEn[Math.Max(0, Math.Min(11, month - 1))],
                DayNameAr = DaysAr[dayOfWeek],
                DayNameEn = DaysEn[dayOfWeek]
            };
        }
    }
}
