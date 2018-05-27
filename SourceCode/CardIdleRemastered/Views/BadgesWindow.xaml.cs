using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;

namespace CardIdleRemastered
{
    /// <summary>
    /// Interaction logic for BadgesWindow.xaml
    /// </summary>
    public partial class BadgesWindow : Window
    {
        private NotifyIcon _ni;

        public BadgesWindow()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            Loaded += OnWindowLoaded;

            var icoUri = new Uri("pack://application:,,,/CardIdleRemastered;component/CardIdle.ico");
            var iconStream = Application.GetResourceStream(icoUri).Stream;
            _ni = new NotifyIcon
                  {
                      Icon = new Icon(iconStream),
                      Text = "Card Idle",
                      Visible = false
                  };

            _ni.DoubleClick += ExpandWindow;
        }

        void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            SizeChanged += OnWindowSizeChanged;
            LocationChanged += OnWindowLocationChanged;
        }

        void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            SaveDimensions();
        }

        void OnWindowLocationChanged(object sender, EventArgs e)
        {
            SaveDimensions();
        }

        private void SaveDimensions()
        {
            if (Vm != null)
            {
                Vm.Storage.Dimensions = new Thickness(Left, Top, RenderSize.Width, RenderSize.Height).ToString();
                Vm.Storage.Save();
            }
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized && !Vm.ShowInTaskbar)
                this.Hide();

            base.OnStateChanged(e);
        }

        private AccountModel _vm;
        private AccountModel Vm
        {
            get { return _vm; }
            set
            {
                if (_vm != null)
                {
                    _vm.PropertyChanged -= VmOnPropertyChanged;
                }

                _vm = value;

                if (_vm != null)
                    _vm.PropertyChanged += VmOnPropertyChanged;
            }
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Vm = DataContext as AccountModel;
            if (Vm != null)
            {
                string x = Vm.Storage.Dimensions;
                if (String.IsNullOrWhiteSpace(x))
                    return;

                var d = (Thickness)(new ThicknessConverter().ConvertFromInvariantString(x));
                WindowStartupLocation = WindowStartupLocation.Manual;
                Left = Math.Max(0, d.Left);
                Top = Math.Max(0, d.Top);
                Width = d.Right;
                Height = d.Bottom;
            }
        }

        private void VmOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "ShowInTaskbar")
                return;
            _ni.Visible = !Vm.ShowInTaskbar;
        }

        /// <summary>
        /// Restores window from tray
        /// </summary>
        /// <see cref="https://stackoverflow.com/questions/10230579/easiest-way-to-have-a-program-minimize-itself-to-the-system-tray-using-net-4"/>
        private void ExpandWindow(object sender, EventArgs e)
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }
    }
}
