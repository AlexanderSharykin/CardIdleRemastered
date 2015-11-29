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
        public AppVisualSettings _defaultVisuals;

        private void LaunchCardIdle(object sender, StartupEventArgs e)
        {
            //Thread.CurrentThread.CurrentUICulture = new CultureInfo("en");

            _defaultVisuals = GetCurrentSettings();
            ApplySavedVisuals();

            DispatcherUnhandledException += LogUnhandledException;

            try
            {
                RegistryKey ie_root = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION");
                RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION", true);
                String programName = Path.GetFileName(Environment.GetCommandLineArgs()[0]);
                key.SetValue(programName, (int)10001, RegistryValueKind.DWord);
            }
            catch (Exception)
            {

            }
            
            if (string.IsNullOrWhiteSpace(Settings.Default.sessionid) || string.IsNullOrWhiteSpace(Settings.Default.steamLogin))            
                new BrowserWindow().Show();
            else
            {
                OpenAccount();
            }
        }

        private void LogUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs arg)
        {
            Logger.Exception(arg.Exception);
        }

        public static App CardIdle
        {
            get { return Current as App; }
        }

        public void OpenAccount()
        {
            AccountModel account = CreateAccount();

            var w = new BadgesWindow();
            w.Account = account;

            account.InitSteamTimer();
            account.StartTimers();

            Current.MainWindow = w;
            w.Show();
        }

        private AccountModel CreateAccount()
        {
            var account = new AccountModel();

            account.ProfileUrl = Settings.Default.myProfileURL;

            // set saved profile settings
            account.UserName = Settings.Default.SteamUserName;
            account.Level = Settings.Default.SteamLevel;

            account.AvatarUrl = Settings.Default.SteamAvatarUrl;
            account.CustomBackgroundUrl = Settings.Default.CustomBackgroundUrl;
            account.BackgroundUrl = Settings.Default.SteamBackgroundUrl;

            if (!String.IsNullOrWhiteSpace(account.AvatarUrl))
                account.Avatar = LoadImage(account.AvatarUrl);

            if (!String.IsNullOrWhiteSpace(account.CustomBackgroundUrl))
                account.CustomBackground = LoadImage(account.CustomBackgroundUrl);
            else
                if (!String.IsNullOrWhiteSpace(account.BackgroundUrl))
                    account.Background = LoadImage(account.BackgroundUrl);

            account.Filter = (BadgeModelFilter)Settings.Default.BadgeFilter;
            account.Idler.Mode = (IdleMode)Settings.Default.IdleMode;

            account.IgnoreClient = Settings.Default.ignoreclient;

            if (Settings.Default.MaxIdleProcessCount > 0)
                account.Idler.MaxIdleInstanceCount = Settings.Default.MaxIdleProcessCount;

            account.BlacklistBadge = (badge) =>
            {
                if (badge.IsBlacklisted)
                    Settings.Default.blacklist.Add(badge.AppId.ToString());
                else
                    Settings.Default.blacklist.Remove(badge.AppId.ToString());
                Settings.Default.Save();
            };

            account.PropertyChanged += SaveConfiguration;
            account.Idler.PropertyChanged += SaveConfiguration;

            account.IdleQueueBadges.CollectionChanged += (sender, args) =>
            {
                Settings.Default.IdleQueue.Clear();
                foreach (var b in account.IdleQueueBadges)                
                    Settings.Default.IdleQueue.Add(b.StringId);                
                Settings.Default.Save();
            };

            return account;
        }

        private void SaveConfiguration(object sender, PropertyChangedEventArgs e)
        {
            bool save = false;

            if (e.PropertyName == "UserName")
            {
                Settings.Default.SteamUserName = (sender as AccountModel).UserName;
                save = true;
            }

            if (e.PropertyName == "Level")
            {
                Settings.Default.SteamLevel = (sender as AccountModel).Level;
                save = true;
            }

            if (e.PropertyName == "BackgroundUrl")
            {
                Settings.Default.SteamBackgroundUrl = (sender as AccountModel).BackgroundUrl;
                save = true;
            }

            if (e.PropertyName == "AvatarUrl")
            {
                Settings.Default.SteamAvatarUrl = (sender as AccountModel).AvatarUrl;
                save = true;
            }

            if (e.PropertyName == "CustomBackgroundUrl")
            {
                Settings.Default.CustomBackgroundUrl = (sender as AccountModel).CustomBackgroundUrl;
                save = true;
            }

            if (e.PropertyName == "Filter")
            {
                Settings.Default.BadgeFilter = (int)(sender as AccountModel).Filter;
                save = true;
            }

            if (e.PropertyName == "Mode")
            {
                Settings.Default.IdleMode = (int)(sender as IdleManager).Mode;
                save = true;
            }

            if (e.PropertyName == "MaxIdleInstanceCount")
            {
                Settings.Default.MaxIdleProcessCount = (sender as IdleManager).MaxIdleInstanceCount;
                save = true;
            }

            if (save)
                Settings.Default.Save();
        }

        public void Logout()
        {
            // Clear the account settings
            Settings.Default.sessionid = string.Empty;
            Settings.Default.steamLogin = string.Empty;
            Settings.Default.myProfileURL = string.Empty;
            Settings.Default.steamparental = string.Empty;
            Settings.Default.SteamUserName = string.Empty;
            Settings.Default.SteamLevel = string.Empty;
            Settings.Default.SteamAvatarUrl = string.Empty;
            Settings.Default.SteamBackgroundUrl = string.Empty;
            Settings.Default.CustomBackgroundUrl = string.Empty;
            Settings.Default.AppBrushes.Clear();
            Settings.Default.IdleQueue.Clear();
            Settings.Default.IdleMode = 0;
            Settings.Default.BadgeFilter = 0;
            Settings.Default.Save();

            foreach (var brush in _defaultVisuals.AppBrushes)            
                Current.Resources[brush.Name] = new SolidColorBrush(brush.BrushColor.Value);            

            var current = Current.MainWindow;

            var w = new BrowserWindow();
            Current.MainWindow = w;
            w.Show();

            // Close badges window
            current.Close();
        }

        public static BitmapImage LoadImage(string url)
        {
            if (String.IsNullOrWhiteSpace(url))
                return null;
            return new BitmapImage(new Uri(url));
        }

        public void SettingsDialog(AccountModel account)
        {            
            var vis = App.CardIdle.GetCurrentSettings();

            vis.BackgroundUrl = account.CustomBackgroundUrl;
            if (String.IsNullOrEmpty(vis.BackgroundUrl))
                vis.BackgroundUrl = account.BackgroundUrl;
            vis.IdleProcessCount = account.Idler.MaxIdleInstanceCount;

            vis.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "BackgroundUrl")
                {
                    account.CustomBackgroundUrl = vis.BackgroundUrl;
                    account.CustomBackground = LoadImage(vis.BackgroundUrl);
                }
                if (e.PropertyName == "IdleProcessCount")
                    account.Idler.MaxIdleInstanceCount = vis.IdleProcessCount;
            };

            var sw = new SettingsWindow();
            sw.DataContext = vis;
            sw.ShowDialog();
        }

        public AppVisualSettings DefaultVisualSettings
        {
            get { return _defaultVisuals; }
        }

        public AppVisualSettings GetCurrentSettings()
        {            
            var s = new AppVisualSettings();
            var res = Current.Resources;
            var br = res.Keys.OfType<string>()
                .Where(key=>key.StartsWith("Dyn"))
                .Select(resKey => new AppBrush(resKey, ((SolidColorBrush)res[resKey]).Color))
                .ToList();
            foreach (var b in br)
            {
                s.AppBrushes.Add(b);
            }
            return s;
        }

        private void ApplySavedVisuals()
        {
            if (Settings.Default.AppBrushes == null)
                return;
            
            foreach (var str in Settings.Default.AppBrushes)
            {
                var a = str.Split(';');
                Current.Resources[a[0]] = new SolidColorBrush((Color) ColorConverter.ConvertFromString(a[1]));
            }
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
    }
}
