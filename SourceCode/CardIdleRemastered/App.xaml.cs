using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CardIdleRemastered.Properties;
using Microsoft.Win32;

namespace CardIdleRemastered
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private AccountModel _account;

        private void LaunchCardIdle(object sender, StartupEventArgs e)
        {
            //Thread.CurrentThread.CurrentUICulture = new CultureInfo("en");
            //Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("ru-Ru");

            // http://stackoverflow.com/questions/1472498/wpf-global-exception-handler
            // http://stackoverflow.com/a/1472562/1506454
            DispatcherUnhandledException += LogUnhandledDispatcherException;
            AppDomain.CurrentDomain.UnhandledException += LogUnhandledDomainException;
            TaskScheduler.UnobservedTaskException += LogTaskSchedulerUnobservedTaskException;

            Logger.Info(String.Format("{0} {1}bit", Environment.OSVersion, Environment.Is64BitOperatingSystem ? 64 : 32));

            LoadVisuals();

            _account = new AccountModel();
            var w = new BadgesWindow { DataContext = _account};                        
            w.Show();

            _account.InitSteamTimer();
            _account.LoadAccount();
        }

        private void LogTaskSchedulerUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs arg)
        {
            Logger.Exception(arg.Exception, "TaskScheduler.UnobservedTaskException");
        }

        private void LogUnhandledDomainException(object sender, UnhandledExceptionEventArgs arg)
        {
            Logger.Exception(arg.ExceptionObject as Exception, "AppDomain.CurrentDomain.UnhandledException", String.Format("IsTerminating = {0}", arg.IsTerminating));           
        }

        private void LogUnhandledDispatcherException(object sender, DispatcherUnhandledExceptionEventArgs arg)
        {
            Logger.Exception(arg.Exception, "DispatcherUnhandledException");
            arg.Handled = true;
        }

        public static App CardIdle
        {
            get { return Current as App; }
        }

        private void LoadVisuals()
        {
            AppVisualSettings.DefaultVisualSettings.GetBrushes();

            if (Settings.Default.AppBrushes == null)
                return;

            var vis = new AppVisualSettings();            
            foreach (var str in Settings.Default.AppBrushes)
            {
                var a = str.Split(';');
                vis.AppBrushes.Add(new AppBrush(a[0], (Color)ColorConverter.ConvertFromString(a[1])));
            }
            vis.ResetBrushes();
        }

        public void SaveSettings(AppVisualSettings vis)
        {
            Settings.Default.CustomBackgroundUrl = vis.BackgroundUrl;

            Settings.Default.AppBrushes.Clear();
            foreach (var b in vis.AppBrushes)
            {
                Settings.Default.AppBrushes.Add(String.Format("{0};{1}", b.Name, b.BrushColor));
            }

            Settings.Default.Save();
        }

        private void StopCardIdle(object sender, ExitEventArgs e)
        {
            if (_account != null)
            {
                _account.Dispose();
            }
        }
    }
}
