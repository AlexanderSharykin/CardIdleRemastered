using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace CardIdleRemastered
{
    public class AppVisualSettings: ObservableModel
    {
        private static AppVisualSettings _defaultVisuals;

        public static AppVisualSettings DefaultVisualSettings
        {
            get
            {
                if (_defaultVisuals == null)
                    _defaultVisuals = new AppVisualSettings();
                return _defaultVisuals;
            }
        }

        private string _backgroundUrl;
        private ObservableCollection<AppBrush> _appBrushes;

        public string BackgroundUrl
        {
            get { return _backgroundUrl; }
            set
            {
                _backgroundUrl = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<AppBrush> AppBrushes
        {
            get
            {
                if (_appBrushes == null)
                    _appBrushes = new ObservableCollection<AppBrush>();
                return _appBrushes;
            }
            private set { _appBrushes = value; }
        }

        public void GetBrushes()
        {            
            var res = App.CardIdle.Resources;
            var br = res.Keys.OfType<string>()
                .Where(key => key.StartsWith("Dyn"))
                .Select(resKey => new AppBrush(resKey, ((SolidColorBrush)res[resKey]).Color))
                .ToList();
            AppBrushes = new ObservableCollection<AppBrush>(br);
        }

        public void ResetBrushes()
        {
            foreach (var brush in AppBrushes)
                App.CardIdle.Resources[brush.Name] = new SolidColorBrush(brush.BrushColor.Value);
        }

        public AppVisualSettings Copy()
        {
            var vis = new AppVisualSettings();
            vis.BackgroundUrl = BackgroundUrl;

            foreach (var b in AppBrushes)
            {
                vis.AppBrushes.Add(new AppBrush(b.Name, b.BrushColor));
            }
            return vis;
        }
    }
}
