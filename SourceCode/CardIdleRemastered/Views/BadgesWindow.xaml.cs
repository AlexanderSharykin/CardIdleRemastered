using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace CardIdleRemastered
{
    /// <summary>
    /// Interaction logic for BadgesWindow.xaml
    /// </summary>
    public partial class BadgesWindow : Window
    {
        public BadgesWindow()
        {
            InitializeComponent();
        }

        private void SetLoadingRowNumber(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }
    }
}
