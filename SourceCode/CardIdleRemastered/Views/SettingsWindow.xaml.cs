using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CardIdleRemastered
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private AppVisualSettings _memo;
        public SettingsWindow()
        {
            InitializeComponent();
        }

        public AppVisualSettings CurrentSettings
        {
            get { return DataContext as AppVisualSettings; }
        }

        private void SettingsWindowLoaded(object sender, RoutedEventArgs e)
        {
            _memo = CurrentSettings.Copy();
        }

        private void CopySettings(AppVisualSettings src, AppVisualSettings target)
        {
            target.BackgroundUrl = src.BackgroundUrl;            

            target.AppBrushes.Clear();
            foreach (var b in src.AppBrushes)
                target.AppBrushes.Add(new AppBrush(b.Name, b.BrushColor));
        }

        private void ResetSettings(AppVisualSettings src, AppVisualSettings target)
        {
            target.BackgroundUrl = src.BackgroundUrl;            

            for (int i = 0; i < src.AppBrushes.Count; i++)
                target.AppBrushes[i].BrushColor = src.AppBrushes[i].BrushColor;
        }

        private void ResetClick(object sender, RoutedEventArgs e)
        {
            var vis = AppVisualSettings.DefaultVisualSettings;
            ResetSettings(vis, CurrentSettings);
            CopySettings(vis, _memo);
            App.CardIdle.SaveSettings(vis);
        }

        private void ApplyClick(object sender, RoutedEventArgs e)
        {
            CopySettings(CurrentSettings, _memo);
            App.CardIdle.SaveSettings(_memo);
            Close();
        }

        private void CancelClick(object sender, RoutedEventArgs e)
        {
            ResetSettings(_memo, CurrentSettings);
            Close();
        }
    }
}
