using System;
using System.Device.Location;
using System.IO;
using System.Net;
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
                request.UserAgent = "ShiaPrayerTimes/2.0";

                using (var response = (HttpWebResponse)request.GetResponse())
                using (var stream = response.GetResponseStream())
                using (var reader = new StreamReader(stream))
                {
                    string json = reader.ReadToEnd();
                    var serializer = new JavaScriptSerializer();
                    var dict = serializer.Deserialize<System.Collections.Generic.Dictionary<string, object>>(json);

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
                            tz = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).TotalHours;
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

            // 2. Try Windows GeoCoordinateWatcher
            try
            {
                using (var watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.Default))
                {
                    watcher.Start();
                    int waited = 0;
                    while (watcher.Status != GeoPositionStatus.Ready && waited < 30)
                    {
                        Thread.Sleep(100);
                        waited++;
                    }

                    if (watcher.Status == GeoPositionStatus.Ready && !watcher.Position.Location.IsUnknown)
                    {
                        var loc = watcher.Position.Location;
                        double tz = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).TotalHours;
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
                ErrorMessage = "تعذر تحديد الموقع تلقائياً. يرجى التأكد من اتصال الإنترنت أو إدخال الإحداثيات يدوياً."
            };
        }
    }
}
