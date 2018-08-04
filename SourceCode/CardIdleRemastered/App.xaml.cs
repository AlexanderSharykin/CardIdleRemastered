using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

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

            string localDatafolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appFolder = Path.Combine(localDatafolder, Path.GetFileNameWithoutExtension(AppSystemName));
            if (Directory.Exists(appFolder) == false)
                Directory.CreateDirectory(appFolder);

            string storageFile = Path.Combine(appFolder, "Settings.txt");
            IsNewUser = !File.Exists(storageFile);

            var storage = new SettingsStorage();
            storage.FileName = storageFile;
            storage.Init();

            CookieClient.Storage = storage;

            _account = new AccountModel();
            _account.Storage = storage;
            _account.ShowcaseStorage = new FileStorage("ShowcaseDb.txt");
            _account.PricesStorage = new FileStorage(Path.Combine(appFolder, "PricesDb.txt"));

            Palette = PaletteItemsCollection.Create();
            if (storage.AppBrushes != null)
                Palette.Deserialize(storage.AppBrushes.OfType<string>());
            Palette.SetNotifier(() =>
            {
                storage.AppBrushes.Clear();
                storage.AppBrushes.AddRange(Palette.Serialize().ToArray());
                storage.Save();
            });

            var w = new BadgesWindow { DataContext = _account };
            w.Show();

            _account.Startup();
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

        public bool IsNewUser { get; set; }

        private static string _appSystemName;

        public static string AppSystemName
        {
            get
            {
                if (_appSystemName == null)
                    _appSystemName = Path.GetFileName(Environment.GetCommandLineArgs()[0]);
                return _appSystemName;
            }
        }

        public PaletteItemsCollection Palette { get; set; }

        private void StopCardIdle(object sender, ExitEventArgs e)
        {
            if (_account != null)
            {
                _account.Dispose();
            }
        }
    }
}
