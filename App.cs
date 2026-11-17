using System;
using System.Threading;
using System.Windows;

namespace ShiaPrayerTimes
{
    public class App : Application
    {
        private static Mutex _appMutex;

        [STAThread]
        public static void Main(string[] args)
        {
            bool createdNew = false;
            try
            {
                _appMutex = new Mutex(true, "ShiaPrayerTimesApp_SingleInstanceMutex_2026", out createdNew);
            }
            catch (AbandonedMutexException)
            {
                createdNew = true;
            }
            catch
            {
                createdNew = true;
            }

            if (!createdNew)
            {
                // Another instance is already running
                MessageBox.Show("تطبيق أوقات الصلاة يعمل بالفعل في الخلفية (بجانب الساعة).\nShia Prayer Times is already running in the system tray.",
                    "أوقات الصلاة - Shia Prayer Times",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            try
            {
                var app = new App();
                app.InitializeComponent();

                bool startInTray = false;
                if (args != null && args.Length > 0)
                {
                    foreach (var arg in args)
                    {
                        if (arg.Equals("--tray", StringComparison.OrdinalIgnoreCase) ||
                            arg.Equals("-tray", StringComparison.OrdinalIgnoreCase) ||
                            arg.Equals("/tray", StringComparison.OrdinalIgnoreCase))
                        {
                            startInTray = true;
                            break;
                        }
                    }
                }

                var settings = AppSettings.Load();
                var mainWindow = new MainWindow(settings);

                if (!startInTray)
                {
                    mainWindow.Show();
                }

                app.Run();
            }
            catch (Exception ex)
            {
                MessageBox.Show("حدث خطأ أثناء تشغيل التطبيق:\n" + ex.Message + "\n" + ex.StackTrace,
                    "خطأ - Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                if (_appMutex != null)
                {
                    _appMutex.ReleaseMutex();
                    _appMutex = null;
                }
            }
        }

        private void InitializeComponent()
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
        }
    }
}
