using System.Windows.Controls;

namespace CardIdleRemastered.Views
{
    /// <summary>
    /// Interaction logic for ShowcasesPage.xaml
    /// </summary>
    public partial class ShowcasesPage : UserControl
    {
        public ShowcasesPage()
        {
            InitializeComponent();
        }

        private void SetLoadingRowNumber(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }
    }
}