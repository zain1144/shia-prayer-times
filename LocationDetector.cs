using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Script.Serialization;

namespace ShiaPrayerTimes
{
    public class LocationDetectionResult
    {
        public bool Success { get; set; }
        public string Name { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Timezone { get; set; }
        public string ErrorMessage { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }

    public static class LocationDetector
    {
        public static LocationDetectionResult DetectLocation()
        {
            // 1. Try IP Geolocation
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                var request = (HttpWebRequest)WebRequest.Create("http://ip-api.com/json/?fields=status,country,city,lat,lon,timezone,offset");
                request.Timeout = 4000;
                request.UserAgent = "ShiaPrayerTimes/2.1";

                using (var response = (HttpWebResponse)request.GetResponse())
                using (var stream = response.GetResponseStream())
                using (var reader = new StreamReader(stream))
                {
                    string json = reader.ReadToEnd();
                    var serializer = new JavaScriptSerializer();
                    var dict = serializer.Deserialize<Dictionary<string, object>>(json);

                    if (dict != null && dict.ContainsKey("status") && dict["status"].ToString() == "success")
                    {
                        double lat = Convert.ToDouble(dict["lat"]);
                        double lon = Convert.ToDouble(dict["lon"]);
                        string city = dict.ContainsKey("city") ? dict["city"].ToString() : "";
                        string country = dict.ContainsKey("country") ? dict["country"].ToString() : "";
                        double tz = 3.0;

                        if (dict.ContainsKey("offset"))
                        {
                            tz = Convert.ToDouble(dict["offset"]) / 3600.0;
                        }
                        else
                        {
                            tz = EstimateTimezone(lon);
                        }

                        string name = string.IsNullOrEmpty(city) ? country : (city + ", " + country);

                        return new LocationDetectionResult
                        {
                            Success = true,
                            Name = name,
                            Latitude = Math.Round(lat, 4),
                            Longitude = Math.Round(lon, 4),
                            Timezone = tz
                        };
                    }
                }
            }
            catch { }

            // 2. Fallback to Windows GeoCoordinateWatcher
            try
            {
                using (var watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.Default))
                {
                    watcher.Start();
                    int waited = 0;
                    while (watcher.Status != GeoPositionStatus.Ready && waited < 20)
                    {
                        Thread.Sleep(100);
                        waited++;
                    }

                    if (watcher.Status == GeoPositionStatus.Ready && !watcher.Position.Location.IsUnknown)
                    {
                        var loc = watcher.Position.Location;
                        double tz = EstimateTimezone(loc.Longitude);
                        watcher.Stop();

                        return new LocationDetectionResult
                        {
                            Success = true,
                            Name = string.Format("موقعي الحالي ({0:0.00}°, {1:0.00}°)", loc.Latitude, loc.Longitude),
                            Latitude = Math.Round(loc.Latitude, 4),
                            Longitude = Math.Round(loc.Longitude, 4),
                            Timezone = tz
                        };
                    }
                    watcher.Stop();
                }
            }
            catch { }

            return new LocationDetectionResult
            {
                Success = false,
                ErrorMessage = "تعذر تحديد الموقع تلقائياً. يمكنك البحث باسم المدينة أو لصق الإحداثيات مباشرة."
            };
        }

        // Smart Coordinate Parser for Google Maps links, clipboard text, DMS, etc.
        public static bool TryParseCoordinates(string text, out double lat, out double lon)
        {
            lat = 0;
            lon = 0;
            if (string.IsNullOrEmpty(text)) return false;
            text = text.Trim();

            try
            {
                // Check for @lat,lon in Google Maps URLs (e.g. https://www.google.com/maps/.../@26.5577,50.0081,17z)
                var atMatch = Regex.Match(text, @"@(-?\d+\.\d+),(-?\d+\.\d+)");
                if (atMatch.Success)
                {
                    if (double.TryParse(atMatch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out lat) &&
                        double.TryParse(atMatch.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out lon))
                    {
                        return IsValid(lat, lon);
                    }
                }

                // Check for ?q=lat,lon or &q=lat,lon or ll=lat,lon
                var qMatch = Regex.Match(text, @"[?&](?:q|ll)=(-?\d+\.\d+),(-?\d+\.\d+)");
                if (qMatch.Success)
                {
                    if (double.TryParse(qMatch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out lat) &&
                        double.TryParse(qMatch.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out lon))
                    {
                        return IsValid(lat, lon);
                    }
                }

                // Check for plain "lat, lon" or "lat  lon"
                var numMatch = Regex.Match(text, @"(-?\d{1,2}(?:\.\d+)?)[,\s]+(-?\d{1,3}(?:\.\d+)?)");
                if (numMatch.Success)
                {
                    if (double.TryParse(numMatch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out lat) &&
                        double.TryParse(numMatch.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out lon))
                    {
                        return IsValid(lat, lon);
                    }
                }

                // Check for DMS format: e.g. 26°33'27.7"N 50°00'29.2"E
                var dmsMatch = Regex.Match(text, @"(\d+)[°\s]+(\d+)['\s]+([\d.]+)?[""\s]*([NSns])[,\s]+(\d+)[°\s]+(\d+)['\s]+([\d.]+)?[""\s]*([EWew])");
                if (dmsMatch.Success)
                {
                    double d1 = double.Parse(dmsMatch.Groups[1].Value);
                    double m1 = double.Parse(dmsMatch.Groups[2].Value);
                    double s1 = string.IsNullOrEmpty(dmsMatch.Groups[3].Value) ? 0 : double.Parse(dmsMatch.Groups[3].Value, CultureInfo.InvariantCulture);
                    lat = d1 + (m1 / 60.0) + (s1 / 3600.0);
                    if (dmsMatch.Groups[4].Value.ToUpper() == "S") lat = -lat;

                    double d2 = double.Parse(dmsMatch.Groups[5].Value);
                    double m2 = double.Parse(dmsMatch.Groups[6].Value);
                    double s2 = string.IsNullOrEmpty(dmsMatch.Groups[7].Value) ? 0 : double.Parse(dmsMatch.Groups[7].Value, CultureInfo.InvariantCulture);
                    lon = d2 + (m2 / 60.0) + (s2 / 3600.0);
                    if (dmsMatch.Groups[8].Value.ToUpper() == "W") lon = -lon;

                    return IsValid(lat, lon);
                }
            }
            catch { }

            return false;
        }

        private static bool IsValid(double lat, double lon)
        {
            return (lat >= -90.0 && lat <= 90.0 && lon >= -180.0 && lon <= 180.0);
        }

        // Online Geocoding by name via OpenStreetMap Nominatim (No Windows Location Services, No GPS required)
        public static List<LocationDetectionResult> SearchLocationByName(string query)
        {
            var results = new List<LocationDetectionResult>();
            if (string.IsNullOrEmpty(query) || query.Trim().Length < 2) return results;

            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                string url = "https://nominatim.openstreetmap.org/search?q=" + Uri.EscapeDataString(query.Trim()) + "&format=json&limit=6&accept-language=ar,en";
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Timeout = 6000;
                request.UserAgent = "ShiaPrayerTimesApp/2.1 (desktop)";

                using (var response = (HttpWebResponse)request.GetResponse())
                using (var stream = response.GetResponseStream())
                using (var reader = new StreamReader(stream))
                {
                    string json = reader.ReadToEnd();
                    var serializer = new JavaScriptSerializer();
                    var items = serializer.Deserialize<object[]>(json);

                    if (items != null)
                    {
                        foreach (var item in items)
                        {
                            var dict = item as Dictionary<string, object>;
                            if (dict != null && dict.ContainsKey("lat") && dict.ContainsKey("lon"))
                            {
                                double lat = Convert.ToDouble(dict["lat"], CultureInfo.InvariantCulture);
                                double lon = Convert.ToDouble(dict["lon"], CultureInfo.InvariantCulture);
                                string dispName = dict.ContainsKey("display_name") ? dict["display_name"].ToString() : "";

                                var parts = dispName.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                string shortName = dispName;
                                if (parts.Length >= 2)
                                {
                                    shortName = parts[0].Trim();
                                    if (parts.Length > 2)
                                    {
                                        shortName += " (" + parts[1].Trim() + "، " + parts[parts.Length - 1].Trim() + ")";
                                    }
                                    else
                                    {
                                        shortName += " (" + parts[1].Trim() + ")";
                                    }
                                }

                                double tz = EstimateTimezone(lon);

                                results.Add(new LocationDetectionResult
                                {
                                    Success = true,
                                    Name = shortName,
                                    Latitude = Math.Round(lat, 4),
                                    Longitude = Math.Round(lon, 4),
                                    Timezone = tz
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Geocoding search error: " + ex.Message);
            }

            return results;
        }

        public static double EstimateTimezone(double lon)
        {
            // Gulf & Middle East specific bounds
            if (lon >= 34.0 && lon <= 55.0) return 3.0; // Saudi Arabia, Iraq, Bahrain, Kuwait, Qatar, Yemen
            if (lon > 55.0 && lon <= 62.0) return 3.5;  // Iran
            if (lon > 62.0 && lon <= 70.0) return 4.0;  // UAE, Oman
            if (lon >= 25.0 && lon < 34.0) return 2.0;  // Lebanon, Syria, Egypt, Jordan

            // General UTC hour approximation
            return Math.Round(lon / 15.0);
        }
    }
}
