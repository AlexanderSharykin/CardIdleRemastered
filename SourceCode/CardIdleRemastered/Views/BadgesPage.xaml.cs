using System.Windows.Controls;

namespace CardIdleRemastered.Views
{
    /// <summary>
    /// Interaction logic for BadgesPage.xaml
    /// </summary>
    public partial class BadgesPage : UserControl
    {
        public BadgesPage()
        {
            InitializeComponent();
        }

        private void SetLoadingRowNumber(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }
    }
}