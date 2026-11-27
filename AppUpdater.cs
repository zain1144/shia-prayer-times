using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace ShiaPrayerTimes
{
    public class ReleaseUpdateInfo
    {
        public string TagName { get; set; }
        public Version Version { get; set; }
        public string Title { get; set; }
        public string Notes { get; set; }
        public string HtmlUrl { get; set; }
        public string DownloadUrl { get; set; }
        public long ExpectedSize { get; set; }
    }

    public static class AppUpdater
    {
        private const string ReleasesLatestApi = "https://api.github.com/repos/zain1144/shia-prayer-times/releases/latest";
        private const string ReleasesWebUrl = "https://github.com/zain1144/shia-prayer-times/releases";
        private static bool _isChecking = false;

        public static Version GetCurrentVersion()
        {
            try
            {
                var v = Assembly.GetExecutingAssembly().GetName().Version;
                if (v != null)
                {
                    return new Version(v.Major, v.Minor, Math.Max(0, v.Build));
                }
            }
            catch { }
            return new Version(1, 2, 0);
        }

        public static string GetCurrentVersionString()
        {
            Version v = GetCurrentVersion();
            return string.Format("v{0}.{1}.{2}", v.Major, v.Minor, v.Build);
        }

        private static Version ParseTagVersion(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return null;
            string clean = tag.Trim();
            if (clean.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            {
                clean = clean.Substring(1);
            }
            int dashIdx = clean.IndexOf('-');
            if (dashIdx > 0) clean = clean.Substring(0, dashIdx);

            Version parsed;
            if (Version.TryParse(clean, out parsed))
            {
                return new Version(parsed.Major, Math.Max(0, parsed.Minor), Math.Max(0, parsed.Build));
            }
            return null;
        }

        public static void CheckForUpdatesAsync(AppSettings settings, bool isManual, Window ownerWindow)
        {
            if (_isChecking) return;
            if (settings == null) return;

            if (!isManual)
            {
                if (!settings.AutoCheckUpdates) return;
                DateTime lastCheck;
                if (!string.IsNullOrEmpty(settings.LastUpdateCheckUtc) &&
                    DateTime.TryParse(settings.LastUpdateCheckUtc, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out lastCheck))
                {
                    if ((DateTime.UtcNow - lastCheck).TotalHours < 20)
                    {
                        return;
                    }
                }
            }

            _isChecking = true;
            Dispatcher uiDispatcher = Application.Current != null ? Application.Current.Dispatcher : Dispatcher.CurrentDispatcher;

            var bgThread = new Thread(() =>
            {
                ReleaseUpdateInfo update = null;
                string errorMsg = null;

                try
                {
                    // Ensure TLS 1.2 (3072) is enabled on .NET 4.x
                    try
                    {
                        ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072 | (SecurityProtocolType)768 | SecurityProtocolType.Tls;
                    }
                    catch { }

                    var req = (HttpWebRequest)WebRequest.Create(ReleasesLatestApi);
                    req.Method = "GET";
                    req.UserAgent = "ShiaPrayerTimes-Updater";
                    req.Accept = "application/vnd.github.v3+json";
                    req.Timeout = 10000;
                    req.ReadWriteTimeout = 10000;

                    using (var resp = (HttpWebResponse)req.GetResponse())
                    using (var stream = resp.GetResponseStream())
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        string json = reader.ReadToEnd();
                        var ser = new JavaScriptSerializer();
                        var dict = ser.DeserializeObject(json) as Dictionary<string, object>;
                        if (dict != null)
                        {
                            string tag = dict.ContainsKey("tag_name") ? Convert.ToString(dict["tag_name"]) : "";
                            Version remoteVer = ParseTagVersion(tag);
                            Version currentVer = GetCurrentVersion();

                            if (remoteVer != null && remoteVer > currentVer)
                            {
                                string dlUrl = "";
                                long dlSize = 0;

                                if (dict.ContainsKey("assets") && dict["assets"] is object[])
                                {
                                    foreach (var item in (object[])dict["assets"])
                                    {
                                        var asset = item as Dictionary<string, object>;
                                        if (asset == null) continue;
                                        string aName = asset.ContainsKey("name") ? Convert.ToString(asset["name"]) : "";
                                        if (aName.Equals("ShiaPrayerTimes.exe", StringComparison.OrdinalIgnoreCase))
                                        {
                                            dlUrl = asset.ContainsKey("browser_download_url") ? Convert.ToString(asset["browser_download_url"]) : "";
                                            if (asset.ContainsKey("size"))
                                            {
                                                long.TryParse(Convert.ToString(asset["size"]), out dlSize);
                                            }
                                            break;
                                        }
                                    }
                                }

                                update = new ReleaseUpdateInfo
                                {
                                    TagName = tag,
                                    Version = remoteVer,
                                    Title = dict.ContainsKey("name") ? Convert.ToString(dict["name"]) : tag,
                                    Notes = dict.ContainsKey("body") ? Convert.ToString(dict["body"]) : "",
                                    HtmlUrl = dict.ContainsKey("html_url") ? Convert.ToString(dict["html_url"]) : ReleasesWebUrl,
                                    DownloadUrl = dlUrl,
                                    ExpectedSize = dlSize
                                };
                            }
                        }
                    }

                    settings.LastUpdateCheckUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
                    settings.Save();
                }
                catch (Exception ex)
                {
                    errorMsg = ex.Message;
                }
                finally
                {
                    _isChecking = false;
                }

                if (uiDispatcher != null)
                {
                    uiDispatcher.BeginInvoke((Action)(() =>
                    {
                        bool isAr = settings.Language == "ar";
                        if (update != null)
                        {
                            ShowUpdateDialog(settings, update, ownerWindow);
                        }
                        else if (isManual)
                        {
                            if (!string.IsNullOrEmpty(errorMsg))
                            {
                                MessageBox.Show(
                                    isAr
                                        ? "تعذر الاتصال بخادم التحديثات (GitHub).\nتأكد من اتصالك بالإنترنت وحاول مجدداً.\n\n" + errorMsg
                                        : "Could not check for updates.\nPlease check your internet connection and try again.\n\n" + errorMsg,
                                    isAr ? "التحقق من التحديثات" : "Check for Updates",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                            }
                            else
                            {
                                MessageBox.Show(
                                    isAr
                                        ? string.Format("أنت تستخدم أحدث إصدار بالفعل ({0}).", GetCurrentVersionString())
                                        : string.Format("You are already using the latest version ({0}).", GetCurrentVersionString()),
                                    isAr ? "التحقق من التحديثات" : "Check for Updates",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Information);
                            }
                        }
                    }));
                }
            })
            {
                IsBackground = true,
                Name = "UpdateCheckThread"
            };
            bgThread.Start();
        }

        private static bool CanWriteToDirectory(string dirPath)
        {
            try
            {
                if (string.IsNullOrEmpty(dirPath) || !Directory.Exists(dirPath)) return false;
                string probe = Path.Combine(dirPath, ".shia_upd_probe_" + Guid.NewGuid().ToString("N") + ".tmp");
                File.WriteAllText(probe, "probe");
                File.Delete(probe);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void ShowUpdateDialog(AppSettings settings, ReleaseUpdateInfo update, Window owner)
        {
            bool isAr = settings.Language == "ar";
            bool isDark = settings.Theme == "Dark";

            string currentExePath = "";
            try { currentExePath = Assembly.GetExecutingAssembly().Location; } catch { }
            if (string.IsNullOrEmpty(currentExePath))
            {
                currentExePath = Process.GetCurrentProcess().MainModule.FileName;
            }
            string targetDir = Path.GetDirectoryName(currentExePath);
            bool needsUac = !CanWriteToDirectory(targetDir);

            var dlg = new Window
            {
                Title = isAr ? "يتوفر تحديث جديد" : "Update Available",
                Width = 540,
                Height = 460,
                MinWidth = 440,
                MinHeight = 360,
                WindowStartupLocation = owner != null && owner.IsVisible ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen,
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                FlowDirection = isAr ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
                Background = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkBg : UIThemeHelper.LightBg),
                Foreground = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkTextPrimary : UIThemeHelper.LightTextPrimary)
            };

            if (owner != null && owner.IsVisible)
            {
                dlg.Owner = owner;
            }

            var border = new Border
            {
                BorderBrush = UIThemeHelper.GetBrush(UIThemeHelper.AccentEmerald),
                BorderThickness = new Thickness(1.5),
                Background = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkBg : UIThemeHelper.LightBg)
            };

            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var titleBar = UIThemeHelper.CreateIntegratedTitleBar(
                dlg,
                isAr ? "تحديث تطبيق أوقات الصلاة" : "Shia Prayer Times Update",
                "🔄",
                isDark, isAr, null, false);
            Grid.SetRow(titleBar, 0);
            root.Children.Add(titleBar);

            var content = new StackPanel { Margin = new Thickness(20, 14, 20, 10) };

            content.Children.Add(new TextBlock
            {
                Text = isAr
                    ? string.Format("🚀 يتوفر إصدار جديد: {0} (الحالي: {1})", update.TagName, GetCurrentVersionString())
                    : string.Format("🚀 New version available: {0} (Current: {1})", update.TagName, GetCurrentVersionString()),
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = UIThemeHelper.GetBrush(UIThemeHelper.AccentGold),
                Margin = new Thickness(0, 0, 0, 10)
            });

            content.Children.Add(new TextBlock
            {
                Text = isAr ? "سجل التغييرات:" : "What's Changed:",
                FontSize = 12.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkTextSecondary : UIThemeHelper.LightTextSecondary),
                Margin = new Thickness(0, 0, 0, 6)
            });

            var notesBox = new TextBox
            {
                Text = string.IsNullOrWhiteSpace(update.Notes) ? (update.Title ?? update.TagName) : update.Notes,
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Height = 205,
                Padding = new Thickness(10),
                FontSize = 12,
                Background = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkCardBg : UIThemeHelper.LightCardBg),
                Foreground = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkTextPrimary : UIThemeHelper.LightTextPrimary),
                BorderBrush = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkBorder : UIThemeHelper.LightBorder),
                BorderThickness = new Thickness(1)
            };
            content.Children.Add(notesBox);

            if (needsUac)
            {
                content.Children.Add(new TextBlock
                {
                    Text = isAr
                        ? string.Format("🛡️ البرنامج موجود داخل ({0}) — سيطلب ويندوز موافقة المسؤول عند التحديث.", targetDir)
                        : string.Format("🛡️ App is in ({0}) — Windows UAC permission will be requested to replace the EXE.", targetDir),
                    FontSize = 11.5,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = UIThemeHelper.GetBrush(UIThemeHelper.AccentGold),
                    Margin = new Thickness(0, 8, 0, 0)
                });
            }

            var progressPanel = new StackPanel { Visibility = Visibility.Collapsed, Margin = new Thickness(0, 10, 0, 0) };
            var statusText = new TextBlock
            {
                Text = isAr ? "جاري تحميل التحديث..." : "Downloading update...",
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 4)
            };
            var progressBar = new ProgressBar
            {
                Height = 10,
                Minimum = 0,
                Maximum = 100,
                Foreground = UIThemeHelper.GetBrush(UIThemeHelper.AccentEmerald),
                Background = UIThemeHelper.GetBrush(isDark ? Color.FromRgb(51, 65, 85) : Color.FromRgb(203, 213, 225))
            };
            progressPanel.Children.Add(statusText);
            progressPanel.Children.Add(progressBar);
            content.Children.Add(progressPanel);

            Grid.SetRow(content, 1);
            root.Children.Add(content);

            var btnRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(20, 6, 20, 16)
            };

            var laterBtn = new Button
            {
                Content = isAr ? "لاحقاً" : "Later",
                Width = 95,
                Height = 34,
                Margin = new Thickness(8, 0, 0, 0),
                Background = UIThemeHelper.GetBrush(isDark ? Color.FromRgb(51, 65, 85) : Color.FromRgb(203, 213, 225)),
                Foreground = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkTextPrimary : UIThemeHelper.LightTextPrimary),
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            laterBtn.Click += (s, e) => dlg.Close();

            var updateBtn = new Button
            {
                Content = isAr ? "تحديث الآن ⬇️" : "Update Now ⬇️",
                Width = 140,
                Height = 34,
                Margin = new Thickness(8, 0, 0, 0),
                Background = UIThemeHelper.GetBrush(UIThemeHelper.AccentEmerald),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            updateBtn.Click += (s, e) =>
            {
                if (string.IsNullOrEmpty(update.DownloadUrl))
                {
                    try { Process.Start(update.HtmlUrl ?? ReleasesWebUrl); } catch { }
                    dlg.Close();
                    return;
                }

                updateBtn.IsEnabled = false;
                laterBtn.IsEnabled = false;
                progressPanel.Visibility = Visibility.Visible;

                string tempExe = Path.Combine(Path.GetTempPath(), "ShiaPrayerTimes_update_" + update.Version + ".exe");
                try { if (File.Exists(tempExe)) File.Delete(tempExe); } catch { }

                try
                {
                    ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072 | (SecurityProtocolType)768 | SecurityProtocolType.Tls;
                }
                catch { }

                var wc = new WebClient();
                wc.Headers.Add("User-Agent", "ShiaPrayerTimes-Updater");
                wc.DownloadProgressChanged += (ws, we) =>
                {
                    progressBar.Value = we.ProgressPercentage;
                    double mbDone = we.BytesReceived / (1024.0 * 1024.0);
                    double mbTotal = we.TotalBytesToReceive > 0 ? (we.TotalBytesToReceive / (1024.0 * 1024.0)) : 0;
                    statusText.Text = mbTotal > 0
                        ? string.Format(isAr ? "جاري التحميل... {0:0.0} / {1:0.0} MB ({2}%)" : "Downloading... {0:0.0} / {1:0.0} MB ({2}%)", mbDone, mbTotal, we.ProgressPercentage)
                        : string.Format(isAr ? "جاري التحميل... {0:0.0} MB" : "Downloading... {0:0.0} MB", mbDone);
                };
                wc.DownloadFileCompleted += (ws, we) =>
                {
                    wc.Dispose();
                    if (we.Error != null)
                    {
                        updateBtn.IsEnabled = true;
                        laterBtn.IsEnabled = true;
                        progressPanel.Visibility = Visibility.Collapsed;
                        MessageBox.Show(
                            (isAr ? "فشل تحميل التحديث:\n" : "Failed to download update:\n") + we.Error.Message,
                            isAr ? "خطأ في التحديث" : "Update Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        return;
                    }

                    try
                    {
                        var fi = new FileInfo(tempExe);
                        if (!fi.Exists || fi.Length < 500000 || (update.ExpectedSize > 0 && fi.Length != update.ExpectedSize))
                        {
                            throw new Exception(isAr ? "الملف المحمّل غير مكتمل أو تالف." : "Downloaded file appears incomplete or corrupted.");
                        }

                        LaunchSwapAndRestart(tempExe, currentExePath, needsUac, isAr, dlg);
                    }
                    catch (Exception ex)
                    {
                        updateBtn.IsEnabled = true;
                        laterBtn.IsEnabled = true;
                        progressPanel.Visibility = Visibility.Collapsed;
                        MessageBox.Show(ex.Message, isAr ? "خطأ في التحديث" : "Update Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                };

                wc.DownloadFileAsync(new Uri(update.DownloadUrl), tempExe);
            };

            btnRow.Children.Add(laterBtn);
            btnRow.Children.Add(updateBtn);
            Grid.SetRow(btnRow, 2);
            root.Children.Add(btnRow);

            border.Child = root;
            dlg.Content = border;
            dlg.ShowDialog();
        }

        private static void LaunchSwapAndRestart(string sourceExe, string destExe, bool needsUac, bool isAr, Window dlg)
        {
            int currentPid = Process.GetCurrentProcess().Id;
            string scriptPath = Path.Combine(Path.GetTempPath(), "shia_prayer_swap_" + Guid.NewGuid().ToString("N") + ".ps1");

            // Escape single quotes for PowerShell single-quoted string literals
            string srcEsc = sourceExe.Replace("'", "''");
            string dstEsc = destExe.Replace("'", "''");

            var sb = new StringBuilder();
            sb.AppendLine("$targetPid = " + currentPid);
            sb.AppendLine("$src = '" + srcEsc + "'");
            sb.AppendLine("$dst = '" + dstEsc + "'");
            sb.AppendLine("$deadline = (Get-Date).AddSeconds(15)");
            sb.AppendLine("while ((Get-Date) -lt $deadline) {");
            sb.AppendLine("    if (-not (Get-Process -Id $targetPid -ErrorAction SilentlyContinue)) { break }");
            sb.AppendLine("    Start-Sleep -Milliseconds 250");
            sb.AppendLine("}");
            sb.AppendLine("Stop-Process -Id $targetPid -Force -ErrorAction SilentlyContinue");
            sb.AppendLine("Start-Sleep -Milliseconds 400");
            sb.AppendLine("$ok = $false");
            sb.AppendLine("for ($i = 0; $i -lt 12; $i++) {");
            sb.AppendLine("    try {");
            sb.AppendLine("        Copy-Item -LiteralPath $src -Destination $dst -Force");
            sb.AppendLine("        $ok = $true; break");
            sb.AppendLine("    } catch {");
            sb.AppendLine("        Start-Sleep -Milliseconds 400");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            sb.AppendLine("if ($ok) {");
            sb.AppendLine("    Remove-Item -LiteralPath $src -Force -ErrorAction SilentlyContinue");
            // Launching through explorer.exe drops elevated UAC token back to normal desktop user session
            sb.AppendLine("    Start-Process -FilePath 'explorer.exe' -ArgumentList \"`\"$dst`\"\"");
            sb.AppendLine("}");
            sb.AppendLine("Remove-Item -LiteralPath $MyInvocation.MyCommand.Path -Force -ErrorAction SilentlyContinue");

            File.WriteAllText(scriptPath, sb.ToString(), new UTF8Encoding(true));

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File \"" + scriptPath + "\"",
                UseShellExecute = needsUac,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            if (needsUac)
            {
                psi.Verb = "runas";
            }

            try
            {
                Process.Start(psi);
            }
            catch (Win32Exception wex)
            {
                try { File.Delete(scriptPath); } catch { }
                try { File.Delete(sourceExe); } catch { }
                if (wex.NativeErrorCode == 1223)
                {
                    throw new Exception(isAr
                        ? "تم إلغاء طلب صلاحيات المسؤول من ويندوز.\nيمكنك التحديث يدوياً أو نقل البرنامج إلى مجلد المستخدم."
                        : "Windows UAC elevation was declined.\nYou can update manually or move the EXE to a user folder.");
                }
                throw;
            }

            try { dlg.Close(); } catch { }
            if (Application.Current != null)
            {
                Application.Current.Shutdown();
            }
        }
    }
}
