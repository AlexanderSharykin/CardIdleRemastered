using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
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

            var icoUri = new Uri("pack://application:,,,/CardIdleRemastered;component/CardIdle.ico");
            var iconStream = Application.GetResourceStream(icoUri).Stream;
            _ni = new NotifyIcon
                  {
                      Icon = new System.Drawing.Icon(iconStream),
                      Text = "Card Idle",
                      Visible = false
                  };

            _ni.DoubleClick += ExpandWindow;
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
            WindowState = WindowState.Normal;
            Activate();
        }

        private void SetLoadingRowNumber(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }
    }
}
