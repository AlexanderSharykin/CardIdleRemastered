using System.Windows.Controls;

namespace CardIdleRemastered.Views
{
    /// <summary>
    /// Interaction logic for QueuePage.xaml
    /// </summary>
    public partial class QueuePage : UserControl
    {
        public QueuePage()
        {
            InitializeComponent();
        }

        private void SetLoadingRowNumber(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }
    }
}